using FontThing.Parsing;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text;

namespace AppThing;

[Flags]
public enum BitmapFontFlags
{
	SubpixelAntialias = 1 << 0,
	Default = 0
}

public sealed class BitmapFont : IDisposable
{
	public readonly struct GlyphTexture(Texture atlas, Rectangle atlasRegion)
	{
		public readonly Texture Atlas = atlas;
		public readonly Rectangle AtlasRegion = atlasRegion;
	}

	public float Size { get; }

	public float LineHeight => _ttf.LineHeight * _scale;

	private readonly BitmapFontFlags _flags;
	private readonly float _pixelSize;
	private readonly float _scale;

	private readonly TrueTypeFont _ttf;

	private readonly List<AtlasGenerator> _textureAtlases = [];

	private readonly Dictionary<Rune, Glyph> _loadedGlyphs = [];
	private readonly Dictionary<(Glyph, float subX, float subY), GlyphTexture> _glyphs = [];

	private BitmapFont(TrueTypeFont ttf, float size, BitmapFontFlags flags)
	{
		_flags = flags;
		_ttf = ttf;
		Size = size;

		_pixelSize = ttf.GetPixelsPerEm(size);
		_scale = ttf.PointSizeToScale(size);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryGetGlyph(Rune character, ref long penX, ref long penY, out GlyphTexture glyphTexture, out Point drawPos)
	{
		if (character.Value == '\n')
		{
			penX = 0;
			penY -= _ttf.LineHeight;

			glyphTexture = default;
			drawPos = Point.Empty;
			return false;
		}

		if (!_loadedGlyphs.TryGetValue(character, out var glyph))
		{
			glyph = _ttf.LoadGlyph(character);
			_loadedGlyphs.Add(character, glyph);
		}

		if (penX == 0.0f)
			penX -= glyph.LeftSideBearing;

		if (glyph.Outline == null)
		{
			glyphTexture = default;
			drawPos = Point.Empty;
			goto end;
		}

		var glyphXPrecise = (penX + glyph.Outline.XMin) * _scale;
		var glyphYPrecise = (penY + glyph.Outline.YMin - _ttf.LineHeight) * _scale;

		var glyphX = (int)glyphXPrecise;
		var glyphY = (int)glyphYPrecise;

		var subX = 0.0f;
		var subY = 0.0f;

		var options = GlyphOutlineRenderOptions.Default;

		if ((_flags & BitmapFontFlags.SubpixelAntialias) != 0)
		{
			options |= GlyphOutlineRenderOptions.SubpixelRgb;
		}
		else
		{
			options &= ~GlyphOutlineRenderOptions.SubpixelRgb;
		}

		var round = _pixelSize <= 100.0f;
		if (round)
		{
			var roundingDivisor = 2.0f;
			if (_pixelSize <= 50.0f)
				roundingDivisor = 4.0f;
			else if (_pixelSize <= 25.0f)
				roundingDivisor = 8.0f;
			else if (_pixelSize <= 12.5f)
				roundingDivisor = 16.0f;

			//if ((_flags & BitmapFontFlags.SubpixelAntialias) == 0)
				subX = (int)((glyphXPrecise - glyphX) * roundingDivisor) / roundingDivisor;

			subY = (int)((glyphYPrecise - glyphY) * roundingDivisor) / roundingDivisor;

			//if ((_flags & BitmapFontFlags.SubpixelAntialias) == 0)
			/*
			{
				if (subX <= 0.0f)
				{
					subX++;
					glyphX--;
				}
			}*/

			if (subY <= 0.0f)
			{
				subY++;
				glyphY++;
			}
		}

		if (!_glyphs.TryGetValue((glyph, subX, subY), out glyphTexture))
		{
			var useSubX = subX;
			if ((_flags & BitmapFontFlags.SubpixelAntialias) != 0)
				useSubX *= 3;
			
			var bitmap = glyph.Outline.Render(Size, options, subpixelOffsetX: useSubX, subpixelOffsetY: subY);

			var size = bitmap.Size;

			if ((_flags & BitmapFontFlags.SubpixelAntialias) != 0)
				size.Width /= 3;

			var texture = TryAllocateRegion(size, out var region);

			texture.AccessPixels(region,
				acc =>
				{
					for (var y = 0; y < size.Height; y++)
					{
						var row = acc.GetRowSpan(y);

						for (var x = 0; x < size.Width; x++)
						{
							var pixelOffset = (bitmap.Size.Height - y - 1) * bitmap.Size.Width;
							if ((_flags & BitmapFontFlags.SubpixelAntialias) != 0)
							{
								var offR = x * 3 + 0;
								var offG = x * 3 + 1;
								var offB = x * 3 + 2;

								var prevB = bitmap.Data[GetIndexSafe(offR - 1)];
								var thisR = bitmap.Data[GetIndexSafe(offR)];
								var thisG = bitmap.Data[GetIndexSafe(offG)];
								var thisB = bitmap.Data[GetIndexSafe(offB)];
								var nextR = bitmap.Data[GetIndexSafe(offB + 1)];

								var r = (prevB + 2 * thisR + thisG) / 4;
								var g = (thisR + 2 * thisG + thisB) / 4;
								var b = (thisG + 2 * thisB + nextR) / 4;
								var a = (r + g + b) / 3;

								r = int.Clamp(r, 0, byte.MaxValue);
								g = int.Clamp(g, 0, byte.MaxValue);
								b = int.Clamp(b, 0, byte.MaxValue);
								a = int.Clamp(a, 0, byte.MaxValue);

								row[x] = Color.FromArgb(a, r, g, b);
								//row[x] = Color.FromArgb(255, thisR, thisG, thisB);

								int GetIndexSafe(int offset) => int.Clamp(pixelOffset + offset, 0, bitmap.Size.Width * bitmap.Size.Height - 1);
							}
							else
							{
								var a = bitmap.Data[x + pixelOffset];
								row[x] = Color.FromArgb(a, a, a);
							}
						}
					}
				});

			glyphTexture = new(texture, region);
			_glyphs.Add((glyph, subX, subY), glyphTexture);
		}

		drawPos = new(glyphX, -glyphY - glyphTexture.AtlasRegion.Size.Height);

		end:
		penX += glyph.AdvanceWidth;
		return glyph.Outline != null;
	}

	private Texture TryAllocateRegion(Size size, out Rectangle rect)
	{
		foreach (var atlas in _textureAtlases)
		{
			if (atlas.TryAllocateRegion(size, out rect))
				return atlas.Texture;
		}

		// Create new atlas
		var atlasSize = (int)(_pixelSize * 16);

		if (atlasSize < 256)
			atlasSize = 256;
		else if (atlasSize > 4096)
			atlasSize = 4096;

		Console.WriteLine($"[BitmapFont] Creating new texture atlas ({atlasSize}x{atlasSize})");

		var textureFormat = TextureFormat.AlphaOnly;
		if ((_flags & BitmapFontFlags.SubpixelAntialias) != 0)
			textureFormat = TextureFormat.RgbAsAlpha;

		var newAtlasTexture = new Texture(new(atlasSize, atlasSize), Color.Black, textureFormat);
		var newAtlas = new AtlasGenerator(newAtlasTexture);
		_textureAtlases.Add(newAtlas);

		newAtlas.TryAllocateRegion(size, out rect);
		return newAtlas.Texture;
	}

	public static BitmapFont FromFile(string path, float size, BitmapFontFlags flags = BitmapFontFlags.Default)
	{
		using var fs = File.OpenRead(path);
		return new(new(fs), size, flags);
	}

	public void Dispose()
	{
		foreach (var atlas in _textureAtlases)
			atlas.Texture.Dispose();
	}
}

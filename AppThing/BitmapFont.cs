using FontThing.TrueType.Parsing;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text;

namespace AppThing;

public sealed class BitmapFont : IDisposable
{
	public readonly struct GlyphTexture(Texture atlas, Rectangle atlasRegion)
	{
		public readonly Texture Atlas = atlas;
		public readonly Rectangle AtlasRegion = atlasRegion;
	}

	public float Size { get; }

	public float LineHeight => _ttf.LineHeight * _scale;
	
	private readonly float _pixelSize;
	private readonly float _scale;

	private readonly TrueTypeFont _ttf;

	private readonly List<AtlasGenerator> _textureAtlases = [];

	private readonly Dictionary<Rune, Glyph> _loadedGlyphs = [];
	private readonly Dictionary<(Glyph, float subX, float subY), GlyphTexture> _glyphs = [];

	private BitmapFont(TrueTypeFont ttf, float size)
	{
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

		var round = _pixelSize <= 100.0f;
		if (round)
		{
			var roundingDivisor = 2.0f;
			if (_pixelSize <= 50.0f)
				roundingDivisor = 4.0f;
			else if (_pixelSize <= 25.0f)
				roundingDivisor = 8.0f;
			
			subX = (int)((glyphXPrecise - glyphX) * roundingDivisor) / roundingDivisor;
			subY = (int)((glyphYPrecise - glyphY) * roundingDivisor) / roundingDivisor;

			if (subX <= -0.0f)
			{
				subX = float.Round(subX + 1, round ? 1 : 0);
				glyphX--;
			}

			if (subY <= -0.0f)
			{
				subY = float.Round(subY + 1, round ? 1 : 0);
				glyphY--;
			}
		}

		if (!_glyphs.TryGetValue((glyph, subX, subY), out glyphTexture))
		{
			var bitmap = glyph.Outline.Render(Size, subpixelOffsetX: subX, subpixelOffsetY: subY);

			var texture = TryAllocateRegion(bitmap.Size, out var region);

			texture.AccessPixels(region,
				acc =>
				{
					for (var y = 0; y < bitmap.Size.Height; y++)
					{
						var row = acc.GetRowSpan(y);
						for (var x = 0; x < bitmap.Size.Width; x++)
						{
							var a = bitmap.Data[x + (acc.Size.Height - y - 1) * acc.Size.Width];
							row[x] = Color.FromArgb(a, a, a);
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
		var newAtlasTexture = new Texture(new(atlasSize, atlasSize), Color.Black, TextureFormat.AlphaOnly);
		var newAtlas = new AtlasGenerator(newAtlasTexture);
		_textureAtlases.Add(newAtlas);

		newAtlas.TryAllocateRegion(size, out rect);
		return newAtlas.Texture;
	}

	public static BitmapFont FromFile(string path, float size)
	{
		using var fs = File.OpenRead(path);
		return new(new(fs), size);
	}

	public void Dispose()
	{
		foreach (var atlas in _textureAtlases)
			atlas.Texture.Dispose();
	}
}

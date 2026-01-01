using FontThing.TrueType.Parsing;
using System.Diagnostics;
using System.Drawing;
using System.Text;

namespace AppThing;

public readonly struct RenderFontGlyph(Texture atlas, Rectangle atlasRegion)
{
	public readonly Texture Atlas = atlas;
	public readonly Rectangle AtlasRegion = atlasRegion;
}

public sealed class RenderFont : IDisposable
{
	public float Size { get; }

	private readonly float _pixelSize;
	private readonly float _scale;

	private readonly TrueTypeFont _ttf;

	private readonly List<AtlasGenerator> _textureAtlases = [];

	private readonly Dictionary<(Glyph, float subX, float subY), RenderFontGlyph> _glyphs = [];

	private RenderFont(TrueTypeFont ttf, float size)
	{
		_ttf = ttf;
		Size = size;

		_pixelSize = ttf.GetPixelsPerEm(size);
		_scale = ttf.PointSizeToScale(size);
	}

	public bool TryGetGlyph(Rune character, ref long penX, ref long penY, out RenderFontGlyph renderFontGlyph, out Point drawPos)
	{
		if (character.Value == '\n')
		{
			penX = 0;
			penY -= _ttf.LineHeight;

			renderFontGlyph = default;
			drawPos = Point.Empty;
			return false;
		}

		var glyph = _ttf.LoadGlyph(character);

		if (penX == 0.0f)
			penX -= glyph.LeftSideBearing;

		if (glyph.Outline == null)
		{
			penX += glyph.AdvanceWidth;

			renderFontGlyph = default;
			drawPos = Point.Empty;
			return false;
		}

		var glyphXPrecise = (penX + glyph.Outline.XMin) * _scale;
		var glyphYPrecise = (penY + glyph.Outline.YMin - _ttf.LineHeight) * _scale;

		var glyphX = (int)glyphXPrecise;
		var glyphY = (int)glyphYPrecise;

		var round = _pixelSize <= 100.0f;

		var subX = float.Round(glyphXPrecise - glyphX, round ? 1 : 0);
		var subY = float.Round(glyphYPrecise - glyphY, round ? 1 : 0);

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

		penX += glyph.AdvanceWidth;

		if (_glyphs.TryGetValue((glyph, subX, subY), out renderFontGlyph))
		{
			drawPos = new(glyphX, -glyphY - renderFontGlyph.AtlasRegion.Size.Height);
			return true;
		}

		var bitmap = glyph.Outline.Render(Size, subpixelOffsetX: subX, subpixelOffsetY: subY);

		var texture = TryAllocateRegion(bitmap.Size, out var region);

		texture.AccessPixels(region,
			acc =>
			{
				for (var y = 0; y < bitmap.Size.Height; y++)
				{
					var row = acc.GetRow(y);
					for (var x = 0; x < bitmap.Size.Width; x++)
					{
						var a = bitmap.Data[x + (acc.Size.Height - y - 1) * acc.Size.Width];
						row[x] = Color.FromArgb(a, Color.White);
					}
				}
			});

		renderFontGlyph = new(texture, region);
		_glyphs.Add((glyph, subX, subY), renderFontGlyph);
		drawPos = new(glyphX, -glyphY - renderFontGlyph.AtlasRegion.Size.Height);
		return true;
	}

	private Texture TryAllocateRegion(Size size, out Rectangle rect)
	{
		foreach (var atlas in _textureAtlases)
		{
			if (atlas.TryAllocateRegion(size, out rect))
				return atlas.Texture;
		}

		// Create new atlas
		var newAtlasTexture = new Texture(new(4096, 4096), Color.Black);
		var newAtlas = new AtlasGenerator(newAtlasTexture);
		_textureAtlases.Add(newAtlas);

		newAtlas.TryAllocateRegion(size, out rect);
		return newAtlas.Texture;
	}

	public static RenderFont FromFile(string path, float size)
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

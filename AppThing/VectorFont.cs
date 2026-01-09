using FontThing.Parsing;
using System.Numerics;
using System.Text;

namespace AppThing;

public readonly struct VectorFontGlyph(IReadOnlyList<List<Vector2>> contours)
{
	public readonly IReadOnlyList<List<Vector2>> Contours = contours;
}

public sealed class VectorFont
{
	private readonly TrueTypeFont _ttf;

	private readonly Dictionary<(Rune character, float scale), VectorFontGlyph> _glyphCache = [];

	private VectorFont(TrueTypeFont ttf)
	{
		_ttf = ttf;
	}

	public static VectorFont FromFile(string path)
	{
		using var fs = File.OpenRead(path);
		return new(new(fs));
	}

	public bool TryGetGlyph(Rune character, float size, ref long penX, ref long penY, out VectorFontGlyph vectorFontGlyph, out Vector2 drawPos)
	{
		if (character.Value == '\n')
		{
			penX = 0;
			penY -= _ttf.LineHeight;

			vectorFontGlyph = default;
			drawPos = Vector2.Zero;
			return false;
		}

		var glyph = _ttf.LoadGlyph(character);

		if (penX == 0.0f)
			penX -= glyph.LeftSideBearing;

		if (glyph.Outline != null)
		{
			var scale = _ttf.PointSizeToScale(size);
			drawPos = new Vector2(penX, _ttf.LineHeight - penY) * scale;

			if (!_glyphCache.TryGetValue((character, scale), out vectorFontGlyph))
			{
				var contours = glyph.Outline.GenerateContours(scale, 0.1f);
				foreach (var contour in contours)
				{
					for (var i = 0; i < contour.Count; i++)
						contour[i] = new(contour[i].X, -contour[i].Y);
				}

				vectorFontGlyph = new(contours);
				_glyphCache.Add((character, scale), vectorFontGlyph);
			}
		}
		else
		{
			vectorFontGlyph = default;
			drawPos = Vector2.Zero;
		}

		penX += glyph.AdvanceWidth;

		return glyph.Outline != null;
	}
}

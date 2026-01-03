using FontThing.TrueType.Parsing;
using System.Numerics;

namespace AppThing;

public readonly struct VectorFontGlyph(Vector2[] vertices)
{
	public readonly Vector2[] Vertices = vertices;
}

public sealed class VectorFont
{
	internal readonly TrueTypeFont _ttf;

	private readonly Dictionary<Glyph, VectorFontGlyph> _glyphs = [];

	private VectorFont(TrueTypeFont ttf)
	{
		_ttf = ttf;
	}

	public static VectorFont FromFile(string path)
	{
		using var fs = File.OpenRead(path);
		return new(new(fs));
	}

	internal bool TryGetGlyph(Glyph glyph, out VectorFontGlyph vectorFontGlyph)
	{
		if (_glyphs.TryGetValue(glyph, out vectorFontGlyph))
			return true;
		
		var outline = glyph.Outline;

		if (outline is not SimpleGlyphOutline simpleOutline)
		{
			vectorFontGlyph = default;	
			return false;
		}

		var points = new List<Vector2>();
		var contours = simpleOutline.GenerateContours(1.0f, _ttf.GetPixelsPerEm(20) / 128.0f);
		foreach (var contour in contours)
		{
			var pivot = (ScalePoint(contour[0]));

			for (var i = 0; i < contour.Count; i++)
			{
				var p0 = (ScalePoint(contour[i]));
				var p1 = (ScalePoint(contour[(i + 1) % contour.Count]));
				points.Add(pivot);
				points.Add(p0);
				points.Add(p1);
			}
		}

		vectorFontGlyph = new VectorFontGlyph(points.ToArray());
		_glyphs.Add(glyph, vectorFontGlyph);
		return true;

		Vector2 ScalePoint(Vector2 point)
		{
			point *= new Vector2(1, -1);
			return point;
		}
	}

}

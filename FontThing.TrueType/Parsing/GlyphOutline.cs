using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using UtilThing;

namespace FontThing.TrueType.Parsing;

[Flags]
public enum GlyphOutlineRenderOptions
{
	None = 0,
	StemDarkening = 1 << 0,
	Gamma = 1 << 1,
	Default = None,
}

public abstract class GlyphOutline(TrueTypeFont font, short xMin, short yMin, short xMax, short yMax)
{
	public readonly TrueTypeFont Font = font;
	public readonly short XMin = xMin;
	public readonly short YMin = yMin;
	public readonly short XMax = xMax;
	public readonly short YMax = yMax;

	public RectangleF GetBounds(float scale = 1.0f) => new(
		XMin * scale,
		YMin * scale,
		float.Ceiling((XMax - XMin) * scale),
		float.Ceiling((YMax - YMin) * scale)
	);

	public abstract IReadOnlyList<List<Vector2>> GenerateContours(float scale, float bezierTolerance);

	public GlyphBitmap Render(float pointSize, GlyphOutlineRenderOptions options = GlyphOutlineRenderOptions.Default, int supersamples = 4, float bezierTolerance = 0.01f, float subpixelOffsetX = 0.0f, float subpixelOffsetY = 0.0f)
	{
		var scale = Font.PointSizeToScale(pointSize);
		var stemDarkening = (options & GlyphOutlineRenderOptions.StemDarkening) != 0 ? TrueTypeRasterizer.CalculateStemDarkening(Font.GetPixelsPerEm(pointSize)) : 0;

		var gamma = (options & GlyphOutlineRenderOptions.Gamma) != 0 ? 1.2f : 1.0f;

		return TrueTypeRasterizer.RenderGlyph(
			this,
			scale,
			supersamples,
			bezierTolerance,
			subpixelOffsetX,
			subpixelOffsetY,
			stemDarkening,
			gamma);
	}
}

public sealed class SimpleGlyphOutline(TrueTypeFont font, short xMin, short yMin, short xMax, short yMax, ushort[] endPointsOfContours, byte[] instructions, GlyphOutlinePoint[] points) : GlyphOutline(font, xMin, yMin, xMax, yMax)
{
	public readonly ushort[] EndPointsOfContours = endPointsOfContours;
	public readonly byte[] Instructions = instructions;
	public readonly GlyphOutlinePoint[] Points = points;
	public int NumberOfContours => EndPointsOfContours.Length;

	public override IReadOnlyList<List<Vector2>> GenerateContours(float scale, float bezierTolerance)
	{
		var contours = new List<Vector2>[NumberOfContours];

		for (var i = 0; i < NumberOfContours; i++)
		{
			var lines = new List<Vector2>();

			ProcessContour(i,
				(p0, p1) =>
				{
					lines.Add(new Vector2(p1.X, p1.Y) * scale);
				},
				(p0, p1, p2) =>
				{
					var result = BezierSubdivider.RecursiveBezier(
						new Vector2(p0.X, p0.Y) * scale,
						new Vector2(p1.X, p1.Y) * scale,
						new Vector2(p2.X, p2.Y) * scale,
						bezierTolerance
					);

					lines.AddRange(result.Skip(1));
				}
			);

			contours[i] = lines;
		}

		return contours;
	}

	public void ProcessContour(int contourIndex, Action<Point, Point> lineCallback, Action<Point, Point, Point> bezierCallback)
	{
		var contourStartpointIndex = contourIndex == 0 ? 0 : EndPointsOfContours[contourIndex - 1] + 1;
		var points = Points.AsSpan()[contourStartpointIndex..(EndPointsOfContours[contourIndex] + 1)];

		// Find first on-curve point
		var startOffset = 0; // Index of first point to actually read in the loop
		while (!points[startOffset].OnCurve)
			startOffset++;

		var p0Vec = ScalePoint(points[startOffset++]);

		for (var i = 0; i < points.Length; i++)
		{
			var pointsIndex = (i + startOffset) % points.Length;

			var p1 = points[pointsIndex];
			var p1Vec = ScalePoint(p1);

			// Two consecutive on-curve points - straight line
			if (p1.OnCurve)
			{
				lineCallback(p0Vec, p1Vec);
				p0Vec = p1Vec;
				continue;
			}

			pointsIndex = (pointsIndex + 1) % points.Length;

			var p2 = points[pointsIndex];
			var p2Vec = ScalePoint(p2);

			if (p2.OnCurve) // Points are: on-curve, off-curve, on-curve - quadratic Bézier
			{
				i++;

				bezierCallback(p0Vec, p1Vec, p2Vec);
				p0Vec = p2Vec;
			}
			else // Points are: on-curve, off-curve, off-curve - insert midpoint as on-curve point for quadratic Bézier
			{
				//var midpoint = Vector2.Lerp(p1Vec, p2Vec, 0.5f);
				var midpoint = new Point(
					(short)((p1Vec.X + p2Vec.X) / 2),
					(short)((p1Vec.Y + p2Vec.Y) / 2)
				);

				bezierCallback(p0Vec, p1Vec, midpoint);
				p0Vec = midpoint;
			}
		}

		return;

		Point ScalePoint(GlyphOutlinePoint outlinePoint) => new(outlinePoint.X, outlinePoint.Y);
	}
}

public readonly struct CompoundGlyphComponent(ushort glyphIndex, int arg1, int arg2, bool argsAreXyValues, F2Dot14 scaleX, F2Dot14 scale01, F2Dot14 scale10, F2Dot14 scaleY)
{
	public readonly ushort GlyphIndex = glyphIndex;
	public readonly int Arg1 = arg1;
	public readonly int Arg2 = arg2;
	public readonly bool ArgsAreXyValues = argsAreXyValues;
	public readonly F2Dot14 ScaleX = scaleX;
	public readonly F2Dot14 Scale01 = scale01;
	public readonly F2Dot14 Scale10 = scale10;
	public readonly F2Dot14 ScaleY = scaleY;
}

public sealed class CompoundGlyphOutline(TrueTypeFont font, short xMin, short yMin, short xMax, short yMax, CompoundGlyphComponent[] components, byte[]? instructions) : GlyphOutline(font, xMin, yMin, xMax, yMax)
{
	public readonly CompoundGlyphComponent[] Components = components;
	public readonly byte[]? Instructions = instructions;

	public override IReadOnlyList<List<Vector2>> GenerateContours(float scale, float bezierTolerance)
	{
		var contours = new List<List<Vector2>>();

		foreach (var component in Components)
		{
			if (!component.ArgsAreXyValues)
				throw new NotImplementedException();

			var componentOutline = Font.GetGlyphOutlineFromIndex(component.GlyphIndex);
			Debug.Assert(componentOutline != null);

			var componentContours = componentOutline.GenerateContours(scale, bezierTolerance);
			foreach (var contour in componentContours)
			{
				for (var i = 0; i < contour.Count; i++)
				{
					ref var p = ref CollectionsMarshal.AsSpan(contour)[i];

					// Apply scaling
					var scaledX = p.X * (float)component.ScaleX + p.Y * (float)component.Scale01;
					var scaledY = p.X * (float)component.Scale10 + p.Y * (float)component.ScaleY;

					// Apply translation
					scaledX += component.Arg1 * scale;
					scaledY += component.Arg2 * scale;

					p = new(scaledX, scaledY);
				}

				contours.Add(contour);
			}
		}

		return contours;
	}
}

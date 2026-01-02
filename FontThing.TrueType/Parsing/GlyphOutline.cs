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
	Default = StemDarkening,
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

public sealed class SimpleGlyphOutline(TrueTypeFont font, short xMin, short yMin, short xMax, short yMax, ushort[] endPointsOfContours, byte[] instructions, GlyphPoint[] points) : GlyphOutline(font, xMin, yMin, xMax, yMax)
{
	public readonly ushort[] EndPointsOfContours = endPointsOfContours;
	public readonly byte[] Instructions = instructions;
	public readonly GlyphPoint[] Points = points;

	public override IReadOnlyList<List<Vector2>> GenerateContours(float scale, float bezierTolerance)
	{
		var contours = new List<Vector2>[EndPointsOfContours.Length];

		// Iterate over all contours
		var contourStartpointIndex = 0;
		for (var contourIndex = 0; contourIndex < EndPointsOfContours.Length; contourIndex++)
		{
			var contourEndpointIndex = EndPointsOfContours[contourIndex];

			contours[contourIndex] = GenerateContour(Points.AsSpan()[contourStartpointIndex..(contourEndpointIndex + 1)], scale, bezierTolerance);

			// Next contour's start point index immediately follows this contour's endpoint index
			contourStartpointIndex = contourEndpointIndex + 1;
		}

		return contours;
	}

	private static List<Vector2> GenerateContour(ReadOnlySpan<GlyphPoint> points, float scale, float bezierTolerance)
	{
		var lines = new List<Vector2>();

		// Find first on-curve point
		var pointOffset = 0; // Index of first point to actually read in the loop
		while (!points[pointOffset].OnCurve)
			pointOffset++;

		var p0Vec = GetScaledGlyphPoint(points[pointOffset++]);

		var i = 0;
		while (i < points.Length)
		{
			var index = (i + pointOffset) % points.Length;

			var p1 = points[index];
			var p1Vec = GetScaledGlyphPoint(p1);

			// "Consume" P1
			i++;

			// Two consecutive on-curve points - straight line
			if (p1.OnCurve)
			{
				lines.Add(p1Vec);
				p0Vec = p1Vec;
				continue;
			}

			index = (i + pointOffset) % points.Length;

			var p2 = points[index];
			var p2Vec = GetScaledGlyphPoint(p2);

			if (p2.OnCurve) // Points are: on-curve, off-curve, on-curve - quadratic Bézier
			{
				i++;

				AddBezier(p0Vec, p1Vec, p2Vec);
				p0Vec = p2Vec;
			}
			else // Points are: on-curve, off-curve, off-curve - insert midpoint as on-curve point for quadratic Bézier
			{
				var midpoint = Vector2.Lerp(p1Vec, p2Vec, 0.5f);
				AddBezier(p0Vec, p1Vec, midpoint);
				p0Vec = midpoint;
			}
		}

		// Next contour's start point index immediately follows this contour's endpoint index
		return lines;

		Vector2 GetScaledGlyphPoint(GlyphPoint point) => new Vector2(point.X, point.Y) * scale;

		void AddBezier(Vector2 p0, Vector2 p1, Vector2 p2)
		{
			var result = BezierSubdivider.RecursiveBezier(p0, p1, p2, bezierTolerance);
			lines.AddRange(CollectionsMarshal.AsSpan(result)[1..]);
		}
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

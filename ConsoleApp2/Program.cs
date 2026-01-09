using FontThing.Parsing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;
using UtilThing;
using Point = System.Drawing.Point;
using RectangleF = System.Drawing.RectangleF;

namespace ConsoleApp2;

internal abstract class Edge(Point[] points)
{
	public Point[] Points { get; } = points;

	// Returns distance from point to edge, and parameter t along edge
	public abstract float GetDistance(Vector2 point, out float t);

	// Returns number of intersections with ray extending to the right from point
	public abstract int IntersectRay(Vector2 point);
}

internal sealed class LineEdge(Point p0, Point p1) : Edge([p0, p1])
{
	public ref Point P0 => ref Points[0];
	public ref Point P1 => ref Points[1];

	public override float GetDistance(Vector2 point, out float t)
	{
		var start = P0.ToVector2();
		var end = P1.ToVector2();

		// Translate line and p such that start is at origin
		var lineVec = end - start;
		var pointVec = point - start;

		t = Vector2.Dot(pointVec, lineVec) / Vector2.Dot(lineVec, lineVec);

		var projection = start + float.Clamp(t, 0.0f, 1.0f) * lineVec;
		return Vector2.Distance(point, projection);
	}

	public override int IntersectRay(Vector2 point)
	{
		var p0 = P0.ToVector2();
		var p1 = P1.ToVector2();

		// Ensure p0.Y <= p1.Y
		if (p0.Y > p1.Y)
			(p0, p1) = (p1, p0);

		// Point is entirely above or below the edge
		if (point.Y < p0.Y || point.Y >= p1.Y)
			return 0;

		return point.X < (p1.X - p0.X) * (point.Y - p0.Y) / (p1.Y - p0.Y) + p0.X ? 1 : 0;
	}
}

internal sealed class BezierEdge(Point p0, Point p1, Point p2) : Edge([p0, p1, p2])
{
	public ref Point P0 => ref Points[0];
	public ref Point P1 => ref Points[1];
	public ref Point P2 => ref Points[2];

	public override float GetDistance(Vector2 point, out float t)
	{
		var p0 = P0.ToVector2();
		var p1 = P1.ToVector2();
		var p2 = P2.ToVector2();

		var (a, b, c, d) = GetDistanceCubicCoefficients(p0, p1, p2, point);
		Span<double> roots = stackalloc double[3];
		roots = Equations.SolveCubic(a, b, c, d, roots);

		var minDistance = float.MaxValue;
		t = 0.0f;
		foreach (var candidate in (ReadOnlySpan<double>)[..roots, 0, 1])
		{
			if (candidate is < 0.0 or > 1.0)
				continue;

			var tCand = (float)candidate;
			var tInv = 1 - tCand;
			var bezierPoint = tInv * tInv * p0 + 2 * tInv * tCand * p1 + tCand * tCand * p2;

			var distance = Vector2.Distance(point, bezierPoint);

			if (distance >= minDistance)
				continue;

			minDistance = distance;
			t = tCand;
		}

		return minDistance;
	}

	private static (float a, float b, float c, float d) GetDistanceCubicCoefficients(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 point)
	{
		// Bezier curve: B(t) = (1 - t)^2 * P0 + 2(1 - t)t * P1 + t^2 * P2
		// Polynomial form: B(t) = At^2 + Bt + C
		//	A = P0 - 2P1 + P2
		//	B = 2(P1 - P0)
		//	C = P0
		var vecA = p0 - 2 * p1 + p2;
		var vecB = 2 * (p1 - p0);
		var vecC = p0 - point; // Translate so point is at origin

		// Distance squared: D(t) = ||B(t) - point||^2
		// D(t) = (At^2 + Bt + C) . (At^2 + Bt + C)
		// Expanding D(t) gives a cubic polynomial:
		// D(t) = at^3 + bt^2 + ct + d
		//	a = 2 * (A . A)
		//	b = 3 * (A . B)
		//	c = (B . B) + 2 * (C . A)
		//	d = (C . B)
		var a = 2 * Vector2.Dot(vecA, vecA);
		var b = 3 * Vector2.Dot(vecA, vecB);
		var c = Vector2.Dot(vecB, vecB) + 2 * Vector2.Dot(vecC, vecA);
		var d = Vector2.Dot(vecC, vecB);
		return (a, b, c, d);
	}

	public override int IntersectRay(Vector2 point)
	{
		var p0 = P0.ToVector2();
		var p1 = P1.ToVector2();
		var p2 = P2.ToVector2();

		var a = p0.Y - 2 * p1.Y + p2.Y;
		var b = 2 * (p1.Y - p0.Y);
		var c = p0.Y - point.Y;

		var intersections = 0;

		if (float.Abs(a) < 1e-5)
		{
			if (float.Abs(b) > 1e-5)
				CheckT(-c / b);
		}
		else
		{
			var discriminant = b * b - 4 * a * c;

			// Only real roots imply intersection
			if (discriminant >= 0)
			{
				var sqrtDiscriminant = float.Sqrt(discriminant);
				var inv2a = 1.0f / (2.0f * a);

				CheckT((-b - sqrtDiscriminant) * inv2a);
				CheckT((-b + sqrtDiscriminant) * inv2a);
			}
		}

		/*
		Span<double> roots = stackalloc double[2];
		roots = Equations.SolveQuadratic(a, b, c, roots);
		foreach (var root in roots)
			CheckT((float)root);
		*/

		return intersections;

		void CheckT(float t)
		{
			if (t is < 0.0f or >= 1.0f)
				return;

			var tInv = 1 - t;
			var bezierPoint = tInv * tInv * p0 + 2 * tInv * t * p1 + t * t * p2;

			if (point.X < bezierPoint.X)
				intersections++;
		}
	}
}

internal sealed class Contour(List<Edge> edges)
{
	public List<Edge> Edges { get; } = edges;
}

internal sealed class Shape(List<Contour> contours)
{
	public List<Contour> Contours { get; } = contours;

	public bool ContainsPoint(Vector2 point)
	{
		var windingNumber = Contours
			.SelectMany(contour => contour.Edges)
			.Sum(edge => edge.IntersectRay(point));

		return windingNumber % 2 != 0;
	}

	public float GetDistance(Vector2 point) => Contours
		.SelectMany(contour => contour.Edges)
		.Select(edge => edge.GetDistance(point, out _))
		.Prepend(float.MaxValue)
		.Min();

	public RectangleF GetBounds()
	{
		var min = new Vector2(float.MaxValue, float.MaxValue);
		var max = new Vector2(float.MinValue, float.MinValue);

		foreach (var point in Contours.SelectMany(contour => contour.Edges).SelectMany(edge => edge.Points))
		{
			var vec = point.ToVector2();
			min = Vector2.Min(min, vec);
			max = Vector2.Max(max, vec);
		}

		return new(min.X, min.Y, max.X - min.X, max.Y - min.Y);
	}
}

internal static class Program
{
	private static void Main()
	{
		const float pxRange = 4.0f;
		const int size = 64;

		var font = TrueTypeFont.FromFile("/usr/share/fonts/TTF/segoeui.ttf");
		var glyph = font.LoadGlyph("&".EnumerateRunes().First());
		if (glyph.Outline is not SimpleGlyphOutline outline)
			throw new("FIXME");

		var contours = new List<Contour>();

		for (var i = 0; i < outline.NumberOfContours; i++)
		{
			var edges = new List<Edge>();

			outline.ProcessContour(i, Line, Bezier);
			contours.Add(new(edges));
			continue;

			void Line(Point p0, Point p1)
			{
				edges.Add(new LineEdge(p0, p1));
			}

			void Bezier(Point p0, Point p1, Point p2)
			{
				edges.Add(new BezierEdge(p0, p1, p2));
			}
		}

		var shape = new Shape(contours);

		var glyphBounds = shape.GetBounds();
		var glyphSize = glyphBounds.Size.ToVector2();
		var glyphLocation = glyphBounds.Location.ToVector2();

		var maxDimension = Math.Max(glyphBounds.Width, glyphBounds.Height);

		// Compute scale to fit shape within (size - 2 * pxRange) box
		var scale = (size - 2 * pxRange) / maxDimension;

		// Compute offset to center of shape
		var offset = new Vector2(-size / 2.0f + pxRange) / scale + (glyphSize / 2.0f) + glyphLocation;

		var sdfBuffer = new byte[size * size];

		for (var y = 0; y < size; y++)
		{
			for (var x = 0; x < size; x++)
			{
				var fontX = (x / scale) + offset.X - (pxRange / scale);
				var fontY = ((size - 1 - y) / scale) + offset.Y - (pxRange / scale);

				var samplePoint = new Vector2(fontX, fontY);

				var distance = shape.GetDistance(samplePoint);
				var inside = shape.ContainsPoint(samplePoint);
				var signedDistance = inside ? distance : -distance;

				var signedDistanceInPixels = signedDistance * scale;
				var normalized = 0.5f + (signedDistanceInPixels / (2 * pxRange));

				var pixelValue = (byte)(float.Clamp(normalized, 0.0f, 1.0f) * 255);

				sdfBuffer[y * size + x] = pixelValue;
			}
		}

		using var image = new Image<Rgba32>(size, size);
		for (var y = 0; y < size; y++)
		{
			for (var x = 0; x < size; x++)
			{
				var value = sdfBuffer[y * size + x];
				image[x, y] = new Rgba32(value, value, value, 255);
			}
		}

		image.SaveAsPng("sdf_output.png");
	}
}

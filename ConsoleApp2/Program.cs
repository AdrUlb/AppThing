using FontThing.Parsing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using UtilThing;
using Point = System.Drawing.Point;
using RectangleF = System.Drawing.RectangleF;

namespace ConsoleApp2;

internal abstract class Edge(Vector2d[] points)
{
	protected const double Epsilon = 1e-9;

	public Vector2d[] Points { get; } = points;

	// Returns distance from point to edge, and parameter t along edge
	public abstract double GetDistance(Vector2d point, out double t);

	// Returns number of intersections with ray extending to the right from point
	public abstract int IntersectRay(Vector2d point);
}

internal sealed class LineEdge(Vector2d p0, Vector2d p1) : Edge([p0, p1])
{
	public ref Vector2d P0 => ref Points[0];
	public ref Vector2d P1 => ref Points[1];

	public override double GetDistance(Vector2d point, out double t)
	{
		var p0 = P0;
		var p1 = P1;
		// Handle zero-length line
		if (Vector2d.Distance(p0, p1) < Epsilon)
		{
			t = 0.0;
			var distance = Vector2d.Distance(point, p0);
			Debug.Assert(!double.IsNaN(distance));
			return distance;
		}

		var lineVec = p1 - p0;
		var pointVec = point - p0;

		t = Vector2d.Dot(pointVec, lineVec) / lineVec.LengthSquared();

		var projection = p0 + double.Clamp(t, 0.0, 1.0) * lineVec;

		{
			var distance = Vector2d.Distance(point, projection);
			Debug.Assert(!double.IsNaN(distance));
			return distance;
		}
	}

	public override int IntersectRay(Vector2d point)
	{
		var p0 = P0;
		var p1 = P1;

		var cond0 = p0.Y > point.Y;
		var cond1 = p1.Y > point.Y;

		// Both points are on same side of ray, no intersection
		if (cond0 == cond1)
			return 0;

		// Compute intersection X coordinate
		var intersectX = (p1.X - p0.X) * (point.Y - p0.Y) / (p1.Y - p0.Y) + p0.X;

		// Check if intersection is to the right of point
		return point.X < intersectX ? 1 : 0;
	}
}

internal sealed class BezierEdge(Vector2d p0, Vector2d p1, Vector2d p2) : Edge([p0, p1, p2])
{
	public ref Vector2d P0 => ref Points[0];
	public ref Vector2d P1 => ref Points[1];
	public ref Vector2d P2 => ref Points[2];

	public override double GetDistance(Vector2d point, out double t)
	{
		var p0 = P0;
		var p1 = P1;
		var p2 = P2;

		var (a, b, c, d) = GetDistanceCubicCoefficients(p0, p1, p2, point);

		Span<double> roots = stackalloc double[3];
		roots = Equations.SolveCubic(a, b, c, d, roots);

		var minDistance = double.MaxValue;
		t = 0.0;
		foreach (var candidate in (ReadOnlySpan<double>)[..roots, 0, 1])
		{
			if (candidate is <= 0.0 or >= 1.0)
				continue;

			var tInv = 1 - candidate;
			var bezierPoint = tInv * tInv * p0 + 2 * tInv * candidate * p1 + candidate * candidate * p2;

			var distance = Vector2d.Distance(point, bezierPoint);

			if (distance < minDistance)
			{
				minDistance = distance;
				t = candidate;
			}
		}

		Debug.Assert(!double.IsNaN(minDistance));
		return minDistance;
	}

	private static (double a, double b, double c, double d) GetDistanceCubicCoefficients(Vector2d p0, Vector2d p1, Vector2d p2, Vector2d point)
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
		var a = 2 * Vector2d.Dot(vecA, vecA);
		var b = 3 * Vector2d.Dot(vecA, vecB);
		var c = Vector2d.Dot(vecB, vecB) + 2 * Vector2d.Dot(vecC, vecA);
		var d = Vector2d.Dot(vecC, vecB);
		return (a, b, c, d);
	}

	public override int IntersectRay(Vector2d point)
	{
		var p0 = P0;
		var p1 = P1;
		var p2 = P2;

		var a = p0.Y - 2 * p1.Y + p2.Y;
		var b = 2 * (p1.Y - p0.Y);
		var c = p0.Y - point.Y;

		var intersections = 0;

		if (double.Abs(a) < 1e-14)
		{
			if (double.Abs(b) > 1e-14)
				CheckT(-c / b);
		}
		else
		{
			var discriminant = b * b - 4 * a * c;

			// Only real roots imply intersection
			if (discriminant >= 0)
			{
				var sqrtDiscriminant = double.Sqrt(discriminant);
				var q = -0.5 * (b + Math.Sign(b) * sqrtDiscriminant);
				if (b == 0)
					q = -0.5 * sqrtDiscriminant;

				CheckT(q / a);
				if (q != 0)
					CheckT(c / q);
			}
		}

		return intersections;

		void CheckT(double t)
		{
			if (t is < 0.0 or >= 1.0)
				return;

			var tInv = 1 - t;
			var x = tInv * tInv * p0.X + 2 * tInv * t * p1.X + t * t * p2.X;

			if (point.X >= x)
				return;

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

	public bool ContainsPoint(Vector2d point)
	{
		var windingNumber = Contours
			.SelectMany(contour => contour.Edges)
			.Sum(edge => edge.IntersectRay(point));

		return windingNumber % 2 != 0;
	}

	public double GetDistance(Vector2d point) => Contours
		.SelectMany(contour => contour.Edges)
		.Select(edge => edge.GetDistance(point, out _))
		.Prepend(double.MaxValue)
		.Min();

	public RectangleD GetBounds()
	{
		var min = new Vector2d(double.MaxValue, double.MaxValue);
		var max = new Vector2d(double.MinValue, double.MinValue);

		foreach (var point in Contours.SelectMany(contour => contour.Edges).SelectMany(edge => edge.Points))
		{
			min = Vector2d.Min(min, point);
			max = Vector2d.Max(max, point);
		}

		return new(min.X, min.Y, max.X - min.X, max.Y - min.Y);
	}
}

internal static class Program
{
	private static void Main()
	{
		const double pxRange = 4.0;
		//const double pxRange = 0.5;
		const int size = 64;

		/*
		//var font = TrueTypeFont.FromFile("/usr/share/fonts/TTF/segoeui.ttf");
		var font = TrueTypeFont.FromFile("/usr/share/fonts/TTF/seguiemj.ttf");
		//var glyph = font.LoadGlyph(Rune.GetRuneAt("👍", 0));
		var glyph = font.LoadGlyph(Rune.GetRuneAt("😭", 0));
		*/

		var font = TrueTypeFont.FromFile("/usr/share/fonts/TTF/segoeui.ttf");
		var glyph = font.LoadGlyph(Rune.GetRuneAt("B", 0));


		var outline = glyph.Outline ?? throw new("FIXME");
		var contours = new List<Contour>();
		LoadContours(outline, contours);
		var shape = new Shape(contours);

		var glyphBounds = shape.GetBounds();
		var glyphSize = glyphBounds.Size;
		var glyphLocation = glyphBounds.Location;

		var maxDimension = Math.Max(glyphBounds.Width, glyphBounds.Height);

		// Compute scale to fit shape within (size - 2 * pxRange) box
		var scale = (size - 2 * pxRange) / maxDimension;

		// Compute offset to center of shape
		var offset = new Vector2d(-size / 2.0 + pxRange) / scale + (glyphSize / 2.0) + glyphLocation;

		var buffer = new byte[size * size];
		GenerateSdf(shape, size, size, buffer, offset, scale, pxRange);
		SaveSdfAsPng("sdf_output.png", size, size, buffer);
	}

	private static void GenerateSdf(Shape shape, int width, int height, Span<byte> sdf, Vector2d offset, double scale, double pxRange)
	{
		for (var y = 0; y < height; y++)
		{
			for (var x = 0; x < width; x++)
			{
				var fontX = (x / scale) + offset.X - (pxRange / scale);
				var fontY = ((height - 1 - y) / scale) + offset.Y - (pxRange / scale);

				var samplePoint = new Vector2d(fontX, fontY);

				var distance = shape.GetDistance(samplePoint);
				var inside = shape.ContainsPoint(samplePoint);
				var signedDistance = inside ? distance : -distance;

				var signedDistanceInPixels = signedDistance * scale;
				var normalized = 0.5 + (signedDistanceInPixels / (2 * 0.5));

				var pixelValue = (byte)(double.Clamp(normalized, 0.0, 1.0) * 255);

				sdf[x + y * width] = pixelValue;
			}
		}
	}

	private static void SaveSdfAsPng(string path, int width, int height, ReadOnlySpan<byte> sdf)
	{
		using var image = new Image<Rgba32>(width, height);
		for (var y = 0; y < height; y++)
		{
			for (var x = 0; x < width; x++)
			{
				var value = sdf[y * width + x];
				image[x, y] = new(value, value, value, 255);
			}
		}

		image.SaveAsPng(path);
	}

	private static void LoadContours(GlyphOutline outline, List<Contour> contours)
	{
		switch (outline)
		{
			case SimpleGlyphOutline o:
				LoadContours(o, contours);
				return;
			case CompoundGlyphOutline o:
				LoadContours(o, contours);
				return;
			default:
				throw new NotSupportedException("Unsupported glyph outline type");
		}

	}

	private static void LoadContours(SimpleGlyphOutline outline, List<Contour> contours)
	{
		for (var i = 0; i < outline.NumberOfContours; i++)
		{
			var edges = new List<Edge>();

			outline.ProcessContour(i, Line, Bezier);
			contours.Add(new(edges));
			continue;

			void Line(Point p0, Point p1)
			{
				edges.Add(new LineEdge(p0.ToVector2d(), p1.ToVector2d()));
			}

			void Bezier(Point p0, Point p1, Point p2)
			{
				edges.Add(new BezierEdge(p0.ToVector2d(), p1.ToVector2d(), p2.ToVector2d()));
			}
		}
	}

	private static void LoadContours(CompoundGlyphOutline outline, List<Contour> contours)
	{
		foreach (var component in outline.Components)
		{
			if (!component.ArgsAreXyValues)
				throw new NotImplementedException();

			var componentOutline = outline.Font.GetGlyphOutlineFromIndex(component.GlyphIndex);
			Debug.Assert(componentOutline != null);

			var index = contours.Count;
			LoadContours(componentOutline, contours);
			for (var i = index; i < contours.Count; i++)
			{
				var contour = contours[i];
				for (var j = 0; j < contour.Edges.Count; j++)
				{
					ref var edge = ref CollectionsMarshal.AsSpan(contour.Edges)[j];

					for (var k = 0; k < edge.Points.Length; k++)
					{
						ref var p = ref edge.Points[k];

						// Apply scaling
						var scaledX = p.X * (double)component.ScaleX + p.Y * (double)component.Scale01;
						var scaledY = p.X * (double)component.Scale10 + p.Y * (double)component.ScaleY;

						// Apply translation
						scaledX += component.Arg1;
						scaledY += component.Arg2;

						p = new(scaledX, scaledY);
					}
				}
			}
		}
	}
}

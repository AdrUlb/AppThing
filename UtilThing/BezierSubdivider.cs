using System.Numerics;
using System.Runtime.CompilerServices;

namespace UtilThing;

public static class BezierSubdivider
{
	public static List<Vector2> RecursiveBezier(Vector2 p0, Vector2 p1, Vector2 p2, float tolerance)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tolerance);

		var output = new List<Vector2>();
		RecursiveBezier(p0, p1, p2, output, tolerance);
		return output;
	}

	public static void RecursiveBezier(Vector2 p0, Vector2 p1, Vector2 p2, IList<Vector2> output, float tolerance)
	{
		if (IsStraightEnough(p0, p1, p2, tolerance))
		{
			if (output.Count == 0)
				output.Add(p0);

			output.Add(p2);
			return;
		}

		Subdivide(p0, p1, p2, out var leftP0, out var leftP1, out var leftP2, out var rightP0, out var rightP1, out var rightP2);

		RecursiveBezier(leftP0, leftP1, leftP2, output, tolerance);
		RecursiveBezier(rightP0, rightP1, rightP2, output, tolerance);
	}

	public static void Subdivide(Vector2 p0, Vector2 p1, Vector2 p2, Span<Vector2> left, Span<Vector2> right)
	{
		// Compute midpoints between control points
		var p01 = Vector2.Lerp(p0, p1, 0.5f);
		var p12 = Vector2.Lerp(p1, p2, 0.5f);
		var p012 = Vector2.Lerp(p01, p12, 0.5f);

		// Populate the left and right spans
		left[0] = p0;
		left[1] = p01;
		left[2] = p012;

		right[0] = p012;
		right[1] = p12;
		right[2] = p2;
	}

	public static void Subdivide(Vector2 p0, Vector2 p1, Vector2 p2, out Vector2 leftP0, out Vector2 leftP1, out Vector2 leftP2, out Vector2 rightP0, out Vector2 rightP1, out Vector2 rightP2)
	{
		// Compute midpoints between control points
		var p01 = Vector2.Lerp(p0, p1, 0.5f);
		var p12 = Vector2.Lerp(p1, p2, 0.5f);
		var p012 = Vector2.Lerp(p01, p12, 0.5f);

		// Populate the left and right spans
		leftP0 = p0;
		leftP1 = p01;
		leftP2 = p012;

		rightP0 = p012;
		rightP1 = p12;
		rightP2 = p2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsStraightEnough(Vector2 p0, Vector2 p1, Vector2 p2, float tolerance)
	{
		// Distance of control point p1 to line p1->p3
		var distance = PointDistanceToLine(p1, p0, p2);
		return distance < tolerance;
	}

	private static float PointDistanceToLine(Vector2 point, Vector2 p0, Vector2 p1)
	{
		var lineVector = p1 - p0;
		var lineLengthSquared = lineVector.LengthSquared();

		if (lineLengthSquared == 0)
			return Vector2.Distance(point, p0);

		var t = float.Max(0, float.Min(1, Vector2.Dot(point - p0, lineVector) / lineLengthSquared));
		var projection = p0 + t * lineVector;
		return Vector2.Distance(point, projection);
	}
}

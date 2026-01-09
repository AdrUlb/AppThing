using System.Drawing;
using System.Numerics;

namespace UtilThing;

public static class PointExtensions
{
	public static Vector2 ToVector2(this Point point) => new(point.X, point.Y);
}

public static class PointFExtensions
{
	public static Vector2 ToVector2(this PointF point) => new(point.X, point.Y);
}

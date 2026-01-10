using System.Drawing;
using System.Numerics;

namespace UtilThing;

public static class SystemDrawingExtensions
{
	extension(Point point)
	{
		public Vector2 ToVector2() => new(point.X, point.Y);

		public Vector2d ToVector2d() => new(point.X, point.Y);
	}

	extension(PointF point)
	{
		public Vector2 ToVector2() => new(point.X, point.Y);

		public Vector2d ToVector2d() => new(point.X, point.Y);
	}
	
	extension(Size size)
	{
		public Vector2 ToVector2() => new(size.Width, size.Height);

		public Vector2d ToVector2d() => new(size.Width, size.Height);
	}

	extension(SizeF size)
	{
		public Vector2 ToVector2() => new(size.Width, size.Height);

		public Vector2d ToVector2d() => new(size.Width, size.Height);
	}
}

using System.Drawing;
using System.Numerics;

namespace UtilThing;

public static class Vector2Extensions
{
	extension(Vector2 vec)
	{
		public SizeF ToSizeF() => new(vec.X, vec.Y);

		public Vector2d ToVector2d() => new(vec.X, vec.Y);
	}
}

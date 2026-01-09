using System.Drawing;
using System.Numerics;

namespace UtilThing;

public static class Vector2Extensions
{
	public static SizeF ToSizeF(this Vector2 vec) => new(vec.X, vec.Y);
}

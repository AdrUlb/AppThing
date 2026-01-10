using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace UtilThing;

public static class Vector128Extensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d AsVector2d(this Vector128<double> value)
	{
		ref var address = ref Unsafe.As<Vector128<double>, byte>(ref value);
		return Unsafe.ReadUnaligned<Vector2d>(ref address);
	}
}

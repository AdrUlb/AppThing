using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace UtilThing;

public struct Vector2d : IEquatable<Vector2d>, IFormattable
{
	public double X;
	public double Y;

	internal const int ElementCount = 2;

	public Vector2d(double value) => this = Create(value);

	public Vector2d(double x, double y) => this = Create(x, y);

	public Vector2d(ReadOnlySpan<double> values) => this = Create(values);

	public static Vector2d Create(double value) => Vector128.Create<double>(value).AsVector2d();

	// FIXME???: public static Vector2d Create(double x, double y) => Vector128.Create<double>(x, y, 0, 0).AsVector2d();
	public static Vector2d Create(double x, double y) => new() { X = x, Y = y };

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d Create(ReadOnlySpan<double> values)
	{
		if (values.Length < ElementCount)
			throw new ArgumentOutOfRangeException(nameof(values));

		return Unsafe.ReadUnaligned<Vector2d>(ref Unsafe.As<double, byte>(ref MemoryMarshal.GetReference(values)));
	}

	public double this[int index]
	{
		readonly get => GetElement(index);
		set => this = WithElement(index, value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d operator +(Vector2d left, Vector2d right) => (left.AsVector128Unsafe() + right.AsVector128Unsafe()).AsVector2d();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d operator /(Vector2d left, Vector2d right) => (left.AsVector128Unsafe() / right.AsVector128Unsafe()).AsVector2d();

	public bool Equals(Vector2d other) => throw new NotImplementedException();

	public override bool Equals(object? obj) => obj is Vector2d other && Equals(other);

	public override int GetHashCode() => throw new NotImplementedException();

	public readonly override string ToString() => ToString("G", CultureInfo.CurrentCulture);

	public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format) => ToString(format, CultureInfo.CurrentCulture);

	public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? formatProvider)
	{
		var separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
		return $"<{X.ToString(format, formatProvider)}{separator} {Y.ToString(format, formatProvider)}>";
	}

	// --- These are extension methods originally ---

	public readonly Vector128<double> AsVector128Unsafe()
	{
		Unsafe.SkipInit(out Vector128<double> result);
		Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<double>, byte>(ref result), this);
		return result;
	}

	public readonly double GetElement(int index)
	{
		if ((uint)index >= ElementCount)
			throw new ArgumentOutOfRangeException(nameof(index));

		return AsVector128Unsafe().GetElement(index);
	}

	public Vector2d WithElement(int index, double value)
	{
		if ((uint)index >= ElementCount)
			throw new ArgumentOutOfRangeException(nameof(index));

		return AsVector128Unsafe().WithElement(index, value).AsVector2d();
	}
}

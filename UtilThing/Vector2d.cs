// Based on .NET source code

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace UtilThing;

/// <summary>Represents a vector with two double-precision floating-point values.</summary>
public struct Vector2d : IEquatable<Vector2d>, IFormattable
{
	internal const int Alignment = 16;

	/// <summary>The X component of the vector.</summary>
	public double X;

	/// <summary>The Y component of the vector.</summary>
	public double Y;

	internal const int ElementCount = 2;

	/// <summary>Creates a new <see cref="Vector2d" /> object whose two elements have the same value.</summary>
	/// <param name="value">The value to assign to both elements.</param>
	public Vector2d(double value) => this = Create(value);

	/// <summary>Creates a vector whose elements have the specified values.</summary>
	/// <param name="x">The value to assign to the <see cref="X" /> field.</param>
	/// <param name="y">The value to assign to the <see cref="Y" /> field.</param>
	public Vector2d(double x, double y) => this = Create(x, y);

	/// <summary>Constructs a vector from the given <see cref="ReadOnlySpan{Single}" />. The span must contain at least 2 elements.</summary>
	/// <param name="values">The span of elements to assign to the vector.</param>
	public Vector2d(ReadOnlySpan<double> values) => this = Create(values);

	/// <summary>Gets or sets the element at the specified index.</summary>
	/// <param name="index">The index of the element to get or set.</param>
	/// <returns>The the element at <paramref name="index" />.</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
	public double this[int index]
	{
		readonly get => GetElement(index);
		set => this = WithElement(index, value);
	}

	/// <summary>Adds two vectors together.</summary>
	/// <param name="left">The first vector to add.</param>
	/// <param name="right">The second vector to add.</param>
	/// <returns>The summed vector.</returns>
	/// <remarks>The <see cref="op_Addition" /> method defines the addition operation for <see cref="Vector2d" /> objects.</remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d operator +(Vector2d left, Vector2d right) => (left.AsVector128Unsafe() + right.AsVector128Unsafe()).AsVector2d();

	/// <summary>Divides the first vector by the second.</summary>
	/// <param name="left">The first vector.</param>
	/// <param name="right">The second vector.</param>
	/// <returns>The vector that results from dividing <paramref name="left" /> by <paramref name="right" />.</returns>
	/// <remarks>The <see cref="Vector2d.op_Division" /> method defines the division operation for <see cref="Vector2d" /> objects.</remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d operator /(Vector2d left, Vector2d right) => (left.AsVector128Unsafe() / right.AsVector128Unsafe()).AsVector2d();

	/// <summary>Divides the specified vector by a specified scalar value.</summary>
	/// <param name="value1">The vector.</param>
	/// <param name="value2">The scalar value.</param>
	/// <returns>The result of the division.</returns>
	/// <remarks>The <see cref="Vector2d.op_Division" /> method defines the division operation for <see cref="Vector2d" /> objects.</remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d operator /(Vector2d value1, double value2) => (value1.AsVector128Unsafe() / value2).AsVector2d();

	/// <summary>Returns a value that indicates whether each pair of elements in two specified vectors is equal.</summary>
	/// <param name="left">The first vector to compare.</param>
	/// <param name="right">The second vector to compare.</param>
	/// <returns><see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are equal; otherwise, <see langword="false" />.</returns>
	/// <remarks>Two <see cref="Vector2d" /> objects are equal if each value in <paramref name="left" /> is equal to the corresponding value in <paramref name="right" />.</remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Vector2d left, Vector2d right) => left.AsVector128() == right.AsVector128();

	/// <summary>Returns a value that indicates whether two specified vectors are not equal.</summary>
	/// <param name="left">The first vector to compare.</param>
	/// <param name="right">The second vector to compare.</param>
	/// <returns><see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are not equal; otherwise, <see langword="false" />.</returns>
	public static bool operator !=(Vector2d left, Vector2d right) => !(left == right);

	/// <summary>Returns a new vector whose values are the product of each pair of elements in two specified vectors.</summary>
	/// <param name="left">The first vector.</param>
	/// <param name="right">The second vector.</param>
	/// <returns>The element-wise product vector.</returns>
	/// <remarks>The <see cref="Vector2d.op_Multiply" /> method defines the multiplication operation for <see cref="Vector2d" /> objects.</remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d operator *(Vector2d left, Vector2d right) => (left.AsVector128Unsafe() * right.AsVector128Unsafe()).AsVector2d();

	/// <summary>Multiplies the specified vector by the specified scalar value.</summary>
	/// <param name="left">The vector.</param>
	/// <param name="right">The scalar value.</param>
	/// <returns>The scaled vector.</returns>
	/// <remarks>The <see cref="Vector2d.op_Multiply" /> method defines the multiplication operation for <see cref="Vector2d" /> objects.</remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d operator *(Vector2d left, double right) => (left.AsVector128Unsafe() * right).AsVector2d();

	/// <summary>Multiplies the scalar value by the specified vector.</summary>
	/// <param name="left">The vector.</param>
	/// <param name="right">The scalar value.</param>
	/// <returns>The scaled vector.</returns>
	/// <remarks>The <see cref="Vector2d.op_Multiply" /> method defines the multiplication operation for <see cref="Vector2d" /> objects.</remarks>
	public static Vector2d operator *(double left, Vector2d right) => right * left;

	/// <summary>Subtracts the second vector from the first.</summary>
	/// <param name="left">The first vector.</param>
	/// <param name="right">The second vector.</param>
	/// <returns>The vector that results from subtracting <paramref name="right" /> from <paramref name="left" />.</returns>
	/// <remarks>The <see cref="op_Subtraction" /> method defines the subtraction operation for <see cref="Vector2d" /> objects.</remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d operator -(Vector2d left, Vector2d right) => (left.AsVector128Unsafe() - right.AsVector128Unsafe()).AsVector2d();

	/// <summary>Negates the specified vector.</summary>
	/// <param name="value">The vector to negate.</param>
	/// <returns>The negated vector.</returns>
	/// <remarks>The <see cref="op_UnaryNegation" /> method defines the unary negation operation for <see cref="Vector2d" /> objects.</remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d operator -(Vector2d value) => (-value.AsVector128Unsafe()).AsVector2d();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d operator &(Vector2d left, Vector2d right) => (left.AsVector128Unsafe() & right.AsVector128Unsafe()).AsVector2d();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d operator |(Vector2d left, Vector2d right) => (left.AsVector128Unsafe() | right.AsVector128Unsafe()).AsVector2d();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d operator ^(Vector2d left, Vector2d right) => (left.AsVector128Unsafe() ^ right.AsVector128Unsafe()).AsVector2d();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d operator <<(Vector2d value, int shiftAmount) => (value.AsVector128Unsafe() << shiftAmount).AsVector2d();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d operator ~(Vector2d value) => (~value.AsVector128Unsafe()).AsVector2d();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d operator >> (Vector2d value, int shiftAmount) => (value.AsVector128Unsafe() >> shiftAmount).AsVector2d();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d operator +(Vector2d value) => value;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d operator >>> (Vector2d value, int shiftAmount) => (value.AsVector128Unsafe() >>> shiftAmount).AsVector2d();


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d Abs(Vector2d value) => Vector128.Abs(value.AsVector128Unsafe()).AsVector2d();

	/// <summary>Adds two vectors together.</summary>
	/// <param name="left">The first vector to add.</param>
	/// <param name="right">The second vector to add.</param>
	/// <returns>The summed vector.</returns>
	public static Vector2d Add(Vector2d left, Vector2d right) => left + right;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool All(Vector2d vector, double value) => Vector128.All(vector.AsVector128Unsafe(), value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool AllWhereAllBitsSet(Vector2d vector) => Vector128.AllWhereAllBitsSet(vector.AsVector128Unsafe());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d AndNot(Vector2d left, Vector2d right) => Vector128.AndNot(left.AsVector128Unsafe(), right.AsVector128Unsafe()).AsVector2d();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool Any(Vector2d vector, double value) => Vector128.Any(vector.AsVector128Unsafe(), value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool AnyWhereAllBitsSet(Vector2d vector) => Vector128.AnyWhereAllBitsSet(vector.AsVector128Unsafe());

	public static Vector2d BitwiseAnd(Vector2d left, Vector2d right) => left & right;

	public static Vector2d BitwiseOr(Vector2d left, Vector2d right) => left | right;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d Clamp(Vector2d value1, Vector2d min, Vector2d max) => Vector128.Clamp(value1.AsVector128Unsafe(), min.AsVector128Unsafe(), max.AsVector128Unsafe()).AsVector2d();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d ClampNative(Vector2d value1, Vector2d min, Vector2d max) => Vector128.ClampNative(value1.AsVector128Unsafe(), min.AsVector128Unsafe(), max.AsVector128Unsafe()).AsVector2d();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d ConditionalSelect(Vector2d condition, Vector2d left, Vector2d right) => Vector128.ConditionalSelect(condition.AsVector128Unsafe(), left.AsVector128Unsafe(), right.AsVector128Unsafe()).AsVector2d();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d CopySign(Vector2d value, Vector2d sign) => Vector128.CopySign(value.AsVector128Unsafe(), sign.AsVector128Unsafe()).AsVector2d();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d Cos(Vector2d vector) => Vector128.Cos(vector.AsVector128()).AsVector2d();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Count(Vector2d vector, double value) => Vector128.Count(vector.AsVector128Unsafe(), value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int CountWhereAllBitsSet(Vector2d vector) => Vector128.CountWhereAllBitsSet(vector.AsVector128Unsafe());


	/// <summary>Creates a new <see cref="Vector2d" /> object whose two elements have the same value.</summary>
	/// <param name="value">The value to assign to all two elements.</param>
	/// <returns>A new <see cref="Vector2d" /> whose two elements have the same value.</returns>
	public static Vector2d Create(double value) => Vector128.Create<double>(value).AsVector2d();

	/// <summary>Creates a vector whose elements have the specified values.</summary>
	/// <param name="x">The value to assign to the <see cref="X" /> field.</param>
	/// <param name="y">The value to assign to the <see cref="Y" /> field.</param>
	/// <returns>A new <see cref="Vector2d" /> whose elements have the specified values.</returns>
	public static Vector2d Create(double x, double y) => Vector128.Create(x, y).AsVector2d();

	/// <summary>Constructs a vector from the given <see cref="ReadOnlySpan{Single}" />. The span must contain at least 2 elements.</summary>
	/// <param name="values">The span of elements to assign to the vector.</param>
	/// <returns>A new <see cref="Vector2d" /> whose elements have the specified values.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d Create(ReadOnlySpan<double> values)
	{
		if (values.Length < ElementCount)
			throw new ArgumentOutOfRangeException(nameof(values));

		return Unsafe.ReadUnaligned<Vector2d>(ref Unsafe.As<double, byte>(ref MemoryMarshal.GetReference(values)));
	}

	/// <summary>Creates a vector with <see cref="X" /> initialized to the specified value and the remaining elements initialized to zero.</summary>
	/// <param name="x">The value to assign to the <see cref="X" /> field.</param>
	/// <returns>A new <see cref="Vector2d" /> with <see cref="X" /> initialized <paramref name="x" /> and the remaining elements initialized to zero.</returns>
	public static Vector2d CreateScalar(double x) => Vector128.CreateScalar(x).AsVector2d();

	/// <summary>Creates a vector with <see cref="X" /> initialized to the specified value and the remaining elements left uninitialized.</summary>
	/// <param name="x">The value to assign to the <see cref="X" /> field.</param>
	/// <returns>A new <see cref="Vector2d" /> with <see cref="X" /> initialized <paramref name="x" /> and the remaining elements left uninitialized.</returns>
	public static Vector2d CreateScalarUnsafe(double x) => Vector128.CreateScalarUnsafe(x).AsVector2d();

	/// <summary>
	/// Returns the z-value of the cross product of two vectors.
	/// Since the Vector2d is in the x-y plane, a 3D cross product only produces the z-value.
	/// </summary>
	/// <param name="value1">The first vector.</param>
	/// <param name="value2">The second vector.</param>
	/// <returns>The value of the z-coordinate from the cross product.</returns>
	/// <remarks>
	/// Return z-value = value1.X * value2.Y - value1.Y * value2.X
	/// <see cref="Cross"/> is the same as taking the <see cref="Dot"/> with the second vector
	/// that has been rotated 90-degrees.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double Cross(Vector2d value1, Vector2d value2) => value1.X * value2.Y - value1.Y * value2.X;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d DegreesToRadians(Vector2d degrees) => Vector128.DegreesToRadians(degrees.AsVector128Unsafe()).AsVector2d();

	/// <summary>Computes the Euclidean distance between the two given points.</summary>
	/// <param name="value1">The first point.</param>
	/// <param name="value2">The second point.</param>
	/// <returns>The distance.</returns>
	public static double Distance(Vector2d value1, Vector2d value2) => double.Sqrt(DistanceSquared(value1, value2));

	/// <summary>Returns the Euclidean distance squared between two specified points.</summary>
	/// <param name="value1">The first point.</param>
	/// <param name="value2">The second point.</param>
	/// <returns>The distance squared.</returns>
	public static double DistanceSquared(Vector2d value1, Vector2d value2) => (value1 - value2).LengthSquared();

	/// <summary>Divides the first vector by the second.</summary>
	/// <param name="left">The first vector.</param>
	/// <param name="right">The second vector.</param>
	/// <returns>The vector resulting from the division.</returns>
	public static Vector2d Divide(Vector2d left, Vector2d right) => left / right;

	/// <summary>Divides the specified vector by a specified scalar value.</summary>
	/// <param name="left">The vector.</param>
	/// <param name="divisor">The scalar value.</param>
	/// <returns>The vector that results from the division.</returns>
	public static Vector2d Divide(Vector2d left, double divisor) => left / divisor;

	/// <summary>Returns the dot product of two vectors.</summary>
	/// <param name="value1">The first vector.</param>
	/// <param name="value2">The second vector.</param>
	/// <returns>The dot product.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double Dot(Vector2d value1, Vector2d value2) => Vector128.Dot(value1.AsVector128(), value2.AsVector128());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d Exp(Vector2d vector) => Vector128.Exp(vector.AsVector128()).AsVector2d();

	/// --------------------------------------------------

	// TODO: Log
	// TODO: Log2
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d Max(Vector2d value1, Vector2d value2) => Vector128.Max(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector2d();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d MaxMagnitude(Vector2d value1, Vector2d value2) => Vector128.MaxMagnitude(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector2d();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d MaxMagnitudeNumber(Vector2d value1, Vector2d value2) => Vector128.MaxMagnitudeNumber(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector2d();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d MaxNative(Vector2d value1, Vector2d value2) => Vector128.MaxNative(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector2d();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d MaxNumber(Vector2d value1, Vector2d value2) => Vector128.MaxNumber(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector2d();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d Min(Vector2d value1, Vector2d value2) => Vector128.Min(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector2d();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d MinMagnitude(Vector2d value1, Vector2d value2) => Vector128.MinMagnitude(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector2d();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d MinMagnitudeNumber(Vector2d value1, Vector2d value2) => Vector128.MinMagnitudeNumber(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector2d();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d MinNative(Vector2d value1, Vector2d value2) => Vector128.MinNative(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector2d();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d MinNumber(Vector2d value1, Vector2d value2) => Vector128.MinNumber(value1.AsVector128Unsafe(), value2.AsVector128Unsafe()).AsVector2d();

	/// <summary>Returns a new vector whose values are the product of each pair of elements in two specified vectors.</summary>
	/// <param name="left">The first vector.</param>
	/// <param name="right">The second vector.</param>
	/// <returns>The element-wise product vector.</returns>
	public static Vector2d Multiply(Vector2d left, Vector2d right) => left * right;

	/// <summary>Multiplies a vector by a specified scalar.</summary>
	/// <param name="left">The vector to multiply.</param>
	/// <param name="right">The scalar value.</param>
	/// <returns>The scaled vector.</returns>
	public static Vector2d Multiply(Vector2d left, double right) => left * right;

	/// <summary>Multiplies a scalar value by a specified vector.</summary>
	/// <param name="left">The scaled value.</param>
	/// <param name="right">The vector.</param>
	/// <returns>The scaled vector.</returns>
	public static Vector2d Multiply(double left, Vector2d right) => left * right;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d MultiplyAddEstimate(Vector2d left, Vector2d right, Vector2d addend) => Vector128.MultiplyAddEstimate(left.AsVector128Unsafe(), right.AsVector128Unsafe(), addend.AsVector128Unsafe()).AsVector2d();

	/// <summary>Negates a specified vector.</summary>
	/// <param name="value">The vector to negate.</param>
	/// <returns>The negated vector.</returns>
	public static Vector2d Negate(Vector2d value) => -value;

	// TODO: None
	// TODO: NoneWhereAllBitsSet

	/// <summary>Returns a vector with the same direction as the specified vector, but with a length of one.</summary>
	/// <param name="value">The vector to normalize.</param>
	/// <returns>The normalized vector.</returns>
	public static Vector2d Normalize(Vector2d value) => value / value.Length();

	public static Vector2d OnesComplement(Vector2d value) => ~value;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d RadiansToDegrees(Vector2d radians) => Vector128.RadiansToDegrees(radians.AsVector128Unsafe()).AsVector2d();

	/// <summary>Returns the reflection of a vector off a surface that has the specified normal.</summary>
	/// <param name="vector">The source vector.</param>
	/// <param name="normal">The normal of the surface being reflected off.</param>
	/// <returns>The reflected vector.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d Reflect(Vector2d vector, Vector2d normal)
	{
		// This implementation is based on the DirectX Math Library XMVector2Reflect method
		// https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathVector.inl

		var tmp = Create(Dot(vector, normal));
		tmp += tmp;
		return MultiplyAddEstimate(-tmp, normal, vector);
	}

	public static Vector2d Round(Vector2d vector) => Vector128.Round(vector.AsVector128Unsafe()).AsVector2d();

	public static Vector2d Round(Vector2d vector, MidpointRounding mode) => Vector128.Round(vector.AsVector128Unsafe(), mode).AsVector2d();

	// TODO: Shuffle

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d Sin(Vector2d vector) => Vector128.Sin(vector.AsVector128()).AsVector2d();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static (Vector2d Sin, Vector2d Cos) SinCos(Vector2d vector)
	{
		var (sin, cos) = Vector128.SinCos(vector.AsVector128());
		return (sin.AsVector2d(), cos.AsVector2d());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2d SquareRoot(Vector2d value) => Vector128.Sqrt(value.AsVector128Unsafe()).AsVector2d();

	/// <summary>Subtracts the second vector from the first.</summary>
	/// <param name="left">The first vector.</param>
	/// <param name="right">The second vector.</param>
	/// <returns>The difference vector.</returns>
	public static Vector2d Subtract(Vector2d left, Vector2d right) => left - right;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double Sum(Vector2d value) => Vector128.Sum(value.AsVector128());


	// TODO: Transform
	// TODO: TransformNormal

	public static Vector2d Truncate(Vector2d vector) => Vector128.Truncate(vector.AsVector128Unsafe()).AsVector2d();

	public static Vector2d Xor(Vector2d left, Vector2d right) => left ^ right;

	/// <summary>Copies the elements of the vector to a specified array.</summary>
	/// <param name="array">The destination array.</param>
	/// <remarks><paramref name="array" /> must have at least two elements. The method copies the vector's elements starting at index 0.</remarks>
	/// <exception cref="NullReferenceException"><paramref name="array" /> is <see langword="null" />.</exception>
	/// <exception cref="ArgumentException">The number of elements in the current instance is greater than in the array.</exception>
	/// <exception cref="RankException"><paramref name="array" /> is multidimensional.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly void CopyTo(double[] array)
	{
		// We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons

		if (array.Length < ElementCount)
			throw new ArgumentException("The destination array is not long enough to copy all the elements.", nameof(array));

		Unsafe.WriteUnaligned(ref Unsafe.As<double, byte>(ref array[0]), this);
	}

	/// <summary>Copies the elements of the vector to a specified array starting at a specified index position.</summary>
	/// <param name="array">The destination array.</param>
	/// <param name="index">The index at which to copy the first element of the vector.</param>
	/// <remarks><paramref name="array" /> must have a sufficient number of elements to accommodate the two vector elements. In other words, elements <paramref name="index" /> and <paramref name="index" /> + 1 must already exist in <paramref name="array" />.</remarks>
	/// <exception cref="NullReferenceException"><paramref name="array" /> is <see langword="null" />.</exception>
	/// <exception cref="ArgumentException">The number of elements in the current instance is greater than in the array.</exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is less than zero.
	/// -or-
	/// <paramref name="index" /> is greater than or equal to the array length.</exception>
	/// <exception cref="RankException"><paramref name="array" /> is multidimensional.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly void CopyTo(double[] array, int index)
	{
		// We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons

		if ((uint)index >= (uint)array.Length)
			throw new ArgumentOutOfRangeException(nameof(index), "Index must be less than the length of the array.");

		if ((array.Length - index) < ElementCount)
			throw new ArgumentOutOfRangeException(nameof(array), "The destination array is not long enough to copy all the elements.");

		Unsafe.WriteUnaligned(ref Unsafe.As<double, byte>(ref array[index]), this);
	}

	/// <summary>Copies the vector to the given <see cref="Span{T}" />.The length of the destination span must be at least 2.</summary>
	/// <param name="destination">The destination span which the values are copied into.</param>
	/// <exception cref="ArgumentException">If number of elements in source vector is greater than those available in destination span.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly void CopyTo(Span<double> destination)
	{
		if (destination.Length < ElementCount)
			throw new ArgumentOutOfRangeException(nameof(destination));

		Unsafe.WriteUnaligned(ref Unsafe.As<double, byte>(ref MemoryMarshal.GetReference(destination)), this);
	}

	/// <summary>Attempts to copy the vector to the given <see cref="Span{Single}" />. The length of the destination span must be at least 2.</summary>
	/// <param name="destination">The destination span which the values are copied into.</param>
	/// <returns><see langword="true" /> if the source vector was successfully copied to <paramref name="destination" />. <see langword="false" /> if <paramref name="destination" /> is not large enough to hold the source vector.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool TryCopyTo(Span<double> destination)
	{
		if (destination.Length < ElementCount)
		{
			return false;
		}

		Unsafe.WriteUnaligned(ref Unsafe.As<double, byte>(ref MemoryMarshal.GetReference(destination)), this);
		return true;
	}

	/// <summary>Returns a value that indicates whether this instance and a specified object are equal.</summary>
	/// <param name="obj">The object to compare with the current instance.</param>
	/// <returns><see langword="true" /> if the current instance and <paramref name="obj" /> are equal; otherwise, <see langword="false" />. If <paramref name="obj" /> is <see langword="null" />, the method returns <see langword="false" />.</returns>
	/// <remarks>The current instance and <paramref name="obj" /> are equal if <paramref name="obj" /> is a <see cref="Vector2d" /> object and their <see cref="X" /> and <see cref="Y" /> elements are equal.</remarks>
	public readonly override bool Equals([NotNullWhen(true)] object? obj) => (obj is Vector2d other) && Equals(other);

	/// <summary>Returns a value that indicates whether this instance and another vector are equal.</summary>
	/// <param name="other">The other vector.</param>
	/// <returns><see langword="true" /> if the two vectors are equal; otherwise, <see langword="false" />.</returns>
	/// <remarks>Two vectors are equal if their <see cref="X" /> and <see cref="Y" /> elements are equal.</remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Equals(Vector2d other) => AsVector128().Equals(other.AsVector128());

	/// <summary>Returns the hash code for this instance.</summary>
	/// <returns>The hash code.</returns>
	public override int GetHashCode() => HashCode.Combine(X, Y);

	/// <summary>Returns the length of the vector.</summary>
	/// <returns>The vector's length.</returns>
	/// <altmember cref="LengthSquared" />
	public readonly double Length() => double.Sqrt(LengthSquared());

	/// <summary>Returns the length of the vector squared.</summary>
	/// <returns>The vector's length squared.</returns>
	/// <remarks>This operation offers better performance than a call to the <see cref="Length" /> method.</remarks>
	/// <altmember cref="Length" />
	public readonly double LengthSquared() => Dot(this, this);


	/// <summary>Returns the string representation of the current instance using default formatting.</summary>
	/// <returns>The string representation of the current instance.</returns>
	/// <remarks>This method returns a string in which each element of the vector is formatted using the "G" (general) format string and the formatting conventions of the current thread culture. The "&lt;" and "&gt;" characters are used to begin and end the string, and the current culture's <see cref="NumberFormatInfo.NumberGroupSeparator" /> property followed by a space is used to separate each element.</remarks>
	public readonly override string ToString() => ToString("G", CultureInfo.CurrentCulture);

	/// <summary>Returns the string representation of the current instance using the specified format string to format individual elements.</summary>
	/// <param name="format">A standard or custom numeric format string that defines the format of individual elements.</param>
	/// <returns>The string representation of the current instance.</returns>
	/// <remarks>This method returns a string in which each element of the vector is formatted using <paramref name="format" /> and the current culture's formatting conventions. The "&lt;" and "&gt;" characters are used to begin and end the string, and the current culture's <see cref="NumberFormatInfo.NumberGroupSeparator" /> property followed by a space is used to separate each element.</remarks>
	/// <related type="Article" href="/dotnet/standard/base-types/standard-numeric-format-strings">Standard Numeric Format Strings</related>
	/// <related type="Article" href="/dotnet/standard/base-types/custom-numeric-format-strings">Custom Numeric Format Strings</related>
	public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format) => ToString(format, CultureInfo.CurrentCulture);

	/// <summary>Returns the string representation of the current instance using the specified format string to format individual elements and the specified format provider to define culture-specific formatting.</summary>
	/// <param name="format">A standard or custom numeric format string that defines the format of individual elements.</param>
	/// <param name="formatProvider">A format provider that supplies culture-specific formatting information.</param>
	/// <returns>The string representation of the current instance.</returns>
	/// <remarks>This method returns a string in which each element of the vector is formatted using <paramref name="format" /> and <paramref name="formatProvider" />. The "&lt;" and "&gt;" characters are used to begin and end the string, and the format provider's <see cref="NumberFormatInfo.NumberGroupSeparator" /> property followed by a space is used to separate each element.</remarks>
	/// <related type="Article" href="/dotnet/standard/base-types/custom-numeric-format-strings">Custom Numeric Format Strings</related>
	/// <related type="Article" href="/dotnet/standard/base-types/standard-numeric-format-strings">Standard Numeric Format Strings</related>
	public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? formatProvider)
	{
		var separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
		return $"<{X.ToString(format, formatProvider)}{separator} {Y.ToString(format, formatProvider)}>";
	}

	// --- These are extension methods originally ---

	public readonly Vector128<double> AsVector128() => Vector128.Create(X, Y);

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

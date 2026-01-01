using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

namespace FontThing;

internal readonly struct F16Dot16 : IComparable, IComparable<F16Dot16>, IMinMaxValue<F16Dot16>, ISignedNumber<F16Dot16>
{
	private const byte _shiftCount = 16;

	private readonly uint _rawValue;

	private F16Dot16(uint rawValue) => _rawValue = rawValue;

	public int CompareTo(F16Dot16 other) => _rawValue.CompareTo(other._rawValue);
	public int CompareTo(object? obj) => obj is F16Dot16 other ? CompareTo(other) : _rawValue.CompareTo(obj);

	public bool Equals(F16Dot16 other) => other._rawValue == _rawValue;

	// FIXME
	public string ToString(string? format, IFormatProvider? formatProvider) => ((double)this).ToString(formatProvider);

	// FIXME
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
		((double)this).TryFormat(destination, out charsWritten, format, provider);

	public override bool Equals(object? obj) => obj is F16Dot16 other && Equals(other);

	public override int GetHashCode() => (int)_rawValue;

	public static F16Dot16 Zero { get; } = (F16Dot16)0;

	public static F16Dot16 One { get; } = (F16Dot16)1;

	public static F16Dot16 NegativeOne { get; } = (F16Dot16)(-1);

	public static F16Dot16 AdditiveIdentity => Zero;

	public static F16Dot16 MultiplicativeIdentity => One;

	public static int Radix => 2;

	public static F16Dot16 FromRaw(uint rawValue) => new(rawValue);

	public static explicit operator F16Dot16(int value) => FromRaw((uint)(value << _shiftCount));

	public static explicit operator F16Dot16(double value) => FromRaw((uint)(int)(value * One._rawValue));

	public static explicit operator double(F16Dot16 value) => (int)value._rawValue / (double)One._rawValue;

	public static bool IsZero(F16Dot16 value) => value == Zero;
	public static bool IsFinite(F16Dot16 value) => true;
	public static bool IsInfinity(F16Dot16 value) => false;
	public static bool IsPositiveInfinity(F16Dot16 value) => false;
	public static bool IsNegativeInfinity(F16Dot16 value) => false;
	public static bool IsRealNumber(F16Dot16 value) => true;
	public static bool IsImaginaryNumber(F16Dot16 value) => false;
	public static bool IsComplexNumber(F16Dot16 value) => false;
	public static bool IsNaN(F16Dot16 value) => false;
	public static bool IsNormal(F16Dot16 value) => false;
	public static bool IsSubnormal(F16Dot16 value) => value != Zero;
	public static bool IsCanonical(F16Dot16 value) => true;
	public static bool IsInteger(F16Dot16 value) => (value._rawValue & 0xFFFF) == 0;
	public static bool IsEvenInteger(F16Dot16 value) => IsInteger(value) && short.IsEvenInteger((short)value);
	public static bool IsOddInteger(F16Dot16 value) => IsInteger(value) && short.IsEvenInteger((short)value);
	public static bool IsPositive(F16Dot16 value) => ((value._rawValue >> 31) & 1) == 0;
	public static bool IsNegative(F16Dot16 value) => !IsPositive(value);

	// Basic arithmetic operations
	public static F16Dot16 operator +(F16Dot16 left, F16Dot16 right) => FromRaw(left._rawValue + right._rawValue);
	public static F16Dot16 operator -(F16Dot16 left, F16Dot16 right) => FromRaw(left._rawValue - right._rawValue);
	public static F16Dot16 operator *(F16Dot16 left, F16Dot16 right) => FromRaw((left._rawValue * right._rawValue) >> _shiftCount);
	public static F16Dot16 operator /(F16Dot16 left, F16Dot16 right) => FromRaw((left._rawValue << _shiftCount) / right._rawValue);
	public static bool operator >(F16Dot16 left, F16Dot16 right) => left._rawValue > right._rawValue;
	public static bool operator <(F16Dot16 left, F16Dot16 right) => left._rawValue < right._rawValue;
	public static bool operator >=(F16Dot16 left, F16Dot16 right) => left._rawValue >= right._rawValue;
	public static bool operator <=(F16Dot16 left, F16Dot16 right) => left._rawValue <= right._rawValue;

	// Increment/decrement
	public static F16Dot16 operator ++(F16Dot16 value) => value + One;
	public static F16Dot16 operator --(F16Dot16 value) => value - One;

	// Unary plus/minus
	public static F16Dot16 operator +(F16Dot16 value) => value;
	public static F16Dot16 operator -(F16Dot16 value) => value * NegativeOne;

	// Equality/inequality
	public static bool operator ==(F16Dot16 left, F16Dot16 right) => left._rawValue == right._rawValue;
	public static bool operator !=(F16Dot16 left, F16Dot16 right) => left._rawValue != right._rawValue;

	public static F16Dot16 MaxValue { get; } = FromRaw(0x7FFF_FFFF);

	public static F16Dot16 MinValue { get; } = FromRaw(0x8000_0000);

	public static F16Dot16 Abs(F16Dot16 value) => IsNegative(value) ? -value : value;

	public static F16Dot16 MaxMagnitude(F16Dot16 x, F16Dot16 y)
	{
		var absX = Abs(x);
		var absY = Abs(y);
		return absX > absY ? absX : absY;
	}

	public static F16Dot16 MinMagnitude(F16Dot16 x, F16Dot16 y)
	{
		var absX = Abs(x);
		var absY = Abs(y);
		return absX < absY ? absX : absY;
	}

	public static F16Dot16 MaxMagnitudeNumber(F16Dot16 x, F16Dot16 y)
	{
		var absX = Abs(x);
		var absY = Abs(y);
		return absX > absY ? x : y;
	}

	public static F16Dot16 MinMagnitudeNumber(F16Dot16 x, F16Dot16 y)
	{
		var absX = Abs(x);
		var absY = Abs(y);
		return absX < absY ? x : y;
	}

	public static F16Dot16 Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => throw new NotImplementedException();
	public static F16Dot16 Parse(string s, NumberStyles style, IFormatProvider? provider) => throw new NotImplementedException();
	public static F16Dot16 Parse(string s, IFormatProvider? provider) => throw new NotImplementedException();
	public static F16Dot16 Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => throw new NotImplementedException();

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out F16Dot16 result) => throw new NotImplementedException();
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out F16Dot16 result) => throw new NotImplementedException();
	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out F16Dot16 result) => throw new NotImplementedException();
	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out F16Dot16 result) => throw new NotImplementedException();

	public static bool TryConvertFromChecked<TOther>(TOther value, out F16Dot16 result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
	public static bool TryConvertToChecked<TOther>(F16Dot16 value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
	public static bool TryConvertFromSaturating<TOther>(TOther value, out F16Dot16 result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
	public static bool TryConvertToSaturating<TOther>(F16Dot16 value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
	public static bool TryConvertFromTruncating<TOther>(TOther value, out F16Dot16 result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
	public static bool TryConvertToTruncating<TOther>(F16Dot16 value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
}

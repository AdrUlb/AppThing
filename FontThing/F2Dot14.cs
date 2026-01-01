using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

namespace FontThing;

public readonly struct F2Dot14 : IComparable, IComparable<F2Dot14>, IMinMaxValue<F2Dot14>, ISignedNumber<F2Dot14>
{
	private const byte _shiftCount = 14;

	private readonly ushort _rawValue;

	private F2Dot14(ushort rawValue) => _rawValue = rawValue;

	public int CompareTo(F2Dot14 other) => _rawValue.CompareTo(other._rawValue);
	public int CompareTo(object? obj) => obj is F2Dot14 other ? CompareTo(other) : _rawValue.CompareTo(obj);

	public bool Equals(F2Dot14 other) => other._rawValue == _rawValue;

	// FIXME
	public string ToString(string? format, IFormatProvider? formatProvider) => ((double)this).ToString(formatProvider);

	// FIXME
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
		((double)this).TryFormat(destination, out charsWritten, format, provider);

	public override bool Equals(object? obj) => obj is F2Dot14 other && Equals(other);

	public override int GetHashCode() => (int)_rawValue;

	public static F2Dot14 Zero { get; } = (F2Dot14)0;

	public static F2Dot14 One { get; } = (F2Dot14)1;

	public static F2Dot14 NegativeOne { get; } = (F2Dot14)(-1);

	public static F2Dot14 AdditiveIdentity => Zero;

	public static F2Dot14 MultiplicativeIdentity => One;

	public static int Radix => 2;

	public static F2Dot14 FromRaw(ushort rawValue) => new(rawValue);

	public static explicit operator F2Dot14(int value) => FromRaw((ushort)(value << _shiftCount));

	public static explicit operator F2Dot14(double value) => FromRaw((ushort)(short)(value * One._rawValue));

	public static explicit operator double(F2Dot14 value) => (short)value._rawValue / (double)One._rawValue;

	public static bool IsZero(F2Dot14 value) => value == Zero;
	public static bool IsFinite(F2Dot14 value) => true;
	public static bool IsInfinity(F2Dot14 value) => false;
	public static bool IsPositiveInfinity(F2Dot14 value) => false;
	public static bool IsNegativeInfinity(F2Dot14 value) => false;
	public static bool IsRealNumber(F2Dot14 value) => true;
	public static bool IsImaginaryNumber(F2Dot14 value) => false;
	public static bool IsComplexNumber(F2Dot14 value) => false;
	public static bool IsNaN(F2Dot14 value) => false;
	public static bool IsNormal(F2Dot14 value) => false;
	public static bool IsSubnormal(F2Dot14 value) => value != Zero;
	public static bool IsCanonical(F2Dot14 value) => true;
	public static bool IsInteger(F2Dot14 value) => (value._rawValue & 0xFFFF) == 0;
	public static bool IsEvenInteger(F2Dot14 value) => IsInteger(value) && short.IsEvenInteger((short)value);
	public static bool IsOddInteger(F2Dot14 value) => IsInteger(value) && short.IsEvenInteger((short)value);
	public static bool IsPositive(F2Dot14 value) => ((value._rawValue >> 15) & 1) == 0;
	public static bool IsNegative(F2Dot14 value) => !IsPositive(value);

	// Basic arithmetic operations
	public static F2Dot14 operator +(F2Dot14 left, F2Dot14 right) => FromRaw((ushort)(left._rawValue + right._rawValue));
	public static F2Dot14 operator -(F2Dot14 left, F2Dot14 right) => FromRaw((ushort)(left._rawValue - right._rawValue));
	public static F2Dot14 operator *(F2Dot14 left, F2Dot14 right) => FromRaw((ushort)((left._rawValue * right._rawValue) >> _shiftCount));
	public static F2Dot14 operator /(F2Dot14 left, F2Dot14 right) => FromRaw((ushort)((left._rawValue << _shiftCount) / right._rawValue));
	public static bool operator >(F2Dot14 left, F2Dot14 right) => left._rawValue > right._rawValue;
	public static bool operator <(F2Dot14 left, F2Dot14 right) => left._rawValue < right._rawValue;
	public static bool operator >=(F2Dot14 left, F2Dot14 right) => left._rawValue >= right._rawValue;
	public static bool operator <=(F2Dot14 left, F2Dot14 right) => left._rawValue <= right._rawValue;

	// Increment/decrement
	public static F2Dot14 operator ++(F2Dot14 value) => value + One;
	public static F2Dot14 operator --(F2Dot14 value) => value - One;

	// Unary plus/minus
	public static F2Dot14 operator +(F2Dot14 value) => value;
	public static F2Dot14 operator -(F2Dot14 value) => value * NegativeOne;

	// Equality/inequality
	public static bool operator ==(F2Dot14 left, F2Dot14 right) => left._rawValue == right._rawValue;
	public static bool operator !=(F2Dot14 left, F2Dot14 right) => left._rawValue != right._rawValue;

	public static F2Dot14 MaxValue { get; } = FromRaw(0x7FFF);

	public static F2Dot14 MinValue { get; } = FromRaw(0b10 << _shiftCount);

	public static F2Dot14 Abs(F2Dot14 value) => IsNegative(value) ? -value : value;

	public static F2Dot14 MaxMagnitude(F2Dot14 x, F2Dot14 y)
	{
		var absX = Abs(x);
		var absY = Abs(y);
		return absX > absY ? absX : absY;
	}

	public static F2Dot14 MinMagnitude(F2Dot14 x, F2Dot14 y)
	{
		var absX = Abs(x);
		var absY = Abs(y);
		return absX < absY ? absX : absY;
	}

	public static F2Dot14 MaxMagnitudeNumber(F2Dot14 x, F2Dot14 y)
	{
		var absX = Abs(x);
		var absY = Abs(y);
		return absX > absY ? x : y;
	}

	public static F2Dot14 MinMagnitudeNumber(F2Dot14 x, F2Dot14 y)
	{
		var absX = Abs(x);
		var absY = Abs(y);
		return absX < absY ? x : y;
	}

	public static F2Dot14 Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => throw new NotImplementedException();
	public static F2Dot14 Parse(string s, NumberStyles style, IFormatProvider? provider) => throw new NotImplementedException();
	public static F2Dot14 Parse(string s, IFormatProvider? provider) => throw new NotImplementedException();
	public static F2Dot14 Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => throw new NotImplementedException();

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out F2Dot14 result) => throw new NotImplementedException();
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out F2Dot14 result) => throw new NotImplementedException();
	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out F2Dot14 result) => throw new NotImplementedException();
	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out F2Dot14 result) => throw new NotImplementedException();

	public static bool TryConvertFromChecked<TOther>(TOther value, out F2Dot14 result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
	public static bool TryConvertToChecked<TOther>(F2Dot14 value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
	public static bool TryConvertFromSaturating<TOther>(TOther value, out F2Dot14 result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
	public static bool TryConvertToSaturating<TOther>(F2Dot14 value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
	public static bool TryConvertFromTruncating<TOther>(TOther value, out F2Dot14 result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
	public static bool TryConvertToTruncating<TOther>(F2Dot14 value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
}

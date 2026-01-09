using System.Buffers.Binary;
using UtilThing;

namespace FontThing.Parsing.Tables;

// https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6head.html

[Flags]
internal enum HeadFlags : ushort
{
	BaselineAtY0 = 1 << 0,
	LeftSidebearingAtX0 = 1 << 1,
	ScaledPointSizeAndActualPointSizeDiffer = 1 << 2,
	UseIntegerScaling = 1 << 3,
	InstructionsMayAlterAdvanceWidth = 1 << 4, // I believe this is an OpenType extension
	VerticalBaselineAtX0 = 1 << 5,
	AlwaysZero = 1 << 6,
	RequiresLayout = 1 << 7,
	AppleCrap = 1 << 8,
	ContainsStrongRightToLeftGlyphs = 1 << 9,
	ContainsIndicTypeRearrangement = 1 << 10,
	DefinedByAdobe1 = 1 << 11,
	DefinedByAdobe2 = 1 << 12,
	DefinedByAdobe3 = 1 << 13,
	GenericSymbolsForCodepointRanges = 1 << 14,
}

[Flags]
internal enum MacStyleFlags
{
	Bold = 1 << 0,
	Italic = 1 << 1,
	Underline = 1 << 2,
	Outline = 1 << 3,
	Shadow = 1 << 4,
	Condensed = 1 << 5,
	Extended = 1 << 6,
}

internal enum FontDirectionHint : short
{
	MixedDirectional = 0,
	StronglyLeftToRight = 1,
	StronglyLeftToRightAndNeutrals = 2,
	StronglyRightToLeft = -1,
	StronglyRightToLeftAndNeutrals = -2
}

internal enum GlyphDataFormat : short
{
	Current = 0
}

internal sealed class HeadTable
{
	public static readonly uint TagValue = BinaryPrimitives.ReadUInt32BigEndian("head"u8);

	public readonly HeadFlags HeadFlags;

	// Range from 64 to 16384
	public readonly ushort UnitsPerEm;

	// International date
	public readonly DateTime Created;
	public readonly DateTime Modified;

	// For all glyph bounding boxes
	public readonly short XMin;
	public readonly short YMin;
	public readonly short XMax;
	public readonly short YMax;

	public readonly MacStyleFlags MacStyleFlags;

	// Smallest readable size in pixels
	public readonly ushort LowestRecPpem;

	public readonly FontDirectionHint FontDirectionHint;
	public readonly IndexToLocFormat IndexToLocFormat;
	public readonly GlyphDataFormat GlyphDataFormat;

	public HeadTable(StreamPrimitiveReader reader)
	{
		var version = F16Dot16.FromRaw(reader.ReadU32());
		if (version != F16Dot16.One)
			throw new NotSupportedException("Only head tables with version 1 are supported.");

		reader.ReadU32(); // fontRevision
		reader.ReadU32(); // checkSumAdjust

		var magicNumber = reader.ReadU32();

		if (magicNumber != 0x5F0F3CF5)
			throw new("Invalid magic numver in 'head' table.");

		HeadFlags = (HeadFlags)reader.ReadU16();
		UnitsPerEm = reader.ReadU16();

		var longDateTimeEpoch = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		Created = longDateTimeEpoch.AddSeconds(reader.ReadI64());
		Modified = longDateTimeEpoch.AddSeconds(reader.ReadI64());

		XMin = reader.ReadI16();
		YMin = reader.ReadI16();
		XMax = reader.ReadI16();
		YMax = reader.ReadI16();

		MacStyleFlags = (MacStyleFlags)reader.ReadU16();
		LowestRecPpem = reader.ReadU16();
		FontDirectionHint = (FontDirectionHint)reader.ReadI16();
		IndexToLocFormat = (IndexToLocFormat)reader.ReadI16();
		GlyphDataFormat = (GlyphDataFormat)reader.ReadI16();
	}
}

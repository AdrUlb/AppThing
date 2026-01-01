using UtilThing;

namespace FontThing.TrueType.Parsing;

internal readonly struct OffsetSubtable(StreamPrimitiveReader reader)
{
	// A tag to indicate the OFA scaler to be used to rasterize this font
	public readonly uint ScalerType = reader.ReadU32();

	// Number of tables
	public readonly ushort NumTables = reader.ReadU16();

	// (maximum power of 2 <= numTables)*16
	public readonly ushort SearchRange = reader.ReadU16();

	// log2(maximum power of 2 <= numTables)
	public readonly ushort EntrySelector = reader.ReadU16();

	// numTables*16-searchRange
	public readonly ushort RangeShift = reader.ReadU16();
}

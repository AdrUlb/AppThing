using System.Buffers.Binary;
using UtilThing;

namespace FontThing.Parsing.Tables;

internal sealed class MaxpTable
{
	public static readonly uint TagValue = BinaryPrimitives.ReadUInt32BigEndian("maxp"u8);

	public readonly ushort NumGlyphs;
	public readonly ushort MaxPoints;
	public readonly ushort MaxContours;
	public readonly ushort MaxComponentPoints;
	public readonly ushort MaxComponentContours;
	public readonly ushort MaxZones;
	public readonly ushort MaxTwilightPoints;
	public readonly ushort MaxStorage;
	public readonly ushort MaxFunctionDefs;
	public readonly ushort MaxInstructionDefs;
	public readonly ushort MaxStackElements;
	public readonly ushort MaxSizeOfInstructions;
	public readonly ushort MaxComponentElements;
	public readonly ushort MaxComponentDepth;

	public MaxpTable(StreamPrimitiveReader reader)
	{
		// Fonts with PostScript outlines (that is, OpenType fonts with 'CFF ' tables) use a six-byte version of the 'maxp' table

		var version = F16Dot16.FromRaw(reader.ReadU32());
		if (version != F16Dot16.One)
			throw new NotSupportedException("Only 'maxp' tables with version 1 are supported.");

		NumGlyphs = reader.ReadU16();
		MaxPoints = reader.ReadU16();
		MaxContours = reader.ReadU16();
		MaxComponentPoints = reader.ReadU16();
		MaxComponentContours = reader.ReadU16();
		MaxZones = reader.ReadU16();
		MaxTwilightPoints = reader.ReadU16();
		MaxStorage = reader.ReadU16();
		MaxFunctionDefs = reader.ReadU16();
		MaxInstructionDefs = reader.ReadU16();
		MaxStackElements = reader.ReadU16();
		MaxSizeOfInstructions = reader.ReadU16();
		MaxComponentElements = reader.ReadU16();
		MaxComponentDepth = reader.ReadU16();
	}
}

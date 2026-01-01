using System.Buffers.Binary;
using UtilThing;

namespace FontThing.TrueType.Parsing.Tables;

internal sealed class HheaTable
{
	public static readonly uint TagValue = BinaryPrimitives.ReadUInt32BigEndian("hhea"u8);

	public readonly short Ascent;
	public readonly short Descent;
	public readonly short LineGap;
	public readonly ushort AdvanceWidthMax;
	public readonly short MinLeftSideBearing;
	public readonly short MinRightSideBearing;
	public readonly short XMaxExtent;
	public readonly short CaretSlopeRise;
	public readonly short CaretSlopeRun;
	public readonly short CaretOffset;
	public readonly ushort NumOfLongHorMetrics;

	public HheaTable(StreamPrimitiveReader reader)
	{
		var version = F16Dot16.FromRaw(reader.ReadU32());
		if (version != F16Dot16.One)
			throw new NotSupportedException("Only head tables with version 1 are supported.");

		Ascent = reader.ReadI16();
		Descent = reader.ReadI16();
		LineGap = reader.ReadI16();
		AdvanceWidthMax = reader.ReadU16();
		MinLeftSideBearing = reader.ReadI16();
		MinRightSideBearing = reader.ReadI16();
		XMaxExtent = reader.ReadI16();
		CaretSlopeRise = reader.ReadI16();
		CaretSlopeRun = reader.ReadI16();
		CaretOffset = reader.ReadI16();

		// Reserved, all 0
		reader.ReadI16();
		reader.ReadI16();
		reader.ReadI16();
		reader.ReadI16();

		var metricDataFormat = reader.ReadI16();
		if (metricDataFormat != 0)
			throw new NotSupportedException("Only metric data format 0 is supported.");
		
		NumOfLongHorMetrics = reader.ReadU16();
	}
}

using System.Buffers.Binary;
using UtilThing;

namespace FontThing.TrueType.Parsing.Tables;

internal sealed class HmtxTable
{
	public static readonly uint TagValue = BinaryPrimitives.ReadUInt32BigEndian("hmtx"u8);

	private readonly LongHorMetric[] _longHorMetrics;
	private readonly short[] _leftSideBearings;

	public HmtxTable(StreamPrimitiveReader reader, MaxpTable maxpTable, HheaTable hheaTable)
	{
		_longHorMetrics = new LongHorMetric[hheaTable.NumOfLongHorMetrics];
		for (var i = 0; i < _longHorMetrics.Length; i++)
		{
			var advanceWidth = reader.ReadU16();
			var leftSideBearing = reader.ReadI16();
			_longHorMetrics[i] = new(advanceWidth, leftSideBearing);
		}

		// If numberOfHMetrics is less than the total number of glyphs, then the hMetrics array is followed by an array for the left side bearing values of the remaining glyphs.
		// The number of elements in the leftSideBearings array will be derived from the numGlyphs field in the 'maxp' table minus numberOfHMetrics.
		_leftSideBearings = new short[maxpTable.NumGlyphs - _longHorMetrics.Length];
		for (var i = 0; i < _leftSideBearings.Length; i++)
			_leftSideBearings[i] = reader.ReadI16();
	}

	public LongHorMetric GetLongHorMetric(uint glyphIndex)
	{
		if (glyphIndex < _longHorMetrics.Length)
			return _longHorMetrics[glyphIndex];

		// As an optimization, the number of records can be less than the number of glyphs,
		// in which case the advance width value of the last record applies to all remaining glyph IDs
		var advanceWidth = _longHorMetrics[^1].AdvanceWidth;
		var leftSideBearing = _leftSideBearings[glyphIndex - _longHorMetrics.Length];

		return new(advanceWidth, leftSideBearing);
	}
}

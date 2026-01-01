using FontThing.TrueType.Parsing.Tables;
using System.Drawing;
using System.Text;
using UtilThing;

namespace FontThing.TrueType.Parsing;

// https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6.html

public sealed class TrueTypeFont
{
	private readonly HeadTable _headTable;
	private readonly LocaTable _locaTable;
	private readonly CmapTable _cmapTable;
	private readonly GlyfTable _glyfTable;
	private readonly HmtxTable _hmtxTable;
	private readonly HheaTable _hheaTable;

	public short Ascent => _hheaTable.Ascent;
	public short Descent => _hheaTable.Descent;
	public short LineGap => _hheaTable.LineGap;

	public TrueTypeFont(Stream stream)
	{
		using var reader = new StreamPrimitiveReader(stream.SubStream(stream.Position, stream.Length - stream.Position), false);

		// The first table is the font directory, it contains the offset subtable and the table directory
		var fontDirectory = new FontDirectory(reader);

		// 0x74727565 ('true') and 0x00010000 are TrueType fonts (at least to OSX and iOS, good enough for me) NOTE: 'true' is APPLE-ONLY
		// 0x74797031 ('typ1') are old style PostScript
		// 0x4F54544F ('OTTO') are OpenType with PostScript outlines ('CFF ' table in 'glyf' table)
		if (fontDirectory.OffsetSubtable.ScalerType is not (0x74727565 or 0x00010000))
			throw new NotSupportedException($"Font scaler type 0x{fontDirectory.OffsetSubtable.ScalerType}:X8}} not supported.");

		var headDir = GetTableDirectoryByTagValue(fontDirectory, HeadTable.TagValue);
		var maxpDir = GetTableDirectoryByTagValue(fontDirectory, MaxpTable.TagValue);
		var hheaDir = GetTableDirectoryByTagValue(fontDirectory, HheaTable.TagValue);

		var cmapDir = GetTableDirectoryByTagValue(fontDirectory, CmapTable.TagValue);
		var locaDir = GetTableDirectoryByTagValue(fontDirectory, LocaTable.TagValue);
		var glyfDir = GetTableDirectoryByTagValue(fontDirectory, GlyfTable.TagValue);
		var hmtxDir = GetTableDirectoryByTagValue(fontDirectory, HmtxTable.TagValue);

		// Note: checksum for 'head' table is always "wrong" (handling this is a little more involved and unnecessary)
		_headTable = new HeadTable(CreateTableReader(reader, headDir, false));
		var maxpTable = new MaxpTable(CreateTableReader(reader, maxpDir));
		_hheaTable = new HheaTable(CreateTableReader(reader, hheaDir));

		_cmapTable = new(CreateTableReader(reader, cmapDir));
		_locaTable = new(CreateTableReader(reader, locaDir), _headTable);
		_glyfTable = new(CreateTableReader(reader, glyfDir), this, _locaTable);
		_hmtxTable = new(CreateTableReader(reader, hmtxDir), maxpTable, _hheaTable);

		// TODO: name: naming
		// TODO: post: postscript
	}

	public float PointSizeToScale(float pointSize) => pointSize * 96.0f / (72.0f * _headTable.UnitsPerEm);

	public float GetPixelsPerEm(float pointSize) => pointSize * 96.0f / 72.0f;

	internal GlyphOutline? GetGlyphOutlineFromCharacter(uint c)
	{
		var glyphIndex = _cmapTable.GetGlyphIndex(c);

		var glyphOffset = _locaTable.Offsets[glyphIndex];

		var nextGlyphIndex = glyphIndex + 1;
		if (nextGlyphIndex < _locaTable.Offsets.Length)
		{
			var glyphOffsetNext = _locaTable.Offsets[glyphIndex + 1];
			if (glyphOffset == glyphOffsetNext)
				return null;
		}

		var glyph = _glyfTable.OutlinesByLocation[glyphOffset];
		return glyph;
	}
	
	internal GlyphOutline? GetGlyphOutlineFromIndex(uint glyphIndex)
	{
		var glyphOffset = _locaTable.Offsets[glyphIndex];

		var nextGlyphIndex = glyphIndex + 1;
		if (nextGlyphIndex < _locaTable.Offsets.Length)
		{
			var glyphOffsetNext = _locaTable.Offsets[glyphIndex + 1];
			if (glyphOffset == glyphOffsetNext)
				return null;
		}

		var glyph = _glyfTable.OutlinesByLocation[glyphOffset];
		return glyph;
	}

	internal LongHorMetric GetLongHorMetrics(uint c)
	{
		var glyphIndex = _cmapTable.GetGlyphIndex(c);
		return _hmtxTable.GetLongHorMetric(glyphIndex);
	}

	public Glyph LoadGlyph(char character)
		=> LoadGlyph(new Rune(character));

	public Glyph LoadGlyph(Rune rune)
	{
		return new(this, rune, GetGlyphOutlineFromCharacter((uint)rune.Value));
	}

	private static TableDirectory GetTableDirectoryByTagValue(FontDirectory fontDirectory, uint tagValue)
	{
		if (!fontDirectory.TableDirectoriesByTagValue.TryGetValue(tagValue, out var dir))
			throw new($"Table directory for '{tagValue}' not found.");

		return dir;
	}

	private static StreamPrimitiveReader CreateTableReader(StreamPrimitiveReader fontReader, TableDirectory tableDir, bool validateChecksum = true)
	{
		var paddedLength = (tableDir.Length + 3) / 4 * 4;
		var tableReader = new StreamPrimitiveReader(fontReader.BaseStream.SubStream(tableDir.Offset, paddedLength), fontReader.LittleEndian);

		// Reset read position
		tableReader.BaseStream.Position = 0;

		// Validate checksum
		if (validateChecksum && CalculateTableChecksum(tableReader) != tableDir.Checksum)
			throw new($"Table checksum does not match!");

		// Reset read position
		tableReader.BaseStream.Position = 0;
		tableReader.BaseStream.SetLength(tableDir.Length);

		return tableReader;
	}

	private static uint CalculateTableChecksum(StreamPrimitiveReader tableReader)
	{
		var sum = 0u;

		for (var i = 0; i < tableReader.BaseStream.Length / 4; i++)
			sum += tableReader.ReadU32();

		return sum;
	}
}

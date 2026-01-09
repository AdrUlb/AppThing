using UtilThing;

namespace FontThing.Parsing.Tables;

internal readonly struct CmapFormat4 : ICmapFormat
{
	private readonly ushort[] _endCode;
	private readonly ushort[] _startCode;
	private readonly short[] _idDelta;
	private readonly ushort[] _idRangeOffset;
	private readonly ushort[] _glyphIndexArray;

	public CmapFormat4(StreamPrimitiveReader reader)
	{
		// Language: Not relevant in modern times (used exlusively for the actively discouraged platform id Macintosh
		reader.ReadU16();

		var segCountX2 = reader.ReadU16();
		var segCount = segCountX2 / 2;

		// Used for binary search, ignored by this implementation
		reader.ReadU16(); // searchRange: (2 * (largest power of 2 <= 39)) = 2 * 32
		reader.ReadU16(); // entrySelector: log2(searchRange/2)
		reader.ReadU16(); // rangeShift: (2 * segCount) - searchRange

		_endCode = new ushort[segCount];
		for (var i = 0; i < segCount; i++)
			_endCode[i] = reader.ReadU16();

		// Reserved padding: should be 0
		reader.ReadU16();

		_startCode = new ushort[segCount];
		for (var i = 0; i < segCount; i++)
			_startCode[i] = reader.ReadU16();

		_idDelta = new short[segCount];
		for (var i = 0; i < segCount; i++)
			_idDelta[i] = reader.ReadI16();

		_idRangeOffset = new ushort[segCount];
		for (var i = 0; i < segCount; i++)
			_idRangeOffset[i] = reader.ReadU16();

		var bytesLeft = reader.BaseStream.Length - reader.BaseStream.Position;
		_glyphIndexArray = new ushort[bytesLeft / sizeof(ushort)];
		for (var i = 0; i < _glyphIndexArray.Length; i++)
			_glyphIndexArray[i] = reader.ReadU16();
	}

	public uint GetGlyphIndex(uint codePoint)
	{
		if (codePoint > ushort.MaxValue)
			return 0;

		// Search for the first endCode that is greater than or equal to the character code to be mapped
		var i = 0;
		for (; i < _endCode.Length; i++)
			if (_endCode[i] >= codePoint)
				break;

		//  If the corresponding startCode is greater than the character code, the missing character glyph is returned.
		var startCode = _startCode[i];
		if (startCode > codePoint)
			return 0;

		var idRangeOffset = _idRangeOffset[i];

		uint glyphIndex;
		// If the idRangeOffset is 0, the idDelta value is added directly to the character code to get the corresponding glyph index.
		if (idRangeOffset == 0)
		{
			glyphIndex = (uint)(_idDelta[i] + codePoint);
		}
		else
		{
			// The TrueType reference manual suggests using this code to determine the glyph index:
			//		glyphIndex = *(&idRangeOffset[i] + idRangeOffset[i] / 2 + (c - startCode[i]))
			// which, of course, can be rewritten as follows:
			//		glyphIndex = *(&idRangeOffset[i + idRangeOffset[i] / 2 + (c - startCode[i])])
			// or even simpler:
			//		glyphIndex = idRangeOffset[i + idRangeOffset[i] / 2 + (c - startCode[i])]
			// and finally:
			//		glyphIndex = glyphIndexArray[i + idRangeOffset[i] / 2 + (c - startCode[i]) - idRangeOffset.Length]
			glyphIndex = _glyphIndexArray[i + idRangeOffset / 2 + (codePoint - startCode) - _idRangeOffset.Length];

			// Once the glyph indexing operation is complete, the glyph ID at the indicated address is checked.
			// If it's not 0 (that is, if it's not the missing glyph), the value is added to idDelta[i] to get the actual glyph ID to use.
			if (glyphIndex != 0)
				glyphIndex = (uint)(glyphIndex + _idDelta[i]);
		}

		// All idDelta[i] arithmetic is modulo 65536 (is this what we're supposed to do?? guess we'll find out lol).
		glyphIndex %= 65536;
		return glyphIndex;
	}
}

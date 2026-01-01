using UtilThing;

namespace FontThing.TrueType.Parsing.Tables;

internal readonly struct CmapFormat12 : ICmapFormat
{
	private readonly record struct Group(uint StartCharCode, uint EndCharCode, uint StartGlyphCode);

	private readonly Group[] _groups;

	public CmapFormat12(StreamPrimitiveReader reader)
	{
		// Language: Not relevant in modern times (used exlusively for the actively discouraged platform id Macintosh
		reader.ReadU32();

		var nGroups = reader.ReadU32();

		_groups = new Group[nGroups];
		for (var i = 0; i < nGroups; i++)
		{
			var startCharCode = reader.ReadU32();
			var endCharCode = reader.ReadU32();
			var startGlyphCode = reader.ReadU32();

			_groups[i] = new(startCharCode, endCharCode, startGlyphCode);
		}
	}

	public uint GetGlyphIndex(uint codePoint)
	{
		var i = 0;
		for (; i < _groups.Length; i++)
			if (_groups[i].EndCharCode >= codePoint)
				break;

		if (i == _groups.Length)
			return 0;

		var group = _groups[i];

		//  If the corresponding startCode is greater than the character code, the missing character glyph is returned.
		if (group.StartCharCode > codePoint)
			return 0;

		return codePoint - group.StartCharCode + group.StartGlyphCode;
	}
}

using System.Collections.Frozen;
using UtilThing;

namespace FontThing.Parsing;

internal readonly struct FontDirectory
{
	public readonly OffsetSubtable OffsetSubtable;
	public readonly TableDirectory[] TableDirectories;
	public readonly FrozenDictionary<uint, TableDirectory> TableDirectoriesByTagValue;

	public FontDirectory(StreamPrimitiveReader reader)
	{
		var tableDirectoriesByTagValue = new Dictionary<uint, TableDirectory>();

		OffsetSubtable = new(reader);
		TableDirectories = new TableDirectory[OffsetSubtable.NumTables];

		for (var i = 0; i < TableDirectories.Length; i++)
		{
			var table = new TableDirectory(reader);
			TableDirectories[i] = table;
			tableDirectoriesByTagValue.Add(table.TagValue, table);
		}

		TableDirectoriesByTagValue = tableDirectoriesByTagValue.ToFrozenDictionary();
	}
}

using System.Buffers.Binary;
using System.Diagnostics;
using UtilThing;

namespace FontThing.TrueType.Parsing.Tables;

// https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6loca.html

internal sealed class LocaTable
{
	public static readonly uint TagValue = BinaryPrimitives.ReadUInt32BigEndian("loca"u8);

	public readonly uint[] Offsets;

	public LocaTable(StreamPrimitiveReader reader, HeadTable headTable)
	{
		var valueSize = headTable.IndexToLocFormat switch {
			IndexToLocFormat.Short => 2,
			IndexToLocFormat.Long => 4,
			_ => throw new NotSupportedException()
		};

		Offsets = new uint[reader.BaseStream.Length / valueSize];

		for (var i = 0; i < Offsets.Length; i++)
		{
			Offsets[i] = valueSize switch {
				2 => (uint)reader.ReadU16() * 2,
				4 => reader.ReadU32(),
				_ => throw new UnreachableException()
			};
		}
	}
}

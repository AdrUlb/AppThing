using System.Buffers.Binary;
using System.Text;
using UtilThing;

namespace FontThing.TrueType.Parsing;

internal sealed class TableDirectory
{
	// 4-byte identifier
	public readonly uint TagValue;

	public readonly string Tag;

	// Checksum for this table
	public readonly uint Checksum;

	// Offset from the beginning of sfnt
	public readonly uint Offset;

	// Length of this table in bytes (actual length not padded length)
	public readonly uint Length;

	public TableDirectory(StreamPrimitiveReader reader)
	{
		TagValue = reader.ReadU32();
		Checksum = reader.ReadU32();
		Offset = reader.ReadU32();
		Length = reader.ReadU32();

		Span<byte> buf4 = stackalloc byte[4];
		BinaryPrimitives.WriteUInt32BigEndian(buf4, TagValue);
		Tag = Encoding.ASCII.GetString(buf4);
	}
}

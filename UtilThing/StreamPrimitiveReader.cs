using System.Buffers.Binary;

namespace UtilThing;

public sealed class StreamPrimitiveReader(Stream stream, bool littleEndian, bool leaveOpen = false) : IDisposable, IAsyncDisposable
{
	public readonly bool LittleEndian = littleEndian;

	public readonly Stream BaseStream = stream;

	private readonly byte[] _buffer = new byte[8];

	public byte ReadU8() => (byte)BaseStream.ReadByte();

	public ushort ReadU16()
	{
		var buf = _buffer.AsSpan()[..2];
		BaseStream.ReadExactly(buf);
		return LittleEndian ? BinaryPrimitives.ReadUInt16LittleEndian(buf) : BinaryPrimitives.ReadUInt16BigEndian(buf);
	}

	public uint ReadU32()
	{
		var buf = _buffer.AsSpan()[..4];
		BaseStream.ReadExactly(buf);
		return LittleEndian ? BinaryPrimitives.ReadUInt32LittleEndian(buf) : BinaryPrimitives.ReadUInt32BigEndian(buf);
	}

	public ulong ReadU64()
	{
		var buf = _buffer.AsSpan()[..8];
		BaseStream.ReadExactly(buf);
		return LittleEndian ? BinaryPrimitives.ReadUInt64LittleEndian(buf) : BinaryPrimitives.ReadUInt64BigEndian(buf);
	}

	public sbyte ReadI8() => (sbyte)ReadU8();
	public short ReadI16() => (short)ReadU16();
	public int ReadI32() => (int)ReadU32();
	public long ReadI64() => (long)ReadU64();
	public void Skip(long count) => BaseStream.Seek(count, SeekOrigin.Current);

	public void Dispose()
	{
		if (!leaveOpen)
			BaseStream.Dispose();
	}

	public async ValueTask DisposeAsync()
	{
		if (!leaveOpen)
			await BaseStream.DisposeAsync();
	}
}

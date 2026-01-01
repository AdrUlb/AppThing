namespace UtilThing;

public class SubStream : Stream
{
	private readonly Stream _stream;
	private readonly long _offset;
	private long _length;

	private long _position = 0;

	private bool _disposed = false;

	public override long Position
	{
		get => _position;
		set
		{
			ArgumentOutOfRangeException.ThrowIfNegative(value);
			ArgumentOutOfRangeException.ThrowIfGreaterThan(value, _length);
			_position = value;
		}
	}

	public override long Length => _length;

	public override bool CanRead => _stream.CanRead;

	public override bool CanWrite { get; }

	public override bool CanSeek => _stream.CanSeek;

	public SubStream(Stream stream, long offset, long length, bool forceReadOnly)
	{
		_stream = stream;
		_offset = offset;
		_length = length;
		CanWrite = stream.CanWrite && !forceReadOnly;

		if (!stream.CanSeek)
			throw new ArgumentException("Stream must be seekable", nameof(stream));

		ArgumentOutOfRangeException.ThrowIfNegative(offset);
		ArgumentOutOfRangeException.ThrowIfNegative(length);
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		switch (origin)
		{
			case SeekOrigin.Begin:
				Position = offset;
				break;
			case SeekOrigin.Current:
				Position += offset;
				break;
			case SeekOrigin.End:
				Position = Length - offset;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
		}

		return Position;
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);

		if (!CanRead)
			throw new NotSupportedException("The stream does not support reading.");

		ArgumentNullException.ThrowIfNull(buffer);
		ArgumentOutOfRangeException.ThrowIfNegative(offset);
		ArgumentOutOfRangeException.ThrowIfNegative(count);

		if (offset + count > buffer.Length)
			throw new ArgumentException("The sum of offset and count is larger than the buffer length.");

		var maxReadCount = _length - _position;
		count = (int)Math.Min(maxReadCount, count);

		_stream.Position = _position + _offset;
		var bytesRead = _stream.Read(buffer, offset, count);
		_position += bytesRead;
		return bytesRead;
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);

		if (!CanWrite)
			throw new NotSupportedException("The stream does not support writing.");

		ArgumentNullException.ThrowIfNull(buffer);
		ArgumentOutOfRangeException.ThrowIfNegative(offset);
		ArgumentOutOfRangeException.ThrowIfNegative(count);

		if (offset + count > buffer.Length)
			throw new ArgumentException("The sum of offset and count is larger than the buffer length.");

		var maxWriteCount = _length - _position;
		count = (int)Math.Min(maxWriteCount, count);

		_stream.Position = _position + _offset;
		_stream.Write(buffer, offset, count);
		_position += count;
	}

	public override void Flush() => _stream.Flush();

	public override void SetLength(long value)
	{
		if (value > _length)
			throw new ArgumentOutOfRangeException(nameof(value), value, "The new length is greater than the current length.");
		
		_length = value;
		if (_position > _length)
			_position = _length;
	}

	~SubStream() => Dispose(false);

	protected override void Dispose(bool disposing)
	{
		if (_disposed)
			return;

		base.Dispose(disposing);

		_disposed = true;
	}
}

namespace UtilThing;

public static class StreamExtensions
{
	public static SubStream SubStream(this Stream stream, long offset, long length, bool forceReadOnly = false) => new(stream, offset, length, forceReadOnly);
}

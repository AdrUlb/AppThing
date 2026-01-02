using System.Drawing;

namespace AppThing;

public readonly ref struct TextureRowAccessor(Size size, Span<Color> pixels, int stride)
{
	public readonly Size Size = size;
	private readonly Span<Color> _pixels = pixels;

	public Span<Color> GetRow(int row)
	{
		if (row < 0 || row >= Size.Height)
			throw new ArgumentOutOfRangeException(nameof(row));

		return _pixels.Slice(row * stride, Size.Width);
	}
}

public enum TextureFormat : byte
{
	Rgba,
	Rgb,
	Red,
}

public sealed class Texture : IDisposable
{
	internal delegate void TextureDisposedHandler(Texture texture);
	internal delegate void TextureChangedHandler(Texture texture, Rectangle region);
	public delegate void AccessHandler(TextureRowAccessor accessor);

	internal event TextureDisposedHandler? Disposed;
	internal event TextureChangedHandler? Changed;

	public readonly Size Size;
	public readonly TextureFormat Format;

	private readonly Color[] _pixels;
	private bool _disposed;

	public Texture(Size size, Color color, TextureFormat format = TextureFormat.Rgba)
	{
		Size = size;
		Format = format;
		_pixels = new Color[size.Width * size.Height];
		_pixels.AsSpan().Fill(color);
	}

	~Texture() => Dispose();

	public void Dispose()
	{
		if (_disposed)
			return;

		Disposed?.Invoke(this);
		GC.SuppressFinalize(this);

		_disposed = true;
	}

	private void Invalidate(Rectangle region) => Changed?.Invoke(this, region);

	public void AccessPixels(AccessHandler handler, bool invalidate = true)
	{
		ArgumentNullException.ThrowIfNull(handler);
		handler(new(Size, _pixels.AsSpan(), Size.Width));
		if (invalidate)
			Invalidate(new(new(0, 0), Size));
	}

	public void AccessPixels(Rectangle rect, AccessHandler handler, bool invalidate = true)
	{
		ArgumentNullException.ThrowIfNull(handler);
		handler(new(rect.Size, _pixels.AsSpan()[(rect.X + rect.Y * Size.Width)..], Size.Width));
		if (invalidate)
			Invalidate(rect);
	}
	
	public Span<Color> UnsafeGetPixelsSpan() => _pixels.AsSpan();
}

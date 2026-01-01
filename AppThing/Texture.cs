using System.Drawing;

namespace AppThing;

public sealed class Texture : IDisposable
{
	internal delegate void TextureEventHandler(Texture texture);
	public delegate void AccessSpanHandler(Span<Color> pixels, Size size);

	internal event TextureEventHandler? Disposed;
	internal event TextureEventHandler? Changed;

	public readonly Size Size;

	private readonly Color[] _pixels;
	private bool _disposed;

	public Texture(Size size, Color color)
	{
		Size = size;
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

	public void Invalidate() => Changed?.Invoke(this);

	public void AccessPixels(AccessSpanHandler handler)
	{
		ArgumentNullException.ThrowIfNull(handler);
		handler(_pixels.AsSpan(), Size);
		Invalidate();
	}
	
	/*
	public Color this[int index]
	{
		get
		{
			if (index < 0 || index >= _pixels.Length)
				throw new ArgumentOutOfRangeException(nameof(index));

			return _pixels[index];
		}

		set
		{
			if (index < 0 || index >= _pixels.Length)
				throw new ArgumentOutOfRangeException(nameof(index));

			_pixels[index] = value;
			Invalidate();
		}
	}

	public Color this[int x, int y]
	{
		get
		{
			if (x < 0 || x >= Size.Width)
				throw new ArgumentOutOfRangeException(nameof(x));

			if (y < 0 || y >= Size.Height)
				throw new ArgumentOutOfRangeException(nameof(y));

			var index = x + (y * Size.Width);
			return _pixels[index];
		}

		set
		{
			if (x < 0 || x >= Size.Width)
				throw new ArgumentOutOfRangeException(nameof(x));

			if (y < 0 || y >= Size.Height)
				throw new ArgumentOutOfRangeException(nameof(y));

			var index = x + (y * Size.Width);
			_pixels[index] = value;
			Invalidate();
		}
	}
	*/
}

using GLCS;
using GLCS.Managed;
using System.Buffers;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace AppThing;

internal sealed class TextureManager : IDisposable
{
	private readonly Dictionary<Texture, GLTexture> _textureMap = [];
	private readonly HashSet<Texture> _disposedTextures = [];
	private readonly Dictionary<Texture, Rectangle> _changedTextures = [];
	private readonly Lock _disposedTexturesLock = new();
	private readonly Lock _changedTexturesLock = new();

	public void Dispose()
	{
		Console.WriteLine($"[TextureManager] Disposing {_textureMap.Count} textures");
		foreach (var texture in _textureMap.Values)
			texture.Dispose();

		_textureMap.Clear();
		_disposedTextures.Clear();
		_changedTextures.Clear();
	}

	public Texture? CurrentTexture { get; private set; } = null;

	public bool Use(Texture texture, bool checkChanges = true)
	{
		// If the texture is already "known", return the corresponding GL texture
		if (_textureMap.TryGetValue(texture, out var glTexture))
		{
			glTexture.Bind();
			if (checkChanges)
			{
				if (HasTextureChanged(texture, out var changedRegion))
				{
					HandleChanged(texture, changedRegion);
					lock (_changedTexturesLock)
						_changedTextures.Remove(texture);
				}
			}

			return false;
		}

		// Subscribe to notifications regarding the texture's state
		texture.Changed += Texture_Changed;
		texture.Disposed += Texture_Disposed;

		// Create a new GL texture
		glTexture = new(TextureTarget.Texture2d)
		{
			MinFilter = TextureMinFilter.Linear,
			MagFilter = TextureMagFilter.Linear
		};

		var internalFormat = texture.Format switch
		{
			TextureFormat.Rgba => InternalFormat.Rgba8,
			TextureFormat.Rgb => InternalFormat.Rgb8,
			TextureFormat.BitmapFont => InternalFormat.Red,
			_ => throw new NotSupportedException($"Unsupported texture format: {texture.Format}")
		};

		glTexture.Image2D(0, internalFormat, texture.Size, 0, PixelFormat.Rgba, PixelType.UnsignedByte);

		// Add the new GL texture to the list of known textures
		_textureMap.Add(texture, glTexture);

		// Also add the texture to the list of changed textures
		lock (_changedTexturesLock)
			_changedTextures.TryAdd(texture, new Rectangle(new Point(0, 0), texture.Size));

		glTexture.Bind();

		HandleChanged(texture, new(new(0, 0), texture.Size));
		lock (_changedTexturesLock)
			_changedTextures.Remove(texture);

		CurrentTexture = texture;
		return true;
	}

	public void EndFrame()
	{
		HandleDisposed();
	}

	public bool HasTextureChanged(Texture texture, out Rectangle changedRegion)
	{
		lock (_changedTexturesLock)
			return _changedTextures.TryGetValue(texture, out changedRegion);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void HandleDisposed()
	{
		lock (_disposedTexturesLock)
		{
			foreach (var texture in _disposedTextures)
			{
				if (!_textureMap.TryGetValue(texture, out var glTexture))
					continue;

				glTexture.Dispose();
				_textureMap.Remove(texture);
				texture.Changed -= Texture_Changed;
				texture.Disposed -= Texture_Disposed;
			}

			_disposedTextures.Clear();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void HandleChanged(Texture texture, Rectangle region)
	{
		if (!_textureMap.TryGetValue(texture, out var glTexture))
			return;

		// Upload new texture data to the GPU
		var pixelsByteCount = region.Width * region.Height * 4;
		var pixels = ArrayPool<byte>.Shared.Rent(pixelsByteCount);
		texture.AccessPixels(region,
			acc =>
			{
				for (var y = 0; y < acc.Size.Height; y++)
				{
					var row = acc.GetRowSpan(y);
					for (var x = 0; x < acc.Size.Width; x++)
					{
						var color = row[x];
						var index = (x + (y * acc.Size.Width)) * 4;
						pixels[index + 0] = color.R;
						pixels[index + 1] = color.G;
						pixels[index + 2] = color.B;
						pixels[index + 3] = color.A;
					}
				}
			},
			false);

		glTexture.SubImage2D(0, region, PixelFormat.Rgba, PixelType.UnsignedByte, pixels.AsSpan()[..pixelsByteCount]);
		ArrayPool<byte>.Shared.Return(pixels);
		glTexture.GenerateMipmap();

		if (CurrentTexture != null)
			Use(CurrentTexture, false);
	}

	// Add textures to the appropriate lists for deferred processing
	private void Texture_Changed(Texture texture, Rectangle region)
	{
		lock (_changedTexturesLock)
		{
			if (!_changedTextures.TryAdd(texture, region))
			{
				var existingRegion = _changedTextures[texture];
				var union = Rectangle.Union(existingRegion, region);
				_changedTextures[texture] = union;
			}
		}
	}

	private void Texture_Disposed(Texture texture)
	{
		lock (_disposedTexturesLock)
			_disposedTextures.Add(texture);
	}
}

using GLCS;
using GLCS.Managed;
using System.Buffers;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AppThing;

internal sealed class QuadRenderer : IDisposable
{
	private readonly struct VertexAttribs(Vector2 position)
	{
		[GLVertexAttrib(0, 2, VertexAttribPointerType.Float, false)]
		[GLVertexAttribDivisor(0)]
		public readonly Vector2 Position = position;
	}

	private readonly struct InstanceAttribs(Vector4 color, in Matrix4x4 model, in Matrix4x4 uvTransform)
	{
		[GLVertexAttrib(1, 4, VertexAttribPointerType.Float, false)]
		[GLVertexAttribDivisor(1)]
		public readonly Vector4 Color = color;

		// Indices 3-6
		[GLVertexAttrib(2, 4 * 4, VertexAttribPointerType.Float, false)]
		[GLVertexAttribDivisor(1)]
		public readonly Matrix4x4 Model = model;

		// Indices 7-10
		[GLVertexAttrib(6, 4 * 4, VertexAttribPointerType.Float, false)]
		[GLVertexAttribDivisor(1)]
		public readonly Matrix4x4 UvTransform = uvTransform;
	}

	private const string _vertexShaderSource =
		"""
		#version 330 core
		layout(location = 0) in vec2 aPos;
		layout(location = 1) in vec4 aColor;
		layout(location = 2) in mat4 aModel;
		layout(location = 6) in mat4 aUvTransform;

		out vec4 vColor;
		out vec2 vTexCoord;

		uniform mat4 uProjection;

		void main()
		{
			vColor = aColor;
			vTexCoord = (aUvTransform * vec4(aPos, 0.0, 1.0)).xy;
			gl_Position = uProjection * aModel * vec4(aPos, 0.0, 1.0);
		}
		""";

	private const string _fragmentShaderSource =
		"""
		#version 330 core
		out vec4 FragColor;

		in vec4 vColor;
		in vec2 vTexCoord;

		uniform sampler2D uTexture;

		void main()
		{
			vec4 texColor = texture(uTexture, vTexCoord);
			FragColor = vColor * texColor;
		}
		""";

	private const int _maxQuadsPerBatch = 1_000_000;

	private readonly GLProgram _program = new();
	private readonly GLBuffer<VertexAttribs> _vertexBuffer = new();
	private readonly GLBuffer<InstanceAttribs> _instanceBuffer = new();
	private readonly GLVertexArray _vao = new();

	private readonly GLUniformLocation _uProjection;

	private readonly List<InstanceAttribs> _instanceAttribs = new(_maxQuadsPerBatch);

	private readonly Dictionary<Texture, GLTexture> _textureMap = [];
	private readonly HashSet<Texture> _disposedTextures = [];
	private readonly HashSet<Texture> _changedTextures = [];
	private readonly Lock _disposedTexturesLock = new();
	private readonly Lock _changedTexturesLock = new();
	private Texture? _currentTexture = null;

	internal int QuadCount
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _instanceAttribs.Count;
	}

	internal unsafe QuadRenderer()
	{
		_vertexBuffer.Data([
				new(new(0.0f, 0.0f)), // Top-left
				new(new(1.0f, 0.0f)), // Top-right
				new(new(0.0f, 1.0f)), // Bottom-left
				new(new(1.0f, 1.0f)), // Bottom-right
			],
			BufferUsageARB.StaticDraw);

		_instanceBuffer.Data(_maxQuadsPerBatch * sizeof(InstanceAttribs), BufferUsageARB.StreamDraw);

		_vao.VertexAttribPointers(_vertexBuffer);
		_vao.VertexAttribPointers(_instanceBuffer);

		using (var vertexShader = new GLShader(ShaderType.VertexShader))
		using (var fragmentShader = new GLShader(ShaderType.FragmentShader))
		{
			vertexShader.Compile(_vertexShaderSource);
			if (vertexShader.Get(ShaderParameterName.CompileStatus) != GL.TRUE)
				throw new($"Vertex shader compilation failed: {vertexShader.GetInfoLog()}");

			fragmentShader.Compile(_fragmentShaderSource);
			if (fragmentShader.Get(ShaderParameterName.CompileStatus) != GL.TRUE)
				throw new($"Fragment shader compilation failed: {fragmentShader.GetInfoLog()}");

			_program.Link(vertexShader, fragmentShader);
		}

		if (_program.Get(ProgramPropertyARB.LinkStatus) != GL.TRUE)
			throw new($"Shader program linking failed: {_program.GetInfoLog()}");

		_uProjection = _program.GetUniformLocation("uProjection");
	}

	public void Dispose()
	{
		_vao.Dispose();
		_instanceBuffer.Dispose();
		_vertexBuffer.Dispose();
		_program.Dispose();

		_instanceAttribs.Capacity = 0;

		Console.WriteLine($"[QuadBatchRenderer] Disposing {_textureMap.Count} textures");
		foreach (var texture in _textureMap.Values)
			texture.Dispose();

		_textureMap.Clear();
		_disposedTextures.Clear();
		_changedTextures.Clear();
	}

	public void Draw(Texture texture, RectangleF destRect, RectangleF sourceRect, in Matrix4x4 transformation, Color color)
	{
		var model =
			Matrix4x4.CreateScale(destRect.Width, destRect.Height, 1.0f) *
			Matrix4x4.CreateTranslation(destRect.X, destRect.Y, 0.0f) *
			transformation;

		var uvTransform =
			Matrix4x4.CreateScale(sourceRect.Width / (float)texture.Size.Width, sourceRect.Height / (float)texture.Size.Height, 1.0f) *
			Matrix4x4.CreateTranslation(sourceRect.X / (float)texture.Size.Width, sourceRect.Y / (float)texture.Size.Height, 0.0f);

		var colorVec = new Vector4(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
		AddQuad(
			new(colorVec, model, uvTransform),
			texture);
	}

	internal void HandleSizeChanged(Size size)
	{
		var projection = Matrix4x4.CreateOrthographicOffCenter(0, size.Width, size.Height, 0, -100.0f, 100.0f);
		_uProjection.Matrix4(ref projection);
	}

	internal void Commit()
	{
		if (_instanceAttribs.Count != 0)
		{
			// Upload instance data to the GPU and draw
			_instanceBuffer.SubData(0, CollectionsMarshal.AsSpan(_instanceAttribs)[..QuadCount]);
			_vao.DrawArraysInstanced(PrimitiveType.TriangleStrip, 0, 4, QuadCount, _program);

			// Clear vertex list, no current texture
			_instanceAttribs.Clear();
		}

		HandleTextures();
	}

	private void AddQuad(InstanceAttribs attribs, Texture texture)
	{
		var quadLimitReached = QuadCount >= _maxQuadsPerBatch;
		var textureDifferent = _currentTexture != null && _currentTexture != texture;

		bool textureChanged;

		lock (_changedTexturesLock)
			textureChanged = _changedTextures.Contains(texture);

		if (quadLimitReached || textureChanged || textureDifferent)
			Commit();

		var newTexture = !_textureMap.ContainsKey(texture);
		GetGlTexture(texture).Bind();
		_currentTexture = texture;

		if (newTexture || textureChanged)
			HandleChangedTexture(texture);

		// Push vertices
		_instanceAttribs.Add(attribs);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void HandleTextures()
	{
		lock (_disposedTexturesLock)
		lock (_changedTexturesLock)
		{
			foreach (var texture in _disposedTextures)
			{
				if (!_textureMap.TryGetValue(texture, out var glTexture))
					continue;

				glTexture.Dispose();
				_textureMap.Remove(texture);
				_changedTextures.Remove(texture);
				texture.Changed -= Texture_Changed;
				texture.Disposed -= Texture_Disposed;
			}

			foreach (var texture in _changedTextures)
				HandleChangedTexture(texture);

			_disposedTextures.Clear();
			_changedTextures.Clear();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void HandleChangedTexture(Texture texture)
	{
		if (!_textureMap.TryGetValue(texture, out var glTexture))
			return;

		// Upload new texture data to the GPU
		var pixelsByteCount = texture.Size.Width * texture.Size.Height * 4;
		var pixels = ArrayPool<byte>.Shared.Rent(pixelsByteCount);
		texture.AccessPixels((span, size) =>
		{
			for (var y = 0; y < size.Height; y++)
			{
				for (var x = 0; x < size.Width; x++)
				{
					var color = span[x + y * size.Width];
					var index = (x + (y * size.Width)) * 4;
					pixels[index + 0] = color.R;
					pixels[index + 1] = color.G;
					pixels[index + 2] = color.B;
					pixels[index + 3] = color.A;
				}
			}
		});

		texture.Invalidate();

		glTexture.Image2D(0, InternalFormat.Rgba8, texture.Size, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixels.AsSpan()[..pixelsByteCount]);
		ArrayPool<byte>.Shared.Return(pixels);
		glTexture.GenerateMipmap();
		GetGlTexture(texture).Bind();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private GLTexture GetGlTexture(Texture texture)
	{
		// If the texture is already "known", return the corresponding GL texture
		if (_textureMap.TryGetValue(texture, out var glTexture))
			return glTexture;

		// Subscribe to notifications regarding the texture's state
		texture.Changed += Texture_Changed;
		texture.Disposed += Texture_Disposed;

		// Create a new GL texture
		glTexture = new(TextureTarget.Texture2d)
		{
			MinFilter = TextureMinFilter.Linear,
			MagFilter = TextureMagFilter.Linear
		};

		// Add the new GL texture to the list of known textures
		_textureMap.Add(texture, glTexture);

		// Also add the texture to the list of changed textures
		lock (_changedTexturesLock)
			_changedTextures.Add(texture);

		return glTexture;
	}

	// Add textures to the appropriate lists for deferred processing
	private void Texture_Changed(Texture texture)
	{
		lock (_changedTexturesLock)
			_changedTextures.Add(texture);
	}

	private void Texture_Disposed(Texture texture)
	{
		lock (_disposedTexturesLock)
			_disposedTextures.Add(texture);
	}
}

using GLCS;
using GLCS.Managed;
using System.Buffers;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AppThing;

internal sealed class BitmapFontRenderer : BatchRenderer, IDisposable
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

		// Indices 2-5
		[GLVertexAttrib(2, 4 * 4, VertexAttribPointerType.Float, false)]
		[GLVertexAttribDivisor(1)]
		public readonly Matrix4x4 Model = model;

		// Indices 6-9
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
			FragColor = vec4(vColor.rgb, vColor.a * texColor.r);
		}
		""";

	private const int _maxQuadsPerBatch = 1_000_000;

	private readonly Renderer _renderer;

	private readonly GLProgram _program = new();
	private readonly GLBuffer<VertexAttribs> _vertexBuffer = new();
	private readonly GLBuffer<InstanceAttribs> _instanceBuffer = new();
	private readonly GLVertexArray _vao = new();

	private readonly GLUniformLocation _uProjection;

	private readonly List<InstanceAttribs> _instanceAttribs = new(_maxQuadsPerBatch);

	internal int QuadCount
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _instanceAttribs.Count;
	}

	internal unsafe BitmapFontRenderer(Renderer renderer)
	{
		_renderer = renderer;

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

		_instanceAttribs.Clear();
		_instanceAttribs.Capacity = 0;
	}

	public void DrawGlyph(Texture texture, RectangleF destRect, RectangleF sourceRect, Color color, in Matrix4x4 transformation)
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
	
	public void DrawText(string str, Point location, BitmapFont font, Color color)
	{
		var chars = ArrayPool<Renderer.RendererChar>.Shared.Rent(str.Length);
		var charCount = 0;

		var penX = 0L;
		var penY = 0L;

		foreach (var c in str)
		{
			if (font.TryGetGlyph(new(c), ref penX, ref penY, out var fontGlyph, out var drawPos))
				chars[charCount++] = new(drawPos, fontGlyph);
		}

		for (var i = 0; i < charCount; i++)
		{
			var charInfo = chars[i];
			DrawGlyph(charInfo.Glyph.Atlas, new(new(charInfo.Dest.X + location.X, charInfo.Dest.Y + location.Y), charInfo.Glyph.AtlasRegion.Size), charInfo.Glyph.AtlasRegion, color, Matrix4x4.Identity);
		}

		ArrayPool<Renderer.RendererChar>.Shared.Return(chars);
	}

	internal void HandleSizeChanged(Size size) => _uProjection.Matrix4(ref _renderer.Projection);

	internal override void Commit()
	{
		if (_instanceAttribs.Count != 0)
		{
			// Upload instance data to the GPU and draw
			_instanceBuffer.SubData(0, CollectionsMarshal.AsSpan(_instanceAttribs)[..QuadCount]);
			_vao.DrawArraysInstanced(PrimitiveType.TriangleStrip, 0, 4, QuadCount, _program);

			// Clear vertex list, no current texture
			_instanceAttribs.Clear();
		}
	}

	private void AddQuad(InstanceAttribs attribs, Texture texture)
	{
		var quadLimitReached = QuadCount >= _maxQuadsPerBatch;
		var textureDifferent = _renderer.TextureManager.CurrentTexture != null && _renderer.TextureManager.CurrentTexture != texture;
		var textureChanged = _renderer.TextureManager.HasTextureChanged(texture, out _);
		if (quadLimitReached || textureChanged || textureDifferent)
			Commit();

		_renderer.TextureManager.Use(texture);

		// Push vertices
		_instanceAttribs.Add(attribs);
	}
}

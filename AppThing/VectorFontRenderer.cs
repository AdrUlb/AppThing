using FontThing.TrueType.Parsing;
using GLCS;
using GLCS.Managed;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;

namespace AppThing;

public sealed class VectorFontRenderer : BatchRenderer, IDisposable
{
	private readonly struct GlyphVertexAttribs(Vector2 position)
	{
		[GLVertexAttrib(0, 2, VertexAttribPointerType.Float, false)]
		public readonly Vector2 Position = position;
	}

	private readonly struct ScreenVertexAttribs(Vector2 position)
	{
		[GLVertexAttrib(0, 2, VertexAttribPointerType.Float, false)]
		public readonly Vector2 Position = position;
	}

	private readonly struct SharedVertexAttribs(Vector4 color)
	{
		[GLVertexAttrib(1, 4, VertexAttribPointerType.Float, false)]
		public readonly Vector4 Color = color;
	}

	private const string _glyphVertexShaderSource =
		"""
		#version 330 core
		layout(location = 0) in vec2 aPos;

		out vec4 vColor;

		uniform mat4 uProjection;

		void main()
		{
			gl_Position = uProjection * vec4(aPos, 0.0, 1.0);
		}
		""";

	private const string _screenVertexShaderSource =
		"""
		#version 330 core
		layout(location = 0) in vec2 aPos;
		layout(location = 1) in vec4 aColor;

		out vec4 vColor;

		void main()
		{
			vColor = aColor;
			gl_Position = vec4(aPos, 0.0, 1.0);
		}
		""";

	private const string _fragmentShaderSource =
		"""
		#version 330 core
		out vec4 FragColor;

		in vec4 vColor;

		void main()
		{
			FragColor = vColor;
		}
		""";

	private readonly Renderer _renderer;

	private readonly GLProgram _glyphProgram = new();
	private readonly GLProgram _screenProgram = new();
	private readonly GLBuffer<GlyphVertexAttribs> _glyphVertexBuffer = new();
	private readonly GLBuffer<ScreenVertexAttribs> _screenVertexBuffer = new();
	private readonly GLBuffer<SharedVertexAttribs> _sharedVertexBuffer = new();
	private readonly GLVertexArray _glyphVao = new();
	private readonly GLVertexArray _screenVao = new();

	private readonly GLUniformLocation _uProjection;

	private readonly List<GlyphVertexAttribs> _vertices = [];
	private readonly List<SharedVertexAttribs> _instanceAttribs = [];

	public VectorFontRenderer(Renderer renderer)
	{
		ReadOnlySpan<ScreenVertexAttribs> screenVertices =
		[
			new(new(-1.0f, -1.0f)),
			new(new(1.0f, -1.0f)),
			new(new(1.0f, 1.0f)),
			new(new(-1.0f, 1.0f))
		];

		_screenVertexBuffer.Data(screenVertices, BufferUsageARB.StaticDraw);

		_renderer = renderer;

		_glyphVao.VertexAttribPointers(_glyphVertexBuffer);
		_glyphVao.VertexAttribPointers(_sharedVertexBuffer);
		_screenVao.VertexAttribPointers(_screenVertexBuffer);
		_screenVao.VertexAttribPointers(_sharedVertexBuffer);

		using (var vertexShader = new GLShader(ShaderType.VertexShader))
		using (var fragmentShader = new GLShader(ShaderType.FragmentShader))
		{
			vertexShader.Compile(_glyphVertexShaderSource);
			if (vertexShader.Get(ShaderParameterName.CompileStatus) != GL.TRUE)
				throw new($"Vertex shader compilation failed: {vertexShader.GetInfoLog()}");

			fragmentShader.Compile(_fragmentShaderSource);
			if (fragmentShader.Get(ShaderParameterName.CompileStatus) != GL.TRUE)
				throw new($"Fragment shader compilation failed: {fragmentShader.GetInfoLog()}");

			_glyphProgram.Link(vertexShader, fragmentShader);
		}

		if (_glyphProgram.Get(ProgramPropertyARB.LinkStatus) != GL.TRUE)
			throw new($"Shader program linking failed: {_glyphProgram.GetInfoLog()}");

		using (var vertexShader = new GLShader(ShaderType.VertexShader))
		using (var fragmentShader = new GLShader(ShaderType.FragmentShader))
		{
			vertexShader.Compile(_screenVertexShaderSource);
			if (vertexShader.Get(ShaderParameterName.CompileStatus) != GL.TRUE)
				throw new($"Vertex shader compilation failed: {vertexShader.GetInfoLog()}");

			fragmentShader.Compile(_fragmentShaderSource);
			if (fragmentShader.Get(ShaderParameterName.CompileStatus) != GL.TRUE)
				throw new($"Fragment shader compilation failed: {fragmentShader.GetInfoLog()}");

			_screenProgram.Link(vertexShader, fragmentShader);
		}

		if (_screenProgram.Get(ProgramPropertyARB.LinkStatus) != GL.TRUE)
			throw new($"Shader program linking failed: {_screenProgram.GetInfoLog()}");

		_uProjection = _glyphProgram.GetUniformLocation("uProjection");
	}

	public void Dispose()
	{
		_glyphProgram.Dispose();
		_screenProgram.Dispose();
		_glyphVao.Dispose();
		_glyphVertexBuffer.Dispose();
		_screenVertexBuffer.Dispose();
		_sharedVertexBuffer.Dispose();
	}

	internal void HandleSizeChanged(Size size)
	{
		_uProjection.Matrix4(ref _renderer.Projection);
	}

	internal override void Commit()
	{
		_sharedVertexBuffer.Data(CollectionsMarshal.AsSpan(_instanceAttribs), BufferUsageARB.StaticDraw);
		_glyphVertexBuffer.Data(CollectionsMarshal.AsSpan(_vertices), BufferUsageARB.StaticDraw);

		ManagedGL.Current.Unmanaged.Enable(EnableCap.StencilTest);
		ManagedGL.Current.Unmanaged.Disable(EnableCap.CullFace);

		ManagedGL.Current.Unmanaged.Clear(ClearBufferMask.StencilBufferBit);

		ManagedGL.Current.Unmanaged.ColorMask(false, false, false, false);
		ManagedGL.Current.Unmanaged.StencilFunc(StencilFunction.Always, 0, 0xFF);
		ManagedGL.Current.Unmanaged.StencilOpSeparate(TriangleFace.Front, StencilOp.Keep, StencilOp.Keep, StencilOp.IncrWrap);
		ManagedGL.Current.Unmanaged.StencilOpSeparate(TriangleFace.Back, StencilOp.Keep, StencilOp.Keep, StencilOp.DecrWrap);
		_glyphVao.DrawArrays(PrimitiveType.Triangles, 0, _vertices.Count, _glyphProgram);

		ManagedGL.Current.Unmanaged.ColorMask(true, true, true, true);
		ManagedGL.Current.Unmanaged.StencilFunc(StencilFunction.Notequal, 0, 0xFF);
		ManagedGL.Current.Unmanaged.StencilOp(StencilOp.Zero, StencilOp.Zero, StencilOp.Zero);
		_screenVao.DrawArrays(PrimitiveType.TriangleFan, 0, 4, _screenProgram);

		_vertices.Clear();
		_instanceAttribs.Clear();

		ManagedGL.Current.Unmanaged.Disable(EnableCap.StencilTest);
		ManagedGL.Current.Unmanaged.Enable(EnableCap.CullFace);
	}

	internal void DrawText(string str, Point location, VectorFont font, float size, Color color)
	{
		var scale = font._ttf.PointSizeToScale(size);
		var colorVec = new Vector4(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);

		var pen = new Vector2(0, 0);

		var first = true;

		foreach (var rune in str.EnumerateRunes())
		{
			if (rune.Value == '\n')
			{
				pen.X = 0;
				pen.Y -= font._ttf.LineHeight;
				continue;
			}

			var glyph = font._ttf.LoadGlyph(rune);

			if (first)
				pen.X -= glyph.LeftSideBearing;

			var outline = glyph.Outline;

			if (outline == null)
			{
				pen.X += glyph.AdvanceWidth;
				continue;
			}

			if (!font.TryGetGlyph(glyph, out var glyph2))
				continue;

			var renderPos = new Vector2(pen.X, font._ttf.LineHeight - pen.Y);

			foreach (var vertex in glyph2.Vertices)
			{
				var v = vertex;
				v += renderPos;
				v *= scale;
				v += new Vector2(location.X, location.Y);

				_vertices.Add(new(v));
				var attribs = new SharedVertexAttribs(colorVec);
				_instanceAttribs.Add(attribs);
			}

			pen.X += glyph.AdvanceWidth;

			first = false;
		}

		return;

		Vector2 ScalePoint(Vector2 point)
		{
			point *= new Vector2(1, -1);
			return point;
		}
	}
}

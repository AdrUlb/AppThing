using FontThing.TrueType.Parsing;
using GLCS;
using GLCS.Managed;
using System.Drawing;
using System.Numerics;

namespace AppThing;

public sealed class TestRenderer : BatchRenderer, IDisposable
{
	private readonly struct GlyphVertexAttribs(Vector2 position, Vector2 uv, byte type)
	{
		[GLVertexAttrib(0, 2, VertexAttribPointerType.Float, false)]
		[GLVertexAttribDivisor(0)]
		public readonly Vector2 Position = position;

		[GLVertexAttrib(1, 2, VertexAttribPointerType.Float, false)]
		[GLVertexAttribDivisor(0)]
		public readonly Vector2 Uv = uv;

		[GLVertexAttrib(2, 1, VertexAttribPointerType.Float, false)]
		[GLVertexAttribDivisor(0)]
		public readonly float Type = type;
	}

	private readonly struct BoundingBoxVertexAttribs(Vector2 position)
	{
		[GLVertexAttrib(0, 2, VertexAttribPointerType.Float, false)]
		[GLVertexAttribDivisor(0)]
		public readonly Vector2 Position = position;
	}

	private const string _glyphVertexShaderSource =
		"""
		#version 330 core
		layout(location = 0) in vec2 aPos;
		layout(location = 1) in vec2 aUv;
		layout(location = 2) in float aType;

		out vec2 vUv;
		flat out float vType;

		uniform mat4 uProjection;

		void main()
		{
			vUv = aUv;
			vType = aType;
			gl_Position = uProjection * vec4(aPos, 0.0, 1.0);
		}
		""";

	private const string _glyphFragmentShaderSource =
		"""
		#version 330 core
		out vec4 FragColor;

		in vec2 vUv;
		flat in float vType;

		void main()
		{
			float alpha = 1.0;

			if (vType > 0.5)
			{
				float f = vUv.x * vUv.x - vUv.y;
				
				float delta = fwidth(f);
				alpha = smoothstep(delta, -delta, f);
				
				if (alpha <= 0.001)
					discard;
			}
			
			FragColor = vec4(1.0, 1.0, 1.0, alpha);
		}
		""";

	private const string _boundingBoxVertexShaderSource =
		"""
		#version 330 core
		layout(location = 0) in vec2 aPos;

		uniform mat4 uProjection;

		void main()
		{
			gl_Position = uProjection * vec4(aPos, 0.0, 1.0);
		}
		""";

	private const string _boundingBoxFragmentShaderSource =
		"""
		#version 330 core
		out vec4 FragColor;

		uniform vec4 uTextColor;

		void main()
		{
			FragColor = uTextColor;
		}
		""";

	private readonly Renderer _renderer;

	private readonly GLProgram _glyphProgram = new();
	private readonly GLProgram _boundingBoxProgram = new();
	private readonly GLBuffer<GlyphVertexAttribs> _glyphVertexBuffer = new();
	private readonly GLBuffer<BoundingBoxVertexAttribs> _boundingBoxVertexBuffer = new();
	private readonly GLVertexArray _glyphVao = new();
	private readonly GLVertexArray _boundingBoxVao = new();

	private readonly GLUniformLocation _uProjection1;
	private readonly GLUniformLocation _uProjection2;
	private readonly GLUniformLocation _uTextColor;

	public TestRenderer(Renderer renderer)
	{
		_renderer = renderer;

		_glyphVao.VertexAttribPointers(_glyphVertexBuffer);
		_boundingBoxVao.VertexAttribPointers(_boundingBoxVertexBuffer);

		using (var vertexShader = new GLShader(ShaderType.VertexShader))
		using (var fragmentShader = new GLShader(ShaderType.FragmentShader))
		{
			vertexShader.Compile(_glyphVertexShaderSource);
			if (vertexShader.Get(ShaderParameterName.CompileStatus) != GL.TRUE)
				throw new($"Vertex shader compilation failed: {vertexShader.GetInfoLog()}");

			fragmentShader.Compile(_glyphFragmentShaderSource);
			if (fragmentShader.Get(ShaderParameterName.CompileStatus) != GL.TRUE)
				throw new($"Fragment shader compilation failed: {fragmentShader.GetInfoLog()}");

			_glyphProgram.Link(vertexShader, fragmentShader);
		}

		if (_glyphProgram.Get(ProgramPropertyARB.LinkStatus) != GL.TRUE)
			throw new($"Shader program linking failed: {_glyphProgram.GetInfoLog()}");

		using (var vertexShader = new GLShader(ShaderType.VertexShader))
		using (var fragmentShader = new GLShader(ShaderType.FragmentShader))
		{
			vertexShader.Compile(_boundingBoxVertexShaderSource);
			if (vertexShader.Get(ShaderParameterName.CompileStatus) != GL.TRUE)
				throw new($"Vertex shader compilation failed: {vertexShader.GetInfoLog()}");

			fragmentShader.Compile(_boundingBoxFragmentShaderSource);
			if (fragmentShader.Get(ShaderParameterName.CompileStatus) != GL.TRUE)
				throw new($"Fragment shader compilation failed: {fragmentShader.GetInfoLog()}");

			_boundingBoxProgram.Link(vertexShader, fragmentShader);
		}

		if (_boundingBoxProgram.Get(ProgramPropertyARB.LinkStatus) != GL.TRUE)
			throw new($"Shader program linking failed: {_boundingBoxProgram.GetInfoLog()}");

		_uProjection1 = _glyphProgram.GetUniformLocation("uProjection");
		_uProjection2 = _boundingBoxProgram.GetUniformLocation("uProjection");
		_uTextColor = _boundingBoxProgram.GetUniformLocation("uTextColor");
	}

	public void Dispose()
	{
		_glyphProgram.Dispose();
		_boundingBoxProgram.Dispose();
		_glyphVao.Dispose();
		_glyphVertexBuffer.Dispose();
	}

	internal void HandleSizeChanged(Size size)
	{
		_uProjection1.Matrix4(ref _renderer.Projection);
		_uProjection2.Matrix4(ref _renderer.Projection);
	}

	internal override void Commit()
	{

	}

	internal void Test(TrueTypeFont font)
	{
		var textColor = Color.FromArgb(147, 161, 161);

		var glyph = font.LoadGlyph("A".EnumerateRunes().First());
		var outline = glyph.Outline;
		if (outline == null)
			return;

		var scale = font.PointSizeToScale(400.0f);
		var width = (int)((outline.XMax - outline.XMin) * scale);
		var height = (int)((outline.YMax - outline.YMin) * scale);
		if (outline is not SimpleGlyphOutline simpleOutline)
			return;

		var vertices = new List<GlyphVertexAttribs>();

		var contourStartpointIndex = 0;
		foreach (var contourEndpointIndex in simpleOutline.EndPointsOfContours)
		{
			var points = simpleOutline.Points.AsSpan()[contourStartpointIndex..(contourEndpointIndex + 1)];

			// Find starting point (first on-curve point)
			var startOffset = 0;
			while (startOffset < points.Length && !points[startOffset].OnCurve)
				startOffset++;

			// No on-curve points found
			if (startOffset == points.Length)
				throw new NotImplementedException();

			var p0Vec = ScalePoint(points[startOffset]);

			var pivot = p0Vec;

			for (var i = 1; i <= points.Length; i++)
			{
				var index = (i + startOffset) % points.Length;

				var p1 = points[index];
				var p1Vec = ScalePoint(p1);

				// Points are: on-curve, on-curve - straight line
				if (p1.OnCurve)
				{
					AddStraight(p0Vec, p1Vec);
					p0Vec = p1Vec;
					continue;
				}

				index = (index + 1) % points.Length;

				var p2 = points[index];
				var p2Vec = ScalePoint(p2);

				if (p2.OnCurve) // Points are: on-curve, off-curve, on-curve - quadratic Bézier
				{
					i++;

					AddBezier(p0Vec, p1Vec, p2Vec);
					p0Vec = p2Vec;
				}
				else // Points are: on-curve, off-curve, off-curve - insert midpoint as on-curve point for quadratic Bézier
				{
					var midpoint = Vector2.Lerp(p1Vec, p2Vec, 0.5f);
					AddBezier(p0Vec, p1Vec, midpoint);
					p0Vec = midpoint;
				}
			}

			// Next contour's start point index immediately follows this contour's endpoint index
			contourStartpointIndex = contourEndpointIndex + 1;

			continue;

			void AddStraight(Vector2 p0, Vector2 p1)
			{
				vertices.Add(new(pivot, new(0.0f, 0.0f), 0));
				vertices.Add(new(p0, new(0.5f, 0.0f), 0));
				vertices.Add(new(p1, new(1.0f, 1.0f), 0));
			}

			void AddBezier(Vector2 p0, Vector2 p1, Vector2 p2)
			{
				vertices.Add(new(pivot, new(0.0f, 0.0f), 0));
				vertices.Add(new(p0, new(0.5f, 0.0f), 0));
				vertices.Add(new(p2, new(1.0f, 1.0f), 0));

				vertices.Add(new(p0, new(0.0f, 0.0f), 1));
				vertices.Add(new(p1, new(0.5f, 0.0f), 1));
				vertices.Add(new(p2, new(1.0f, 1.0f), 1));
			}
		}

		ReadOnlySpan<BoundingBoxVertexAttribs> boundingBox =
		[
			new(new Vector2(outline.XMin, outline.YMin) * scale),
			new(new Vector2(outline.XMax, outline.YMin) * scale),
			new(new Vector2(outline.XMax, outline.YMax) * scale),
			new(new Vector2(outline.XMin, outline.YMax) * scale)
		];

		_uTextColor.Vec4(textColor.R / 255.0f, textColor.G / 255.0f, textColor.B / 255.0f, textColor.A / 255.0f);

		_glyphVertexBuffer.Data(vertices.ToArray(), BufferUsageARB.StaticDraw);
		_boundingBoxVertexBuffer.Data(boundingBox, BufferUsageARB.StaticDraw);

		ManagedGL.Current.Unmanaged.Clear(ClearBufferMask.StencilBufferBit);

		ManagedGL.Current.Unmanaged.ColorMask(false, false, false, false);
		ManagedGL.Current.Unmanaged.DepthMask(false);

		ManagedGL.Current.Unmanaged.Enable(EnableCap.StencilTest);
		ManagedGL.Current.Unmanaged.Disable(EnableCap.CullFace);

		ManagedGL.Current.Unmanaged.StencilFunc(StencilFunction.Always, 0, 0xFF);

		ManagedGL.Current.Unmanaged.StencilOpSeparate(TriangleFace.Front, StencilOp.Keep, StencilOp.Keep, StencilOp.IncrWrap);
		ManagedGL.Current.Unmanaged.StencilOpSeparate(TriangleFace.Back, StencilOp.Keep, StencilOp.Keep, StencilOp.DecrWrap);

		_glyphVao.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count, _glyphProgram);

		ManagedGL.Current.Unmanaged.ColorMask(true, true, true, true);
		ManagedGL.Current.Unmanaged.DepthMask(true);

		ManagedGL.Current.Unmanaged.StencilFunc(StencilFunction.Notequal, 0, 0xFF);
		ManagedGL.Current.Unmanaged.StencilOp(StencilOp.Zero, StencilOp.Zero, StencilOp.Zero);

		_glyphVao.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count, _glyphProgram);

		//_boundingBoxVao.DrawArrays(PrimitiveType.TriangleFan, 0, boundingBox.Length, _boundingBoxProgram);

		ManagedGL.Current.Unmanaged.Disable(EnableCap.StencilTest);
		ManagedGL.Current.Unmanaged.Enable(EnableCap.CullFace);

		return;

		Vector2 ScalePoint(GlyphPoint point) => new Vector2(point.X, point.Y) * scale;
	}
}

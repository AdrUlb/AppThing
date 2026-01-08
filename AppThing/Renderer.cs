using GLCS;
using GLCS.Managed;
using SDL3CS;
using System.Buffers;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace AppThing;

public sealed class Renderer : IDisposable
{
	private readonly Window _window;
	internal static readonly ManagedGL Gl = new(static proc => Sdl.GL_GetProcAddress(proc));

	private readonly Sdl.GLContext _glContext;
	private readonly MultisampleFramebuffer _msaaBuffer;
	internal readonly TextureManager TextureManager;
	private readonly QuadRenderer _quadBatch;
	private readonly BitmapFontRenderer _bitmapFontBatch;
	private readonly VectorFontRenderer _vectorFontBatch;
	private readonly int _msaaSamples;

	internal Matrix4x4 Projection;
	private bool _disposed;
	private Size _size;
	private volatile bool _sizeChanged = false;

	private BatchRenderer? _currentBatch = null;

	public Color ClearColor { get; set; } = Color.Black;

	internal Renderer(Window window)
	{
		_window = window;

		// Create GL context
		Console.Write("[Renderer] Creating GL context... ");
		_glContext = Sdl.GL_CreateContext(window.SdlWindowPtr.Value);
		if (_glContext.IsNull)
		{
			throw new($"Failed to create GL context: {Sdl.GetError()}");
		}

		Console.WriteLine($"done");

		MakeCurrent();

		_msaaBuffer = new(supportDepthBuffer: true);
		TextureManager = new();
		_quadBatch = new(this);
		_bitmapFontBatch = new(this);
		_vectorFontBatch = new(this);

		Console.WriteLine($"[Renderer] OpenGL Renderer: {Gl.GetString(StringName.Renderer)}");
		Console.WriteLine($"[Renderer] OpenGL Version: {Gl.GetString(StringName.Version)}");

		Sdl.GL_SetSwapInterval(0);

		Gl.Enable(EnableCap.Multisample);
		Gl.Enable(EnableCap.CullFace);
		Gl.Enable(EnableCap.Blend);

		Gl.Unmanaged.FrontFace(FrontFaceDirection.Cw);
		Gl.Unmanaged.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

		_msaaSamples = Math.Min(4, Gl.GetInteger(GetPName.MaxFramebufferSamples));
		DoneCurrent();
	}

	public void Dispose()
	{
		MakeCurrent();

		if (_disposed)
			return;

		_msaaBuffer.Dispose();
		_quadBatch.Dispose();
		_bitmapFontBatch.Dispose();
		_vectorFontBatch.Dispose();

		TextureManager.Dispose();
		_disposed = true;

		DoneCurrent();

		Sdl.GL_DestroyContext(_glContext);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void SwapBuffers() => Sdl.GL_SwapWindow(_window.SdlWindowPtr.Value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void MakeCurrent()
	{
		Sdl.GL_MakeCurrent(_window.SdlWindowPtr.Value, _glContext);
		Gl.MakeCurrent();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void DoneCurrent()
	{
		ManagedGL.DoneCurrent();
		Sdl.GL_MakeCurrent(_window.SdlWindowPtr.Value, Sdl.GLContext.Null);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void SetSize(Size size)
	{
		_size = size;
		_sizeChanged = true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void BeginFrame()
	{
		if (_sizeChanged)
		{
			Gl.Viewport(0, 0, _size.Width, _size.Height);
			Projection = Matrix4x4.CreateOrthographicOffCenter(0, _size.Width, _size.Height, 0, -100.0f, 100.0f);

			_msaaBuffer.Setup(_size, _msaaSamples);
			_quadBatch.HandleSizeChanged(_size);
			_bitmapFontBatch.HandleSizeChanged(_size);
			_vectorFontBatch.HandleSizeChanged(_size);
			_sizeChanged = false;
		}

		//_msaaBuffer.BeginFrame();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void Clear()
	{
		Gl.Clear(ClearColor, ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void EndFrame()
	{
		_currentBatch?.Commit();
		//_msaaBuffer.EndFrame();
		//_msaaBuffer.Blit();
		TextureManager.EndFrame();

		Sdl.GL_SwapWindow(_window.SdlWindowPtr.Value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Draw(Texture texture, Rectangle destRect, Rectangle sourceRect, Color color, in Matrix4x4 transformation)
	{
		UseBatch(_quadBatch).Draw(texture, destRect, sourceRect, color, transformation);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Draw(Texture texture, Rectangle destRect, Rectangle sourceRect, Color color) => Draw(texture, destRect, sourceRect, color, Matrix4x4.Identity);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Draw(Texture texture, Rectangle destRect, Rectangle sourceRect) => Draw(texture, destRect, sourceRect, Color.White);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Draw(Texture texture, Rectangle destRect) => Draw(texture, destRect, new(Point.Empty, texture.Size));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Draw(Texture texture, Point location) => Draw(texture, new Rectangle(location, texture.Size));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void DrawText(string str, Point location, BitmapFont font, Color color) => UseBatch(_bitmapFontBatch).DrawText(str, location, font, color);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void DrawText(string str, Point location, VectorFont font, float size, Color color) => UseBatch(_vectorFontBatch).DrawText(str, location, font, size, color);

	internal readonly struct RendererChar(Point dest, BitmapFontGlyph glyph)
	{
		public readonly Point Dest = dest;
		public readonly BitmapFontGlyph Glyph = glyph;
	}

	private T UseBatch<T>(T batch) where T : BatchRenderer
	{
		if (_currentBatch != batch)
		{
			_currentBatch?.Commit();
			_currentBatch = batch;
		}

		return batch;
	}
}

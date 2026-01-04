using SDL3CS;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace AppThing;

public sealed class Window : IDisposable
{
	public delegate void UpdateEventHandler(double delta);
	public delegate void DrawEventHandler(Renderer renderer, double delta);
	public delegate void CloseRequestedEventHandler(Window window);

	public string Title
	{
		get => Sdl.GetWindowTitle(SdlWindowPtr.Value) ?? "";
		set => Sdl.SetWindowTitle(SdlWindowPtr.Value, value);
	}

	public bool Visible
	{
		get => (Sdl.GetWindowFlags(SdlWindowPtr.Value) & Sdl.WindowFlags.Hidden) == 0;
		set
		{
			if (value)
				Sdl.ShowWindow(SdlWindowPtr.Value);
			else
				Sdl.HideWindow(SdlWindowPtr.Value);
		}
	}

	public Size Size
	{
		get
		{
			Sdl.GetWindowSize(SdlWindowPtr.Value, out var width, out var height);
			return new Size(width, height);
		}
		set => Sdl.SetWindowSize(SdlWindowPtr.Value, value.Width, value.Height);
	}

	public event CloseRequestedEventHandler? CloseRequested;
	public event UpdateEventHandler? Update;
	public event DrawEventHandler? Draw;

	public Renderer Renderer { get; }

	internal Sdl.WindowID SdlWindowId { get; }

	internal readonly Sdl.Ptr<Sdl.Window> SdlWindowPtr;

	private readonly Thread _renderThread;
	private volatile bool _keepRenderThreadAlive = false;

	private bool _disposed;

	public Window(string title, Size size)
	{
		_renderThread = new(RenderThreadProc);

		LibraryManager.EnsureSdl(Sdl.InitFlags.Video);

		Sdl.GL_SetAttribute(Sdl.GLAttr.DoubleBuffer, 1);
		Sdl.GL_SetAttribute(Sdl.GLAttr.ContextMajorVersion, 3);
		Sdl.GL_SetAttribute(Sdl.GLAttr.ContextMinorVersion, 3);
		Sdl.GL_SetAttribute(Sdl.GLAttr.ContextFlags, Sdl.GLContextFlags.ForwardCompatible);
		Sdl.GL_SetAttribute(Sdl.GLAttr.ContextProfileMask, Sdl.GLProfile.Core);
		Sdl.GL_SetAttribute(Sdl.GLAttr.StencilSize, 8);

		// Create window and get ID
		SdlWindowPtr = Sdl.CreateWindow(title, size.Width, size.Height, Sdl.WindowFlags.Hidden | Sdl.WindowFlags.OpenGL | Sdl.WindowFlags.Resizable);
		if (SdlWindowPtr.IsNull)
		{
			throw new($"Failed to create window: {Sdl.GetError()}");
		}
		SdlWindowId = Sdl.GetWindowID(SdlWindowPtr.Value);

		// Initialize renderer
		Renderer = new(this);

		App.RegisterWindow(this);

		StartRenderThread();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void StartRenderThread()
	{
		_keepRenderThreadAlive = true;
		_renderThread.Start();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void StopRenderThread()
	{
		_keepRenderThreadAlive = false;
		_renderThread.Join();
	}

	private long _lastUpdate = Stopwatch.GetTimestamp();
	private long _lastDraw = Stopwatch.GetTimestamp();

	internal void RenderThreadProc()
	{
		Renderer.MakeCurrent();

		while (_keepRenderThreadAlive)
		{
			var thisTime = Stopwatch.GetTimestamp();
			var deltaTime = (thisTime - _lastDraw) / (double)Stopwatch.Frequency;

			Renderer.BeginFrame();
			
			Renderer.Clear();
			Draw?.Invoke(Renderer, deltaTime);
			
			Renderer.EndFrame();
			
			_lastDraw = thisTime;
		}

		Renderer.DoneCurrent();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void HandlePixelSizeChanged(Size size) => Renderer.SetSize(size);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void HandleCloseRequested() => CloseRequested?.Invoke(this);

	internal void HandleUpdate()
	{
		var thisTime = Stopwatch.GetTimestamp();
		var deltaTime = (thisTime - _lastUpdate) / (double)Stopwatch.Frequency;

		Update?.Invoke(deltaTime);

		_lastUpdate = thisTime;
	}

	private void Dispose(bool disposing)
	{
		if (_disposed)
			return;
		
		Console.WriteLine($"[Window] Waiting for render thread to finish before disposing...");
		StopRenderThread();
		App.UnregisterWindow(this);

		if (disposing)
			Renderer.Dispose();

		Sdl.DestroyWindow(SdlWindowPtr.Value);

		_disposed = true;
	}

	~Window() => Dispose(disposing: false);

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}

using SDL3CS;

namespace AppThing;

public static class App
{
	private static bool _running = false;

	private static readonly List<Window> _windows = [];
	private static readonly Dictionary<Sdl.WindowID, Window> _windowsById = [];

	public static void Run()
	{
		if (_running)
			return;

		_running = true;
		LibraryManager.EnsureSdl(Sdl.InitFlags.Events);
		while (_running)
		{
			while (Sdl.PollEvent(out var ev))
			{
				switch (ev.Type)
				{
					case Sdl.EventType.WindowCloseRequested:
						{
							var window = _windowsById[ev.Window.WindowID];
							window.HandleCloseRequested();
							break;
						}
					case Sdl.EventType.WindowPixelSizeChanged:
						{
							var window = _windowsById[ev.Window.WindowID];
							window.HandlePixelSizeChanged(new(ev.Window.Data1, ev.Window.Data2));
							break;
						}
				}
			}

			foreach (var window in _windows)
				window.HandleUpdate();
		}
	}

	public static void Quit()
	{
		_running = false;
	}

	internal static void RegisterWindow(Window window)
	{
		_windows.Add(window);
		_windowsById.Add(window.SdlWindowId, window);
	}

	internal static void UnregisterWindow(Window window)
	{
		_windows.Remove(window);
		_windowsById.Remove(window.SdlWindowId);
	}
}

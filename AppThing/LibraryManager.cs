using SDL3CS;
using System.Reflection;
using System.Runtime.InteropServices;

namespace AppThing;

internal static class LibraryManager
{
	private static readonly Dictionary<string, nint> _loadedLibraries = [];

	private static readonly Lock _sdlInitLock = new();
	private static Sdl.InitFlags _sdlInit = 0;
	private static bool _sdlQuitRegistered = false;

	public static void EnsureSdl(Sdl.InitFlags initFlags)
	{
		initFlags = (initFlags | Sdl.InitFlags.Events) & ~_sdlInit;
		if (initFlags == 0)
			return;

		lock (_sdlInitLock)
		{
			if (!_sdlQuitRegistered)
			{
				NativeLibrary.SetDllImportResolver(typeof(Sdl).Assembly, ImportResolver);
				AppDomain.CurrentDomain.ProcessExit += (_, _) => Sdl.Quit();
				_sdlQuitRegistered = true;
			}

			if (!Sdl.Init(initFlags))
				throw new InvalidOperationException($"Failed to initialize SDL: {Sdl.GetError()}");

			_sdlInit |= initFlags;
		}
	}

	public static nint ImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
	{
		if (_loadedLibraries.TryGetValue(libraryName, out var handle))
			return handle;

		var rid = RuntimeInformation.RuntimeIdentifier;
		var prefix =
			OperatingSystem.IsWindows() ? "" :
			OperatingSystem.IsLinux() ? "lib" :
			OperatingSystem.IsMacOS() ? "lib" :
			throw new PlatformNotSupportedException();

		var suffix =
			OperatingSystem.IsWindows() ? ".dll" :
			OperatingSystem.IsLinux() ? ".so" :
			OperatingSystem.IsMacOS() ? ".dylib" :
			throw new PlatformNotSupportedException();

		var fullName = $"{prefix}{libraryName}{suffix}";
		var path = Path.Combine("runtimes", rid, "native", fullName);
		var appPath = Path.Combine(AppContext.BaseDirectory, path);

		nint libraryHandle;

		if ((File.Exists(path) && (libraryHandle = NativeLibrary.Load(path)) != 0) ||
		    (File.Exists(appPath) && (libraryHandle = NativeLibrary.Load(path)) != 0))
		{
			Console.WriteLine($"[LibraryManager] Loaded native library {libraryName} from {path}");
			_loadedLibraries.Add(libraryName, libraryHandle);
			return libraryHandle;
		}

		return 0;
	}
}

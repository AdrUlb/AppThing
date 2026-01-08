using AppThing;
using System.Drawing;

namespace ConsoleApp1;

internal static class Program
{
	private static void Main(string[] args)
	{
		using var window = new Window("Test Window", new(1600, 900));

		window.Renderer.ClearColor = Color.FromArgb(0, 43, 54);

		const string fontFile = @"/usr/share/fonts/TTF/times.ttf";
		var fontSize = 40.0f;

		var font = BitmapFont.FromFile(fontFile, fontSize);
		//var font = VectorFont.FromFile(fontFile);

		var textColor = Color.FromArgb(147, 161, 161);

		window.Visible = true;

		var frameCount = 0;
		var accumulator = 0.0;

		var fpsString = "FPS: N/A";

		window.Draw += (renderer, delta) =>
		{
			accumulator += delta;
			frameCount++;

			if (accumulator >= 0.5)
			{
				fpsString = $"FPS: {frameCount / accumulator:F2}";
				accumulator = 0;
				frameCount = 0;
			}

			var str = $"{fpsString}\nHello, World!\nTesting 123...\nThe quick brown fox jumps over the lazy dog.\nFranz jagt im komplett verwahrlosten Taxi quer durch Bayern.";
			//var str = "ABCDEFGHIJKLMNOPQRSTUVWXYZ\nabcdefghijklmnopqrstuvwxyz\n0123456789\n!@#$%^&*()_+-=[]{}|;':\",.<>/?`~";
			//renderer.DrawText("Hello, World!\nTest", new(0, 0), font, textColor);

			renderer.DrawText(str, new(10, 0), font, textColor);
			//renderer.DrawText(str, new(10, 0), font, fontSize, textColor);

			//fontSize += (float)delta * 20f;
			Console.WriteLine(fpsString + ", Font Size: " + fontSize);
		};

		window.CloseRequested += w =>
		{
			w.Visible = false;
			App.Quit();
		};

		App.Run();
	}
}

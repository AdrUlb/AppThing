using AppThing;
using FontThing.TrueType;
using FontThing.TrueType.Parsing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using Color = System.Drawing.Color;
using Point = System.Drawing.Point;

namespace ConsoleApp1;

internal static class Program
{
	/*
	private static void Main(string[] args)
	{
		using var window = new Window("Test Window", new(1280, 720));

		window.Renderer.ClearColor = Color.FromArgb(0, 43, 54);

		var font = TrueTypeFont.FromFile("/usr/share/fonts/TTF/times.ttf");

		window.Visible = true;

		var lastTimestamp = Stopwatch.GetTimestamp();

		window.Draw += (renderer, delta) =>
		{
			var thisTimestamp = Stopwatch.GetTimestamp();
			var elapsedSeconds = (thisTimestamp - lastTimestamp) / (double)Stopwatch.Frequency;
			lastTimestamp = thisTimestamp;

			renderer.Test(font);
		};

		window.CloseRequested += w =>
		{
			w.Visible = false;
			App.Quit();
		};

		App.Run();
	}
	*/

	private static void Main(string[] args)
	{
		using var window = new Window("Test Window", new(1280, 720));

		window.Renderer.ClearColor = Color.FromArgb(0, 43, 54);

		const string fontFile = "/usr/share/fonts/TTF/calibri.ttf";
		var fontSize = 50.0f;

		//var font = BitmapFont.FromFile(fontFile, fontSize);
		var font = VectorFont.FromFile(fontFile);

		var textColor = Color.FromArgb(147, 161, 161);

		window.Visible = true;

		var lastTimestamp = Stopwatch.GetTimestamp();

		window.Draw += (renderer, delta) =>
		{
			var thisTimestamp = Stopwatch.GetTimestamp();
			var elapsedSeconds = (thisTimestamp - lastTimestamp) / (double)Stopwatch.Frequency;
			lastTimestamp = thisTimestamp;

			var str = $"Frame time: {elapsedSeconds * 1000.0f:F3}ms\nHello, World!\nTesting 123...\nThe quick brown fox jumps over the lazy dog.";
			//var str = "ABCDEFGHIJKLMNOPQRSTUVWXYZ\nabcdefghijklmnopqrstuvwxyz\n0123456789\n!@#$%^&*()_+-=[]{}|;':\",.<>/?`~";
			//renderer.DrawText("Hello, World!\nTest", new(0, 0), font, textColor);

			//renderer.DrawText(str, new(10, 0), font, textColor);
			renderer.DrawText(str, new(10, 0), font, fontSize, textColor);

			//size += delta * 0.01f;
		};

		window.CloseRequested += w =>
		{
			w.Visible = false;
			App.Quit();
		};

		App.Run();
	}
}

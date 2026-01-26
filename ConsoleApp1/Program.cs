using AppThing;
using System.Drawing;

namespace ConsoleApp1;

internal static class Program
{
	private static void Main(string[] args)
	{
		using var window = new Window("Test Window", new(1600, 900));

		window.Renderer.ClearColor = Color.FromArgb(0, 43, 54);

		const string fontFile = @"/usr/share/fonts/TTF/segoeui.ttf";
		var fontSize = 8.0f;

		var font = BitmapFont.FromFile(fontFile, fontSize, BitmapFontFlags.Default);
		var fontSubpixel = BitmapFont.FromFile(fontFile, fontSize, BitmapFontFlags.SubpixelRgb);
		var subpixel = true;
		//var font = VectorFont.FromFile(fontFile);

		var textColor = Color.FromArgb(147, 161, 161);
		//var textColor = Color.White;

		window.Visible = true;

		var frameCount = 0;
		var accumulator = 0.0;

		var text = $"\n\nHello, World!\nTesting 123...\nThe quick brown fox jumps over the lazy dog.\nFranz jagt im komplett verwahrlosten Taxi quer durch Bayern.\nLorem ipsum dolor sit amet, consectetur adipiscing elit.";

		var textLayout = new TextLayout(text, font);
		var textLayoutSubpixel = new TextLayout(text, fontSubpixel);
		var fpsString = "0";

		var solid = new Texture(new(1, 1), Color.White);

		// Calculations for FPS stuff
		window.Draw += (renderer, delta) =>
		{
			accumulator += delta;
			frameCount++;

			var fpsText = $"FPS: {fpsString}\nSubpixel antialiasing: {(subpixel ? "on" : "off")}";
			if (accumulator >= 0.5)
			{
				fpsString = $"{frameCount / accumulator:F2}";
				accumulator = 0;
				frameCount = 0;
			}

			//renderer.DrawText(fpsString, new(10, 0), font, fontSize, textColor);
			//renderer.DrawText(str, new(10, 0), font, fontSize, textColor);

			ref var layout = ref subpixel ? ref textLayoutSubpixel : ref textLayout;
			var f = subpixel ? fontSubpixel : font;

			renderer.DrawText(layout, new(10, 0), textColor);

			var fpsPos = new Point(10, 0);
			var fpsRect = f.MeasureText(fpsText);
			fpsRect.Offset(fpsPos);
			renderer.Draw(solid, fpsRect, Color.FromArgb(20, 20, 20));
			renderer.DrawText(fpsText, fpsPos, f, Color.Orange);

			if (Console.KeyAvailable)
			{
				Console.ReadKey(true);
				subpixel = !subpixel;
			}
		};

		window.CloseRequested += w =>
		{
			w.Visible = false;
			App.Quit();
		};

		App.Run();
	}
}

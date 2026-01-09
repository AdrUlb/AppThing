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
		var fontSize = 11.0f;

		var font = BitmapFont.FromFile(fontFile, fontSize);
		//var font = VectorFont.FromFile(fontFile);

		var textColor = Color.FromArgb(147, 161, 161);

		window.Visible = true;

		var frameCount = 0;
		var accumulator = 0.0;
		var fpsString = "FPS: N/A";

		var text = $"\nFPS: Hello, World!\nTesting 123...\nThe quick brown fox jumps over the lazy dog.\nFranz jagt im komplett verwahrlosten Taxi quer durch Bayern.";

		for (var i = 0; i < 1000; i++)
			text += "\nLorem ipsum dolor sit amet, consectetur adipiscing elit. Praesent pharetra, magna quis convallis feugiat, nisl est condimentum ipsum, sit amet accumsan velit lectus ut orci. Aenean cursus mattis elementum. Mauris gravida orci vel tempor feugiat. In non congue elit.";

		var textLayout = new TextLayout(text);

		// Calculations for FPS stuff
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

			//renderer.DrawText(fpsString, new(10, 0), font, fontSize, textColor);
			//renderer.DrawText(str, new(10, 0), font, fontSize, textColor);

			renderer.DrawText(textLayout, new(10, 0), font, textColor);
			renderer.DrawText(new TextLayout(fpsString), new(10, 0), font, Color.White);
		};

		window.CloseRequested += w =>
		{
			w.Visible = false;
			App.Quit();
		};

		App.Run();
	}
}

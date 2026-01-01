using AppThing;
using System.Diagnostics;
using System.Drawing;

using var window = new Window("Test Window", new(800, 600));

window.Renderer.ClearColor = Color.FromArgb(0, 43, 54);

window.Visible = true;

var lastTimestamp = Stopwatch.GetTimestamp();

var font = RenderFont.FromFile("/usr/share/fonts/TTF/calibri.ttf", 30.0f);
var textColor = Color.FromArgb(147, 161, 161);

window.Draw += (renderer, delta) =>
{
	var thisTimestamp = Stopwatch.GetTimestamp();
	var elapsedSeconds = (thisTimestamp - lastTimestamp) / (double)Stopwatch.Frequency;
	lastTimestamp = thisTimestamp;

	var str = $"Frame time: {elapsedSeconds * 1000.0f:F3}ms\nHello, World!\nTesting 123...\nThe quick brown fox jumps over the lazy dog.";
	renderer.DrawText(str, new(0, 0), font, textColor);
};

window.CloseRequested += w =>
{
	w.Visible = false;
	App.Quit();
};

App.Run();

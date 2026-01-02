using AppThing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using Color = System.Drawing.Color;

using var window = new Window("Test Window", new(1280, 720));

window.Renderer.ClearColor = Color.FromArgb(0, 43, 54);

window.Visible = true;

var lastTimestamp = Stopwatch.GetTimestamp();

var font = BitmapFont.FromFile("/usr/share/fonts/TTF/times.ttf", 40.0f);
var textColor = Color.FromArgb(147, 161, 161);

window.Draw += (renderer, delta) =>
{
	var thisTimestamp = Stopwatch.GetTimestamp();
	var elapsedSeconds = (thisTimestamp - lastTimestamp) / (double)Stopwatch.Frequency;
	lastTimestamp = thisTimestamp;

	var str = $"Frame time: {elapsedSeconds * 1000.0f:F3}ms\nHello, World!\nTesting 123...\nThe quick brown fox jumps over the lazy dog.";
	renderer.DrawText(str, new(20, 0), font, textColor);
	//renderer.Draw(font._textureAtlases[0].Texture, new Point(0, 0));
};

window.CloseRequested += w =>
{
	w.Visible = false;
	App.Quit();
};

App.Run();

for (var i = 0; i < font._textureAtlases.Count; i++)
{
	var atlas = font._textureAtlases[i];
	var path = $"atlas{i}.png";
	var image = new Image<Rgba32>(atlas.Texture.Size.Width, atlas.Texture.Size.Height);
	image.ProcessPixelRows(acc =>
	{
		var span = atlas.Texture.UnsafeGetPixelsSpan();
		for (var y = 0; y < acc.Height; y++)
		{
			var row = acc.GetRowSpan(y);
			for (var x = 0; x < acc.Width; x++)
			{
				var color = span[x + (y * acc.Width)];
				row[x] = new Rgba32(color.R, color.G, color.B, color.A);
			}
		}
	});

	image.Save(path);
}

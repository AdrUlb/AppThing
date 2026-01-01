using AppThing;
using FontThing.TrueType;
using FontThing.TrueType.Parsing;
using System.Diagnostics;
using System.Drawing;
using System.Text;

using var window = new Window("Test Window", new(1280, 720));

window.Renderer.ClearColor = Color.FromArgb(0, 43, 54);

TrueTypeFont font;
using (var stream = File.OpenRead("/usr/share/fonts/TTF/calibri.ttf"))
	font = new(stream);

const float pointSize = 60.0f;

var scale = font.PointSizeToScale(pointSize);

var lineHeight = font.Ascent - font.Descent + font.LineGap;
var baseline = font.Ascent;

window.Visible = true;

var lastTimestamp = Stopwatch.GetTimestamp();

window.Draw += (renderer, delta) =>
{
	var thisTimestamp = Stopwatch.GetTimestamp();
	var elapsedSeconds = (thisTimestamp - lastTimestamp) / (double)Stopwatch.Frequency;
	lastTimestamp = thisTimestamp;

	var penX = 0.0f;
	var penY = 0.0f;

	foreach (var c in $"Frame time: {elapsedSeconds:F3}s\nHello, World!\nTesting 123...\nThe quick brown fox jumps over the lazy dog.")
	{
		if (c == '\n')
		{
			penX = 0;
			penY -= lineHeight;
			continue;
		}

		var metrics = font.GetLongHorMetrics(c);

		var glyph = font.LoadGlyph(c);

		if (glyph.Outline != null)
		{
			var glyphXPrecise = (penX + glyph.Outline.XMin) * scale;
			var glyphYPrecise = (penY + glyph.Outline.YMin - baseline) * scale;

			var glyphX = (int)glyphXPrecise;
			var glyphY = (int)glyphYPrecise;

			var subX = float.Round(glyphXPrecise - glyphX, 1);
			var subY = float.Round(glyphYPrecise - glyphY, 1);
			var bitmap = glyph.Outline.Render(pointSize, subpixelOffsetX: subX, subpixelOffsetY: subY);

			var drawX = glyphX;
			var drawY = -glyphY - bitmap.Size.Height;

			using var texture = new Texture(bitmap.Size, Color.Black);

			texture.AccessPixels((pixels, size) =>
			{
				for (var y = 0; y < bitmap.Size.Height; y++)
				{
					for (var x = 0; x < bitmap.Size.Width; x++)
					{
						var a = bitmap.Data[x + (size.Height - y - 1) * size.Width];
						pixels[x + y * size.Width] = Color.FromArgb(a, 147, 161, 161);
					}
				}
			});

			renderer.Draw(texture, new RectangleF(new(drawX, drawY), texture.Size));
		}

		penX += metrics.AdvanceWidth;
	}
};

window.CloseRequested += w =>
{
	w.Visible = false;
	App.Quit();
};

App.Run();

using AppThing;
using FontThing.TrueType;
using FontThing.TrueType.Parsing;
using System.Drawing;

using var window = new Window("Test Window", new(1280, 720));

window.Renderer.ClearColor = Color.FromArgb(0, 43, 54);

TrueTypeFont font;
using (var stream = File.OpenRead("/usr/share/fonts/TTF/comic.ttf"))
	font = new(stream);

const float pointSize = 40.0f;
const int supersamples = 16;
const float bezierTolerance = 0.01f;

var scale = font.PointSizeToScale(pointSize);

var lineHeight = font.Ascent - font.Descent + font.LineGap;
var baseline = font.Ascent;
var stemDarkeningAmount = TrueTypeRenderer.CalculateStemDarkeningAmount(font.GetPixelsPerEm(pointSize));

window.Visible = true;
window.Draw += (renderer, delta) =>
{
	var penX = 0.0f;
	var penY = 0.0f;

	foreach (var c in "Hello, World!\nTesting 123...\nThe quick brown fox jumps over the lazy dog.")
	{
		if (c == '\n')
		{
			penX = 0;
			penY -= lineHeight;
			continue;
		}

		var outline = font.GetGlyphOutline(c);
		var metrics = font.GetLongHorMetrics(c);

		if (outline != null)
		{
			var unscaledBounds = outline.GetScaledBounds(1.0f);

			var glyphX = (penX + unscaledBounds.X) * scale;
			var glyphY = (penY + unscaledBounds.Y) * scale;

			var glyphXInt = (int)glyphX;
			var glyphYInt = (int)glyphY;

			var subpixelOffsetX = float.Round(glyphX - glyphXInt, 2);
			var subpixelOffsetY = float.Round(glyphY - glyphYInt, 2);

			var renderedGlyph = TrueTypeRenderer.Render(outline, scale, supersamples, bezierTolerance, subpixelOffsetX, subpixelOffsetY, stemDarkeningAmount);

			var drawX = glyphXInt;
			var drawY = (int)(baseline * scale - renderedGlyph.AlphaSize.Height - glyphYInt);

			using var texture = new Texture(renderedGlyph.AlphaSize, Color.Black);

			texture.AccessPixels((pixels, size) =>
			{
				for (var y = 0; y < renderedGlyph.AlphaSize.Height; y++)
				{
					for (var x = 0; x < renderedGlyph.AlphaSize.Width; x++)
					{
						var a = renderedGlyph.Alpha[x + (size.Height - y - 1) * size.Width];
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

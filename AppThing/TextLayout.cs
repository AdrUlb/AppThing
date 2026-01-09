using System.Drawing;
using System.Runtime.InteropServices;

namespace AppThing;

public class TextLayout(string text)
{
	internal readonly struct BitmapChar(Point dest, BitmapFont.GlyphTexture glyphTexture)
	{
		public readonly Point Dest = dest;
		public readonly BitmapFont.GlyphTexture GlyphTexture = glyphTexture;
	}

	public readonly string Text = text;

	private List<BitmapChar>? _chars;

	internal ReadOnlySpan<BitmapChar> GetChars(BitmapFont font, Size size = default)
	{
		if (_chars != null)
			return CollectionsMarshal.AsSpan(_chars);

		var chars = new List<BitmapChar>(Text.Length);
		ComputeLayout(Text, font, chars, size);
		_chars = chars;
		return CollectionsMarshal.AsSpan(chars);
	}

	internal static void ComputeLayout(string text, BitmapFont font, List<BitmapChar> chars, Size size = default)
	{
		var penX = 0L;
		var penY = 0L;
		
		foreach (var c in text.EnumerateRunes())
		{
			if (!font.TryGetGlyph(c, ref penX, ref penY, out var fontGlyph, out var drawPos))
				continue;

			if (size != default)
			{
				if (drawPos.Y >= size.Height)
					break;

				if (drawPos.X >= size.Width)
					continue;
			}

			chars.Add(new(drawPos, fontGlyph));
		}
	}
}

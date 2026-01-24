using System.Drawing;
using System.Runtime.InteropServices;

namespace AppThing;

public class TextLayout(string text, BitmapFont font)
{
	internal readonly struct BitmapChar(Point dest, BitmapFont.GlyphTexture glyphTexture)
	{
		public readonly Point Dest = dest;
		public readonly BitmapFont.GlyphTexture GlyphTexture = glyphTexture;
	}

	public readonly string Text = text;
	public readonly BitmapFont Font = font;

	private List<BitmapChar>? _chars;
	private Size _size = Size.Empty;

	internal ReadOnlySpan<BitmapChar> GetChars(Size size)
	{
		if (_chars != null && size.Width < _size.Width && size.Height < _size.Height)
			return CollectionsMarshal.AsSpan(_chars);

		var chars = new List<BitmapChar>(Text.Length);
		_size = size;
		ComputeLayout(Text, Font, chars, size);
		_chars = chars;
		return CollectionsMarshal.AsSpan(chars);
	}

	internal static void ComputeLayout(string text, BitmapFont font, List<BitmapChar> chars, Size size)
	{
		var measurer = new TextPen();
		foreach (var c in text.EnumerateRunes())
		{
			if (!font.TryGetGlyph(c, ref measurer, out var fontGlyph, out var drawPos))
				continue;

			if (drawPos.Y >= size.Height)
				break;

			if (drawPos.X >= size.Width)
				continue;

			chars.Add(new(drawPos, fontGlyph));
		}
	}
}

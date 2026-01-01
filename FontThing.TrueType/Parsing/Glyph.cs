using System.Drawing;
using System.Text;

namespace FontThing.TrueType.Parsing;

public sealed class GlyphBitmap(byte[] data, Size size)
{
	public readonly byte[] Data = data;
	public readonly Size Size = size;
}

public readonly struct Glyph
{
	public readonly TrueTypeFont Font;
	public readonly Rune Character;
	public readonly GlyphOutline? Outline;
	public readonly uint AdvanceWidth;
	public readonly short LeftSideBearing;

	public Glyph(TrueTypeFont font, Rune character, GlyphOutline? outline)
	{
		Font = font;
		Character = character;
		Outline = outline;
		var metrics = font.GetLongHorMetrics((uint)character.Value);
		AdvanceWidth = metrics.AdvanceWidth;
		LeftSideBearing = metrics.LeftSideBearing;
	}
}

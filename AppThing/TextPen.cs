using FontThing.Parsing;

namespace AppThing;

internal ref struct TextPen()
{
	public long X = 0;
	public long Y = 0;
	private bool _newline = true;
	public int Left = int.MaxValue;
	public long Width = 0;

	public bool PrepareGlyph(Glyph glyph)
	{
		if (glyph.Character.Value == '\n')
		{
			X = 0;
			Y -= glyph.Font.LineHeight;
			_newline = true;
			return false;
		}

		return true;
	}

	public void DoGlyph(Glyph glyph, out long x, out long y)
	{
		if (_newline)
		{
			_newline = false;
			if (glyph.Outline != null)
			{
				if (glyph.Outline.XMin < Left)
					Left = glyph.Outline.XMin;
			}
		}

		if (glyph.Outline != null)
			Width = X + glyph.Outline.XMax - Left;

		x = X;
		y = Y;

		X += glyph.AdvanceWidth;
	}
}

using System.Drawing;

namespace FontThing.TrueType.Parsing;

[Flags]
public enum GlyphOutlineRenderOptions
{
	None = 0,
	StemDarkening = 1 << 0,
	GammaCorrection = 1 << 1,
	Default = StemDarkening | GammaCorrection,
}

public abstract class GlyphOutline(TrueTypeFont font, short xMin, short yMin, short xMax, short yMax)
{
	public readonly TrueTypeFont Font = font;
	public readonly short XMin = xMin;
	public readonly short YMin = yMin;
	public readonly short XMax = xMax;
	public readonly short YMax = yMax;

	public RectangleF GetBounds(float scale = 1.0f) => new(
		XMin * scale,
		YMin * scale,
		float.Ceiling((XMax - XMin) * scale),
		float.Ceiling((YMax - YMin) * scale)
	);

	public GlyphBitmap Render(float pointSize, GlyphOutlineRenderOptions options = GlyphOutlineRenderOptions.Default, int supersamples = 8, float bezierTolerance = 0.01f, float subpixelOffsetX = 0.0f, float subpixelOffsetY = 0.0f)
	{
		var scale = Font.PointSizeToScale(pointSize);
		var stemDarkening = (options & GlyphOutlineRenderOptions.StemDarkening) != 0 ? TrueTypeFontRasterizer.CalculateStemDarkening(Font.GetPixelsPerEm(pointSize)) : 0;

		var gamma = (options & GlyphOutlineRenderOptions.GammaCorrection) != 0 ? 1.2f : 1.0f;

		return TrueTypeFontRasterizer.RenderGlyph(
			this,
			scale,
			supersamples,
			bezierTolerance,
			subpixelOffsetX,
			subpixelOffsetY,
			stemDarkening,
			gamma);
	}
}

public sealed class SimpleGlyphOutline(TrueTypeFont font, short xMin, short yMin, short xMax, short yMax, ushort[] endPointsOfContours, byte[] instructions, GlyphPoint[] points) : GlyphOutline(font, xMin, yMin, xMax, yMax)
{
	public readonly ushort[] EndPointsOfContours = endPointsOfContours;
	public readonly byte[] Instructions = instructions;
	public readonly GlyphPoint[] Points = points;
}

public readonly struct CompoundGlyphComponent(ushort glyphIndex, int arg1, int arg2, bool argsAreXyValues, F2Dot14 scaleX, F2Dot14 scale01, F2Dot14 scale10, F2Dot14 scaleY)
{
	public readonly ushort GlyphIndex = glyphIndex;
	public readonly int Arg1 = arg1;
	public readonly int Arg2 = arg2;
	public readonly bool ArgsAreXyValues = argsAreXyValues;
	public readonly F2Dot14 ScaleX = scaleX;
	public readonly F2Dot14 Scale01 = scale01;
	public readonly F2Dot14 Scale10 = scale10;
	public readonly F2Dot14 ScaleY = scaleY;
}

public sealed class CompoundGlyphOutline(TrueTypeFont font, short xMin, short yMin, short xMax, short yMax, CompoundGlyphComponent[] components, byte[]? instructions) : GlyphOutline(font, xMin, yMin, xMax, yMax)
{
	public readonly CompoundGlyphComponent[] Components = components;
	public readonly byte[]? Instructions = instructions;
}

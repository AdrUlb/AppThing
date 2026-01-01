using System.Drawing;

namespace FontThing.TrueType.Parsing;

public abstract class GlyphOutline(short xMin, short yMin, short xMax, short yMax)
{
	public readonly short XMin = xMin;
	public readonly short YMin = yMin;
	public readonly short XMax = xMax;
	public readonly short YMax = yMax;

	public RectangleF GetScaledBounds(float scale) => new(
		XMin * scale,
		YMin * scale,
		float.Ceiling((XMax - XMin) * scale),
		float.Ceiling((YMax - YMin) * scale)
	);
}

public sealed class SimpleGlyphOutline(short xMin, short yMin, short xMax, short yMax, ushort[] endPointsOfContours, byte[] instructions, GlyphPoint[] points) : GlyphOutline(xMin, yMin, xMax, yMax)
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

public sealed class CompoundGlyphOutline(short xMin, short yMin, short xMax, short yMax, CompoundGlyphComponent[] components, byte[]? instructions) : GlyphOutline(xMin, yMin, xMax, yMax)
{
	public readonly CompoundGlyphComponent[] Components = components;
	public readonly byte[]? Instructions = instructions;
}

namespace FontThing.Parsing.Tables;

internal interface ICmapFormat
{
	public uint GetGlyphIndex(uint codePoint);
}

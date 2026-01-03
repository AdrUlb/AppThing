using FontThing.TrueType.Parsing;
using System.Numerics;

namespace AppThing;

public sealed class VectorFont
{
	internal readonly TrueTypeFont Ttf;


	private VectorFont(TrueTypeFont ttf)
	{
		Ttf = ttf;
	}

	public static VectorFont FromFile(string path)
	{
		using var fs = File.OpenRead(path);
		return new(new(fs));
	}
}

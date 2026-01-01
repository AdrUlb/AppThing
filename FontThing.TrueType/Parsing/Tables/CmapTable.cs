using System.Buffers.Binary;
using UtilThing;

namespace FontThing.TrueType.Parsing.Tables;

// https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6cmap.html

internal enum PlatformId : ushort
{
	Unicode = 0,
	Macintosh = 1,
	Windows = 3
}

internal enum UnicodePlatformSpecificId : ushort
{
	Version10Semantics = 0,
	Version11Semantics = 1,
	Iso106461993Semantics = 2,
	Unicode20OrLaterSemanticsBmpOnly = 3,
	Unicode20OrLaterSemantics = 4,
	UnicodeVariationSequences = 5,
	LastResort = 6
}

internal enum WindowsPlatformSpecificId : ushort
{
	Symbol = 0,
	UnicodeBmp = 1,
	ShiftJis = 2,
	Prc = 3,
	Big5 = 4,
	Wansung = 5,
	Johab = 6,
	UnicodeFull = 10
}

internal sealed class CmapTable
{
	public static readonly uint TagValue = BinaryPrimitives.ReadUInt32BigEndian("cmap"u8);
	private readonly ICmapFormat _format;

	public CmapTable(StreamPrimitiveReader reader)
	{
		// Version
		reader.ReadU16();

		var numberSubTables = reader.ReadU16();

		List<(PlatformId PlatformId, ushort PlatformSpecificId, uint Offset)> encodings = [];

		for (var i = 0; i < numberSubTables; i++)
		{
			var platformId = (PlatformId)reader.ReadU16();
			var platformSpecificId = reader.ReadU16();
			var offset = reader.ReadU32();

			encodings.Add((platformId, platformSpecificId, offset));
		}

		var platform = encodings.OrderBy(p => GetPlatformSubtablePriority(p.PlatformId, p.PlatformSpecificId)).First();
		if (GetPlatformSubtablePriority(platform.PlatformId, platform.PlatformSpecificId) == int.MaxValue)
			throw new("No supported 'cmap' platform found.");

		reader.BaseStream.Position = platform.Offset;
		var platformFormat = reader.ReadU16();
		uint platformLength = reader.ReadU16();

		if (platformFormat == 12)
		{
			if (platformLength != 0)
				throw new FormatException();

			platformLength = reader.ReadU32();
		}

		using var formatReader = new StreamPrimitiveReader(reader.BaseStream.SubStream(reader.BaseStream.Position, platformLength - 4), reader.LittleEndian);

		_format = platformFormat switch {
			4 => new CmapFormat4(formatReader),
			12 => new CmapFormat12(formatReader),
			_ => throw new NotSupportedException($"Unsupported platform format {platformFormat}.")
		};
	}

	public uint GetGlyphIndex(uint codePoint) => _format.GetGlyphIndex(codePoint);

	private static int GetPlatformSubtablePriority(PlatformId platformId, ushort platformSpecificId) =>
		(platformId, platformSpecificId) switch {
			(PlatformId.Unicode, (ushort)UnicodePlatformSpecificId.Unicode20OrLaterSemantics) => 0,
			(PlatformId.Windows, (ushort)WindowsPlatformSpecificId.UnicodeFull) => 1,
			(PlatformId.Unicode, (ushort)UnicodePlatformSpecificId.Unicode20OrLaterSemanticsBmpOnly) => 2,
			(PlatformId.Windows, (ushort)WindowsPlatformSpecificId.UnicodeBmp) => 3,
			_ => int.MaxValue
		};
}

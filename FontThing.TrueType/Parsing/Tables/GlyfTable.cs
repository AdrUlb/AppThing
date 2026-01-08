using System.Buffers.Binary;
using System.Collections.Frozen;
using UtilThing;

namespace FontThing.TrueType.Parsing.Tables;

[Flags]
internal enum SimpleGlyphOutlineFlags : byte
{
	OnCurve = 1 << 0,
	XShortVector = 1 << 1,
	YShortVector = 1 << 2,
	Repeat = 1 << 3,
	SameXOrPositiveXShortVector = 1 << 4,
	SameYOrPositiveYShortVector = 1 << 5,
}

[Flags]
internal enum CompoundGlyphOutlineFlags : ushort
{
	Arg1And2AreWords = 1 << 0,
	ArgsAreXYValues = 1 << 1,
	RoundXYToGrid = 1 << 2,
	WeHaveAScale = 1 << 3,
	MoreComponents = 1 << 5,
	WeHaveAnXAndYScale = 1 << 6,
	WeHaveATwoByTwo = 1 << 7,
	WeHaveInstructions = 1 << 8,
	UseMyMetrics = 1 << 9,
	OverlapCompound = 1 << 10,

	// Additonally from the OpenType docs:
	ScaledComponentOffset = 1 << 11, // Bit 11: The composite is designed to have the component offset scaled. Ignored if ArgsAreXYValues is not set.
	UnscaledComponentOffset = 1 << 12 // Bit 12: The composite is designed not to have the component offset scaled. Ignored if ArgsAreXYValues is not set.
}

internal sealed class GlyfTable
{
	public static readonly uint TagValue = BinaryPrimitives.ReadUInt32BigEndian("glyf"u8);

	public readonly FrozenDictionary<uint, GlyphOutline> OutlinesByIndex;
	public readonly FrozenDictionary<uint, GlyphOutline> OutlinesByLocation;

	public GlyfTable(StreamPrimitiveReader reader, TrueTypeFont font, LocaTable locaTable)
	{
		var outlinesByIndex = new Dictionary<uint, GlyphOutline>();
		var outlinesByLocation = new Dictionary<uint, GlyphOutline>();

		var simpleOutlineFlags = new List<SimpleGlyphOutlineFlags>();

		for (uint locaIndex = 0; locaIndex < locaTable.Offsets.Length - 1; locaIndex++)
		{
			var locaOffset = locaTable.Offsets[locaIndex];
			var nextLocaOffset = locaTable.Offsets[locaIndex + 1];
			reader.BaseStream.Position = locaOffset;
			if (locaOffset == nextLocaOffset)
				continue;

			var numberOfContours = reader.ReadI16();
			var xMin = reader.ReadI16();
			var yMin = reader.ReadI16();
			var xMax = reader.ReadI16();
			var yMax = reader.ReadI16();

			GlyphOutline glyph;

			if (numberOfContours >= 0) // If the number of contours is positive or zero, it is a single glyph 
			{
				var endPointsOfContours = new ushort[numberOfContours];
				for (var i = 0; i < endPointsOfContours.Length; i++)
					endPointsOfContours[i] = reader.ReadU16();

				var instructions = new byte[reader.ReadU16()];
				for (var i = 0; i < instructions.Length; i++)
					instructions[i] = reader.ReadU8();

				var pointCount = endPointsOfContours[^1] + 1;

				var points = new GlyphOutlinePoint[pointCount];

				simpleOutlineFlags.Clear();

				for (var i = 0; i < points.Length; i++)
				{
					var flags = (SimpleGlyphOutlineFlags)reader.ReadU8();
					simpleOutlineFlags.Add(flags);

					points[i].OnCurve = (flags & SimpleGlyphOutlineFlags.OnCurve) != 0;

					if ((flags & SimpleGlyphOutlineFlags.Repeat) == 0)
						continue;

					var repeatCount = reader.ReadU8();

					while (repeatCount-- > 0)
					{
						i++;
						points[i].OnCurve = (flags & SimpleGlyphOutlineFlags.OnCurve) != 0;
						simpleOutlineFlags.Add(flags);
					}
				}

				for (var i = 0; i < points.Length; i++)
				{
					var f = simpleOutlineFlags[i];
					var prev = i != 0 ? points[i - 1].X : (short)0;

					// If set, the corresponding x-coordinate is 1 byte long
					if ((f & SimpleGlyphOutlineFlags.XShortVector) != 0)
					{
						short value = reader.ReadU8();
						// If the x-Short Vector bit is set, this bit describes the sign of the value, with a value of 1 equalling positive and a zero value negative.
						if ((f & SimpleGlyphOutlineFlags.SameXOrPositiveXShortVector) == 0)
							value *= -1;

						points[i].X = value;
					}
					// Otherwise, the corresponding x-coordinate is 2 bytes long
					else
					{
						// If the x-short Vector bit is not set, and this bit is set, then the current x-coordinate is the same as the previous x-coordinate.
						if ((f & SimpleGlyphOutlineFlags.SameXOrPositiveXShortVector) != 0)
						{
							points[i].X = 0;
						}
						// If the x-short Vector bit is not set, and this bit is not set, the current x-coordinate is a signed 16-bit delta vector.
						// In this case, the delta vector is the change in x
						else
						{
							points[i].X = reader.ReadI16();
						}
					}

					points[i].X += prev;
				}

				for (var i = 0; i < points.Length; i++)
				{
					var flags = simpleOutlineFlags[i];
					var prev = i != 0 ? points[i - 1].Y : (short)0;

					// If set, the corresponding x-coordinate is 1 byte long
					if ((flags & SimpleGlyphOutlineFlags.YShortVector) != 0)
					{
						short value = reader.ReadU8();
						// If the y-Short Vector bit is set, this bit describes the sign of the value, with a value of 1 equalling positive and a zero value negative.
						if ((flags & SimpleGlyphOutlineFlags.SameYOrPositiveYShortVector) == 0)
							value *= -1;

						points[i].Y = value;
					}
					// Otherwise, the corresponding y-coordinate is 2 bytes long
					else
					{
						// If the x-short Vector bit is not set, and this bit is set, then the current x-coordinate is the same as the previous x-coordinate.
						if ((flags & SimpleGlyphOutlineFlags.SameYOrPositiveYShortVector) != 0)
						{
							points[i].Y = 0;
						}
						// If the x-short Vector bit is not set, and this bit is not set, the current x-coordinate is a signed 16-bit delta vector.
						// In this case, the delta vector is the change in x
						else
						{
							points[i].Y = reader.ReadI16();
						}
					}

					points[i].Y += prev;
				}

				glyph = new SimpleGlyphOutline(font, xMin, yMin, xMax, yMax, endPointsOfContours, instructions, points);
			}
			else // If the number of contours less than zero, the glyph is compound
			{
				CompoundGlyphOutlineFlags flags;

				var components = new List<CompoundGlyphComponent>();

				do
				{
					flags = (CompoundGlyphOutlineFlags)reader.ReadU16();
					var glyphIndex = reader.ReadU16();

					var arg1And2AreWords = (flags & CompoundGlyphOutlineFlags.Arg1And2AreWords) != 0;
					var argsAreXyValues = (flags & CompoundGlyphOutlineFlags.ArgsAreXYValues) != 0;

					var scaleX = F2Dot14.One;
					var b = F2Dot14.Zero;
					var c = F2Dot14.Zero;
					var scaleY = F2Dot14.One;
					var arg1 = -1;
					var arg2 = -1;

					if (arg1And2AreWords && argsAreXyValues)
					{
						// 1st short contains the value of e
						arg1 = reader.ReadI16();
						// 2nd short contains the value of f
						arg2 = reader.ReadI16();
					}
					else if (!arg1And2AreWords && argsAreXyValues)
					{
						// 1st byte contains the value of e
						arg1 = reader.ReadI8();
						// 2nd byte contains the value of f
						arg2 = reader.ReadI8();
					}
					else if (arg1And2AreWords && !argsAreXyValues)
					{
						// 1st short contains the index of matching point in compound being constructed
						arg1 = reader.ReadU16();
						// 2nd short contains index of matching point in component
						arg2 = reader.ReadU16();
					}
					else if (!arg1And2AreWords && !argsAreXyValues)
					{
						// 1st byte containing index of matching point in compound being constructed#
						arg1 = reader.ReadU8();
						// 2nd byte containing index of matching point in component
						arg2 = reader.ReadU8();
					}

					if ((flags & CompoundGlyphOutlineFlags.WeHaveAScale) != 0)
					{
						scaleX = scaleY = F2Dot14.FromRaw(reader.ReadU16());
					}
					else if ((flags & CompoundGlyphOutlineFlags.WeHaveAnXAndYScale) != 0)
					{
						scaleX = F2Dot14.FromRaw(reader.ReadU16());
						scaleY = F2Dot14.FromRaw(reader.ReadU16());
					}
					else if ((flags & CompoundGlyphOutlineFlags.WeHaveATwoByTwo) != 0)
					{
						scaleX = F2Dot14.FromRaw(reader.ReadU16());
						b = F2Dot14.FromRaw(reader.ReadU16());
						c = F2Dot14.FromRaw(reader.ReadU16());
						scaleY = F2Dot14.FromRaw(reader.ReadU16());
					}

					components.Add(new(
						glyphIndex,
						arg1,
						arg2,
						argsAreXyValues,
						scaleX,
						b,
						c,
						scaleY
					));
				} while ((flags & CompoundGlyphOutlineFlags.MoreComponents) != 0);

				byte[]? instructions = null;
				if ((flags & CompoundGlyphOutlineFlags.WeHaveInstructions) != 0)
				{
					var instructionLength = reader.ReadU16();
					instructions = new byte[instructionLength];
					for (var i = 0; i < instructions.Length; i++)
						instructions[i] = reader.ReadU8();
				}

				glyph = new CompoundGlyphOutline(font, xMin, yMin, xMax, yMax, components.ToArray(), instructions);
			}

			outlinesByIndex.Add(locaIndex, glyph);
			outlinesByLocation.Add(locaOffset, glyph);
		}

		OutlinesByIndex = outlinesByIndex.ToFrozenDictionary();
		OutlinesByLocation = outlinesByLocation.ToFrozenDictionary();
	}
}

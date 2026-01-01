namespace FontThing.TrueType;

public readonly struct LongHorMetric(ushort advanceWidth, short leftSideBearing)
{
	public readonly ushort AdvanceWidth = advanceWidth;
	public readonly short LeftSideBearing = leftSideBearing;
}

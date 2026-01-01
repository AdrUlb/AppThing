using System.Drawing;

namespace AppThing;

public class AtlasGenerator
{
	public readonly Texture Texture;
	private readonly List<Rectangle> _availableRegions = [];

	public AtlasGenerator(Texture texture)
	{
		Texture = texture;
		_availableRegions.Add(new Rectangle(new(0, 0), texture.Size));
	}

	public bool TryAllocateRegion(Size size, out Rectangle rect)
	{
		var smallestArea = float.MaxValue;
		var bestIndex = -1;

		for (var i = 0; i < _availableRegions.Count; i++)
		{
			var region = _availableRegions[i];

			// Check if region is large enough
			if (region.Width < size.Width || region.Height < size.Height)
				continue;

			var area = region.Width * region.Height;
			if (area < smallestArea)
			{
				smallestArea = area;
				bestIndex = i;
				continue;
			}
		}

		if (bestIndex == -1)
		{
			rect = Rectangle.Empty;
			return false;
		}

		var bestRegion = _availableRegions[bestIndex];
		rect = new(bestRegion.Location, size);

		_availableRegions.RemoveAt(bestIndex);

		// Add remaining regions
		var remainingRight = bestRegion.Width - size.Width;
		var remainingBottom = bestRegion.Height - size.Height;

		if (remainingRight < remainingBottom)
		{
			if (remainingRight > 0)
				_availableRegions.Add(new(bestRegion.X + size.Width, bestRegion.Y, remainingRight, size.Height));

			if (remainingBottom > 0)
				_availableRegions.Add(new(bestRegion.X, bestRegion.Y + size.Height, bestRegion.Width, remainingBottom));
		}
		else
		{
			if (remainingBottom > 0)
				_availableRegions.Add(new(bestRegion.X, bestRegion.Y + size.Height, size.Width, remainingBottom));

			if (remainingRight > 0)
				_availableRegions.Add(new(bestRegion.X + size.Width, bestRegion.Y, remainingRight, bestRegion.Height));
		}

		return true;
	}
}

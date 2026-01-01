using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using FontThing.TrueType.Parsing;
using System.Drawing;
using System.Text;
using UtilThing;

namespace FontThing.TrueType;

internal static class TrueTypeFontRasterizer
{
	public static float CalculateStemDarkening(float pixelsPerEm)
	{
		const float minPpem = 10.0f;
		const float maxPpem = 36.0f;
		const float maxDarkening = 0.25f;

		if (pixelsPerEm <= minPpem)
			return maxDarkening;

		if (pixelsPerEm >= maxPpem)
			return 0.0f;

		var t = (pixelsPerEm - minPpem) / (maxPpem - minPpem);
		return float.Lerp(maxDarkening, 0.0f, t);
	}

	public static GlyphBitmap RenderGlyph(GlyphOutline glyphOutline, float scale, int supersamples, float bezierTolerance, float subpixelOffsetX, float subpixelOffsetY, float stemDarkeningAmount, float gamma)
	{
		var gammaTable = GenerateGammaTable(gamma);
		var contours = GenerateContours(glyphOutline, scale * supersamples, bezierTolerance * supersamples);

		var scaledBounds = glyphOutline.GetBounds(scale);
		var bitmapWidth = (int)(scaledBounds.Width + subpixelOffsetX) + 2;
		var bitmapHeight = (int)(scaledBounds.Height + subpixelOffsetY) + 2;

		var alphaWidth = bitmapWidth * supersamples;
		var alphaHeight = bitmapHeight * supersamples;

		var boolPool = ArrayPool<bool>.Shared;
		var supersampled = boolPool.Rent(alphaWidth * alphaHeight);

		RenderGlyph(contours, scaledBounds.X * supersamples, scaledBounds.Y * supersamples, supersampled, alphaWidth, alphaHeight, (subpixelOffsetX + 1) * supersamples, (subpixelOffsetY + 1) * supersamples, stemDarkeningAmount);

		var bitmap = new byte[bitmapWidth * bitmapHeight];
		var downsampledPixelContrib = 1.0f / (supersamples * supersamples);

		for (var y = 0; y < bitmapHeight; y++)
		{
			for (var x = 0; x < bitmapWidth; x++)
			{
				var a = 0.0f;

				for (var offY = 0; offY < supersamples; offY++)
				{
					var yy = y * supersamples + offY;
					var rowOffset = yy * alphaWidth;
					var prevRowOffset = (yy > 0) ? (yy - 1) * alphaWidth : rowOffset;
					var nextRowOffset = (yy < alphaHeight - 1) ? (yy + 1) * alphaWidth : rowOffset;

					for (var offX = 0; offX < supersamples; offX++)
					{
						var xx = x * supersamples + offX;

						var lit = supersampled[xx + rowOffset];
						var prevLit = supersamples >= 4 && supersampled[xx + prevRowOffset];
						var nextLit = supersamples >= 4 && supersampled[xx + nextRowOffset];

						if (lit || prevLit || nextLit)
							a += downsampledPixelContrib;
					}
				}

				var alpha = (byte)(a * 255);
				bitmap[x + y * bitmapWidth] = gammaTable[alpha];
			}
		}

		boolPool.Return(supersampled);

		return new(bitmap, new(bitmapWidth, bitmapHeight));
	}

	private static byte[] GenerateGammaTable(float gamma)
	{
		var table = new byte[256];
		for (var i = 0; i < table.Length; i++)
		{
			// Convert to 0-1
			var linear = i / 255.0f;

			// Gamma correction
			var corrected = MathF.Pow(linear, 1.0f / gamma);

			// Convert back to 0-255
			table[i] = (byte)Math.Clamp(corrected * 255, 0, 255);
		}

		return table;
	}

	private static void RenderGlyph(IReadOnlyList<List<Vector2>> contours, float glyphXMin, float glyphYMin, Span<bool> pixels, int width, int height, float subpixelOffsetX, float subpixelOffsetY, float stemDarkeningAmount)
	{
		Debug.Assert(pixels.Length >= width * height);

		var renderOffset = new Vector2(-glyphXMin, -glyphYMin);

		Span<bool> contoursClockwise = stackalloc bool[contours.Count];
		for (var i = 0; i < contours.Count; i++)
			contoursClockwise[i] = IsContourClockwise(contours[i]);

		var changes = new List<(float X, bool Clockwise, bool GoingDown)>();

		for (var y = 0; y < height; y++)
		{
			changes.Clear();

			var sampleY = y - subpixelOffsetY;

			for (var contourIndex = 0; contourIndex < contours.Count; contourIndex++)
			{
				var contour = contours[contourIndex];
				var clockwise = contoursClockwise[contourIndex];
				for (var pointIndex = 0; pointIndex < contour.Count; pointIndex++)
				{
					var p1 = contour[pointIndex] + renderOffset;
					var p2 = contour[(pointIndex + 1) % contour.Count] + renderOffset;

					// Both points entirely above or below this scanline
					if ((p1.Y < sampleY && p2.Y < sampleY) || p1.Y >= sampleY && p2.Y >= sampleY)
						continue;

					var goingDown = p2.Y < p1.Y;

					var lowerY = float.Min(p1.Y, p2.Y);
					var boxHeight = float.Abs(p1.Y - p2.Y);
					if (boxHeight == 0.0f)
						continue;

					var amount = (sampleY - lowerY) / boxHeight;

					var changeX = !goingDown ? float.Lerp(p1.X, p2.X, amount) : float.Lerp(p2.X, p1.X, amount);

					if (!goingDown)
						changeX -= stemDarkeningAmount;
					else
						changeX += stemDarkeningAmount;

					changes.Add((changeX, clockwise, goingDown));
				}
			}

			changes.Sort((a, b) => a.X.CompareTo(b.X));

			var fillCount = 0;
			var noFillCount = 0;
			var nextChangeI = 0;

			var fill = false;

			for (var x = 0; x < width; x++)
			{
				var sampleX = x - subpixelOffsetX;

				while (nextChangeI < changes.Count && sampleX >= changes[nextChangeI].X)
				{
					var change = changes[nextChangeI];

					if (change.Clockwise)
					{
						if (change.GoingDown)
							fillCount--;
						else
							fillCount++;
					}
					else
					{
						if (!change.GoingDown)
							noFillCount--;
						else
							noFillCount++;
					}

					nextChangeI++;

					fill = noFillCount == 0 && fillCount > 0;
				}

				var i = x + y * width;
				pixels[i] = fill;
			}
		}
	}

	private static bool IsContourClockwise(List<Vector2> points)
	{
		var sum = 0.0f;

		for (var i = 0; i < points.Count; i++)
		{
			var p1 = points[i];
			var p2 = points[(i + 1) % points.Count];
			sum += (p2.X - p1.X) * (p2.Y + p1.Y);
		}

		return sum > 0.0f;
	}

	private static IReadOnlyList<List<Vector2>> GenerateContours(GlyphOutline glyphOutline, float scale, float bezierTolerance)
	{
		switch (glyphOutline)
		{
			case SimpleGlyphOutline simpleGlyphOutline:
				return GenerateContours(simpleGlyphOutline, scale, bezierTolerance);
			case CompoundGlyphOutline compoundGlyphOutline:
				return GenerateContours(compoundGlyphOutline, scale, bezierTolerance);
			default:
				throw new NotSupportedException("Only simple and compound glyph outlines are supported.");
		}
	}

	private static List<Vector2>[] GenerateContours(SimpleGlyphOutline simpleGlyphOutline, float scale, float bezierTolerance)
	{
		var contours = new List<Vector2>[simpleGlyphOutline.EndPointsOfContours.Length];

		// Iterate over all contours
		var contourStartpointIndex = 0;
		for (var contourIndex = 0; contourIndex < simpleGlyphOutline.EndPointsOfContours.Length; contourIndex++)
		{
			var contourEndpointIndex = simpleGlyphOutline.EndPointsOfContours[contourIndex];

			contours[contourIndex] = GenerateContour(simpleGlyphOutline.Points.AsSpan()[contourStartpointIndex..(contourEndpointIndex + 1)], scale, bezierTolerance);

			// Next contour's start point index immediately follows this contour's endpoint index
			contourStartpointIndex = contourEndpointIndex + 1;
		}

		return contours;
	}

	private static List<List<Vector2>> GenerateContours(CompoundGlyphOutline compoundGlyphOutline, float scale, float bezierTolerance)
	{
		var contours = new List<List<Vector2>>();

		foreach (var component in compoundGlyphOutline.Components)
		{
			if (!component.ArgsAreXyValues)
				throw new NotImplementedException();

			var componentOutline = compoundGlyphOutline.Font.GetGlyphOutlineFromIndex(component.GlyphIndex);
			Debug.Assert(componentOutline != null);

			var componentContours = GenerateContours(componentOutline, scale, bezierTolerance);
			foreach (var contour in componentContours)
			{
				for (var i = 0; i < contour.Count; i++)
				{
					ref var p = ref CollectionsMarshal.AsSpan(contour)[i];

					// Apply scaling
					var scaledX = p.X * (float)component.ScaleX + p.Y * (float)component.Scale01;
					var scaledY = p.X * (float)component.Scale10 + p.Y * (float)component.ScaleY;

					// Apply translation
					scaledX += component.Arg1 * scale;
					scaledY += component.Arg2 * scale;

					p = new(scaledX, scaledY);
				}

				contours.Add(contour);
			}
		}

		return contours;
	}

	private static List<Vector2> GenerateContour(ReadOnlySpan<GlyphPoint> points, float scale, float bezierTolerance)
	{
		var lines = new List<Vector2>();

		// Find first on-curve point
		var pointOffset = 0; // Index of first point to actually read in the loop
		while (!points[pointOffset].OnCurve)
			pointOffset++;

		var p0Vec = GetScaledGlyphPoint(points[pointOffset++]);

		var i = 0;
		while (i < points.Length)
		{
			var index = (i + pointOffset) % points.Length;

			var p1 = points[index];
			var p1Vec = GetScaledGlyphPoint(p1);

			// "Consume" P1
			i++;

			// Two consecutive on-curve points - straight line
			if (p1.OnCurve)
			{
				lines.Add(p1Vec);
				p0Vec = p1Vec;
				continue;
			}

			index = (i + pointOffset) % points.Length;

			var p2 = points[index];
			var p2Vec = GetScaledGlyphPoint(p2);

			if (p2.OnCurve) // Points are: on-curve, off-curve, on-curve - quadratic Bézier
			{
				i++;

				AddBezier(p0Vec, p1Vec, p2Vec);
				p0Vec = p2Vec;
			}
			else // Points are: on-curve, off-curve, off-curve - insert midpoint as on-curve point for quadratic Bézier
			{
				var midpoint = Vector2.Lerp(p1Vec, p2Vec, 0.5f);
				AddBezier(p0Vec, p1Vec, midpoint);
				p0Vec = midpoint;
			}
		}

		// Next contour's start point index immediately follows this contour's endpoint index
		return lines;

		Vector2 GetScaledGlyphPoint(GlyphPoint point) => new Vector2(point.X, point.Y) * scale;

		void AddBezier(Vector2 p0, Vector2 p1, Vector2 p2)
		{
			var result = BezierSubdivider.RecursiveBezier(p0, p1, p2, bezierTolerance);
			lines.AddRange(CollectionsMarshal.AsSpan(result)[1..]);
		}
	}
}

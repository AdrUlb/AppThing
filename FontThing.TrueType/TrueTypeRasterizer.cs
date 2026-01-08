using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using FontThing.TrueType.Parsing;

namespace FontThing.TrueType;

public static class TrueTypeRasterizer
{
	public static float CalculateStemDarkening(float pixelsPerEm)
	{
		const float minPpem = 10.0f;
		const float maxPpem = 36.0f;
		const float maxDarkening = 0.10f;

		if (pixelsPerEm <= minPpem)
			return maxDarkening;

		if (pixelsPerEm >= maxPpem)
			return 0.0f;

		var t = (pixelsPerEm - minPpem) / (maxPpem - minPpem);
		return float.Lerp(maxDarkening, 0.0f, t);
	}

	public static GlyphBitmap RenderGlyph(GlyphOutline glyphOutline, float scale, int supersamples, float bezierTolerance, float subpixelOffsetX, float subpixelOffsetY, float stemDarkeningAmount, float gamma)
	{
		Span<byte> gammaTable = stackalloc byte[256];
		GenerateGammaTable(gamma, gammaTable);

		var scaledBounds = glyphOutline.GetBounds(scale);
		var bitmapWidth = (int)(scaledBounds.Width + subpixelOffsetX) + 1;
		var bitmapHeight = (int)(scaledBounds.Height + subpixelOffsetY) + 1;

		var supersampledWidth = bitmapWidth * supersamples;
		var supersampledHeight = bitmapHeight * supersamples;

		var supersampledPool = ArrayPool<bool>.Shared;
		var supersampled = supersampledPool.Rent(supersampledWidth * supersampledHeight);

		RenderGlyph(glyphOutline, scale * supersamples, bezierTolerance, subpixelOffsetX * supersamples, subpixelOffsetY * supersamples, stemDarkeningAmount * supersamples, supersampled, supersampledWidth, supersampledHeight);

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
					var rowOffset = yy * supersampledWidth;

					for (var offX = 0; offX < supersamples; offX++)
					{
						var xx = x * supersamples + offX;

						if (supersampled[xx + rowOffset])
							a += downsampledPixelContrib;
					}
				}

				var alpha = (byte)(a * 255);
				bitmap[x + y * bitmapWidth] = gammaTable[alpha];
			}
		}

		supersampledPool.Return(supersampled);

		return new(bitmap, new(bitmapWidth, bitmapHeight));
	}

	private static void GenerateGammaTable(float gamma, Span<byte> buffer)
	{
		Debug.Assert(buffer.Length >= 256);
		for (var i = 0; i < buffer.Length; i++)
		{
			// Convert to 0-1
			var linear = i / 255.0f;

			// Gamma correction
			var corrected = MathF.Pow(linear, 1.0f / gamma);

			// Convert back to 0-255
			buffer[i] = (byte)Math.Clamp(corrected * 255, 0, 255);
		}
	}

	public static void RenderGlyph(GlyphOutline glyphOutline, float scale, float bezierTolerance, float subpixelOffsetX, float subpixelOffsetY, float stemDarkeningAmount, Span<bool> pixels, int width, int height)
	{
		var glyphXMin = glyphOutline.XMin * scale;
		var glyphYMin = glyphOutline.YMin * scale;
		var contours = glyphOutline.GenerateContours(scale, bezierTolerance);
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
}

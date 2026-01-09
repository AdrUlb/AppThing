namespace UtilThing;

public static class Equations
{
	public static Span<double> SolveCubic(double a, double b, double c, double d, Span<double> roots)
	{
		const double epsilon = 1e-9;

		if (double.Abs(a) < epsilon)
			return SolveQuadratic(b, c, d, roots);

		// Normalize coefficients
		b /= a;
		c /= a;
		d /= a;

		// Convert to form t^3 + pt + q = 0
		var p = c - (b * b) / 3.0;
		var q = (2.0 * b * b * b) / 27.0 - (b * c) / 3.0 + d;

		var discriminant = (q * q) / 4.0 + (p * p * p) / 27.0;

		switch (discriminant)
		{
			case < -epsilon: // Discriminant negative: three real roots
				{
					var r = double.Sqrt(-p * p * p / 27.0);
					var phi = double.Acos(double.Clamp(-q / (2.0 * r), -1.0, 1.0));
					var t = 2.0 * double.Cbrt(r);

					roots[0] = t * double.Cos(phi / 3.0) - b / 3.0;
					roots[1] = t * double.Cos((phi + 2.0 * double.Pi) / 3.0) - b / 3.0;
					roots[2] = t * double.Cos((phi + 4.0 * double.Pi) / 3.0) - b / 3.0;
					return roots[..3];
				}
			case > epsilon: // Discriminant positive: one real root
				{
					var sqrtDiscriminant = double.Sqrt(discriminant);
					var u = double.Cbrt(-q / 2.0 + sqrtDiscriminant);
					var v = double.Cbrt(-q / 2.0 - sqrtDiscriminant);
					roots[0] = u + v - b / 3.0;
					return roots[..1];
				}
			default: // Discriminant zero: two real roots
				{
					var u = double.Cbrt(-q / 2.0);
					roots[0] = 2.0 * u - b / 3.0;
					roots[1] = -u - b / 3.0;
					return roots[..2];
				}
		}
	}

	public static Span<double> SolveQuadratic(double a, double b, double c, Span<double> roots)
	{
		const double epsilon = 1e-9;

		if (double.Abs(a) < epsilon)
		{
			roots[0] = -c / b;
			return roots[..1];
		}

		var discriminant = b * b - 4.0 * a * c;

		switch (discriminant)
		{
			case < -epsilon: // Discriminant negative: no real roots
				return Span<double>.Empty;
			case > epsilon: // Discriminant positive: two real roots
				{
					var sqrtDiscriminant = double.Sqrt(discriminant);

					//roots[0] = (-b + sqrtDiscriminant) / (2.0 * a);
					//roots[1] = (-b - sqrtDiscriminant) / (2.0 * a);
					
					// Use numerically stable version of quadratic formula
					var q = -0.5 * (b + Math.CopySign(sqrtDiscriminant, b));
					roots[0] = q / a;
					roots[1] = c / q;
					return roots[..2];
				}
			default: // Discriminant zero: one real root
				roots[0] = -b / (2.0 * a);
				return roots[..1];
		}
	}
}

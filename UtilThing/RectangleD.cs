// Based on .NET source code

namespace UtilThing;

public struct RectangleD
{
	public static readonly RectangleD Empty;

	private double x;
	private double y;
	private double width;
	private double height;
	
	public double X
	{
		readonly get => x;
		set => x = value;
	}

	public double Y
	{
		readonly get => y;
		set => y = value;
	}

	public double Width
	{
		readonly get => width;
		set => width = value;
	}

	public double Height
	{
		readonly get => height;
		set => height = value;
	}
	
	public RectangleD(double x, double y, double width, double height)
	{
		this.x = x;
		this.y = y;
		this.width = width;
		this.height = height;
	}

	public static RectangleD FromLTRB(double left, double top, double right, double bottom) =>
		new RectangleD(left, top, right - left, bottom - top);

	public Vector2d Location
	{
		readonly get => new(X, Y);
		set
		{
			X = value.X;
			Y = value.Y;
		}
	}

	public Vector2d Size
	{
		readonly get => new(Width, Height);
		set
		{
			Width = value.X;
			Height = value.Y;
		}
	}
}

using System;

public class Vector
{
	public double X;
	public double Y;

	public Vector(double x, double y)
	{
		X = x;
		Y = y;
	}

	public static Vector operator +(Vector a, Vector b) => new Vector(a.X + b.X, a.Y + b.Y);
	public static Vector operator -(Vector a, Vector b) => new Vector(a.X - b.X, a.Y - b.Y);
	public static Vector operator *(Vector a, double scalar) => new Vector(a.X * scalar, a.Y * scalar);
	public static Vector operator /(Vector a, double scalar) => new Vector(a.X / scalar, a.Y / scalar);

	public double Magnitude => Math.Sqrt(X * X + Y * Y);
	public Vector Normalize() => Magnitude > 1e-9 ? new Vector(X / Magnitude, Y / Magnitude) : new Vector(0, 0);

	public bool IsCloseTo(Vector other, double epsilon = 0.01)
	{
		return Math.Abs(X - other.X) < epsilon && Math.Abs(Y - other.Y) < epsilon;
	}
}
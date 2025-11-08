using System.Numerics;

class Vector
{
	public Vector2 Value;

	public Vector(float x, float y)
	{
		Value = new Vector2(x, y);
	}

	public float X => Value.X;
	public float Y => Value.Y;

	public static Vector operator +(Vector a, Vector b)
			=> new Vector(a.X + b.X, a.Y + b.Y);

	public static Vector operator -(Vector a, Vector b)
			=> new Vector(a.X - b.X, a.Y - b.Y);

	public static Vector operator *(Vector v, float scalar)
			=> new Vector(v.X * scalar, v.Y * scalar);

	public static Vector operator /(Vector v, float scalar)
			=> new Vector(v.X / scalar, v.Y / scalar);

	public float Magnitude => Value.Length();

	public Vector Normalize() => new Vector(Vector2.Normalize(Value).X, Vector2.Normalize(Value).Y);

	public static implicit operator Vector2(Vector v) => v.Value;
	public static implicit operator Vector(Vector2 v) => new Vector(v.X, v.Y);

	public bool IsCloseTo(Vector other, double epsilon = 0.01)
	{
		return Math.Abs(X - other.X) < epsilon && Math.Abs(Y - other.Y) < epsilon;
	}
}
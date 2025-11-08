using System.Numerics;

public class Particle
{
	public int Id;
	public Vector2 Position;
	public Vector2 Velocity;
	public Vector2 Acceleration;
	public Vector2 Jerk { get; set; } = Vector2.Zero; // derivace akcelerace
	public float Mass;
	public float TimeStep { get; set; } = 0.1f;
	public float NextUpdateTime { get; set; } = 0f; // kdy bude další aktualizace
	public Particle(int id, Vector2 position, Vector2 velocity, float mass)
	{
		Id = id;
		Position = position;
		Velocity = velocity;
		Mass = mass;
		Acceleration = Vector2.Zero;
	}
}

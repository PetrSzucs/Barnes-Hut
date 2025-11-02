public class Particle
{
	public int Id;
	public Vector Position;
	public Vector Velocity;
	public Vector Acceleration;
	public double Mass;

	public Particle(int id, Vector position, Vector velocity, double mass)
	{
		Id = id;
		Position = position;
		Velocity = velocity;
		Acceleration = new Vector(0, 0);
		Mass = mass;
	}
}

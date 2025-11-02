using System.Collections.Generic;

public class PhysicsEngine
{
	public double Theta { get; set; } = 0.5;
	public double DeltaTime { get; set; } = 0.1;
	public double G { get; set; } = 1.0; // gravitační konstanta (můžeš doladit)

	public PhysicsEngine(double theta = 0.5, double deltaTime = 0.1, double g = 1.0)
	{
		Theta = theta;
		DeltaTime = deltaTime;
		G = g;
	}

	public void Update(List<Particle> particles, QuadTree tree)
	{
		foreach (var p in particles)
		{
			Vector force = tree.CalculateForce(p, Theta);
			// Započti gravitační konstantu
			force *= G;

			// F = m * a  →  a = F / m
			p.Acceleration = force / p.Mass;

			// Eulerova integrace
			p.Velocity += p.Acceleration * DeltaTime;
			p.Position += p.Velocity * DeltaTime;
		}
	}
}

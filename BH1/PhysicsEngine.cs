using System.Collections.Generic;
using System.Diagnostics;

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
	// Standardní update, paralelizovaný
	public void Update(List<Particle> particles, QuadTree tree)
	{
		Parallel.ForEach(particles, p =>
		{
			Vector force = tree.CalculateForce(p, Theta);
			force *= G;

			p.Acceleration = force / p.Mass;

			p.Velocity += p.Acceleration * DeltaTime;
			p.Position += p.Velocity * DeltaTime;
		});
	}

	// Sekvenční update pro porovnání rychlosti
	public void UpdateSequential(List<Particle> particles, QuadTree tree)
	{
		foreach (var p in particles)
		{
			Vector force = tree.CalculateForce(p, Theta);
			force *= G;

			p.Acceleration = force / p.Mass;

			p.Velocity += p.Acceleration * DeltaTime;
			p.Position += p.Velocity * DeltaTime;
		}
	}

	// Metoda pro rychlé benchmarky
	public long RunBenchmark(List<Particle> particles, QuadTree tree, int iterations, bool parallel = true)
	{
		Stopwatch sw = Stopwatch.StartNew();

		for (int i = 0; i < iterations; i++)
		{
			if (parallel)
				Update(particles, tree);
			else
				UpdateSequential(particles, tree);
		}

		sw.Stop();
		return sw.ElapsedMilliseconds;
	}



	/*public void Update(List<Particle> particles, QuadTree tree)
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
	}*/
}

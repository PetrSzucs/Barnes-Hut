using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

public class PhysicsEngine
{
	public float Theta { get; set; } = 0.5f;
  public float DeltaTime { get; set; } = 0.1f;
  public float G { get; set; } = 1f;
	public float CutoffDistance { get; set; } = 200f; // max vzdálenost pro výpočet jerku

	public PhysicsEngine(float theta = 0.5f, float deltaTime = 0.1f, float g = 1f)
	{
		Theta = theta;
		DeltaTime = deltaTime;
		G = g;
	}

	private Vector2 ComputeJerk(Particle target, List<Particle> particles)
	{
		Vector2 jerk = Vector2.Zero;

		foreach (var other in particles)
		{
			if (other == target) continue;

			Vector2 r = other.Position - target.Position;
			Vector2 v = other.Velocity - target.Velocity;

			float dist2 = Vector2.Dot(r, r) + 1e-5f;
			float dist = MathF.Sqrt(dist2);
			float invDist3 = 1f / (dist2 * dist);

			float mass = other.Mass;

			// jerk = G * m * (v/dist³ - 3*(r·v)*r/dist⁵)
			float rv = Vector2.Dot(r, v);
			Vector2 j = (v - 3f * rv / dist2 * r) * (mass * invDist3);

			jerk += j;
		}

		return jerk * (float)G;
	}
	/*
	public void Update(List<Particle> particles, QuadTree tree, float currentTime)
	{
		float baseStep = (float)DeltaTime;

		Parallel.ForEach(particles, p =>
		{
			if (p.NextUpdateTime > currentTime)
				return;

			// 1️⃣ Výpočet aktuálních sil
			Vector2 a0 = tree.CalculateForce(p, (float)Theta) * (float)(G / p.Mass);
			Vector2 j0 = ComputeJerk(p, particles); // derivace zrychlení

			float dt = p.TimeStep;

			// 2️⃣ Predikce nové pozice a rychlosti
			Vector2 r_pred = p.Position + p.Velocity * dt + a0 * (0.5f * dt * dt) + j0 * (dt * dt * dt / 6f);
			Vector2 v_pred = p.Velocity + a0 * dt + j0 * (0.5f * dt * dt);

			// 3️⃣ Spočítej nové síly pro predikované pozice
			Vector2 a1 = tree.CalculateForce(new Particle(0, r_pred, v_pred, p.Mass), (float)Theta) * (float)(G / p.Mass);
			Vector2 j1 = ComputeJerk(new Particle(0, r_pred, v_pred, p.Mass), particles);

			// 4️⃣ Korekce (Hermite average)
			p.Position += (p.Velocity + v_pred) * (dt / 2f) + (a0 - a1) * (dt * dt / 12f);
			p.Velocity += (a0 + a1) * (dt / 2f) + (j0 - j1) * (dt * dt / 12f);

			// aktualizace akcelerace a jerku
			p.Acceleration = a1;
			p.Jerk = j1;

			// 5️⃣ adaptivní krok
			float accelMag = a1.Length();
			float jerkMag = j1.Length();
			float eta = 0.01f; // parametr přesnosti

			float newDt = (float)Math.Sqrt(
					eta * (Math.Sqrt(accelMag * accelMag + 1e-8f) / (jerkMag + 1e-8f))
			);

			// zarovnej na mocninu 1/2
			float ratio = baseStep / newDt;
			int power = Math.Clamp((int)Math.Round(Math.Log2(ratio)), -6, 0);
			newDt = baseStep / (float)Math.Pow(2, power);

			p.TimeStep = Math.Clamp(newDt, baseStep / 64f, baseStep);
			p.NextUpdateTime = currentTime + p.TimeStep;
		});
	}
	*/

	
	//public void Update_(List<Particle> particles, QuadTree tree, float currentTime)
	//{
	//	float baseStep = (float)DeltaTime; // např. 0.1f

	//	Parallel.ForEach(particles, p =>
	//	{
	//		// částice, které ještě nemají čas na update, přeskočíme
	//		if (p.NextUpdateTime > currentTime)
	//			return;

	//		// Výpočet síly a akcelerace
	//		Vector2 force = tree.CalculateForce(p, (float)Theta) * (float)G;
	//		Vector2 acceleration = force / p.Mass;
	//		p.Acceleration = acceleration;

	//		// adaptivní úprava časového kroku podle akcelerace
	//		float accelMag = acceleration.Length();

	//		// čím větší zrychlení, tím menší krok (pouze mocniny 1/2)
	//		float desiredStep = baseStep;
	//		if (accelMag > 50f) desiredStep = baseStep / 16f;
	//		else if (accelMag > 10f) desiredStep = baseStep / 8f;
	//		else if (accelMag > 5f) desiredStep = baseStep / 4f;
	//		else if (accelMag > 2f) desiredStep = baseStep / 2f;

	//		// zarovnej krok na nejbližší mocninu 1/2
	//		float ratio = baseStep / desiredStep;
	//		int power = (int)Math.Round(Math.Log2(ratio));
	//		desiredStep = baseStep / (float)Math.Pow(2, power);

	//		// omezení rozsahu
	//		desiredStep = Math.Clamp(desiredStep, baseStep / 64f, baseStep);

	//		p.TimeStep = desiredStep;

	//		// integrace pohybu (Euler)
	//		p.Velocity += acceleration * p.TimeStep;
	//		p.Position += p.Velocity * p.TimeStep;

	//		// naplánuj příští update na budoucí čas
	//		p.NextUpdateTime = currentTime + p.TimeStep;
	//	});
	//}

	/*
	public void Update(List<Particle> particles, QuadTree tree, float currentTime)
	{
		float maxAccel = 0f;

		// 1️⃣ Výpočet akcelerace a zjištění největší hodnoty
		Parallel.ForEach(particles, () => 0f, (p, state, localMax) =>
		{
			Vector2 force = tree.CalculateForce(p, (float)Theta) * (float)G;
			Vector2 accel = force / p.Mass;
			p.Acceleration = accel;

			float accelMag = accel.Length();
			return accelMag > localMax ? accelMag : localMax;

		}, localMax =>
		{
			// Redukce lokálních maxim na globální maximum
			float initial, computed;
			do
			{
				initial = maxAccel;
				computed = Math.Max(initial, localMax);
			}
			while (initial != Interlocked.CompareExchange(ref maxAccel, computed, initial));
		});

		// 2️⃣ Adaptivní krok podle maximálního zrychlení
		float adaptiveDt = (float)(DeltaTime / (1 + maxAccel * 0.1f));
		adaptiveDt = Math.Clamp(adaptiveDt, 0.01f, (float)DeltaTime);

		// 3️⃣ Aktualizace pozic a rychlostí (také paralelně)
		Parallel.ForEach(particles, p =>
		{
			p.Velocity += p.Acceleration * adaptiveDt;
			p.Position += p.Velocity * adaptiveDt;
		});
	}
	*/
	// Standardní update, paralelizovaný
	public void Update(List<Particle> particles, QuadTree tree, float currentTime)
	{
		Parallel.ForEach(particles, p =>
		{
			p.Velocity += 0.5f*p.Acceleration * DeltaTime;
		});
		Parallel.ForEach(particles, p =>
		{
			p.Position += p.Velocity * DeltaTime;
		});
	}
	// Standardní update, paralelizovaný
	//public void Update(List<Particle> particles, QuadTree tree, float currentTime)
	//{
	//	Parallel.ForEach(particles, p =>
	//	{
	//		Vector2 force = tree.CalculateForce(p, Theta);
	//		force *= G;

	//		p.Acceleration = force / p.Mass;

	//		p.Velocity += p.Acceleration * DeltaTime;
	//		p.Position += p.Velocity * DeltaTime;
	//	});
	//}

	// Sekvenční update pro porovnání rychlosti
	//public void UpdateSequential(List<Particle> particles, QuadTree tree)
	//{
	//	foreach (var p in particles)
	//	{
	//		Vector2 force = tree.CalculateForce(p, Theta);
	//		force *= G;

	//		p.Acceleration = force / p.Mass;

	//		p.Velocity += p.Acceleration * DeltaTime;
	//		p.Position += p.Velocity * DeltaTime;
	//	}
	//}

	// Metoda pro rychlé benchmarky
	public long RunBenchmark(List<Particle> particles, QuadTree tree, int iterations, bool parallel = true)
	{
		Stopwatch sw = Stopwatch.StartNew();

		/*for (int i = 0; i < iterations; i++)
		{
			if (parallel)
				Update(particles, tree);
			else
				UpdateSequential(particles, tree);
		}*/

		sw.Stop();
		return sw.ElapsedMilliseconds;
	}

	public double Energy(List<Particle> particles)
	{
		double Ekin = 0; double Epot = 0; double energy = 0;
		foreach (var particle in particles)
		{
			Ekin+=(particle.Mass/G)* 0.5*(particle.Velocity.X*particle.Velocity.X+particle.Velocity.Y*particle.Velocity.Y);
		}


		foreach (var p1 in particles)
		{
			foreach (var p2 in particles)
			{
				if (p1.Position==p2.Position) continue;
				Vector2 dir = p1.Position - p2.Position;
				float distSq = dir.LengthSquared();
				Epot= Epot  -  G * (p1.Mass)/G * (p2.Mass/G)/distSq;
			}
		}
		energy=Ekin+Epot;
		return energy;
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

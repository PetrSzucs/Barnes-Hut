using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

class ParticleForm : Form
{
	private List<Particle> particles;
	private QuadTree quadTree;
	private PhysicsEngine physics;
	private Timer timer;
	private Random rnd = new Random();
	private ScenarioType scenario = ScenarioType.SunSystem; // vybraný scénář

	public ParticleForm(List<Particle> particles, QuadTree quadTree, PhysicsEngine physics)
	{
		this.particles = particles;
		this.quadTree = quadTree;
		this.physics = physics;

		DoubleBuffered = true;
		ClientSize = new Size(850, 850);
		Text = "Barnes-Hut Simulation";

		// Náhodné počáteční rychlosti
		foreach (var p in particles)
		{
			//p.Velocity = new Vector(rnd.NextDouble() * 2 - 1, rnd.NextDouble() * 2 - 1);
		}

		timer = new Timer { Interval = 16 }; // cca 60 FPS
		timer.Tick += (s, e) => { StepSimulation(); Invalidate(); };
		timer.Start();
		InitializeParticles();
	}

	enum ScenarioType
	{
		SunSystem,
		RandomCluster,
		TwoGalaxies,
		Explosion
	}


	private void InitializeParticles()
	{
		particles.Clear();
		Random rnd = new Random();

		switch (scenario)
		{
			case ScenarioType.SunSystem:
				CreateSunSystem(rnd);
				break;

			case ScenarioType.RandomCluster:
				CreateRandomCluster(rnd);
				break;

			case ScenarioType.TwoGalaxies:
				CreateTwoGalaxies(rnd);
				break;

			case ScenarioType.Explosion:
				CreateExplosion(rnd);
				break;
		}
	}

	private void CreateSunSystem(Random rnd)
	{
		Particle sun = new Particle(0, new Vector(425, 425), new Vector(0, 0), 10000);
		particles.Add(sun);

		int planetCount = 1000;
		double G = 1;

		for (int i = 0; i < planetCount; i++)
		{
			double orbitRadius = 100 + rnd.NextDouble() * 300;
			double angle = 2 * Math.PI * i / planetCount;

			double x = sun.Position.X + orbitRadius * Math.Cos(angle);
			double y = sun.Position.Y + orbitRadius * Math.Sin(angle);

			double v = Math.Sqrt(G * sun.Mass / orbitRadius);
			Vector velocity = new Vector(-v * Math.Sin(angle), v * Math.Cos(angle));

			velocity.X += (rnd.NextDouble() - 0.5) * 0.1;
			velocity.Y += (rnd.NextDouble() - 0.5) * 0.1;

			particles.Add(new Particle(i + 1, new Vector(x, y), velocity, 1));
		}
	}

	private void CreateRandomCluster(Random rnd)
	{
		int count = 1000;
		for (int i = 0; i < count; i++)
		{
			double x = 425 + rnd.NextDouble() * 200 - 100;
			double y = 425 + rnd.NextDouble() * 200 - 100;
			double vx = (rnd.NextDouble() - 0.5) * 0.5;
			double vy = (rnd.NextDouble() - 0.5) * 0.5;
			double mass = 1;

			particles.Add(new Particle(i, new Vector(x, y), new Vector(vx, vy), mass));
		}
	}

	private void CreateTwoGalaxies(Random rnd)
	{
		int galaxySize = 5000;
		double G = 1;
		double centerDistance = 500;

		Vector centerA = new Vector(325, 425);
		Vector centerB = new Vector(525, 425);

		Particle coreA = new Particle(0, centerA, new Vector(0, 0.5), 2000);
		Particle coreB = new Particle(1, centerB, new Vector(0, -0.5), 2000);

		particles.Add(coreA);
		particles.Add(coreB);

		for (int i = 0; i < galaxySize; i++)
		{
			double rA = 50 + rnd.NextDouble() * 150;
			double angleA = rnd.NextDouble() * Math.PI * 2;
			double xA = centerA.X + rA * Math.Cos(angleA);
			double yA = centerA.Y + rA * Math.Sin(angleA);
			double vA = Math.Sqrt(G * coreA.Mass / rA);
			Vector vVecA = new Vector(-vA * Math.Sin(angleA), vA * Math.Cos(angleA));
			vVecA += coreA.Velocity;
			particles.Add(new Particle(2 + i, new Vector(xA, yA), vVecA, 1));

			double rB = 50 + rnd.NextDouble() * 150;
			double angleB = rnd.NextDouble() * Math.PI * 2;
			double xB = centerB.X + rB * Math.Cos(angleB);
			double yB = centerB.Y + rB * Math.Sin(angleB);
			double vB = Math.Sqrt(G * coreB.Mass / rB);
			Vector vVecB = new Vector(-vB * Math.Sin(angleB), vB * Math.Cos(angleB));
			vVecB += coreB.Velocity;
			particles.Add(new Particle(2 + galaxySize + i, new Vector(xB, yB), vVecB, 1));
		}
	}

	private void CreateExplosion(Random rnd)
	{
		int count = 1000;
		Vector center = new Vector(425, 425);

		for (int i = 0; i < count; i++)
		{
			double angle = rnd.NextDouble() * Math.PI * 2;
			double speed = 2 + rnd.NextDouble() * 3;

			Vector velocity = new Vector(
					speed * Math.Cos(angle),
					speed * Math.Sin(angle)
			);

			particles.Add(new Particle(i, center, velocity, 1));
		}
	}


	private void StepSimulation()
	{
		quadTree = new QuadTree(1, new RectangleF(0, 0, 850, 850));
		// 1️⃣ Zvětši hranice podle všech částic
		foreach (var p in particles)
			quadTree.EnsureContains(p);

		// 2️⃣ Postav znovu strom pro aktuální rozložení
		quadTree = new QuadTree(1, quadTree.boundary);
		foreach (var p in particles)
			quadTree.Insert(p);

		// 3️⃣ Aktualizuj pozice částic
		physics.Update(particles, quadTree);		
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		var g = e.Graphics;
		quadTree.Draw(g);

		foreach (var p in particles)
		{
			g.FillEllipse(Brushes.Blue, (float)p.Position.X - 2, (float)p.Position.Y - 2, 4, 4);
		}
	}
}

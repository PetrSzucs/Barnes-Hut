using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

class ParticleForm : Form
{
	private List<Particle> particles;
	private float simulationTime = 0f;
	private QuadTree quadTree;
	private PhysicsEngine physics;
	private Timer timer;
	private Random rnd = new Random();
	private ScenarioType scenario = ScenarioType.Benchmark; // vybraný scénář
	private int logCounter=0;

	public ParticleForm(List<Particle> particles, QuadTree quadTree, PhysicsEngine physics)
	{
		this.particles = particles;
		this.quadTree = quadTree;
		this.physics = physics;

		DoubleBuffered = true;
		ClientSize = new Size(850, 850);
		Text = "Barnes-Hut Simulation";			

		timer = new Timer { Interval = 16 }; // cca 60 FPS
		timer.Tick += (s, e) => { StepSimulation(); Invalidate(); };
		InitializeParticles();

		// 🧠 Zastavíme vykreslování během benchmarku
		timer.Stop();
	
		RunBenchmark(100);

		timer.Start();
	}

	enum ScenarioType
	{
		SunSystem,
		RandomCluster,
		TwoGalaxies,
		Explosion,
		Benchmark      // nový scénář pro měření výkonu
	}

	private void LogStats()
	{
		if (particles == null || particles.Count == 0 || quadTree == null) return;

		// rozsah pozic
		float minX = float.MaxValue, minY = float.MaxValue;
		float maxX = float.MinValue, maxY = float.MinValue;
		foreach (var p in particles)
		{
			if (float.IsNaN(p.Position.X) || float.IsNaN(p.Position.Y)) continue;
			if (p.Position.X < minX) minX = p.Position.X;
			if (p.Position.Y < minY) minY = p.Position.Y;
			if (p.Position.X > maxX) maxX = p.Position.X;
			if (p.Position.Y > maxY) maxY = p.Position.Y;
		}

		var bounds = quadTree.Bounds;
		Console.WriteLine($"[Log {logCounter++}] Particles: {particles.Count}, pos min=({minX:F1},{minY:F1}) max=({maxX:F1},{maxY:F1})");
		Console.WriteLine($" Root bounds: X={bounds.X:F1} Y={bounds.Y:F1} W={bounds.Width:F1} H={bounds.Height:F1}");
		Console.WriteLine($" Nodes={quadTree.CountNodes()}, Leaves={quadTree.CountLeaves()}, MaxDepth={quadTree.MaxDepth()}");
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

			case ScenarioType.Benchmark:
				CreateBenchmarkScenario();
				break;
		}
	}

	private void CreateBenchmarkScenario()
	{
		
		  particles.Clear();
		
		  //Inicializace částic a prostoru
		
			particles.Add(new Particle(1, new Vector(400, 400), new Vector(0.0f, 0.0f), 1200.0f));
		  particles.Add(new Particle(2,new Vector(400, 200), new Vector(1.3f, 0.0f), 0.1f));
			particles.Add(new Particle(3,new Vector(400, 450), new Vector(5.5f, 0), 1));
			particles.Add(new Particle(4,new Vector(400, 550), new Vector(1.25f, 0), 1));
			//new Particle(5,new Vector(150, 120), new Vector(0, 0), 10),
			//new Particle(6,new Vector(220, 180), new Vector(0, 0), 10),
			//new Particle(5,new Vector(330, 330), new Vector(0, 0), 10),
			//new Particle(6,new Vector(50, 60), new Vector(0, 0), 10),
			//new Particle(5,new Vector(75, 220), new Vector(0, 0), 10)

		Console.WriteLine($"Benchmark scénář vytvořen: {particles.Count} částic");
	}
	


	private void CreateBenchmarkScenario_()
	{
		int gridSize = 100;       // 100 × 100 = 10 000 částic
		float spacing = 8f;       // vzdálenost mezi částicemi
		float startX = 50f;
		float startY = 50f;
		float mass = 1f;

		particles.Clear();
		int id = 0;

		for (int y = 0; y < gridSize; y++)
		{
			for (int x = 0; x < gridSize; x++)
			{
				Vector2 position = new Vector2(startX + x * spacing, startY + y * spacing);
				Vector2 velocity = Vector2.Zero;
				particles.Add(new Particle(id++, position, velocity, mass));
			}
		}

		Console.WriteLine($"Benchmark scénář vytvořen: {particles.Count} částic");
	}


	private void CreateSunSystem(Random rnd)
	{
		Particle sun = new Particle(0, new Vector(425, 425), new Vector(0, 0),500);
		particles.Add(sun);

		int planetCount = 500;
		double G = 1;

		for (int i = 0; i < planetCount; i++)
		{
			double orbitRadius = 100 + rnd.NextDouble() * 300;
			double angle = 2 * Math.PI * i / planetCount;

			double x = sun.Position.X + orbitRadius * Math.Cos(angle);
			double y = sun.Position.Y + orbitRadius * Math.Sin(angle);

			double v = Math.Sqrt(G * sun.Mass / orbitRadius);
			Vector2 velocity = new Vector2((float)(-v * Math.Sin(angle)), (float)(v * Math.Cos(angle)));

			velocity.X += (float)((rnd.NextDouble() - 0.5) * 0.1);
			velocity.Y += (float)((rnd.NextDouble() - 0.5) * 0.1);

			particles.Add(new Particle(i + 1, new Vector2((float)x, (float)y), velocity, 1));
		}
	}

	private void CreateRandomCluster(Random rnd)
	{
		int count = 1000;
		for (int i = 0; i < count; i++)
		{
			double x = 425 + rnd.NextDouble() * 200 - 100;
			double y = 425 + rnd.NextDouble() * 200 - 100;
			double vx = (rnd.NextDouble() - 0.01) * 0.01;
			double vy = (rnd.NextDouble() - 0.01) * 0.01;
			double mass = 0.1;

			particles.Add(new Particle(i, new Vector2((float)x, (float)y), new Vector2((float)vx, (float)vy), (float)mass));
		}
	}

	private void CreateTwoGalaxies(Random rnd)
	{
		int galaxySize = 5000;
		double G = 1;
		double centerDistance = 500;

		Vector centerA = new Vector(325, 425);
		Vector centerB = new Vector(525, 425);

		Particle coreA = new Particle(0, centerA, new Vector(0, 0.5f), 2000);
		Particle coreB = new Particle(1, centerB, new Vector(0, -0.5f), 2000);

		particles.Add(coreA);
		particles.Add(coreB);

		for (int i = 0; i < galaxySize; i++)
		{
			double rA = 50 + rnd.NextDouble() * 150;
			double angleA = rnd.NextDouble() * Math.PI * 2;
			double xA = centerA.X + rA * Math.Cos(angleA);
			double yA = centerA.Y + rA * Math.Sin(angleA);
			double vA = Math.Sqrt(G * coreA.Mass / rA);
			Vector2 vVecA = new Vector2((float)(-vA * Math.Sin(angleA)),(float)( vA * Math.Cos(angleA)));
			vVecA += coreA.Velocity;
			particles.Add(new Particle(2 + i, new Vector2((float)xA, (float)yA), vVecA, 1));

			double rB = 50 + rnd.NextDouble() * 150;
			double angleB = rnd.NextDouble() * Math.PI * 2;
			double xB = centerB.X + rB * Math.Cos(angleB);
			double yB = centerB.Y + rB * Math.Sin(angleB);
			double vB = Math.Sqrt(G * coreB.Mass / rB);
			Vector2 vVecB = new Vector2(-(float)(vB * Math.Sin(angleB)), (float)(vB * Math.Cos(angleB)));
			vVecB += coreB.Velocity;
			particles.Add(new Particle(2 + galaxySize + i, new Vector2((float)xB, (float)yB), vVecB, 1));
		}
	}

	private void CreateExplosion(Random rnd)
	{
		int count = 1000;
		Vector2 center = new Vector(425, 425);

		for (int i = 0; i < count; i++)
		{
			float angle = (float)(rnd.NextDouble() * Math.PI * 2);
			float speed = (float)(2 + rnd.NextDouble() * 3);

			Vector2 velocity = new Vector2(
					(float)(speed * Math.Cos(angle)),
					(float)(speed * Math.Sin(angle))
			);

			particles.Add(new Particle(i, center, velocity, 1));
		}
	}

	private void RebuildTree_()
	{
		if (quadTree == null)
			return;

		quadTree.Clear();

		foreach (var p in particles)
		{
			// ✅ Zajistí, že se částice vejde do oblasti stromu
			quadTree.EnsureContains(p);

			// ✅ Vloží částici do stromu
			quadTree.Insert(p);
		}
	}

	private void RebuildTree()
	{
		// Pokud nemáme částice, žádný strom netvoříme
		if (particles == null || particles.Count == 0)
		{
			quadTree = new QuadTree(1, new RectangleF(0, 0, 1, 1));
			return;
		}

		// 1) Spočítáme ohraničující box nad všemi platnými (nenaN/Inf) pozicemi
		float minX = float.MaxValue, minY = float.MaxValue;
		float maxX = float.MinValue, maxY = float.MinValue;
		bool anyValid = false;

		foreach (var p in particles)
		{
			if (float.IsNaN(p.Position.X) || float.IsNaN(p.Position.Y) ||
					float.IsInfinity(p.Position.X) || float.IsInfinity(p.Position.Y))
				continue;

			anyValid = true;
			if (p.Position.X < minX) minX = p.Position.X;
			if (p.Position.Y < minY) minY = p.Position.Y;
			if (p.Position.X > maxX) maxX = p.Position.X;
			if (p.Position.Y > maxY) maxY = p.Position.Y;
		}

		if (!anyValid)
		{
			// žádné platné pozice — vytvoříme malý defaultní strom
			quadTree = new QuadTree(1, new RectangleF(0, 0, 1, 1));
			return;
		}

		// 2) padding a čtvercové root boundary (symetrické kolem středu)
		const float padding = 10f;
		float width = Math.Max(1f, (maxX - minX) + padding * 2f);
		float height = Math.Max(1f, (maxY - minY) + padding * 2f);
		float size = Math.Max(width, height);
		float cx = (minX + maxX) / 2f;
		float cy = (minY + maxY) / 2f;
		float left = cx - size / 2f;
		float top = cy - size / 2f;

		// 3) vytvoříme nový QuadTree jako root (tím se vyhneme postupné expanzi)
		quadTree = new QuadTree(1, new RectangleF(left, top, size, size));

		// 4) vložíme všechny platné částice
		foreach (var p in particles)
		{
			if (float.IsNaN(p.Position.X) || float.IsNaN(p.Position.Y) ||
					float.IsInfinity(p.Position.X) || float.IsInfinity(p.Position.Y))
				continue;

			// uíjistí se, že pozice spadá do root (mělo by ano), ale Insert vrací false, pokud ne
			quadTree.Insert(p);
		}
	}


	/*
	private void RebuildTree()
	{
		// pokud pole prázdné, nic nedělej
		if (particles.Count == 0) return;

		// spočítat BBOX
		float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
		foreach (var p in particles)
		{
			if (float.IsNaN(p.Position.X) || float.IsNaN(p.Position.Y)) continue;
			if (p.Position.X < minX) minX = p.Position.X;
			if (p.Position.Y < minY) minY = p.Position.Y;
			if (p.Position.X > maxX) maxX = p.Position.X;
			if (p.Position.Y > maxY) maxY = p.Position.Y;
		}

		// trochou paddingu a aby byl čtverec
		float padding = 10f;
		float width = Math.Max(1f, (maxX - minX) + padding * 2f);
		float height = Math.Max(1f, (maxY - minY) + padding * 2f);
		float size = Math.Max(width, height);
		float cx = (minX + maxX) / 2f;
		float cy = (minY + maxY) / 2f;
		float left = cx - size / 2f;
		float top = cy - size / 2f;

		// vytvořit nový root místo ExpandBoundary
		quadTree = new QuadTree(1, new RectangleF(left, top, size, size));

		// vložit všechny částice
		foreach (var p in particles)
		{
			if (float.IsNaN(p.Position.X) || float.IsNaN(p.Position.Y)) continue;
			quadTree.Insert(p);
		}
	}
	*/

	/*private void RebuildTree()
	{
		quadTree.Clear(); // zruší staré rozdělení (viz úprava níže)

		// Pokud částice vyletěly mimo, uprav hranice
		RectangleF newBounds = quadTree.Bounds;
		bool needsResize = false;

		foreach (var p in particles)
		{
			if (!newBounds.Contains((float)p.Position.X, (float)p.Position.Y))
			{
				needsResize = true;
				break;
			}
		}

		if (needsResize)
		{
			// Rozšíříme hranice kolem všech částic
			float minX = float.MaxValue, minY = float.MaxValue;
			float maxX = float.MinValue, maxY = float.MinValue;

			foreach (var p in particles)
			{
				if (p.Position.X < minX) minX = (float)p.Position.X;
				if (p.Position.Y < minY) minY = (float)p.Position.Y;
				if (p.Position.X > maxX) maxX = (float)p.Position.X;
				if (p.Position.Y > maxY) maxY = (float)p.Position.Y;
			}

			float width = maxX - minX;
			float height = maxY - minY;
			quadTree.Resize(new RectangleF(minX, minY, width, height));
		}

		// znovu vlož všechny částice
		foreach (var p in particles)
			quadTree.Insert(p);
	}
	*/


	private void DumpMaxVals()
	{
		float maxSpeed = 0f, maxAccel = 0f;
		foreach (var p in particles)
		{
			float v = p.Velocity.Length();
			float a = p.Acceleration.Length();
			if (v > maxSpeed) maxSpeed = v;
			if (a > maxAccel) maxAccel = a;
		}
		Console.WriteLine($"MaxSpeed={maxSpeed}, MaxAccel={maxAccel}");
	}


	private void StepSimulation()
	{

		// rychlý monitor
		float maxPos=0f,mp = 0f;
		foreach (var p in particles)
		{
			maxPos = MathF.Max(MathF.Abs(p.Position.X), MathF.Abs(p.Position.Y));
			if (maxPos>mp) mp = maxPos;
		}

		if (mp > 1e5f)
		{
			timer.Stop();
			Console.WriteLine("Positions exploded beyond 1e6 — simulation paused.");
			return;
		}


		// 1) Přestavíme strom podle aktuálních pozic
		RebuildTree();

		// 2) Spočítáme akcelerace z aktuálního stromu (potřebné pro "first half-kick")
		foreach (var p in particles)
			p.Acceleration = quadTree.CalculateAcceleration(p, physics.Theta) * physics.G;

		// 3) Provedeme leapfrog krok (kick-drift-kick) uvnitř physics.Update
		physics.Update(particles, quadTree);

		// 4) Aktualizujeme simul. čas (pokud ho používáš)
		simulationTime += (float)physics.DeltaTime;
		if ((int)physics.DeltaTime % 100 == 0) 
			LogStats();
	}
	private void StepSimulation_()
	{
		//quadTree = new QuadTree(1, new RectangleF(0, 0, 850, 850));
		//// 1️⃣ Zvětši hranice podle všech částic
		//foreach (var p in particles)
		//	quadTree.EnsureContains(p);

		//// 2️⃣ Postav znovu strom pro aktuální rozložení
		//quadTree = new QuadTree(1, quadTree.boundary);
		//foreach (var p in particles)
		//	quadTree.Insert(p);

		// 3️⃣ Aktualizuj pozice částic
		physics.Update(particles, quadTree);
		simulationTime += (float)physics.DeltaTime; // globální krok, kdy kontrolujeme aktualizace
		RebuildTree();
	}

	private void RunBenchmark(int iterations)
	{
		// Zastavíme timer a vypneme překreslování
		timer.Stop();

		// Uložíme původní stav částic, abychom je mohli po testu obnovit
		var originalParticles = particles.Select(p => new Particle(
				p.Id,
				new Vector2(p.Position.X, p.Position.Y),
				new Vector2(p.Velocity.X, p.Velocity.Y),
				p.Mass)).ToList();
		var enerfirst = physics.Energy(particles);
		var stopwatch = new System.Diagnostics.Stopwatch();
		stopwatch.Start();

		for (int i = 0; i < iterations; i++)
		{
			// Klasický krok simulace bez vykreslování
			StepSimulation();
			//Console.WriteLine(particles[0].Position);
		}

		stopwatch.Stop();

		// 🔄 Po benchmarku můžeme
		var enerfinal = physics.Energy(particles);
		double enrozdil = enerfinal-enerfirst;
		double enrelativprocrozdil = 100*enrozdil/enerfirst;
		Console.WriteLine($"enfirst={enerfirst}  enfin={enerfinal}  enrelat={enrelativprocrozdil}");

		// Výsledek
		MessageBox.Show(
				$"Benchmark dokončen.\nPočet iterací: {iterations}\nČas: {stopwatch.ElapsedMilliseconds} ms",
				"Výkon simulace",
				MessageBoxButtons.OK,
				MessageBoxIcon.Information
		);


		// Obnovíme částice do původního stavu
		particles = originalParticles;
		RebuildTree();
		// Znovu spustíme timer (grafickou simulaci)
		timer.Start();
	}


	protected override void OnPaint(PaintEventArgs e)
	{
		var g = e.Graphics;
		base.OnPaint(e);
		var clip = e.ClipRectangle;
		bool drawQuadTree = true;
		if (drawQuadTree)
			quadTree?.Draw(e.Graphics, e.ClipRectangle, maxDepth: 8);
		//quadTree?.Draw(e.Graphics, clip, maxDepth: 12);

		foreach (var p in particles)
		{
			g.FillEllipse(Brushes.Blue, (float)p.Position.X - 2, (float)p.Position.Y - 2, 4, 4);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;


class BarnesHutSimulation
{

	static void Main()
	{
		List<Particle> particles= new List<Particle>();
		var rnd = new Random();		
		
		// Inicializace částic a prostoru
		//List<Particle> particles = new List<Particle>
		//		{
		//				new Particle(1,new Vector(500, 500), new Vector(0, 0), 4000),
		//				new Particle(2,new Vector(500, 650), new Vector(2, 0), 1),
		//				new Particle(3,new Vector(500, 750), new Vector(1.5, 0), 1),
		//				new Particle(4,new Vector(500, 850), new Vector(1, 0), 1),
		//				//new Particle(5,new Vector(150, 120), new Vector(0, 0), 10),
		//				//new Particle(6,new Vector(220, 180), new Vector(0, 0), 10),
		//				//new Particle(5,new Vector(330, 330), new Vector(0, 0), 10),
		//				//new Particle(6,new Vector(50, 60), new Vector(0, 0), 10),
		//				//new Particle(5,new Vector(75, 220), new Vector(0, 0), 10)
		//		};


		/*for (int i = 0; i < 1000; i++)
		{
			particles.Add(new Particle(
				i,
				new Vector(rnd.Next(100, 750), rnd.Next(100, 750)),
				new Vector(0, 0),
				mass: rnd.NextDouble() * 5 + 5
			));
		}*/

		var quadTree = new QuadTree(1, new RectangleF(0, 0, 850, 850));
		var physics = new PhysicsEngine(theta: 1.5f, deltaTime: 1.1f, g: 1);

		Application.Run(new ParticleForm(particles, quadTree, physics));
	}

}

/*
class Program
{
	static void Main()
	{
		//List<Particle> particles = new List<Particle>();
		//int pocetCastic = 50;
		//int xMin = 0, yMin = 0;
		//int xMax = 850, yMax = 850;
		//int xmin, ymin;
		//Random rnd = new Random();
		//for (int i = 0; i<pocetCastic; i++)
		//{
		//	xmin=rnd.Next(xMin, xMax);
		//	ymin=rnd.Next(yMin, yMax);
		//	particles.Add(new Particle(i,new Vector(xmin, ymin), new Vector(0, 0), 10));
		//}

		// Inicializace částic a prostoru
		List<Particle> particles = new List<Particle>
				{
						new Particle(1,new Vector(10, 100), new Vector(0, 0), 10),
						new Particle(2,new Vector(20, 150), new Vector(0, 0), 10),
						new Particle(3,new Vector(300, 300), new Vector(0, 0), 10),
						new Particle(4,new Vector(50, 50), new Vector(0, 0), 10),
						//new Particle(5,new Vector(150, 120), new Vector(0, 0), 10),
						//new Particle(6,new Vector(220, 180), new Vector(0, 0), 10),
						new Particle(5,new Vector(330, 330), new Vector(0, 0), 10),
						new Particle(6,new Vector(50, 60), new Vector(0, 0), 10),
						//new Particle(5,new Vector(75, 220), new Vector(0, 0), 10)
				};

		QuadTree quadTree = new QuadTree(1,new RectangleF(0, 0, 850, 850));

		foreach (var particle in particles)
		{
			quadTree.Insert(particle);
		}

		// Spustíme aplikaci
		Application.Run(new ParticleForm(particles, quadTree));
	}
}
*/
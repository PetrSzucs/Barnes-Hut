using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

class Particle
{
	public int Id;
	public Vector Position;
	public Vector Velocity;
	public double Mass;

	public Particle(int id,Vector position, Vector velocity, double mass)
	{
		Id=id;
		Position = position;
		Velocity = velocity;
		Mass = mass;
	}
}

class Vector
{
	public double X;
	public double Y;

	public Vector(double x, double y)
	{
		X = x;
		Y = y;
	}

	public static Vector operator +(Vector a, Vector b)
	{
		return new Vector(a.X + b.X, a.Y + b.Y);
	}

	public static Vector operator -(Vector a, Vector b)
	{
		return new Vector(a.X - b.X, a.Y - b.Y);
	}
	public bool IsCloseTo(Vector other, double epsilon = 0.01)
	{
		return Math.Abs(X - other.X) < epsilon && Math.Abs(Y - other.Y) < epsilon;
	}
	public double Magnitude => Math.Sqrt(X * X + Y * Y);
	public Vector Normalize() => new Vector(X / Magnitude, Y / Magnitude);
}

class QuadTree
{
	private const float MinSize = 1.0f; // minimální velikost kvadrantu
	private const double PositionTolerance = 0.01; // tolerance pro porovnání pozic

	private int capacity;
	private RectangleF boundary;
	private List<Particle> particles;
	private QuadTree[] subtrees;
	private Vector centerOfMass;
	private double totalMass;
	private bool divided;
	private int depth;

	public QuadTree(int capacity, RectangleF boundary, int depth = 0)
	{
		this.capacity = capacity;
		this.boundary = boundary;
		this.depth = depth;
		this.particles = new List<Particle>();
		this.subtrees = null;
		this.centerOfMass = new Vector(0, 0);
		this.totalMass = 0;
		this.divided = false;
	}

	public bool Insert(Particle particle)
	{
		if (!boundary.Contains(new PointF((float)particle.Position.X, (float)particle.Position.Y)))
			return false;

		// Pokud už máme podstromy, vlož přímo do nich
		if (subtrees != null)
		{
			foreach (var subtree in subtrees)
			{
				if (subtree.Insert(particle))
				{
					UpdateCenterOfMass(particle);
					return true;
				}
			}
			return false;
		}

		// Pokud částice je příliš blízko jiné, nevytvářej podstrom
		foreach (var existing in particles)
		{
			if (particle.Position.IsCloseTo(existing.Position, PositionTolerance))
			{
				particles.Add(particle);
				UpdateCenterOfMass(particle);
				return true;
			}
		}

		// Pokud máme místo, přidej částici
		if (particles.Count < capacity)
		{
			particles.Add(particle);
			UpdateCenterOfMass(particle);
			return true;
		}

		// Pokud je kvadrant příliš malý, nepokračuj v dělení
		if (boundary.Width / 2 < MinSize || boundary.Height / 2 < MinSize)
		{
			particles.Add(particle);
			UpdateCenterOfMass(particle);
			return true;
		}

		// Rozděl kvadrant
		Subdivide();

		// Přesuň existující částice do podstromů
		for (int i = particles.Count - 1; i >= 0; i--)
		{
			var p = particles[i];
			foreach (var subtree in subtrees)
			{
				if (subtree.Insert(p))
				{
					particles.RemoveAt(i);
					break;
				}
			}
		}

		// Vlož novou částici do podstromu
		foreach (var subtree in subtrees)
		{
			if (subtree.Insert(particle))
			{
				UpdateCenterOfMass(particle);
				return true;
			}
		}

		return false;
	}

	private void Subdivide()
	{
		float x = boundary.X;
		float y = boundary.Y;
		float w = boundary.Width / 2;
		float h = boundary.Height / 2;

		subtrees = new QuadTree[4];
		subtrees[0] = new QuadTree(capacity, new RectangleF(x, y, w, h), depth + 1);
		subtrees[1] = new QuadTree(capacity, new RectangleF(x + w, y, w, h), depth + 1);
		subtrees[2] = new QuadTree(capacity, new RectangleF(x, y + h, w, h), depth + 1);
		subtrees[3] = new QuadTree(capacity, new RectangleF(x + w, y + h, w, h), depth + 1);

		divided = true;
	}

	private void UpdateCenterOfMass(Particle particle)
	{
		totalMass += particle.Mass;
		centerOfMass.X = (centerOfMass.X * (totalMass - particle.Mass) + particle.Position.X * particle.Mass) / totalMass;
		centerOfMass.Y = (centerOfMass.Y * (totalMass - particle.Mass) + particle.Position.Y * particle.Mass) / totalMass;
	}

	public Vector CalculateForce(Particle particle, double theta = 0.5)
	{
		// Pokud není v tomto kvadrantu žádná hmota, návrat nulové síly
		if (totalMass == 0)
			return new Vector(0, 0);

		// Neber v úvahu kvadrant, pokud obsahuje pouze danou částici
		if (particles.Count == 1 && particles[0].Id == particle.Id)
			return new Vector(0, 0);

		// Výpočet vzdálenosti ke středu hmotnosti
		double dx = centerOfMass.X - particle.Position.X;
		double dy = centerOfMass.Y - particle.Position.Y;
		double distance = Math.Sqrt(dx * dx + dy * dy) + 1e-5; // malý epsilon kvůli stabilitě
		double size = Math.Max(boundary.Width, boundary.Height);

		// Poměr rozměru kvadrantu k vzdálenosti (Barnes–Hut podmínka)
		double ratio = size / distance;

		if (subtrees == null || ratio < theta)
		{
			// Použij aproximaci - celý podstrom jako jeden objekt
			double G = 1.0; // gravitační konstanta (pro jednoduchost)
			double forceMagnitude = G * particle.Mass * totalMass / (distance * distance);

			Vector direction = new Vector(dx / distance, dy / distance);
			return new Vector(direction.X * forceMagnitude, direction.Y * forceMagnitude);
		}
		else
		{
			// Rekurzivně spočítej síly ze všech podstromů
			Vector totalForce = new Vector(0, 0);
			foreach (var subtree in subtrees)
			{
				totalForce += subtree.CalculateForce(particle, theta);
			}
			return totalForce;
		}
	}


	//public Vector CalculateForce(Particle particle, double theta = 5)
	//{
	//	if (particles.Count == 0 && subtrees == null)
	//		return new Vector(0, 0);

	//	double distance = Math.Sqrt(Math.Pow(particle.Position.X - centerOfMass.X, 2) + Math.Pow(particle.Position.Y - centerOfMass.Y, 2));
	//	double size = Math.Max(boundary.Width, boundary.Height);

	//	if (subtrees == null || size / distance < theta)
	//	{
	//		double forceMagnitude = (particle.Mass * totalMass) / (distance * distance + 1e-5); // přidáno epsilon pro stabilitu
	//		Vector direction = new Vector(centerOfMass.X - particle.Position.X, centerOfMass.Y - particle.Position.Y).Normalize();
	//		return new Vector(direction.X * forceMagnitude, direction.Y * forceMagnitude);
	//	}
	//	else
	//	{
	//		Vector totalForce = new Vector(0, 0);
	//		foreach (var subtree in subtrees)
	//		{
	//			totalForce += subtree.CalculateForce(particle, theta);
	//		}
	//		return totalForce;
	//	}
	//}

	public void Draw(Graphics g)
	{
		g.DrawRectangle(Pens.Red, boundary.X, boundary.Y, boundary.Width, boundary.Height);
		//g.DrawString($"{totalMass}", SystemFonts.DefaultFont, Brushes.Green, (float)centerOfMass.X-0, (float)centerOfMass.Y-0);

		if (totalMass > 0)
		{
			double size = Math.Max(boundary.Width, boundary.Height);
			g.DrawString($"s/d={(size / Math.Sqrt(Math.Pow(centerOfMass.X - boundary.X, 2) + Math.Pow(centerOfMass.Y - boundary.Y, 2))):F2}",
					SystemFonts.DefaultFont, Brushes.Gray, (float)centerOfMass.X + 5, (float)centerOfMass.Y + 5);
		}

		if (subtrees != null)
		{
			foreach (var subtree in subtrees)
			{
				subtree.Draw(g);
			}
		}
	}
}


/*
class QuadTree
{
	private int capacity;
	private RectangleF boundary;
	private List<Particle> particles;
	private QuadTree[] subtrees;
	private Vector centerOfMass;
	private double totalMass;
	


	public QuadTree( int capacity,RectangleF boundary)
	{
		this.boundary = boundary;
		this.particles = new List<Particle>();
		this.subtrees = null;
		this.centerOfMass = new Vector(0, 0);
		this.totalMass = 0;		
		this.capacity=capacity;
	}

	//public bool Insert(Particle particle)
	//{
	//	if (!boundary.Contains(new PointF((float)particle.Position.X, (float)particle.Position.Y)))
	//		return false;

	//	// Přidej částici
	//	particles.Add(particle);
	//	UpdateCenterOfMass(particle);

	//	// Pokud máme více než jednu částici, musíme kvadrant rozdělit
	//	if (particles.Count > 1)
	//	{
	//		// Pokud nejsou subtrees, rozdělíme kvadrant
	//		if (subtrees == null)
	//			Subdivide();

	//		// Přesuneme částice do podstromů
	//		for (int i = particles.Count - 1; i >= 0; i--)
	//		{
	//			Particle p = particles[i];
	//			foreach (var subtree in subtrees)
	//			{
	//				if (subtree.Insert(p))
	//				{
	//					particles.RemoveAt(i);
	//					break;
	//				}
	//			}
	//		}
	//	}

	//	return true;
	//}

	public bool Insert(Particle particle)
	{
		if (!boundary.Contains(new PointF((float)particle.Position.X, (float)particle.Position.Y)))
			return false;

		if (subtrees == null && particles.Count < capacity)
		{
			particles.Add(particle);
			UpdateCenterOfMass(particle);
			return true;
		}

		if (subtrees == null)
		{
			Subdivide();
			
			// Přesun existujících částic do podstromů
			for (int i = particles.Count - 1; i >= 0; i--)
			{
				Particle p = particles[i];
				foreach (var subtree in subtrees)
				{
					if (subtree.Insert(p))
					{
						particles.RemoveAt(i);
						break;
					}
				}
			}
		}

		// Vlož novou částici do podstromu
		foreach (var subtree in subtrees)
		{
			if (subtree.Insert(particle))
			{
				UpdateCenterOfMass(particle);
				return true;
			}
		}

		return false;
	}


	//public bool Insert(Particle particle)
	//{
	//	if (!boundary.Contains(new PointF((float)particle.Position.X, (float)particle.Position.Y)))
	//		return false;

	//	if (this.particles.Count<this.capacity)
	//		this.particles.Add(particle);
	//	else
	//	{

	//		// Pokud nejsou subtrees, rozdělíme kvadrant
	//		if (subtrees == null)
	//		{
	//			Subdivide();
	//			this.divided=true;
	//		}

	//		// Přidej částici
	//		this.particles.Add(particle);
	//		UpdateCenterOfMass(particle);


	//		// Přesuneme částice do podstromů
	//		for (int i = particles.Count - 1; i >= 0; i--)
	//		{
	//			Particle p = particles[i];
	//			foreach (var subtree in subtrees)
	//			{
	//				if (subtree.Insert(p))
	//				{
	//					particles.RemoveAt(i);
	//					break;
	//				}
	//			}
	//		}
		
	//	}

	//	return true;
	//}

	private void Subdivide()
	{
		float x = boundary.X;
		float y = boundary.Y;
		float w = boundary.Width / 2;
		float h = boundary.Height / 2;

		subtrees = new QuadTree[4];
		subtrees[0] = new QuadTree(this.capacity,new RectangleF(x, y, w, h));
		subtrees[1] = new QuadTree(this.capacity, new RectangleF(x + w, y, w, h));
		subtrees[2] = new QuadTree(this.capacity, new RectangleF(x, y + h, w, h));
		subtrees[3] = new QuadTree(this.capacity, new RectangleF(x + w, y + h, w, h));
	}

	private void UpdateCenterOfMass(Particle particle)
	{
		totalMass += particle.Mass;
		centerOfMass.X = (centerOfMass.X * (totalMass - particle.Mass) + particle.Position.X * particle.Mass) / totalMass;
		centerOfMass.Y = (centerOfMass.Y * (totalMass - particle.Mass) + particle.Position.Y * particle.Mass) / totalMass;
	}

	public Vector CalculateForce(Particle particle, double theta = 5)
	{
		if (particles.Count == 0)
			return new Vector(0, 0);

		double distance = Math.Sqrt(Math.Pow(particle.Position.X - centerOfMass.X, 2) + Math.Pow(particle.Position.Y - centerOfMass.Y, 2));
		double size = Math.Max(boundary.Width, boundary.Height);

		if (size / distance < theta)
		{
			// Použijeme střed hmotnosti pro výpočet síly
			double forceMagnitude = (particle.Mass * totalMass) / (distance * distance);
			Vector direction = new Vector(centerOfMass.X - particle.Position.X, centerOfMass.Y - particle.Position.Y).Normalize();
			return new Vector(direction.X * forceMagnitude, direction.Y * forceMagnitude);
		}
		else
		{
			Vector totalForce = new Vector(0, 0);
			if (subtrees != null)
			{
				foreach (var subtree in subtrees)
				{
					totalForce += subtree.CalculateForce(particle, theta);
				}
			}
			return totalForce;
		}
	}

	public void Draw(Graphics g)
	{
		// Vykresli obdélník pro aktuální kvadrant
		g.DrawRectangle(Pens.Red, boundary.X, boundary.Y, boundary.Width, boundary.Height);

		// Pokud má tento QuadTree podstromečky, zavoláme Draw na každý z nich
		if (subtrees != null)
		{
			foreach (var subtree in subtrees)
			{
				subtree.Draw(g);
			}
		}
	}
}
*/
class ParticleForm : Form
{
	private List<Particle> particles;
	private QuadTree quadTree;

	public ParticleForm(List<Particle> particles, QuadTree quadTree)
	{
		this.particles = particles;
		this.quadTree = quadTree;
		this.DoubleBuffered = true;
		this.ClientSize = new Size(850, 850);
		this.Paint += new PaintEventHandler(OnPaint);
		this.Text = "Particle Simulation with QuadTree";
	}

	private void OnPaint(object sender, PaintEventArgs e)
	{
		Graphics g = e.Graphics;

		// Vykreslení kvadrantů
		quadTree.Draw(g);
		string output;
		// Vykreslení částic
		foreach (var particle in particles)
		{
			output=string.Format("{0} {1} {2} {3}",particle.Id.ToString(),particle.Position.X.ToString(), particle.Position.Y.ToString(), particle.Mass.ToString());
			g.FillEllipse(Brushes.Blue, (float)particle.Position.X - 2, (float)particle.Position.Y - 2, 4, 4);
			//g.DrawString($"{particle.Mass}", SystemFonts.DefaultFont, Brushes.Green, (float)particle.Position.X-5, (float)particle.Position.Y-15);
			//g.DrawString(output, this.Font, Brushes.Black, (float)particle.Position.X, (float)particle.Position.Y);
		}
	}
}

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

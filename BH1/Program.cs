using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

class Particle
{
	public Vector Position;
	public Vector Velocity;
	public double Mass;

	public Particle(Vector position, Vector velocity, double mass)
	{
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

	public double Magnitude => Math.Sqrt(X * X + Y * Y);
	public Vector Normalize() => new Vector(X / Magnitude, Y / Magnitude);
}

class QuadTree
{
	private RectangleF boundary;
	private List<Particle> particles;
	private QuadTree[] subtrees;
	private Vector centerOfMass;
	private double totalMass;

	public QuadTree(RectangleF boundary)
	{
		this.boundary = boundary;
		particles = new List<Particle>();
		subtrees = null;
		centerOfMass = new Vector(0, 0);
		totalMass = 0;
	}

	public bool Insert(Particle particle)
	{
		if (!boundary.Contains(new PointF((float)particle.Position.X, (float)particle.Position.Y)))
			return false;

		// Přidej částici
		particles.Add(particle);
		UpdateCenterOfMass(particle);

		// Pokud máme více než jednu částici, musíme kvadrant rozdělit
		if (particles.Count > 1)
		{
			// Pokud nejsou subtrees, rozdělíme kvadrant
			if (subtrees == null)
				Subdivide();

			// Přesuneme částice do podstromů
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

		return true;
	}

	private void Subdivide()
	{
		float x = boundary.X;
		float y = boundary.Y;
		float w = boundary.Width / 2;
		float h = boundary.Height / 2;

		subtrees = new QuadTree[4];
		subtrees[0] = new QuadTree(new RectangleF(x, y, w, h));
		subtrees[1] = new QuadTree(new RectangleF(x + w, y, w, h));
		subtrees[2] = new QuadTree(new RectangleF(x, y + h, w, h));
		subtrees[3] = new QuadTree(new RectangleF(x + w, y + h, w, h));
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

class ParticleForm : Form
{
	private List<Particle> particles;
	private QuadTree quadTree;

	public ParticleForm(List<Particle> particles, QuadTree quadTree)
	{
		this.particles = particles;
		this.quadTree = quadTree;
		this.DoubleBuffered = true;
		this.ClientSize = new Size(400, 400);
		this.Paint += new PaintEventHandler(OnPaint);
		this.Text = "Particle Simulation with QuadTree";
	}

	private void OnPaint(object sender, PaintEventArgs e)
	{
		Graphics g = e.Graphics;

		// Vykreslení kvadrantů
		quadTree.Draw(g);

		// Vykreslení částic
		foreach (var particle in particles)
		{
			g.FillEllipse(Brushes.Blue, (float)particle.Position.X - 2, (float)particle.Position.Y - 2, 4, 4);
		}
	}
}

class Program
{
	static void Main()
	{
		// Inicializace částic a prostoru
		List<Particle> particles = new List<Particle>
				{
						new Particle(new Vector(10, 100), new Vector(0, 0), 10),
						new Particle(new Vector(20, 200), new Vector(0, 0), 10),
						new Particle(new Vector(300, 300), new Vector(0, 0), 10),
						new Particle(new Vector(100, 50), new Vector(0, 0), 10),
						new Particle(new Vector(150, 120), new Vector(0, 0), 10),
						new Particle(new Vector(220, 180), new Vector(0, 0), 10),
						new Particle(new Vector(330, 330), new Vector(0, 0), 10),
						new Particle(new Vector(50, 60), new Vector(0, 0), 10),
						new Particle(new Vector(75, 220), new Vector(0, 0), 10)
				};

		QuadTree quadTree = new QuadTree(new RectangleF(0, 0, 400, 400));

		foreach (var particle in particles)
		{
			quadTree.Insert(particle);
		}

		// Spustíme aplikaci
		Application.Run(new ParticleForm(particles, quadTree));
	}
}

using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

public class QuadTree
{
	private int capacity;
	public RectangleF boundary;
	private List<Particle> particles;
	private QuadTree[] subtrees;
	private Vector2 centerOfMass;
	private float totalMass;
	public RectangleF Bounds => boundary;

	public QuadTree(int capacity, RectangleF boundary)
	{
		this.capacity = capacity;
		this.boundary = boundary;
		this.particles = new List<Particle>();
		this.subtrees = null;
		this.centerOfMass = Vector2.Zero;
		this.totalMass = 0;
	}


	public void Clear()
	{
		particles.Clear();
		if (subtrees != null)
		{
			foreach (var s in subtrees)
				s?.Clear();
		}
		subtrees = null;
		totalMass = 0;
		centerOfMass = Vector2.Zero;
	}

	public void Resize(RectangleF newBounds)
	{
		boundary = newBounds;
		Clear();
	}

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
		}

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
		subtrees[0] = new QuadTree(capacity, new RectangleF(x, y, w, h));
		subtrees[1] = new QuadTree(capacity, new RectangleF(x + w, y, w, h));
		subtrees[2] = new QuadTree(capacity, new RectangleF(x, y + h, w, h));
		subtrees[3] = new QuadTree(capacity, new RectangleF(x + w, y + h, w, h));
	}

	private void UpdateCenterOfMass(Particle particle)
	{
		totalMass += particle.Mass;
		centerOfMass.X = (centerOfMass.X * (totalMass - particle.Mass) + particle.Position.X * particle.Mass) / totalMass;
		centerOfMass.Y = (centerOfMass.Y * (totalMass - particle.Mass) + particle.Position.Y * particle.Mass) / totalMass;
	}
	public Vector2 CalculateAcceleration(Particle p, float theta, float G = 1f)
	{
		// prázdný uzel
		if (totalMass <= 0f)
			return Vector2.Zero;

		// pokud je tento list jen s touto částicí, ignoruj
		if (subtrees == null && particles != null && particles.Count == 1 && particles[0] == p)
			return Vector2.Zero;

		// směr od p k hmotnému středu
		Vector2 direction = centerOfMass - p.Position;
		float distSq = direction.LengthSquared();

		// ochrana proti dělení nulou a NaN
		if (distSq < 1e-10f || float.IsNaN(distSq))
			return Vector2.Zero;

		float distance = MathF.Sqrt(distSq);
		float size = MathF.Max(boundary.Width, boundary.Height);

		// rozhodnutí Barnes-Hut: aproximovat nebo rozbalit
		if (subtrees == null || subtrees.Length == 0 || (size / distance) < theta)
		{
			// Pokud tento uzel **obsahuje i cílovou částici p**, můžeme (volitelně)
			// odečíst její příspěvek z celkové hmotnosti a středu hmoty,
			// aby se zabránilo "self-force" aprox.
			float effMass = totalMass;
			Vector2 effCenter = centerOfMass;

			if (particles != null && particles.Count > 0 && particles.Contains(p))
			{
				// odečteme příspěvek p
				effMass = totalMass - p.Mass;
				if (effMass <= 0f)
					return Vector2.Zero; // po odečtení nic nezbývá

				effCenter = (centerOfMass * totalMass - p.Position * p.Mass) / effMass;

				// přepočítej směr a vzdálenost vůči efektivnímu středu
				direction = effCenter - p.Position;
				distSq = direction.LengthSquared();
				if (distSq < 1e-10f || float.IsNaN(distSq))
					return Vector2.Zero;
				distance = MathF.Sqrt(distSq);
			}

			// výpočet akcelerace: a = G * M / r^2 * (dir / r) = G * M * dir / r^3
			float invDist = 1f / distance;
			float invDist3 = invDist / distSq; // = 1 / (r^3)

			Vector2 accel = direction * (G * effMass * invDist3);

			// bezpečnostní clamp (volitelné) - zabrání extrémním akceleracím
			if (float.IsNaN(accel.X) || float.IsNaN(accel.Y))
				return Vector2.Zero;

			return accel;
		}
		else
		{
			// rekurzivní průchod podstromy
			Vector2 acc = Vector2.Zero;
			if (subtrees != null)
			{
				foreach (var st in subtrees)
				{
					if (st != null)
						acc += st.CalculateAcceleration(p, theta, G);
				}
			}
			return acc;
		}
	}


	/*
	public Vector2 CalculateForce(Particle p, float theta)
	{
		// Pokud uzel neobsahuje částice
		if ((particles == null || particles.Count == 0) && subtrees == null)
			return Vector2.Zero;

		if ((subtrees==null) && particles.Count==1 && particles[0]==p)
			return Vector2.Zero;

		Vector2 dir = centerOfMass - p.Position;
		float distSq = dir.LengthSquared();

		// Pokud je vzdálenost nulová nebo nesmyslná, sílu ignorujeme
		if (distSq < 1e-6f || float.IsNaN(distSq))
			return Vector2.Zero;

		float distance = MathF.Sqrt(distSq);
		float size = MathF.Max(boundary.Width, boundary.Height);

		// Barnes–Hut zjednodušení
		if (subtrees == null || size / distance < theta)
		{
			// Gravitační konstanta (pokud používáš nějaké měřítko)
			const float G = 1f;

			float forceMag = G * (p.Mass * totalMass) / distSq;

			// Omez příliš velké síly, aby nevznikaly přetížení
			//if (forceMag > 1e6f)
				//forceMag = 1e6f;

			Vector2 dirNorm = dir / distance;
			return dirNorm * forceMag;
		}
		else
		{
			Vector2 totalForce = Vector2.Zero;

			foreach (var st in subtrees)
			{
				totalForce += st.CalculateForce(p, theta);
			}

			return totalForce;
		}
	}*/


	//public Vector2 CalculateForce_(Particle p, float theta)
	//{
	//	if (particles.Count == 0 && subtrees == null)
	//		return Vector2.Zero;

	//	Vector2 dir = centerOfMass - p.Position;
	//	float distance = dir.Length() + 1e-5f;
	//	float size = Math.Max(boundary.Width, boundary.Height);

	//	if (subtrees == null || size / distance < theta)
	//	{
	//		float forceMag = (p.Mass * totalMass) / (distance * distance);
	//		return Vector2.Normalize(dir) * forceMag;
	//	}
	//	else
	//	{
	//		Vector2 totalForce = Vector2.Zero;
	//		foreach (var st in subtrees)
	//			totalForce += st.CalculateForce(p, theta);
	//		return totalForce;
	//	}
	//}


	public void EnsureContains(Particle particle)
	{
		// pokud je částice mimo, expanduj dokud se nevejde
		while (!boundary.Contains(particle.Position.X, particle.Position.Y))
		{
			ExpandBoundary(particle.Position);
		}
	}

	private void ExpandBoundary(Vector2 pos)
	{
		float newWidth = boundary.Width * 2;
		float newHeight = boundary.Height * 2;
		float newX = boundary.X;
		float newY = boundary.Y;

		if (pos.X < boundary.X) newX = boundary.X - boundary.Width;
		if (pos.X > boundary.X + boundary.Width) newX = boundary.X;
		if (pos.Y < boundary.Y) newY = boundary.Y - boundary.Height;
		if (pos.Y > boundary.Y + boundary.Height) newY = boundary.Y;

		boundary = new RectangleF(newX, newY, newWidth, newHeight);
	
	}

	/*
	private void ExpandBoundary(Vector pos)
	{
		float x = boundary.X;
		float y = boundary.Y;
		float w = boundary.Width;
		float h = boundary.Height;

		// zvětši oblast 2×
		float newW = w * 2;
		float newH = h * 2;
		float newX = x;
		float newY = y;

		// umístění původního boundary podle polohy částice
		if (pos.X < x) newX -= w;
		if (pos.Y < y) newY -= h;

		// nový větší boundary
		var newBoundary = new RectangleF(newX, newY, newW, newH);

		// vytvoř nový root strom
		var newRoot = new QuadTree(this.capacity, newBoundary);

		// přenést původní částice
		foreach (var p in this.particles)
			newRoot.Insert(p);

		// přenést podstromy, pokud existují
		if (this.subtrees != null)
		{
			foreach (var subtree in this.subtrees)
			{
				subtree.TransferParticlesTo(newRoot);
			}
		}

		// nahradit staré hodnoty
		this.boundary = newBoundary;
		this.particles = newRoot.particles;
		this.subtrees = newRoot.subtrees;
		this.totalMass = newRoot.totalMass;
		this.centerOfMass = newRoot.centerOfMass;
	}

	private void TransferParticlesTo(QuadTree target)
	{
		foreach (var p in this.particles)
			target.Insert(p);
		if (this.subtrees != null)
		{
			foreach (var subtree in this.subtrees)
				subtree.TransferParticlesTo(target);
		}
	}*/


	public void Draw(Graphics g)
	{
		g.DrawRectangle(Pens.Red, boundary.X, boundary.Y, boundary.Width, boundary.Height);
		//g.FillEllipse(Brushes.Red, (float)centerOfMass.X - 2, (float)centerOfMass.Y - 2, 4, 4);

		if (subtrees != null)
			foreach (var subtree in subtrees)
				subtree.Draw(g);
	}
}

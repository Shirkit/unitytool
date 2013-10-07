using UnityEngine;

public class Distribution
{
	
	public class Pair
	{
		public int x, y;

		public Pair (int x, int y)
		{
			this.x = x;
			this.y = y;
		}
	}
	
	private int size;
	private float[] prob, probCumulative;
	private Pair[] pairs;
	
	public Distribution (int size, params Pair[] pairs)
	{
		this.size = size;
		if (pairs == null) {
			pairs = new Pair[0];
		}
		this.pairs = pairs;
		this.prob = new float[size * size];
		
		Compute ();
	}
	
	private void Compute ()
	{
		probCumulative = new float[size * size];
		
		float sumAll = 0f;
		for (int i = 0; i < prob.Length; i++) {
			// Compute the Row and Col for the point
			int posx = i / size;
			int posy = i % size;
			prob [i] = 1f;
			
			foreach (Pair p in pairs) {
				prob [i] += 1f / (Mathf.Pow ((posx - p.x), 2) + Mathf.Pow (posy - p.y, 2) + 0.1f);
			}
			
			// Sum of all probabilities
			sumAll += prob [i];
		}
		
		// Normalize the data
		for (int i = 0; i < prob.Length; i++) {
			prob [i] /= sumAll;
		}
		
		sumAll = 0f;
		// Do the cumulative function
		for (int i = 0; i < prob.Length; i++) {
			probCumulative [i] = sumAll;
			sumAll += prob [i];
		}
	}
	
	private float F (int y)
	{
		return probCumulative [y];
	}
	
	private int X (float p, int i, int u, int j)
	{
		if (p >= F (u)) {
			if (u == j)
				return u;
			
			if (p <= F (u + 1))
				return u;
			else
				return X (p, u, Mathf.CeilToInt ((((float)(j - u)) / 2f) + u), j);
		} else
			return X (p, i, Mathf.FloorToInt ((((float)u - i)) / 2f) + i, u);
	}
	
	public Pair NextRandom ()
	{
		int random = X (Random.Range (0f, 1f), 0, Mathf.FloorToInt ((prob.Length - 1) * 0.5f), prob.Length - 1);
		Pair pair = new Pair (random / size, random % size);
		return pair;
	}
	
	public int NextRandomDebug ()
	{
		int random = X (Random.Range (0f, 1f), 0, Mathf.FloorToInt ((prob.Length - 1f) * 0.5f), prob.Length - 1);
		return random;
	}
}

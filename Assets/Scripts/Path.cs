using System;
using System.Collections.Generic;
using UnityEngine;

public class Path
{
	public String name;
	public List<Node> points;
	public Color color;
	// Metrics
	public float time, length2d, length3d, danger, los, danger3, los3, danger3Norm, los3Norm, crazy, velocity;
	
	public Path (List<Node> points)
	{
		this.points = points;
		if (points == null)
			throw new ArgumentNullException("Points can't be null");
	}
	
	public void ZeroValues() {
		time = length2d = length3d = danger = los = danger3 = los3 = danger3Norm = los3Norm = crazy = velocity = 0f;
	}
}

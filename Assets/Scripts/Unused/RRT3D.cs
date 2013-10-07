using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

public class RRT3D
{
	
	public Dictionary<string, Node> nodeMap;
	private Cell[][][] nodeMatrix;
	private float angle;
	
	public void Start ()
	{
	}
	
	// Gets the node at specified position from the NodeMap, or create the Node based on the Cell position for that Node
	private Node GetNode (int t, int x, int y)
	{
		Node n = null;
		nodeMap.TryGetValue (t + "-" + x + "-" + y, out n);
		if (n == null) {
			n = new Node ();
			n.x = x;
			n.y = y;
			n.t = t;
			try {
				n.cell = nodeMatrix [t] [x][y];
			} catch {
				Debug.Log (t + "-" + x + "-" + y);
				Debug.Log (n);
				Debug.Log (nodeMatrix.GetLength (t));
				Debug.Log (nodeMatrix [t]);
			}
			nodeMap.Add (n.ToString (), n);
		}
		return n;
	}

	public List<Node> Compute (int startX, int startY, int endX, int endY, int attemps, float speed, Cell[][][] matrix, bool smooth = false)
	{
		// Initialization
		nodeMap = new Dictionary<string, Node> ();
		nodeMatrix = matrix;
		
		//Start and ending node
		Node start = GetNode (0, startX, startY);
		start.visited = true; 
		start.parent = null; 
		
		Node end = GetNode (0, endX, endY);
		
		//List of all the openned nodes to look at for the closest node
		List<Node> visited = new List<Node> ();
		
		//Add the start node
		visited.Add (start);
		
		// Prepare the variables		
		Node nodeVisiting = null;
		Node nodeTheClosestTo = null;
		
		float tan = speed / 1;
		angle = 90f - Mathf.Atan (tan) * Mathf.Rad2Deg;
	
		//RRT algo
		for (int i = 0; i <= attemps; i++) {

			//Get random point
			int rt = Random.Range (0, nodeMatrix.GetLength (0));
			int rx = Random.Range (0, nodeMatrix [rt].GetLength (0));
			int ry = Random.Range (0, nodeMatrix [rt].GetLength (1));
			nodeVisiting = GetNode (rt, rx, ry);
			if (nodeVisiting.visited || !nodeVisiting.cell.IsWalkable()) {
				i--;
				continue;
			}
			
			//Get nearest node to the randomly picked node
			float distanceToNodeMax = float.MaxValue;
			
			foreach (Node n in visited) {
				float distanceTest = n.DistanceFrom (nodeVisiting);
				if (distanceTest < distanceToNodeMax) {
					distanceToNodeMax = distanceTest;
					nodeTheClosestTo = n;
				}
			}
			
			// Skip downwards movement
			if (nodeTheClosestTo.t > nodeVisiting.t)
				continue;
			
			// Only add if we are going in ANGLE degrees or higher
			Vector3 p1 = nodeVisiting.GetVector3 ();
			Vector3 p2 = nodeTheClosestTo.GetVector3 ();
			Vector3 pd = p1 - p2;
			if (Vector3.Angle (pd, new Vector3 (pd.x, 0f, pd.z)) < angle) {
				continue;
			}
			
			// And we have line of sight
			if (!nodeVisiting.cell.IsWalkable() || CheckCollision (nodeVisiting, nodeTheClosestTo))
				continue;
			
			if (!visited.Contains (nodeVisiting))
				visited.Add (nodeVisiting);
				
			nodeVisiting.parent = nodeTheClosestTo;
			nodeVisiting.visited = true;
			
			if (Random.Range (0, 1000) > 990) {
				p1 = nodeVisiting.GetVector3();
				p2 = end.GetVector3();
				p2.y = p1.y;
				float dist = Vector3.Distance(p1, p2);
				
				float t = dist * Mathf.Tan(angle);
				pd = p2;
				pd.y += t;
				
				if (pd.y <= nodeMatrix.GetLength(0)) {
					Node endNode = GetNode((int) pd.y, (int) pd.x, (int) pd.z);
					if (!CheckCollision (nodeVisiting, endNode, 0)) {
						Debug.Log ("Done3");
						endNode.parent = nodeVisiting;
						return ReturnPath (endNode, smooth);
					}
				}
			}
				
			//Might be adding the voisin as a the goal
			if (nodeVisiting.x == end.x & nodeVisiting.y == end.y) {
				//Debug.Log ("Done2");
				return ReturnPath (nodeVisiting, smooth);
					
			}
		}
				
		return new List<Node> ();
	}
	
	// Checks for collision between two nodes and their children
	private bool CheckCollision (Node n1, Node n2, int deep = 0)
	{
		if (deep > 5)
			return false;
		int x = (n1.x + n2.x) / 2;
		int y = (n1.y + n2.y) / 2;
		int t = (n1.t + n2.t) / 2;
		Node n3 = GetNode (t, x, y);
		return !n3.cell.IsWalkable() || CheckCollision (n1, n3, deep + 1) || CheckCollision (n2, n3, deep + 1);
		
	}
	
	// Returns the computed path by the RRT, and smooth it if that's the case
	private List<Node> ReturnPath (Node endNode, bool smooth)
	{
		Node n = endNode;
		List<Node> points = new List<Node> ();
		
		while (n != null) {
			points.Add (n);
			n = n.parent;
		}
		points.Reverse ();
		
		// If we didn't find a path
		if (points.Count == 1)
			points.Clear ();
		else if (smooth) {
			// Smooth out the path
			Node final = null;
			foreach (Node each in points) {
				final = each;
				while (SmoothNode(final)) {
				}
			}
			
			points.Clear ();
			
			while (final != null) {
				points.Add (final);
				final = final.parent;
			}
			points.Reverse ();
		}
		
		return points;
	}
	
	private bool SmoothNode (Node n)
	{
		if (n.parent != null && n.parent.parent != null) {
			if (CheckCollision (n, n.parent.parent))
				return false;
			else {
				n.parent = n.parent.parent;
				return true;
			}
		} else
			return false;
	}
}

	


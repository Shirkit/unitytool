using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using Common;

namespace Exploration {
	public class RRT : MonoBehaviour
	{
		
		public Node[,] newMatrix;
		public Vector3 startPosition;
		public Vector3 endPosition;
		
		public void Start ()
		{
	
		}
	
		public List<Node> Compute (int startX, int startY, int endX, int endY, Cell[,] matrix)
		{
			newMatrix = new Node[matrix.GetLength (0), matrix.GetLength (1)];
			
			List<Node> allNodesToVisit = new List<Node> (); 
			
			for (int x = 0; x < matrix.GetLength(0); x++) {
				for (int y = 0; y < matrix.GetLength(1); y++) {
					newMatrix [x, y] = new Node ();
					newMatrix [x, y].parent = null;
					newMatrix [x, y].cell = matrix [x, y];
					newMatrix [x, y].x = x;
					newMatrix [x, y].y = y;
					
					allNodesToVisit.Add (newMatrix [x, y]);
				}
			}
			
			//Start and ending node
			Node start = newMatrix [startX, startY];
			start.visited = true; 
			start.parent = start; 
			
			Node end = newMatrix [endX, endY];
			
			//Get vectors position
			startPosition = start.GetVector3 ();
			endPosition = end.GetVector3 ();
			
			//List of all the openned nodes to look at for the closest node
			List<Node> opennedNode = new List<Node> ();
			
			//Add the start node voisins
			foreach (Node n in getAdjacent(start,newMatrix)) {
				opennedNode.Add (n);
				n.parent = start; 
			}
			
			//Remove The start from the list where you pick random points
			allNodesToVisit.Remove (start); 
			
			Node nodeVisiting = null;
			Node nodeTheClosestTo = null;
			
			//RRT algo
			for (int i = 0; i <= 200; i++) {
	
				//Get random point			
				nodeVisiting = allNodesToVisit [Random.Range (0, allNodesToVisit.Count)];
				
				
				//Get nearest node to the randomly picked node
				float distanceToNodeMax = 1000000;
				
				
				foreach (Node n in opennedNode) {
					float distanceTest = n.DistanceFrom (nodeVisiting);
					if (distanceTest < distanceToNodeMax) {
						distanceToNodeMax = distanceTest;
						nodeTheClosestTo = n;
						
					}
				}
				
				//Dont sample this one again
				//nodeTheClosestTo.visited = true; 
				allNodesToVisit.Remove (nodeTheClosestTo);
				
				//Might be picking the goal as the one to expand
				if (nodeTheClosestTo.equalTo (end)) {
					return ReturnPath (nodeTheClosestTo);
				}
				
				//if can add new vector to the tree. 
				foreach (Node n in getAdjacent(nodeTheClosestTo,newMatrix)) {	
					if (!opennedNode.Contains (n))
						opennedNode.Add (n);
					
					if (n.parent == null)
						n.parent = nodeTheClosestTo;
					
					//Might be adding the voisin as a the goal
					if (n.equalTo (end)) {
						return ReturnPath (nodeTheClosestTo);
						
					}
				}
				opennedNode.Remove (nodeTheClosestTo);
			}
					
			return new List<Node> ();
		}
		
		private List<Node> ReturnPath (Node endNode)
		{
			Node n = endNode;
			List<Node> points = new List<Node> ();
			
			while (n.parent != n) {
				points.Add (n);
				n = n.parent;
			}
			points.Reverse ();
			
			// If we didn't find a path
			if (points.Count == 1)
				points.Clear ();
			return points;
		}
		
		// Gets all the adjancent cells from the current one
		private List<Node> getAdjacent (Node n, Node[,] matrix)
		{
			List<Node> adj = new List<Node> ();
			for (int xx = n.x - 1; xx <= n.x +1; xx++) {
				for (int yy = n.y - 1; yy <= n.y +1; yy++) {
					if (xx <= n.x + 1 && xx >= 0 && xx < matrix.GetLength (0) && yy <= n.y + 1 && yy >= 0 && yy < matrix.GetLength (1) && (xx == n.x || yy == n.y)) {
						Node c = matrix [xx, yy];
						if (c.cell.IsWalkable()) {
							adj.Add (c);
						}
					}
				}
			}
			adj.Remove (n);
			return adj;
		}
		
		public void OnDrawGizmos ()
		{
			//Draw the balls
			//Start position and end position
				
			Gizmos.color = Color.green;
			Gizmos.DrawSphere (startPosition, 0.5f);
			Gizmos.color = Color.blue;
			Gizmos.DrawSphere (endPosition, 0.5f);
			
		}
		
		public void Update ()
		{
			
			//If button press
			//Run rrt
			
			//Draw the RRT
			
		}
	}
}
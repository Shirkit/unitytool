using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Common;

namespace Exploration {
	public class AStar : System.Collections.IComparer {
		Node start, end;
			
		public AStar () {
		}
		
		public int Compare (object x, object y) {
			return f ((Node)x) > f ((Node)y) ? 1 : -1;
		}
	
		public List<Node> Compute (int startX, int startY, int endX, int endY, Cell[][] matrix, bool improve) {
			List<Node> opened = new List<Node> ();
			Priority_Queue.IPriorityQueue<Node> heap2 = new Priority_Queue.HeapPriorityQueue<Node> (600);
			
			List<Node> closed = new List<Node> ();
				
			// Initialize our version of the matrix (can we skip this?)
			Node[][] newMatrix = new Node[matrix.Length][];
			for (int x = 0; x < matrix.Length; x++) {
				newMatrix [x] = new Node[matrix [x].Length];
				for (int y = 0; y < matrix[x].Length; y++) {
					newMatrix [x] [y] = new Node ();
					newMatrix [x] [y].parent = null;
					newMatrix [x] [y].cell = matrix [x] [y];
					newMatrix [x] [y].x = x;
					newMatrix [x] [y].y = y;
				}
			}
	
			// Do the work for the first cell before firing the algorithm
			start = newMatrix [startX] [startY];
			end = newMatrix [endX] [endY];
				
			closed.Add (start);
				
			foreach (Node c in getAdjacent(start, newMatrix)) {
				c.parent = start;
				if (improve)
					heap2.Enqueue (c, f (c));
				else
					opened.Add (c);
			}
				
			while ((improve && heap2.Count > 0) || (!improve && opened.Count > 0)) {
					
				// Pick the closest to the goal
				Node minF = null;
				if (improve) {
					minF = heap2.Dequeue ();
				} else {
					for (int i = 0; i < opened.Count; i++) {
						if (minF == null || f (minF) > f (opened [i]))
							minF = opened [i];
					}
					opened.Remove (minF);
				}
				
				closed.Add (minF);
					
				// Found it
				if (minF == end)
					break;
					
				foreach (Node adj in getAdjacent(minF, newMatrix)) {
						
					float soFar = g (minF) + h (adj, minF);
						
					// Create the links between cells (picks the best path)
					if (closed.Contains (adj)) {
						if (g (adj) > soFar) {
							adj.parent = minF;
						}
					} else {
						if ((improve && heap2.Contains (adj)) || (!improve && opened.Contains (adj))) {
							if (g (adj) > soFar) {
								adj.parent = minF;
							}
						} else {
							adj.parent = minF;
							if (improve)
								heap2.Enqueue (adj, f (adj));
							else
								opened.Add (adj);
						}
					}
				}
			}
				
			// Creates the result list
			Node n = end;
			List<Node> points = new List<Node> ();
			while (n != null) {
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
		private List<Node> getAdjacent (Node n, Node[][] matrix) {
			List<Node> adj = new List<Node> ();
			for (int xx = n.x - 1; xx <= n.x +1; xx++) {
				for (int yy = n.y - 1; yy <= n.y +1; yy++) {
					if (xx <= n.x + 1 && xx >= 0 && xx < matrix.Length && yy <= n.y + 1 && yy >= 0 && yy < matrix [0].Length) {
						Node c = matrix [xx] [yy];
						if (!((Cell)c.cell).blocked) {
							adj.Add (c);
						}
					}
				}
			}
			adj.Remove (n);
			return adj;
		}
			
		// current + heuristic costs summed up
		private float f (Node current) {
			return g (current) + h (current, end);
		}
			
		// current moving cost
		private float g (Node current) {
			if (current.parent == null)
				return 0;
			else
				return h (current, current.parent) + g (current.parent);
		}
			
		// heuristic cost
		private float h (Node current, Node to) {
			return Vector2.Distance (new Vector2 (to.x, to.y), new Vector2 (current.x, current.y));
		}	
	}
}
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Common;
using Objects;
using System;
using Extensions;

namespace Exploration {

	public class DavAStar3d : System.Collections.IComparer {
		private Node start, end;
		private double speed;
		
		public int Compare (object x, object y) {
			return f ((Node)x) > f ((Node)y) ? 1 : -1;
		}
		
		public DavAStar3d () {
		}

		private List<Node> getAdjacent (Node n, Node[][][] matrix) {
			List<Node> adj = new List<Node> ();
			int time = 0;

			var epsilon = new RealExtensions.Epsilon(1E-30);
			if (speed >= 1.0)
				time = (int) Math.Floor(speed);

			if (n.accSpeed.LE(1.0, epsilon))
				time++;

			if (n.t + time >= matrix.Length)
				return adj;
			for (int xx = n.x - 3; xx <= n.x +3; xx++) {
				for (int yy = n.y - 3; yy <= n.y +3; yy++) {
					if (xx <= n.x + 3 && xx >= 0 && xx < matrix.Length && yy <= n.y + 3 && yy >= 0 && yy < matrix [0].Length) {
						Node c = matrix [n.t + time] [xx] [yy];
						//if (!((Cell)c.cell).blocked) {
						if ((c.cell).IsWalkable ()) {
							adj.Add (c);
						}
					}
				}
			}
			//adj.Remove (n);
			return adj;
		}
		
		private float h (Node current, Node to) {
			return Vector2.Distance (new Vector2 (to.x, to.y), new Vector2 (current.x, current.y));
		}
		
		private float f (Node current) {
			return (g (current) + h (current, end));
		}

		// current moving cost
		private float g (Node current) {
			if (current.parent == null)
				return 0;
			else
				return h (current, current.parent) + g (current.parent);
		}

		private void acc(Node n) {
			var epsilon = new RealExtensions.Epsilon(1E-30);
			if (speed >= 1.0) {
				n.accSpeed = speed - Math.Abs(speed) + n.parent.accSpeed;
				if (n.parent.accSpeed.LE(1.0, epsilon))
					n.accSpeed -= 1.0;
			} else {
				n.accSpeed = n.parent.accSpeed + speed;
				if (n.parent.accSpeed.LE(1.0, epsilon))
					n.accSpeed -= 1.0;
			}
		}
		
		public List<Node> Compute (int startX, int startY, int endX, int endY, Cell[][][] matrix, float playerSpeed) {
			this.speed = 1.0d / playerSpeed;
			try {
				Priority_Queue.IPriorityQueue<Node> heap2 = new Priority_Queue.HeapPriorityQueue<Node> (1000000);
				List<Node> closed = new List<Node> ();
				
				// Initialize our version of the matrix (can we skip this?)
				Node[][][] newMatrix = new Node[matrix.Length][][];
				for (int t=0; t<matrix.Length; t++) {
					newMatrix [t] = new Node[matrix [t].Length][];
					for (int x = 0; x < matrix [t].Length; x++) {
						newMatrix [t] [x] = new Node[matrix [t] [x].Length];
						for (int y = 0; y < matrix [t] [x].Length; y++) {
							newMatrix [t] [x] [y] = new Node ();
							newMatrix [t] [x] [y].parent = null;
							newMatrix [t] [x] [y].cell = matrix [t] [x] [y];
							newMatrix [t] [x] [y].x = x;
							newMatrix [t] [x] [y].y = y;
							newMatrix [t] [x] [y].t = t;
						}
					}
				}
				// Do the work for the first cell before firing the algorithm
				start = newMatrix [0] [startX] [startY];
				end = newMatrix [0] [endX] [endY];
				start.parent = null;
				start.visited = true;
				start.accSpeed = speed - Math.Floor(speed);
				foreach (Node n in getAdjacent(start, newMatrix)) {
					n.parent = start;
					float fVal = f (n);
					n.Priority = fVal;
					heap2.Enqueue (n, fVal);
				}
				while (heap2.Count != 0) {
					Node first = heap2.Dequeue ();
					if (first.x == end.x && first.y == end.y) {
						end = newMatrix [first.t] [end.x] [end.y];
						break;
					}
					first.visited = true;
					foreach (Node m in getAdjacent(first, newMatrix)) {
						float currentG = g (first) + h (m, first);
						float gVal = g (m);
						if (m.visited) {
							if (gVal > currentG) {
								m.parent = first;
								acc(m);
							}
						} else {
							if (!heap2.Contains (m)) {
								m.parent = first;
								m.Priority = f (m);
								heap2.Enqueue (m, m.Priority);
								acc(m);
							} else {
								if (gVal > currentG) {
									m.parent = first;
									m.Priority = f (m);
									heap2.UpdatePriority (m, m.Priority);
									acc(m);
								}
							}
						}
					}
				}
				// Creates the result list
				Node e = end;
				List<Node> points = new List<Node> ();
				while (e != null) {
					points.Add (e);
					e = e.parent;
				}
				points.Reverse ();
				
				// If we didn't find a path
				if (points.Count == 1)
					points.Clear ();
				return points;
			} catch (System.Exception e) {
				Debug.Log (e.Message);
				Debug.Log (e.StackTrace);
				Debug.Log ("ERROR 2");
				return null;
			}
		}
		
	}
}



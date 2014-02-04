using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using KDTreeDLL;
using Common;
using Objects;
using Extra;

namespace Exploration {
	public class RRTKDTreeCombat : NodeProvider {
		private Cell[][][] nodeMatrix;
		private float angle;
		public KDTree tree;
		// Only do noisy calculations if enemies is different from null
		public Enemy[] enemies;
		public Vector3 min;
		public float tileSizeX, tileSizeZ;
		
		// Gets the node at specified position from the NodeMap, or create the Node based on the Cell position for that Node
		public Node GetNode (int t, int x, int y) {
			object o = tree.search (new double[] {x, t, y});
			if (o == null) {
				Node n = new Node ();

				n.x = x;
				n.y = y;
				n.t = t;
				n.enemyhp = new float[enemies.Length];
				n.cell = nodeMatrix [t] [x] [y];
				o = n;
			}
			return (Node)o;
		}
	
		public List<Node> Compute (int startX, int startY, int endX, int endY, int attemps, float speed, float dps, Cell[][][] matrix, bool smooth = false) {
			// Initialization
			tree = new KDTree (3);
			nodeMatrix = matrix;
			
			//Start and ending node
			Node start = GetNode (0, startX, startY);
			start.visited = true; 
			start.parent = null;
			start.playerhp = 10000;
			start.enemyhp = new float[enemies.Length];
			for (int i = 0; i < enemies.Length; i++) 
				start.enemyhp[i] = enemies[i].maxHealth;
			
			// Prepare start and end node
			Node end = GetNode (0, endX, endY);
			tree.insert (start.GetArray (), start);
			
			// Prepare the variables		
			Node nodeVisiting = null;
			Node nodeTheClosestTo = null;
			
			float tan = speed / 1;
			angle = 90f - Mathf.Atan (tan) * Mathf.Rad2Deg;
			
			List<Distribution.Pair> pairs = new List<Distribution.Pair> ();
			
			for (int x = 0; x < matrix[0].Length; x++) 
				for (int y = 0; y < matrix[0].Length; y++) 
					if (((Cell)matrix [0] [x] [y]).waypoint)
						pairs.Add (new Distribution.Pair (x, y));
			
			pairs.Add (new Distribution.Pair (end.x, end.y));
			
			//Distribution rd = new Distribution(matrix[0].Length, pairs.ToArray());
		
			DDA dda = new DDA (tileSizeX, tileSizeZ, nodeMatrix[0].Length, nodeMatrix[0][0].Length);

			//RRT algo
			for (int i = 0; i <= attemps; i++) {
	
				//Get random point
				int rt = Random.Range (1, nodeMatrix.Length);
				//Distribution.Pair p = rd.NextRandom();
				int rx = Random.Range (0, nodeMatrix [rt].Length);
				int ry = Random.Range (0, nodeMatrix [rt] [rx].Length);
				//int rx = p.x, ry = p.y;
				nodeVisiting = GetNode (rt, rx, ry);
				if (nodeVisiting.visited || !nodeVisiting.cell.IsWalkable ()) {
					i--;
					continue;
				}
				
				nodeTheClosestTo = (Node)tree.nearest (new double[] {rx, rt, ry});
				
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
				if (nodeVisiting.cell.blocked) {
					continue;
				}

				List<Cell[][][]> seenList = new List<Cell[][][]>();
				for (int j = 0; j < enemies.Length; j++) {
					if (nodeTheClosestTo.enemyhp[j] > 0)
						seenList.Add(enemies[j].seenCells);
				}

				Node hit = dda.Los3D(nodeMatrix, nodeTheClosestTo, nodeVisiting, seenList.ToArray());

				if (hit != null) {
					if (hit.cell.blocked)
						continue;
					else {
						Enemy toFight = null;
						int index = -1;
						foreach (Enemy e in enemies) {
							index++;
							if (e.seenCells[hit.t][hit.x][hit.y] != null)
								toFight = e;
						}

						float timef = nodeTheClosestTo.enemyhp[index] / dps;
						int timeT = Mathf.CeilToInt(timef);
						
						Node toAdd = GetNode(hit.t, hit.x, hit.y);
						nodeVisiting = GetNode(hit.t + timeT, hit.x, hit.y);
						
						nodeVisiting.parent = toAdd;
						toAdd.parent = nodeTheClosestTo;
						
						nodeVisiting.playerhp -= timef * toFight.dps;
						nodeVisiting.enemyhp[index] = 0;

						toAdd.playerhp = nodeTheClosestTo.playerhp;
						copy(nodeTheClosestTo.enemyhp, toAdd.enemyhp);
						toAdd.fighting = toFight;
					}
				} else {
					nodeVisiting.parent = nodeTheClosestTo;
					nodeVisiting.playerhp = nodeTheClosestTo.playerhp;
					copy(nodeTheClosestTo.enemyhp, nodeVisiting.enemyhp);
				}

				try {
					tree.insert (nodeVisiting.GetArray (), nodeVisiting);
				} catch (KeyDuplicateException) {
				}
				
				nodeVisiting.visited = true;
				
				// Attemp to connect to the end node
				if (true) {
					// Compute minimum time to reach the end node
					p1 = nodeVisiting.GetVector3 ();
					p2 = end.GetVector3 ();
					p2.y = p1.y;
					float dist = Vector3.Distance (p1, p2);
					
					float t = dist * Mathf.Tan (angle);
					pd = p2;
					pd.y += t;
					
					if (pd.y <= nodeMatrix.GetLength (0)) {
						Node endNode = GetNode ((int)pd.y, (int)pd.x, (int)pd.z);
						// Try connecting

						seenList = new List<Cell[][][]>();
						for (int j = 0; j < enemies.Length; j++) {
							if (nodeTheClosestTo.enemyhp[j] > 0)
								seenList.Add(enemies[j].seenCells);
						}
						
						hit = dda.Los3D(nodeMatrix, nodeVisiting, endNode, seenList.ToArray());


						if (hit == null) {
							endNode.parent = nodeVisiting;
							return ReturnPath (endNode, smooth);
						}
					}
				}
					
				//Might be adding the neighboor as a the goal
				if (nodeVisiting.x == end.x & nodeVisiting.y == end.y) {
					//Debug.Log ("Done2");
					return ReturnPath (nodeVisiting, smooth);
						
				}
			}
					
			return new List<Node> ();
		}

		private Vector3 GridToWorldCoords(int x, int y) {
			Vector3 coord = new Vector3();
			coord.x = (x * tileSizeX) - min.x;
			coord.y = 0f;
			coord.z = (y * tileSizeZ) - min.z;
			return coord;
		}

		private void copy(float[] source, float[] dest) {
			for (int i = 0; i < source.Length; i++)
				dest[i] = source[i];
		}
		
		// Returns the computed path by the RRT, and smooth it if that's the case
		private List<Node> ReturnPath (Node endNode, bool smooth) {
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
					while (Extra.Collision.SmoothNode(final, this, SpaceState.Editor, true)) {
					}
				}
				
				points.Clear ();
				
				while (final != null) {
					points.Add (final);
					final = final.parent;
				}
				points.Reverse ();
			}

			// Updating the stuff after the player/enemies have fought each other
			Node fought = null;
			foreach (Node each in points) {
				if (fought != null) {
					for (int t = fought.t; t < each.t; t++) {
						// Update positioning while fighting
						fought.fighting.positions[t] = fought.fighting.positions[fought.t];
						fought.fighting.rotations[t] = fought.fighting.rotations[fought.t];
						fought.fighting.forwards[t] = fought.fighting.forwards[fought.t];

						// And update it's FOV
						Cell[][] seen = fought.fighting.seenCells[fought.t];
						for (int x = 0; x < seen.Length; x++)
							for (int y = 0; y < seen[0].Length; y++)
								if (seen[x][y] != null) {
									fought.fighting.seenCells[t][x][y] = this.nodeMatrix[t][x][y];
									fought.fighting.seenCells[t][x][y].safe = true;
								}
								else if (fought.fighting.seenCells[t][x][y] != null) {
									fought.fighting.seenCells[t][x][y].seen = false;
									fought.fighting.seenCells[t][x][y] = null;
								}
					}

					// After the enemy is dead
					Vector3 outside = new Vector3(100f, 0f, 100f);
					for (int t = each.t; t < this.nodeMatrix.Length; t++) {
						// Move the guy to a place far away
						fought.fighting.positions[t] = outside;
						fought.fighting.rotations[t] = fought.fighting.rotations[fought.t];
						fought.fighting.forwards[t] = fought.fighting.forwards[fought.t];

						// And remove any seen cells by him
						for (int x = 0; x < fought.fighting.seenCells[0].Length; x++)
							for (int y = 0; y < fought.fighting.seenCells[0][0].Length; y++) 
								if (fought.fighting.seenCells[t][x][y] != null) {
									fought.fighting.seenCells[t][x][y].seen = false;
									fought.fighting.seenCells[t][x][y] = null;
								}

					}

					// Go for next enemy!
					fought = null;
				}
				if (each.fighting != null) {
					fought = each;
				}
			}
			
			return points;
		}
		

	}
}
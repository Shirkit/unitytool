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
		private KDTree tree;
		public List<List<Node>> deathPaths;
		// Only do noisy calculations if enemies is different from null
		public Enemy[] enemies;
		public HealthPack[] packs;
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
				n.enemyhp = new Dictionary<Enemy, float> ();
				n.cell = nodeMatrix [t] [x] [y];
				o = n;
			}
			return (Node)o;
		}

		// Creates a node with the specified coordinates, no matter if it already exists in the tree or not
		// This is mainly to create the connections between nodes that shouldn't be added to the tree
		// Like the nodes with combat in then
		// This avoids crashing exceptions collisions while retrieving nodes
		private Node NewNode (int t, int x, int y) {
			Node n = new Node ();
			
			n.x = x;
			n.y = y;
			n.t = t;
			n.enemyhp = new Dictionary<Enemy, float> ();
			n.cell = nodeMatrix [t] [x] [y];
			return n;
		}
	
		public List<Node> Compute (int startX, int startY, int endX, int endY, int attemps, float stepSize, float playerMaxHp, float playerSpeed, float playerDps, Cell[][][] matrix, bool smooth = false) {
			// Initialization
			tree = new KDTree (3);
			deathPaths = new List<List<Node>> ();
			nodeMatrix = matrix;
			
			//Start and ending node
			Node start = GetNode (0, startX, startY);
			start.visited = true; 
			start.parent = null;
			start.playerhp = playerMaxHp;
			start.enemyhp = new Dictionary<Enemy, float> ();
			start.picked = new List<HealthPack>();
			foreach (Enemy e in enemies) {
				start.enemyhp.Add (e, e.maxHealth);
			}

			// Prepare start and end node
			Node end = GetNode (0, endX, endY);
			tree.insert (start.GetArray (), start);
			
			// Prepare the variables		
			Node nodeVisiting = null;
			Node nodeTheClosestTo = null;
			
			float tan = playerSpeed / 1;
			angle = 90f - Mathf.Atan (tan) * Mathf.Rad2Deg;
			
			/*Distribution algorithm
			 * List<Distribution.Pair> pairs = new List<Distribution.Pair> ();
			
			for (int x = 0; x < matrix[0].Length; x++) 
				for (int y = 0; y < matrix[0].Length; y++) 
					if (((Cell)matrix [0] [x] [y]).waypoint)
						pairs.Add (new Distribution.Pair (x, y));
			
			pairs.Add (new Distribution.Pair (end.x, end.y));
			
			Distribution rd = new Distribution(matrix[0].Length, pairs.ToArray());*/
			 
			DDA dda = new DDA (tileSizeX, tileSizeZ, nodeMatrix [0].Length, nodeMatrix [0] [0].Length);
			//RRT algo
			for (int i = 0; i <= attemps; i++) {

				//Get random point
				int rt = Random.Range (1, nodeMatrix.Length);
				int rx = Random.Range (0, nodeMatrix [rt].Length);
				int ry = Random.Range (0, nodeMatrix [rt] [rx].Length);
				//Distribution.Pair p = rd.NextRandom();
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

				// Skip if player is dead
				if (nodeTheClosestTo.playerhp <= 0)
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

				// Check for all alive enemies
				List<Cell[][][]> seenList = new List<Cell[][][]> ();
				foreach (Enemy e in enemies) {
					if (nodeTheClosestTo.enemyhp [e] > 0)
						seenList.Add (e.seenCells);
				}

				Node hit = dda.Los3D (nodeMatrix, nodeTheClosestTo, nodeVisiting, seenList.ToArray ());

				if (hit != null) {
					if (hit.cell.blocked) // Collision with obstacle, ignore
						continue;
					else {
						// Which enemy has seen me?
						Enemy toFight = null;
						foreach (Enemy e in enemies) {
							if (e.seenCells [hit.t] [hit.x] [hit.y] != null && nodeTheClosestTo.enemyhp [e] > 0)
								toFight = e;
						}

						// Solve the time
						float timef = nodeTheClosestTo.enemyhp [toFight] / (playerDps * stepSize);
						int timeT = Mathf.CeilToInt (timef);

						// Search for more enemies
						List<object> more = new List<object> ();
						foreach (Enemy e2 in enemies) {
							if (toFight != e2)
								for (int t = hit.t; t < hit.t + timeT; t++)
									if (e2.seenCells [t] [hit.x] [hit.y] != null && nodeTheClosestTo.enemyhp [e2] > 0) {
										Tuple<Enemy, int> whenSeen = new Tuple<Enemy, int> (e2, t);
										more.Add (whenSeen);
										break; // Skip this enemy
									}
						}

						// Did another enemy saw the player while he was fighting?
						if (more.Count > 0) {

							// Who dies when
							List<object> dyingAt = new List<object> ();

							// First, save when the first fight starts
							Node firstFight = NewNode (hit.t, hit.x, hit.y);
							firstFight.parent = nodeTheClosestTo;

							// Then when the first guy dies
							Tuple<Enemy, int> death = new Tuple<Enemy, int> (toFight, firstFight.t + timeT);
							dyingAt.Add (death);

							// And proccess the other stuff
							firstFight.fighting = new List<Enemy> ();
							firstFight.fighting.Add (toFight);
							copy (nodeTheClosestTo.enemyhp, firstFight.enemyhp);

							// Navigation node
							Node lastNode = firstFight;

							// Solve for all enemies joining the fight
							foreach (object o in more) {
								Tuple<Enemy, int> joined = (Tuple<Enemy, int>)o;						

								// Calculate dying time
								timef = timef + lastNode.enemyhp [joined.First] / (playerDps * stepSize);
								timeT = Mathf.CeilToInt (timef);
								death = new Tuple<Enemy, int> (joined.First, timeT + hit.t);
								dyingAt.Add (death);

								// Create the node structure
								Node startingFight = NewNode (joined.Second, hit.x, hit.y);

								// Add to fighting list
								startingFight.fighting = new List<Enemy> ();
								startingFight.fighting.AddRange (lastNode.fighting);
								startingFight.fighting.Add (joined.First);
								copy (lastNode.enemyhp, startingFight.enemyhp);

								// Correct parenting
								startingFight.parent = lastNode;
								lastNode = startingFight;
							}

							// Solve for all deaths
							foreach (object o in dyingAt) {

								Tuple<Enemy, int> dead = (Tuple<Enemy, int>)o;
								Node travel = lastNode;
								bool didDie = false;
								while (!didDie && travel.parent != null) {

									// Does this guy dies between two nodes?
									if (dead.Second > travel.parent.t && dead.Second < travel.t) {

										// Add the node
										Node adding = NewNode (dead.Second + hit.t, hit.x, hit.y);
										adding.fighting = new List<Enemy> ();
										adding.fighting.AddRange (travel.parent.fighting);

										// And remove the dead people
										adding.fighting.Remove (dead.First);
										adding.died = dead.First;

										Node remove = lastNode;

										// Including from nodes deeper in the tree
										while (remove != travel.parent) {
											remove.fighting.Remove (dead.First);
											remove = remove.parent;
										}

										// Reparent the nodes
										adding.parent = travel.parent;
										travel.parent = adding;
										didDie = true;
									}

									travel = travel.parent;
								}
								if (!didDie) {
									// The guy didn't die between, that means he's farthest away than lastNode
									Node adding = NewNode (dead.Second, hit.x, hit.y);
									adding.fighting = new List<Enemy> ();
									adding.fighting.AddRange (lastNode.fighting);
									adding.fighting.Remove (dead.First);
									copy (lastNode.enemyhp, adding.enemyhp);
									adding.enemyhp [dead.First] = 0;
									adding.died = dead.First;
									adding.parent = lastNode;

									// This is the new lastNode
									lastNode = adding;
								}
							}

							// Grab the first node with fighting
							Node first = lastNode;
							while (first.parent != nodeTheClosestTo)
								first = first.parent;

							while (first != lastNode) {

								Node navigate = lastNode;
								// And grab the node just after the first
								while (navigate.parent != first)
									navigate = navigate.parent;

								// Copy the damage already applied
								navigate.playerhp = first.playerhp;

								// And deal more damage
								foreach (Enemy dmgDealer in first.fighting)
									navigate.playerhp -= (navigate.t - first.t) * dmgDealer.dps * stepSize;

								// Goto next node
								first = navigate;

							}
							// Make the tree structure
							nodeVisiting = lastNode;
						} else {
							// Only one enemy has seen me
							Node toAdd = NewNode (hit.t, hit.x, hit.y);
							nodeVisiting = NewNode (hit.t + timeT, hit.x, hit.y);
							
							nodeVisiting.parent = toAdd;
							toAdd.parent = nodeTheClosestTo;

							toAdd.playerhp = nodeTheClosestTo.playerhp;
							toAdd.fighting = new List<Enemy> ();
							toAdd.fighting.Add (toFight);
							copy (nodeTheClosestTo.enemyhp, toAdd.enemyhp);

							copy (nodeTheClosestTo.enemyhp, nodeVisiting.enemyhp);
							nodeVisiting.playerhp = toAdd.playerhp - timef * toFight.dps * stepSize;
							nodeVisiting.enemyhp [toFight] = 0;
							nodeVisiting.died = toFight;
						}
					}
				} else {
					// Nobody has seen me
					nodeVisiting.parent = nodeTheClosestTo;
					nodeVisiting.playerhp = nodeTheClosestTo.playerhp;
					copy (nodeTheClosestTo.enemyhp, nodeVisiting.enemyhp);
				}

				try {
					// Pass along all HealthPack information
					Node travel = nodeVisiting;
					Node dest = nodeTheClosestTo;
					while (dest != nodeVisiting) {
						while (travel.parent != dest)
							travel = travel.parent;

						travel.picked = new List<HealthPack>();
						travel.picked.AddRange(travel.parent.picked);

						dest = travel;
						travel = nodeVisiting;
					}

					tree.insert (nodeVisiting.GetArray (), nodeVisiting);
				} catch (KeyDuplicateException) {
				}
				
				nodeVisiting.visited = true;

				// Add the path to the death paths list
				if (nodeVisiting.playerhp <= 0) {
					Node playerDeath = nodeVisiting;
					while (playerDeath.parent.playerhp <= 0)
						playerDeath = playerDeath.parent;

					deathPaths.Add (ReturnPath (playerDeath, smooth));
				}

				// Attemp to connect to the end node
				if (nodeVisiting.playerhp > 0) {
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

						seenList = new List<Cell[][][]> ();
						foreach (Enemy e in enemies) {
							if (nodeTheClosestTo.enemyhp [e] > 0)
								seenList.Add (e.seenCells);
						}
						
						hit = dda.Los3D (nodeMatrix, nodeVisiting, endNode, seenList.ToArray ());

						// To simplify things, only connect if player isn't seen or collides with an obstacle
						if (hit == null) {
							endNode.parent = nodeVisiting;
							copy (endNode.parent.enemyhp, endNode.enemyhp);
							endNode.playerhp = endNode.parent.playerhp;
							List<Node> done = ReturnPath (endNode, smooth);
							//UpdateNodeMatrix (done);
							return done;
						}
					}

					if (nodeVisiting.playerhp < 100)
						// Health pack solving
						foreach (HealthPack pack in packs) {
							if (!nodeVisiting.picked.Contains(pack)) {
								// Compute minimum time to reach the pack
								p1 = nodeVisiting.GetVector3 ();
								p2 = new Vector3(pack.posX, p1.y, pack.posZ);
								dist = Vector3.Distance (p1, p2);

								t = dist * Mathf.Tan (angle);
								pd = p2;
								pd.y += t;

								if (pd.y <= nodeMatrix.GetLength (0)) {
									// TODO If the node is already on the Tree, things may break!
									// but we need to add it to the tree and retrieve from it to make it a possible path!
									Node packNode = GetNode ((int)pd.y, (int)pd.x, (int)pd.z);

									// Try connecting
									seenList = new List<Cell[][][]> ();
									foreach (Enemy e in enemies) {
										if (nodeVisiting.enemyhp [e] > 0)
											seenList.Add (e.seenCells);
									}
									
									hit = dda.Los3D (nodeMatrix, nodeVisiting, packNode, seenList.ToArray ());
									
									// To simplify things, only connect if player isn't seen or collides with an obstacle
									if (hit == null) {
										packNode.parent = nodeVisiting;
										packNode.picked = new List<HealthPack>();
										packNode.picked.AddRange(nodeVisiting.picked);
										packNode.picked.Add(pack);
										copy (packNode.parent.enemyhp, packNode.enemyhp);
										packNode.playerhp = 100;
										try {
											tree.insert(packNode.GetArray(), packNode);
										} catch (KeyDuplicateException) {
										}
									}
								}
							}
						}
				}
					
				//Might be adding the neighboor as a the goal
				if (nodeVisiting.x == end.x & nodeVisiting.y == end.y) {
					//Debug.Log ("Done2");
					List<Node> done = ReturnPath (nodeVisiting, smooth);
					//UpdateNodeMatrix (done);
					return done;
						
				}
			}
					
			return new List<Node> ();
		}

		private Vector3 GridToWorldCoords (int x, int y) {
			Vector3 coord = new Vector3 ();
			coord.x = (x * tileSizeX) - min.x;
			coord.y = 0f;
			coord.z = (y * tileSizeZ) - min.z;
			return coord;
		}

		private void copy (Dictionary<Enemy, float> source, Dictionary<Enemy, float> dest) {
			foreach (Enemy e in enemies)
				dest.Add (e, source [e]);
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
						
			return points;
		}

		private void UpdateNodeMatrix (List<Node> points) {
			// Updating the stuff after the player/enemies have fought each other
			Node lastNode = null;
			foreach (Node each in points) {
				if (each.died != null) {
					// After the enemy is dead
					Vector3 outside = new Vector3 (100f, 0f, 100f);
					for (int t = each.t; t < this.nodeMatrix.Length; t++) {
						// Move the guy to a place far away
						each.died.positions [t] = outside;
						each.died.rotations [t] = each.died.rotations [each.t];
						each.died.forwards [t] = each.died.forwards [each.t];
						
						// And remove any seen cells by him
						for (int x = 0; x < each.died.seenCells[0].Length; x++)
							for (int y = 0; y < each.died.seenCells[0][0].Length; y++) 
								if (each.died.seenCells [t] [x] [y] != null) {
									bool cellSeen = false;
									foreach (Enemy e in enemies)
										if (e != each.died) {
											Node correct = points [points.Count - 1];
											while (correct.t > t)
												correct = correct.parent;
									
											if (correct.enemyhp [e] > 0 && e.seenCells [t] [x] [y] != null)
												cellSeen = true;
										}
									if (!cellSeen)
										each.died.seenCells [t] [x] [y].seen = false;
									each.died.seenCells [t] [x] [y] = null;
								}
					}
				}
				
				if (lastNode != null && lastNode.fighting != null) {
					foreach (Enemy e in lastNode.fighting) {
						
						Node fightStarted = lastNode;
						while (fightStarted.parent.fighting != null && fightStarted.parent.fighting.Contains(e))
							fightStarted = fightStarted.parent;
						
						Cell[][] seen = e.seenCells [fightStarted.t];
						
						for (int t = lastNode.t; t < each.t; t++) {
							e.positions [t] = e.positions [fightStarted.t];
							e.rotations [t] = e.rotations [fightStarted.t];
							e.forwards [t] = e.forwards [fightStarted.t];
							
							for (int x = 0; x < seen.Length; x++) {
								for (int y = 0; y < seen[0].Length; y++) {
									if (seen [x] [y] != null) {
										e.seenCells [t] [x] [y] = this.nodeMatrix [t] [x] [y];
										e.seenCells [t] [x] [y].seen = true;
										//e.seenCells [t] [x] [y].safe = true;
									} else if (e.seenCells [t] [x] [y] != null) {
										e.seenCells [t] [x] [y].seen = false;
										e.seenCells [t] [x] [y] = null;
									}
								}
							}
						}
					}
				}
				
				lastNode = each;
			}
		}
	}
}
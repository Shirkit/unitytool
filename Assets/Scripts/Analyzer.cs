using System;
using System.Collections.Generic;
using UnityEngine;
using Common;
using Exploration;
using Objects;

namespace Extra {
	public class Analyzer {
		
		public enum Heuristic : int {
			Velocity = 0,
			Crazyness = 1,
			Danger = 2,
			Danger3 = 3,
			Danger3Norm = 4,
			Los = 5,
			Los3 = 6,
			Los3Norm = 7
		}
		// [Heuristic] [Time]
		public static Dictionary<Path, float[][]> pathMap = new Dictionary<Path, float[][]> ();
		
		public static float[][] ComputeSeenValuesGrid (Cell[][][] fullMap, out float max) {
			float[][] metric = new float[fullMap [0].Length][];
			max = 0f;
			for (int x = 0; x < metric.Length; x++) {
				metric [x] = new float[fullMap [0] [0].Length];
				for (int y = 0; y < metric[0].Length; y++) {
					for (int t = 0; t < fullMap.Length; t++) {
						metric [x] [y] += ((Cell)fullMap [t] [x] [y]).seen ? 1f : 0f;
					}
					if (metric [x] [y] > max)
						max = metric [x] [y];
				}
			}
			return metric;
		}
		
		public static void Swap<T> (ref T x, ref T y) {
			T tmp = y;
			y = x;
			x = tmp;
		}
		
		private static float ComputeLength3D (List<Node> length) {
			float total = 0f;
			Node n = length [length.Count - 1];
			while (n.parent != null) {
				total += n.DistanceFrom (n.parent);
				n = n.parent;
			}
			return total;
		}
		
		// This must be called before a single path analysis or a batch path analysis
		public static void PreparePaths (List<Path> paths) {
			Heuristic[] values = (Heuristic[])Enum.GetValues (typeof(Analyzer.Heuristic));
			
			pathMap.Clear ();
			
			foreach (Path path in paths) {
				if (!(pathMap.ContainsKey (path))) {
					pathMap.Add (path, new float[Enum.GetValues (typeof(Analyzer.Heuristic)).Length][]);
				}
				
				foreach (Heuristic metric in values)
					pathMap [path] [(int)metric] = new float[path.points [path.points.Count - 1].t];
			}
		}
		
		#region Comparers
		
		public class TimeComparer : IComparer<Path> {
			public int Compare (Path path1, Path path2) {
				return Mathf.FloorToInt (path1.time - path2.time);
			}
		}
		
		public class Length2dComparer : IComparer<Path> {
			public int Compare (Path path1, Path path2) {
				return Mathf.FloorToInt (path1.length2d - path2.length2d);
			}
		}
		
		public class DangerComparer : IComparer<Path> {
			public int Compare (Path path1, Path path2) {
				return Mathf.FloorToInt (path2.danger - path1.danger);
			}
		}
		
		public class Danger3Comparer : IComparer<Path> {
			public int Compare (Path path1, Path path2) {
				return Mathf.FloorToInt (path2.danger3 - path1.danger3);
			}
		}
		
		public class Danger3NormComparer : IComparer<Path> {
			public int Compare (Path path1, Path path2) {
				return Mathf.FloorToInt (path2.danger3Norm - path1.danger3Norm);
			}
		}
		
		public class LoSComparer : IComparer<Path> {
			public int Compare (Path path1, Path path2) {
				return Mathf.FloorToInt (path2.los - path1.los);
			}
		}
		
		public class LoS3Comparer : IComparer<Path> {
			public int Compare (Path path1, Path path2) {
				return Mathf.FloorToInt (path2.los3 - path1.los3);
			}
		}
		
		public class LoS3NormComparer : IComparer<Path> {
			public int Compare (Path path1, Path path2) {
				return Mathf.FloorToInt (path2.los3Norm - path1.los3Norm);
			}
		}
		
		public class CrazyComparer : IComparer<Path> {
			public int Compare (Path path1, Path path2) {
				return Mathf.FloorToInt (path2.crazy - path1.crazy);
			}
		}
		
		public class VelocityComparer : IComparer<Path> {
			public int Compare (Path path1, Path path2) {
				return Mathf.FloorToInt (path2.velocity - path1.velocity);
			}
		}
		
		#endregion
		
		#region Paths values
		
		public static void ComputePathsTimeValues (List<Path> paths) {
			foreach (Path path in paths)
				path.time = path.points [path.points.Count - 1].t;
		}
		
		public static void ComputePathsLengthValues (List<Path> paths) {
			foreach (Path path in paths) {
				Node n = path.points [path.points.Count - 1];
				while (n.parent != null) {
					path.length2d += Vector2.Distance (n.GetVector2 (), n.parent.GetVector2 ());
					path.length3d += n.DistanceFrom (n.parent);
					n = n.parent;
				}
			}
		}
		
		public static void ComputePathsVelocityValues (List<Path> paths) {
			foreach (Path path in paths) {
				Node n = path.points [path.points.Count - 1];
				while (n.parent != null && n.parent.parent != null) {
					Vector3 p1 = n.GetVector3 ();
					Vector3 p2 = n.parent.GetVector3 ();
					Vector3 p3 = n.parent.parent.GetVector3 ();
					Vector3 v1 = p1 - p2;
					Vector3 v2 = p2 - p3;
					v1.z = 0f;
					v2.z = 0f;
					v1.Normalize ();
					v2.Normalize ();
					float angle1 = Vector3.Angle (v1, Vector3.up);
					float angle2 = Vector3.Angle (v2, Vector3.up);
					pathMap [path] [(int)Heuristic.Velocity] [n.parent.t] = Mathf.Abs (angle2 - angle1);
					path.velocity += pathMap [path] [(int)Heuristic.Velocity] [n.parent.t];
					n = n.parent;
				}
				if (path.length3d == 0)
					path.length3d = ComputeLength3D (path.points);
				path.velocity /= path.length3d;
			}
		}
		
		public static void ComputePathsLoSValues (List<Path> paths, Enemy[] enemies, Vector3 min, float tileSizeX, float tileSizeZ, Cell[][][] fullMap, float[][] dangerCells, float maxDanger) {
			// Compute the y = ax + b equation for each FoV (i.e. enemy=)
			float[][] formula = new float[enemies.Length][];
			for (int i = 0; i < formula.Length; i++) {
				formula [i] = new float[2];
				formula [i] [0] = (180 - enemies [i].fovAngle) * -2;
				formula [i] [1] = enemies [i].fovAngle - formula [i] [0];
			}

			DDA dda = new DDA(tileSizeX, tileSizeZ, fullMap [0].Length, fullMap [0] [0].Length);
			
			foreach (Path currentPath in paths) {
				if (currentPath.length3d == 0)
					currentPath.length3d = ComputeLength3D (currentPath.points);
					
				Node cur = currentPath.points [currentPath.points.Count - 1];
				Node par = cur.parent;
				while (cur.parent != null) {
						
					Vector3 p1 = cur.GetVector3 ();
					Vector3 p2 = par.GetVector3 ();
					Vector3 pd = p1 - p2;
						
					float pt = (cur.t - par.t);
						
					// Navigate through time to find the right cells to start from
					for (int t = 0; t < pt; t++) {
							
						float delta = ((float)t) / pt;
							
						Vector3 pos = p2 + pd * delta;
						
						for (int enemyIndex = 0; enemyIndex < enemies.Length; enemyIndex++) {
							Vector3 enemy = enemies [enemyIndex].positions [par.t + t];
							
							Vector2 pos2d = new Vector2 (pos.x, pos.z);
							Vector2 enemy2d = new Vector2 ((enemy.x - min.x) / tileSizeX, (enemy.z - min.z) / tileSizeZ);

							bool seen = dda.HasLOS(fullMap[par.t + t], pos2d, enemy2d);
							
							// If the cell is in LoS
							if (seen && enemies [enemyIndex].cells [par.t + t].Length > 0) {
								// Grab the cell closest to my position
								Vector2 shortest = enemies [enemyIndex].cells [par.t + t] [0];
								float dist = Vector2.Distance (shortest, pos2d);
								float ddist;
								for (int k = 1; k < enemies[enemyIndex].cells[par.t + t].Length; k++) {
									ddist = Vector2.Distance (enemies [enemyIndex].cells [par.t + t] [k], pos2d);
									if (ddist < dist) {
										dist = ddist;
										shortest = enemies [enemyIndex].cells [par.t + t] [k];
									}
								}
								
								// Calculate the angle between them
								float angle = Vector2.Angle (enemies [enemyIndex].positions [par.t + t].normalized, (pos2d - enemy2d).normalized);
								if (angle <= enemies [enemyIndex].fovAngle)
									angle = 1;
								else {
									angle = (angle - formula [enemyIndex] [1]) / formula [enemyIndex] [0];
								}
								
								float f = angle / (pos2d - shortest).sqrMagnitude;
								float g = angle / Mathf.Pow ((pos2d - shortest).magnitude, 3);
								if (f == Mathf.Infinity) {
									f = angle / (pos2d - enemy2d).sqrMagnitude;
								}
								if (g == Mathf.Infinity) {
									g = angle / Mathf.Pow ((pos2d - enemy2d).magnitude, 3);
								}
								
								// Store in 'per-time' metric
								pathMap [currentPath] [(int)Heuristic.Los] [par.t + t] = f;
								pathMap [currentPath] [(int)Heuristic.Los3] [par.t + t] = g;
								pathMap [currentPath] [(int)Heuristic.Los3Norm] [par.t + t] = g * (dangerCells [(int)pos2d.x] [(int)pos2d.y] / maxDanger);
								
								// Increment the total metric
								currentPath.los += pathMap [currentPath] [(int)Heuristic.Los] [par.t + t];
								currentPath.los3 += pathMap [currentPath] [(int)Heuristic.Los3] [par.t + t];
								currentPath.los3Norm += pathMap [currentPath] [(int)Heuristic.Los3Norm] [par.t + t];
							}
						}
					}
						
					cur = par;
					par = par.parent;
				}
				currentPath.los /= currentPath.length3d;
				currentPath.los3 /= currentPath.length3d;
				currentPath.los3Norm /= currentPath.length3d;
			}
		}
		
		public static void ComputePathsDangerValues (List<Path> paths, Enemy[] enemies, Vector3 min, float tileSizeX, float tileSizeZ, Cell[][][] fullMap, float[][] dangerCells, float maxDanger) {
			AStar astar = new AStar ();
			foreach (Path currentPath in paths) {
				if (currentPath.length3d == 0)
					currentPath.length3d = ComputeLength3D (currentPath.points);
					
				Node cur = currentPath.points [currentPath.points.Count - 1];
				Node par = cur.parent;
				while (cur.parent != null) {
						
					Vector3 p1 = cur.GetVector3 ();
					Vector3 p2 = par.GetVector3 ();
					Vector3 pd = p1 - p2;
						
					float pt = (cur.t - par.t);
						
					// Navigate through time to find the right cells to start from
					for (int t = 0; t < pt; t++) {
							
						float delta = ((float)t) / pt;
							
						Vector3 pos = p2 + pd * delta;
							
						foreach (Enemy each in enemies) {
							Vector3 enemy = each.positions [par.t + t];
							
							int startX = (int)pos.x;
							int startY = (int)pos.z;
							int endX = (int)((enemy.x - min.x) / tileSizeX);
							int endY = (int)((enemy.z - min.z) / tileSizeZ);
								
							// Do A* from the player to the enemy to compute the cost
							List<Node> astarpath = astar.Compute (startX, startY, endX, endY, fullMap [par.t + t], true);
							if (astarpath.Count > 0) {
								float l = ComputeLength3D (astarpath);
								
								pathMap [currentPath] [(int)Heuristic.Danger] [par.t + t] = 1 / (l * l);
								pathMap [currentPath] [(int)Heuristic.Danger3] [par.t + t] = 1 / (l * l * l);
								pathMap [currentPath] [(int)Heuristic.Danger3Norm] [par.t + t] = (1 / (l * l * l)) * (dangerCells [startX] [startY] / maxDanger);
								
								currentPath.danger += pathMap [currentPath] [(int)Heuristic.Danger] [par.t + t];
								currentPath.danger3 += pathMap [currentPath] [(int)Heuristic.Danger3] [par.t + t];
								currentPath.danger3Norm += pathMap [currentPath] [(int)Heuristic.Danger3Norm] [par.t + t];
							}
						}
					}
						
					cur = par;
					par = par.parent;
				}
				currentPath.danger /= currentPath.length3d;
				currentPath.danger3 /= currentPath.length3d;
				currentPath.danger3Norm /= currentPath.length3d;
			}
		}
		
		public static void ComputeCrazyness (List<Path> paths, Cell[][][] fullMap, int stepsBehind) {
			foreach (Path currentPath in paths) {
					
				Node cur = currentPath.points [currentPath.points.Count - 1];
				Node par = cur.parent;
				while (cur.parent != null) {
						
					Vector3 p1 = cur.GetVector3 ();
					Vector3 p2 = par.GetVector3 ();
					Vector3 pd = p1 - p2;
						
					float pt = (cur.t - par.t);
	
					// Navigate through time to find the right cells to start from
					for (int t = 0; t < pt; t++) {
							
						float delta = ((float)t) / pt;
							
						Vector3 pos = p2 + pd * delta;
							
						int x = (int)pos.x;
						int y = (int)pos.z;
						
						float tempCrazy = 0f;
						
						for (int back = 1; back <= stepsBehind; back++) {
							
							// Look back in time to search for visible cells
							if ((par.t + t - back) > 0 && fullMap [par.t + t - back] [x] [y].seen) {
								tempCrazy += Mathf.Pow (stepsBehind - back, 2);
							}
							
							//Do the in front
							if ((par.t + t + back) < fullMap.Length && fullMap [par.t + t + back] [x] [y].seen) {
								tempCrazy += Mathf.Pow (stepsBehind - back, 2);
							}
							
						}
						
						pathMap [currentPath] [(int)Heuristic.Crazyness] [par.t + t] = tempCrazy;
						currentPath.crazy += pathMap [currentPath] [(int)Heuristic.Crazyness] [par.t + t];
					}
						
					cur = par;
					par = par.parent;
				}
			}
		}
		
		#endregion
		
		public static int[][,] Compute3DHeatMap (List<Path> paths, int sizeX, int sizeY, int sizeT, out int[] maxN) {
			// Initialization
			int[][,] heatMap = new int[sizeT][,];
			for (int t = 0; t < sizeT; t++)
				heatMap [t] = new int[sizeX, sizeY];
			
			maxN = new int[sizeT];
			
			foreach (Path path in paths) {
				foreach (Node node in path.points) {
					if (node.parent == null) {
						continue;
					}
					
					Vector3 pos = new Vector3 (node.x, node.y, node.t);
					Vector3 p = new Vector3 (node.parent.x, node.parent.y, node.parent.t);
					Vector3 res = (p - pos).normalized;
									
					//which box of the map we're in
					int mapX = node.x;
					int mapY = node.y;
					int mapT = node.t;
	       
					//length of ray from current position to next x or y-side
					float sideDistX;
					float sideDistY;
					float sideDistT;
	       
					//length of ray from one x or y-side to next x or y-side
					float deltaDistX = 1 / Mathf.Abs (res.x);
					float deltaDistY = 1 / Mathf.Abs (res.y);
					float deltaDistT = 1 / Mathf.Abs (res.z);
					
	       
					//what direction to step in x or y-direction (either +1 or -1)
					int stepX;
					int stepY;
					int stepT;
									
					//calculate step and initial sideDist
					if (res.x < 0) {
						stepX = -1;
						sideDistX = (pos.x - mapX) * deltaDistX;
					} else {
						stepX = 1;
						sideDistX = (mapX + 1 - pos.x) * deltaDistX;
					}
					if (res.y < 0) {
						stepY = -1;
						sideDistY = (pos.y - mapY) * deltaDistY;
					} else {
						stepY = 1;
						sideDistY = (mapY + 1 - pos.y) * deltaDistY;
					}
					if (res.z < 0) {
						stepT = -1;
						sideDistT = (pos.z - mapT) * deltaDistT;
					} else {
						stepT = 1;
						sideDistT = (mapT + 1 - pos.z) * deltaDistT;
					}
					
					bool done = false;
					//perform DDA
					while (!done) {
						
						heatMap [mapT] [mapX, mapY] += 10;
						
						//jump to next map square, OR in x-direction, OR in y-direction
						if (sideDistX <= sideDistY && sideDistX <= sideDistT) {
							sideDistX += deltaDistX;
							mapX += stepX;
						} else if (sideDistY <= sideDistT) {
							sideDistY += deltaDistY;
							mapY += stepY;
						} else {
							sideDistT += deltaDistT;
							mapT += stepT;
						}
						
						// Check map boundaries
						if (stepX == 1) {
							if (mapX > node.parent.x)
								done = true;
						} else if (mapX < node.parent.x)
							done = true;
						
						if (stepY == 1) {
							if (mapY > node.parent.y)
								done = true;
						} else if (mapY < node.parent.y)
							done = true;
						
						if (stepT == 1) {
							if (mapT > node.parent.t)
								done = true;
						} else if (mapT < node.parent.t)
							done = true;
						
						// Paint adjacent ones
						if (!done)
							for (int t = mapT - 1; t <= mapT + 1; t++)
								for (int x = mapX - 1; x <= mapX + 1; x++)
									for (int y = mapY - 1; y <= mapY + 1; y++)
										if (!(x < 0 || y < 0 || x >= sizeX || y >= sizeY || t < 0 || t >= sizeT)) {
											heatMap [t] [x, y]++;
											if (heatMap [t] [x, y] > maxN [t])
												maxN [t] = heatMap [t] [x, y];
										}
						
						if (node.parent.x == mapX && node.parent.y == mapY) {
							// End the algorithm on reach
							done = true;
						}
					}
				}
			}
			
			return heatMap;
		}

		public static int[,] ComputeDeathHeatMap (List<Path> paths, int sizeX, int sizeY, out int maxN) {
			maxN = -1;
			int[,] heatMap = new int[sizeX, sizeY];
			
			foreach (Path path in paths) {
				Node death = path.points[path.points.Count-1];

				// Paint adjacent nodes
				for (int x = death.x -2; x >= 0 && x <= death.x + 2 && x < sizeX; x++)
					for (int y = death.y -2; y >= 0 && y <= death.y + 2 && y < sizeY; y++) {
						int dx = Math.Abs(death.x - x);
						int dy = Math.Abs(death.y - y);
						heatMap[x,y] += (5 - dx - dy);
					}

				// Main node
				heatMap[death.x, death.y] += 10;
			}

			// Get maxN
			for (int x = 0; x < sizeX; x++) {
				for (int y = 0; y < sizeY; y++) {
					if (maxN < heatMap [x, y])
						maxN = heatMap [x, y];
				}
			}
			
			return heatMap;
		}

		
		public static int[][,] Compute3dDeathHeatMap (List<Path> paths, int sizeX, int sizeY, int sizeT, out int[] maxN) {
			maxN = new int[sizeT];
			int[][,] heatMap = new int[sizeT][,];
			for (int t = 0; t < sizeT; t++)
				heatMap[t] = new int[sizeX,sizeY];
			
			foreach (Path path in paths) {
				Node death = path.points[path.points.Count-1];
				
				// Paint adjacent nodes
				for (int t = death.t -2; t >= 0 && t <= death.t + 2 && t < sizeT; t++){
					for (int x = death.x -2; x >= 0 && x <= death.x + 2 && x < sizeX; x++){
						for (int y = death.y -2; y >= 0 && y <= death.y + 2 && y < sizeY; y++) {
							int dx = Math.Abs(death.x - x);
							int dy = Math.Abs(death.y - y);
							int dt = Math.Abs(death.t - t);
							heatMap[t][x,y] += (7 - dx - dy - dt);
						}
					}
				}
				
				// Main node
				heatMap[death.t][death.x, death.y] += 15;
			}
			
			// Get maxN
			for (int t = 0; t < sizeT; t++) {
				for (int x = 0; x < sizeX; x++) {
					for (int y = 0; y < sizeY; y++) {
						if (maxN[t] < heatMap [t][x, y])
							maxN[t] = heatMap [t][x, y];
					}
				}
			}
			
			return heatMap;
		}


		public static int[,] Compute2DHeatMap (List<Path> paths, int sizeX, int sizeY, out int maxN) {
			maxN = -1;
			int[,] heatMap = new int[sizeX, sizeY];
			
			foreach (Path path in paths) {
				int[,] hm = new int[sizeX, sizeY];
				foreach (Node node in path.points) {
					if (node.parent == null || node.parent == node) {
						hm [node.x, node.y] += 10;
						continue;
					}
					
					// Perform the DDA line algorithm
					// Based on http://lodev.org/cgtutor/raycasting.html
					
					Vector2 pos = new Vector2 (node.x, node.y);
					Vector2 p = new Vector2 (node.parent.x, node.parent.y);
					Vector2 res = (p - pos).normalized;
									
					//which box of the map we're in
					int mapX = Mathf.FloorToInt (node.x);
					int mapY = Mathf.FloorToInt (node.y);
	       
					//length of ray from current position to next x or y-side
					float sideDistX;
					float sideDistY;
	       
					//length of ray from one x or y-side to next x or y-side
					float deltaDistX = Mathf.Sqrt (1 + (res.y * res.y) / (res.x * res.x));
					float deltaDistY = Mathf.Sqrt (1 + (res.x * res.x) / (res.y * res.y));
	       
					//what direction to step in x or y-direction (either +1 or -1)
					int stepX;
					int stepY;
									
					//calculate step and initial sideDist
					if (res.x < 0) {
						stepX = -1;
						sideDistX = (pos.x - mapX) * deltaDistX;
					} else {
						stepX = 1;
						sideDistX = (mapX + 1 - pos.x) * deltaDistX;
					}
					if (res.y < 0) {
						stepY = -1;
						sideDistY = (pos.y - mapY) * deltaDistY;
					} else {
						stepY = 1;
						sideDistY = (mapY + 1 - pos.y) * deltaDistY;
					}
									
					bool done = false;
					//perform DDA
					while (!done) {
						
						hm [mapX, mapY] += 10;
						
						//jump to next map square, OR in x-direction, OR in y-direction
						if (sideDistX <= sideDistY) {
							sideDistX += deltaDistX;
							mapX += stepX;
						} else {
							sideDistY += deltaDistY;
							mapY += stepY;
						}
						
						for (int x = mapX - 1; x <= mapX + 1; x++)
							for (int y = mapY - 1; y <= mapY + 1; y++)
								if (!(x < 0 || y < 0 || x >= sizeX || y >= sizeY))
									hm [x, y]++;
						
						// Check map boundaries
						if (stepX == 1) {
							if (mapX > node.parent.x)
								done = true;
						} else if (mapX < node.parent.x)
							done = true;
						
						if (stepY == 1) {
							if (mapY > node.parent.y)
								done = true;
						} else if (mapY < node.parent.y)
							done = true;
						
						if (node.parent.x == mapX && node.parent.y == mapY) {
							// End the algorithm
							done = true;
						}
					}
				}
				
				// Pass to the global heatmap
				for (int x = 0; x < sizeX; x++) {
					for (int y = 0; y < sizeY; y++) {
						heatMap [x, y] += hm [x, y];
					}
				}
			}
			
			for (int y = 0; y < sizeX; y++) {
				String s = "";
				for (int x = 0; x < sizeY; x++) {
					s += "" + heatMap [x, y] + " ";
					if (maxN < heatMap [x, y])
						maxN = heatMap [x, y];
				}
			}
			
			return heatMap;
		}

		public static int[,] Compute2DCombatHeatMap (List<Path> paths, List<Path> deaths, int sizeX, int sizeY, out int maxN) {
			List<Path> all = new List<Path>(paths);
			all.AddRange(deaths);

			int[,] map = new int[sizeX,sizeY];
			maxN = 0;
			foreach (Path path in all) {
				foreach (Node n in path.points) {
					if (n.fighting != null && n.fighting.Count > 0) {

						// Paint adjacent nodes
						for (int x = n.x -2; x >= 0 && x <= n.x + 2 && x < sizeX; x++){
							for (int y = n.y -2; y >= 0 && y <= n.y + 2 && y < sizeY; y++) {
								int dx = Math.Abs(n.x - x);
								int dy = Math.Abs(n.y - y);
								map[x,y] += (5 - dx - dy);
							}
						}


						map[n.x,n.y] += 10;
						maxN = Mathf.Max(map[n.x, n.y], maxN);
					}
				}
			}

			return map;
		}
	}
}
using System;
using Common;
using Objects;
using UnityEngine;

namespace Extra {
	public interface NodeProvider {
		Node GetNode (int t, int x, int y);
	}

	public class Collision {

		// Checks for collision between two nodes and their children
		public static bool CheckCollision (Node n1, Node n2, NodeProvider provider, SpaceState state, bool noisy = false, int deep = 0) {
			if (deep > 5)
				return false;
			int x = (n1.x + n2.x) / 2;
			int y = (n1.y + n2.y) / 2;
			int t = (n1.t + n2.t) / 2;
			Node n3 = provider.GetNode (t, x, y);

			// Noisy calculation
			if (state.enemies != null && ((Cell)n3.cell).noisy) {
				foreach (Enemy enemy in state.enemies) {
					Vector3 dupe = enemy.positions [t];
					dupe.x = (dupe.x - state.floorMin.x) / state.tileSize.x;
					dupe.y = n3.t;
					dupe.z = (dupe.z - state.floorMin.z) / state.tileSize.y;

					// This distance is in number of cells size radius i.e. a 10 tilesize circle around the point
					if (Vector3.Distance (dupe, n3.GetVector3 ()) < 10)
						return true;
				} 
			}

			return !n3.cell.IsWalkable () || CheckCollision (n1, n3, provider, state, noisy, deep + 1) || CheckCollision (n2, n3, provider, state, noisy, deep + 1);

		}

		public static bool SmoothNode (Node n, NodeProvider provider, SpaceState state, bool noisy = false) {
			if (n.parent != null && n.parent.parent != null) {
				if (CheckCollision (n, n.parent.parent, provider, state, noisy))
					return false;
				else {
					n.parent = n.parent.parent;
					return true;
				}
			} else
				return false;
		}

	}

	public class DDA {

		private float tileSizeX, tileSizeZ;
		private int cellsX, cellsZ;

		public DDA (float tileSizeX, float tileSizeZ, int cellsX, int cellsZ) {
			this.tileSizeX = tileSizeX;
			this.tileSizeZ = tileSizeZ;
			this.cellsX = cellsX;
			this.cellsZ = cellsZ;
		}

		public Node Los3D(Cell[][][] im, Node start, Node end, Cell[][][][] seenList = null) {
			Vector3 pos = new Vector3 (start.x, start.y, start.t);
			Vector3 p = new Vector3 (end.x, end.y, end.t);
			Vector3 res = (p - pos).normalized;
			if (seenList == null)
				seenList = new Cell[0][][][];
			
			//which box of the map we're in
			int mapX = start.x;
			int mapY = start.y;
			int mapT = start.t;
			
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
			Node result = null;
			//perform DDA
			while (!done) {			
				
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
					if (mapX > end.x)
						done = true;
				} else if (mapX < end.x)
					done = true;
				
				if (stepY == 1) {
					if (mapY > end.y)
						done = true;
				} else if (mapY < end.y)
					done = true;
				
				if (stepT == 1) {
					if (mapT > end.t)
						done = true;
				} else if (mapT < end.t)
					done = true;

				if (Vector3.Distance (p, new Vector3 (mapX, mapY, mapT)) > Vector3.Distance (p, pos)) {
					// Check for maximum distance
					done = true;
				}

				if (end.x == mapX && end.y == mapY) {
					// End the algorithm on reach
					done = true;
				}

				if (!done && !im[mapT][mapX][mapY].safe) {
					// Check for ending conditions: safespot, blocked or seen by enemy
					bool ignored = true;
					if (im[mapT][mapX][mapY].blocked)
						ignored = false;
					else 
						foreach (Cell[][][] map in seenList) 
							if (map[mapT][mapX][mapY] != null) 
								ignored = false;

					if (!ignored) {
						result = new Node();
						result.t = mapT;
						result.x = mapX;
						result.y = mapY;
						result.cell = im[mapT][mapX][mapY];
						done = true;
					}
				}
			}

			return result;
		}

		public bool HasLOS (Cell[][] im, Vector2 p1, Vector2 p2) {
			return HasLOS (im, p1, p2, (p2 - p1).normalized);
		}
		
		public bool HasLOS (Cell[][] im, Vector2 p1, Vector2 p2, Vector2 res) {
			return HasLOS (im, p1, p2, res, Mathf.FloorToInt (p1.x), Mathf.FloorToInt (p1.y));
		}
		
		public bool HasLOS (Cell[][] im, Vector2 p1, Vector2 p2, Vector2 res, int x, int y) {
			// Perform the DDA line algorithm
			// Based on http://lodev.org/cgtutor/raycasting.html

			//which box of the map we're in
			int mapX = Mathf.FloorToInt (p2.x);
			int mapY = Mathf.FloorToInt (p2.y);
			
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
				sideDistX = (p2.x - mapX) * deltaDistX;
			} else {
				stepX = 1;
				sideDistX = (mapX + tileSizeX - p2.x) * deltaDistX;
			}
			if (res.y < 0) {
				stepY = -1;
				sideDistY = (p2.y - mapY) * deltaDistY;
			} else {
				stepY = 1;
				sideDistY = (mapY + tileSizeZ - p2.y) * deltaDistY;
			}

			// Don't skip seen cells
			bool done = im [x] [y].blocked;
			bool seen = false;
			//perform DDA
			while (!done) {
				//jump to next map square, OR in x-direction, OR in y-direction
				if (sideDistX < sideDistY) {
					sideDistX += deltaDistX;
					mapX += stepX;
				} else {
					sideDistY += deltaDistY;
					mapY += stepY;
				}
				
				if (Vector2.Distance (p2, new Vector2 (mapX, mapY)) > Vector2.Distance (p1, p2)) {
					seen = true;
					done = true;
				}
				// Check map boundaries
				if (mapX < 0 || mapY < 0 || mapX >= cellsX || mapY >= cellsZ) {
					seen = true;
					done = true;
				} else {
					//Check if ray has hit a wall
					if (im [mapX] [mapY].blocked) {
						done = true;
					}
					// End the algorithm
					if (x == mapX && y == mapY) {
						seen = true;
						done = true;
					}
				}
			}
			
			return seen;
		}
	}
}


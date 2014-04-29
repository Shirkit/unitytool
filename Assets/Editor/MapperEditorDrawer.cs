using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Common;
using Extra;

namespace EditorArea {

	public class MapperEditorDrawer : MonoBehaviour {

		public Cell[][][] fullMap;
		public float[][] seenNeverSeen;
		public List<Node> rrtMap;
		public Dictionary<Path, bool> paths = new Dictionary<Path, bool> ();
		public int[,] heatMap, deathHeatMap, combatHeatMap;
		public int[][,] heatMap3d, deathHeatMap3d;
		public float heatMapMax = 0, seenNeverSeenMax, deathHeatMapMax, combatHeatMap2dMax;
		public int[] heatMapMax3d, deathHeatMapMax3d;
		public int timeSlice;
		public Vector2 zero = new Vector2 ();
		public Vector2 tileSize = new Vector2 ();
		public bool drawMap = true, drawNeverSeen = false, drawHeatMap = true, drawPath = false, editGrid = false, drawFoVOnly = false, drawCombatLines = false;
		public Cell[][] editingGrid;
		public List<Tuple<Vector3, string>> textDraw;
		// Fixed values
		private Color orange = new Color (1.0f, 0.64f, 0f, 1f), transparent = new Color (1f, 1f, 1f, 0f);
		
		public void Start () {
			hideFlags = HideFlags.HideInInspector;
		}
		
		public void OnDrawGizmos () {
			// We need 2 if blocks since we are using 2 different variables to poke the data from
			if (editGrid && editingGrid != null) {
				for (int x = 0; x < editingGrid.Length; x++)
					for (int y = 0; y < editingGrid[x].Length; y++) {
						Cell c = editingGrid [x] [y];
					
						if (drawFoVOnly) {
							if (c != null && c.seen)
								Gizmos.color = orange;
							else
								Gizmos.color = transparent;
						} else {
							if (c == null)
								Gizmos.color = Color.gray;
							else if (c.safe)
								Gizmos.color = Color.blue;
							else if (c.blocked)
								Gizmos.color = Color.red;
							else if (c.seen)
								Gizmos.color = orange;
							else if (c.noisy)
								Gizmos.color = Color.yellow;
							else if (c.waypoint)
								Gizmos.color = Color.cyan;
							else if (c.cluster > 0)
								Gizmos.color = Color.white;
							else
								Gizmos.color = Color.gray;
						}
					
						Gizmos.DrawCube (new Vector3
								(x * tileSize.x + zero.x + tileSize.x / 2f,
								0.1f,
								y * tileSize.y + zero.y + tileSize.y / 2f),
								new Vector3
									(tileSize.x - tileSize.x * 0.05f,
									0.0f,
									tileSize.y - tileSize.y * 0.05f));
					}
			} else if (drawMap && fullMap != null) {
				for (int x = 0; x < fullMap[timeSlice].Length; x++)
				for (int y = 0; y < fullMap[timeSlice][x].Length; y++) {
					Cell c = fullMap [timeSlice] [x] [y];
							
						if (drawHeatMap) {
							if (heatMap != null)
								Gizmos.color = Color.Lerp (Color.white, Color.black, (float)heatMap [x, y] / (heatMapMax * 6f / 8f));
							else if (combatHeatMap != null)
								Gizmos.color = Color.Lerp (Color.white, Color.black, (float)combatHeatMap [x, y] / (combatHeatMap2dMax * 6f / 8f));
							else if (heatMap3d != null)
								Gizmos.color = Color.Lerp (Color.white, Color.black, heatMapMax3d [timeSlice] != 0 ? (float)heatMap3d [timeSlice] [x, y] / (float)heatMapMax3d [timeSlice] : 0f);
							else if (deathHeatMap != null)
								Gizmos.color = Color.Lerp (Color.white, Color.black, (float)deathHeatMap [x, y] / (deathHeatMapMax * 6f / 8f));
							else if (deathHeatMap3d != null)
							Gizmos.color = Color.Lerp (Color.white, Color.black, deathHeatMapMax3d [timeSlice] != 0 ? (float)deathHeatMap3d [timeSlice] [x, y] / (float)deathHeatMapMax3d [timeSlice] : 0f);
						} else {
							if (drawFoVOnly) {
								if (c.seen)
									Gizmos.color = orange;
								else
									Gizmos.color = transparent;
							} else {
								if (c.safe)
									Gizmos.color = Color.blue;
								else if (c.blocked)
									Gizmos.color = Color.red;
								else if (c.seen)
									Gizmos.color = orange;
								else if (c.noisy)
									Gizmos.color = Color.yellow;
								else if (c.waypoint)
									Gizmos.color = Color.cyan;
								else if (c.cluster > 0)
									Gizmos.color = Color.white;
								else if (drawNeverSeen)
									Gizmos.color = Color.Lerp (Color.green, Color.magenta, seenNeverSeen [x] [y] / (seenNeverSeenMax * 3f / 8f));
								else
									Gizmos.color = Color.green;
							}
						}
					
						Gizmos.DrawCube (new Vector3
								(x * tileSize.x + zero.x + tileSize.x / 2f,
								0.1f,
								y * tileSize.y + zero.y + tileSize.y / 2f),
								new Vector3
									(tileSize.x - tileSize.x * 0.05f,
									0.0f,
									tileSize.y - tileSize.y * 0.05f));
					}
			}
				
			// All Paths drawning
			if (drawPath) {
				Gizmos.color = Color.blue;
				foreach (KeyValuePair<Path, bool> kv in paths)
					if (kv.Value) {
						foreach (Node n in kv.Key.points) {
							Gizmos.color = kv.Key.color;
							if (n.parent != null) {
								Gizmos.DrawLine (new Vector3
									((n.x * tileSize.x + zero.x),
									0.1f,
									(n.y * tileSize.x + zero.y)),
								
								new Vector3
									((n.parent.x * tileSize.y + zero.x),
									0.1f,
									(n.parent.y * tileSize.y + zero.y)));

								if (drawCombatLines && n.parent.fighting != null && n.parent.fighting.Count > 0 && n.t >= timeSlice && n.parent.t <= timeSlice) {
									Gizmos.color = Color.red;

								for (int ei = 0; ei < n.parent.fighting.Count; ei++)
									Gizmos.DrawLine (new Vector3
							                 	((n.x * tileSize.x + zero.x),
												 0.1f,
							 					(n.y * tileSize.x + zero.y)),
							                 n.parent.fighting[ei].positions[timeSlice]);
								}
							}
						}
					}
			}

			GUIStyle style = new GUIStyle();
			style.normal.textColor = Color.red;
			style.fontSize = 16;
			style.normal.background = new Texture2D(100,20);

			if (textDraw != null)
				foreach (Tuple<Vector3, string> t in textDraw)
					Handles.Label(t.First, t.Second, style);
		}
	}

}
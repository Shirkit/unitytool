using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class MapperEditorDrawer : MonoBehaviour
{
	
	public Cell[][][] fullMap;
	public Node[,] rrtMatrix;
	public float[][] seenNeverSeen;
	public Dictionary<string, Node> rrtMap;
	public List<Node> path = null;
	public Dictionary<Path, bool> paths = new Dictionary<Path, bool> ();
	public int[,] heatMap;
	public int[][,] heatMap3d;
	public float heatMapMax = 0, seenNeverSeenMax;
	public int[] heatMapMax3d;
	public int timeSlice;
	public Vector2 zero = new Vector2 ();
	public Vector2 tileSize = new Vector2 ();
	public bool drawMap = true, drawMoveMap = false, drawNeverSeen = false, draw3dExploration = false, drawHeatMap = true, drawPath = false, editGrid = false;
	public Cell[][] editingGrid;
	//
	private Color orange = new Color(1.0f, 0.64f, 0f, 1f);
	/*private LineRenderer lines;
	private List<Node> lastPath = null;*/
	
	public void Start ()
	{
		Debug.Log ("Started");
		hideFlags = HideFlags.HideInInspector;
	}
	
	public void OnDrawGizmos ()
	{
		if (editGrid && editingGrid != null) {
			for (int x = 0; x < editingGrid.Length; x++)
				for (int y = 0; y < editingGrid[x].Length; y++) {
					Cell c = editingGrid [x] [y];
				
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
					else
						Gizmos.color = Color.gray;
				
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
						else if (heatMap3d != null)
							Gizmos.color = Color.Lerp (Color.white, Color.black, heatMapMax3d [timeSlice] != 0 ? (float)heatMap3d [timeSlice] [x, y] / (float)heatMapMax3d [timeSlice] : 0f);
					
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
						else if (drawNeverSeen)
							Gizmos.color = Color.Lerp(Color.green, Color.magenta, seenNeverSeen[x][y] / (seenNeverSeenMax * 3f/8f));
						else
							Gizmos.color = Color.green;
					}
				
					Gizmos.DrawCube (new Vector3
							(x * tileSize.x + zero.x + tileSize.x / 2f,
							drawMoveMap ? timeSlice : 0.1f,
							y * tileSize.y + zero.y + tileSize.y / 2f),
							new Vector3
								(tileSize.x - tileSize.x * 0.05f,
								0.0f,
								tileSize.y - tileSize.y * 0.05f));
				}
		}
		
		Gizmos.color = Color.magenta;
		if (rrtMatrix != null)
			for (int x = 0; x < rrtMatrix.GetLength(0); x++) 
				for (int y = 0; y < rrtMatrix.GetLength(1); y++)
					if (rrtMatrix [x, y].parent != null)
						Gizmos.DrawLine (new Vector3
								(rrtMatrix [x, y].x * tileSize.x + zero.x + tileSize.x / 2f,
								0.1f,
								rrtMatrix [x, y].y * tileSize.y + zero.y + tileSize.y / 2f), 
								
							new Vector3 
								(rrtMatrix [x, y].parent.x * tileSize.x + zero.x + tileSize.x / 2f,
								0.1f,
								rrtMatrix [x, y].parent.y * tileSize.y + zero.y + tileSize.y / 2f));
			
		if (draw3dExploration && rrtMap != null) {
			foreach (KeyValuePair<string, Node> pair in rrtMap) {
				Node n = pair.Value;
				if (n.parent != null)
					Gizmos.DrawLine (new Vector3
							(n.x * tileSize.x + zero.x + tileSize.x / 2f,
							n.t * (tileSize.x + tileSize.y) / 2,
							n.y * tileSize.y + zero.y + tileSize.y / 2f), 
							new Vector3
							(n.parent.x * tileSize.x + zero.x + tileSize.x / 2f,
							n.parent.t * (tileSize.x + tileSize.y) / 2,
							n.parent.y * tileSize.y + zero.y + tileSize.y / 2f));
			}
		}
			
		if (drawPath) {
			Gizmos.color = Color.blue;
			foreach (KeyValuePair<Path, bool> kv in paths)
				if (kv.Value) {
				Gizmos.color = kv.Key.color;
					foreach (Node n in kv.Key.points)
						if (n.parent != null)
							Gizmos.DrawLine (new Vector3
								((n.x * tileSize.x + zero.x),
								0.1f,
								(n.y * tileSize.x + zero.y)),
							
							new Vector3
								((n.parent.x * tileSize.y + zero.x),
								0.1f,
								(n.parent.y * tileSize.y + zero.y)));
			}
			
			/*if (lines == null) {
				lines = gameObject.GetComponent<LineRenderer>();
				if (lines == null)
					lines = gameObject.AddComponent<LineRenderer>();
			}
			else {
				lines.enabled = true;
			}
			
			if (lastPath != path) {
				
				lines.SetVertexCount(path.Count);
				for (int i = 0; i < path.Count; i++) {
					Vector3 pos = path[i].GetVector3();
					if (!drawMoveMap)
						pos.y = 0.2f;
					pos.x = pos.x * tileSize.x + zero.x;
					pos.z = pos.z * tileSize.y + zero.y;
					lines.SetPosition(i, pos);
				}
				
				lastPath = path;
			}
		} else if (lines != null) {
			lines.enabled = false;*/
		}
		
	}
}
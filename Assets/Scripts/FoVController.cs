using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Common;
using Objects;

public class FoVController : MonoBehaviour
{
	
	public GameObject floor, end;
	public int gridSize = 60;
	public float stepSize = 0.1f;
	public int ticksBehind = 0;
	public bool smoothPlayerPath = false;
	//
	private Mapper mapper;
	private Cell[][] obstaclesMap;
	private Cell[][][] fullMap;
	private int endX, endY;
	private GameObject player;
	private List<List<Vector2>> cells;
	private Path playerPath;
	private List<Vector3> playerPoints;
	
	
	// Use this for initialization
	void Start ()
	{
		// First prepare the mapper class
		if (mapper == null && floor != null) {
			mapper = floor.GetComponent<Mapper> ();
			if (floor == null)
				mapper = floor.AddComponent<Mapper> ();
		}
		
		// Then, we setup the enviornment needed to make it work
		if (mapper != null) {
			mapper.ComputeTileSize (SpaceState.Running, floor.collider.bounds.min, floor.collider.bounds.max, gridSize, gridSize);
			obstaclesMap = mapper.ComputeObstacles ();
			
			GameObject[] en = GameObject.FindGameObjectsWithTag ("Enemy") as GameObject[];
			Enemy[] enemies = new Enemy[en.Length];
			
			for (int i = 0; i < en.Length; i++) {
				enemies [i] = en [i].GetComponent<Enemy> ();
				enemies [i].positions = new Vector3[10000];
				enemies [i].forwards = new Vector3[10000];
				enemies [i].rotations = new Quaternion[10000];
				enemies [i].cells = new Vector2[10000][];
			}
			
			cells = new List<List<Vector2>> ();
			// Prepare the cells by enemy
			for (int i = 0; i < enemies.Length; i++) {
				cells.Add (new List<Vector2> ());
			}
			
			fullMap = new Cell[10000][][];
			
			SpaceState.Running.fullMap = fullMap;
			SpaceState.Running.enemies = enemies;
			
			player = GameObject.FindGameObjectWithTag ("Player");
			if (player == null)
				player = GameObject.FindGameObjectWithTag ("AI");
			
			playerPath = new Path (new List<Node> ());
			playerPoints = new List<Vector3> ();
			
			if (end == null) {
				end = GameObject.Find ("End");	
			}
			
			endX = (int)((end.transform.position.x - floor.collider.bounds.min.x) / SpaceState.Running.tileSize.x);
			endY = (int)((end.transform.position.z - floor.collider.bounds.min.z) / SpaceState.Running.tileSize.y);
			
			obstaclesMap [endX] [endY].goal = true;
			
			// Run this once before enemies moving
			acc += stepSize + 1;
			LateUpdate ();
		}
	}
	
	// Update is called once per frame
	void Update ()
	{
	}
	
	private float acc = 0f;
	private Node last;
	
	// After all Update() are called, this method is invoked
	void LateUpdate ()
	{
		if (acc > stepSize) {
			for (int en = 0; en < SpaceState.Running.enemies.Length; en++)
				cells [en].Clear ();
			
			Cell[][] computed = mapper.ComputeMap (obstaclesMap, SpaceState.Running.enemies, cells);
			fullMap [SpaceState.Running.timeSlice] = computed;
			
			// Store the seen cells in the enemy class
			List<Vector2>[] arr = cells.ToArray ();
			for (int i = 0; i < SpaceState.Running.enemies.Length; i++) {
				SpaceState.Running.enemies [i].cells [SpaceState.Running.timeSlice] = arr [i].ToArray ();
				SpaceState.Running.enemies [i].positions [SpaceState.Running.timeSlice] = SpaceState.Running.enemies [i].transform.position;
				SpaceState.Running.enemies [i].forwards [SpaceState.Running.timeSlice] = SpaceState.Running.enemies [i].transform.forward;
				arr [i].Clear ();
			}
			
			Vector2 pos = new Vector2 ((player.transform.position.x - SpaceState.Running.floorMin.x) / SpaceState.Running.tileSize.x, (player.transform.position.z - SpaceState.Running.floorMin.z) / SpaceState.Running.tileSize.y);
			int mapX = (int)pos.x;
			int mapY = (int)pos.y;
			
			Node curr = new Node ();
			curr.t = SpaceState.Running.timeSlice;
			curr.x = mapX;
			curr.y = mapY;
			curr.cell = fullMap [curr.t] [curr.x] [curr.y];
			curr.parent = last;
			last = curr;
			
			playerPath.points.Add (last);
			playerPoints.Add (player.transform.position);
			
			SpaceState.Running.timeSlice++;
			acc -= stepSize;
		}
		acc += Time.deltaTime;
	}
	
	public void OnApplicationQuit ()
	{
		if (smoothPlayerPath) {
			Node final = null;
			foreach (Node each in playerPath.points) {
				final = each;
				while (SmoothNode(final)) {
				}
			}
			
			playerPath.points.Clear ();
			
			while (final != null) {
				playerPath.points.Add (final);
				final = final.parent;
			}
			playerPath.points.Reverse ();
		}
		List<Path> paths = new List<Path> ();
		paths.Add (playerPath);
		PathBulk.SavePathsToFile ("playerPath.xml", paths);
		PathML.SavePathsToFile ("playerML.xml", playerPoints);
	}
	
	// TODO: Need to remove this funciton and make it a common library for this and RRT
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
	
	private bool CheckCollision (Node n1, Node n2, int deep = 0)
	{
		if (deep > 5)
			return false;
		int x = (n1.x + n2.x) / 2;
		int y = (n1.y + n2.y) / 2;
		int t = (n1.t + n2.t) / 2;
		Node n3 = new Node ();
		n3.cell = fullMap [t] [x] [y];
		n3.x = x;
		n3.t = t;
		n3.y = y;
		
		// Noisy calculation
		if (SpaceState.Running.enemies != null && ((Cell)n3.cell).noisy) {
			foreach (Enemy enemy in SpaceState.Running.enemies) {
				Vector3 dupe = enemy.positions [t];
				dupe.x = (dupe.x - SpaceState.Running.floorMin.x) / SpaceState.Running.tileSize.x;
				dupe.y = n3.t;
				dupe.z = (dupe.z - SpaceState.Running.floorMin.z) / SpaceState.Running.tileSize.y;
				
				// This distance is in number of cells size radius i.e. a 10 tilesize circle around the point
				if (Vector3.Distance (dupe, n3.GetVector3 ()) < 10)
					return true;
			} 
		}
		
		return !n3.cell.IsWalkable () || CheckCollision (n1, n3, deep + 1) || CheckCollision (n2, n3, deep + 1);
		
	}
}
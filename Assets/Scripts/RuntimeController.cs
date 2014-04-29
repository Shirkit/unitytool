using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Common;
using Objects;
using Extra;

public class RuntimeController : MonoBehaviour, NodeProvider {
	
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
	void Start () {
		// First prepare the mapper class
		if (mapper == null && floor != null) {
			mapper = floor.GetComponent<Mapper> ();
			if (floor == null)
				mapper = floor.AddComponent<Mapper> ();
		} else {
			Debug.LogError("No floor set, can't continue");
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
			
			// Run this once before enemies moving so we compute the first iteration of map
			acc += stepSize + 1;
			LateUpdate ();
		}
	}
	
	// Update is called once per frame
	void Update () {
	}
	
	private float acc = 0f;
	private Node last;
	
	// After all Update() are called, this method is invoked
	void LateUpdate () {
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
	
	public void OnApplicationQuit () {
		if (smoothPlayerPath) {
			Node final = null;

			foreach (Node each in playerPath.points) {
				final = each;
				while (Extra.Collision.SmoothNode(final, this, SpaceState.Running, true)) {
				}
			}
				
			playerPath.points.Clear ();
				
			while (final != null) {
				playerPath.points.Add (final);
				final = final.parent;
			}
			playerPath.points.Reverse ();				
		}

		playerPath.color = new Color (UnityEngine.Random.Range (0.0f, 1.0f), UnityEngine.Random.Range (0.0f, 1.0f), UnityEngine.Random.Range (0.0f, 1.0f));

		List<Path> paths = new List<Path> ();
		paths.Add (playerPath);
		PathBulk.SavePathsToFile ("playerPath.xml", paths);
		new PathML (SpaceState.Running).SavePathsToFile ("playerML.xml", playerPoints);
	}

	public Node GetNode (int t, int x, int y) {
		Node n3 = new Node ();
		n3.cell = SpaceState.Running.fullMap [t] [x] [y];
		n3.x = x;
		n3.t = t;
		n3.y = y;
		return n3;
	}
}
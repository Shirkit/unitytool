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
	//
	private Mapper mapper;
	private Cell[][] obstaclesMap;
	private Cell[][][] fullMap;
	private int endX, endY;
	private List<List<Vector2>> cells;
	
	
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
				enemies [i].positions = new Vector3[1];
				enemies [i].forwards = new Vector3[1];
				enemies [i].rotations = new Quaternion[1];
				enemies [i].cells = new Vector2[1][];
			}
			
			cells = new List<List<Vector2>> ();
			// Prepare the cells by enemy
			for (int i = 0; i < enemies.Length; i++) {
				cells.Add (new List<Vector2> ());
			}
			
			fullMap = new Cell[1][][];
			
			SpaceState.Running.fullMap = fullMap;
			SpaceState.Running.enemies = enemies;
			
			if (end == null) {
				end = GameObject.Find ("End");	
			}
			
			endX = (int)((end.transform.position.x - floor.collider.bounds.min.x) / SpaceState.Running.tileSize.x);
			endY = (int)((end.transform.position.z - floor.collider.bounds.min.z) / SpaceState.Running.tileSize.y);
			
			obstaclesMap[endX][endY].goal = true;
			
			// Run this once before enemies moving
			LateUpdate ();
		}
	}
	
	// Update is called once per frame
	void Update ()
	{
	}
	
	void LateUpdate ()
	{
		for (int en = 0; en < SpaceState.Running.enemies.Length; en++)
			cells [en].Clear ();
		
		Cell[][] computed = mapper.ComputeMap (obstaclesMap, SpaceState.Running.enemies, cells);
		fullMap [SpaceState.Running.timeSlice] = computed;
		
		// Store the seen cells in the enemy class
		List<Vector2>[] arr = cells.ToArray ();
		for (int i = 0; i < SpaceState.Running.enemies.Length; i++) {
			SpaceState.Running.enemies [i].cells [0] = arr [0].ToArray ();
			arr [0].Clear ();
		}
	}
}
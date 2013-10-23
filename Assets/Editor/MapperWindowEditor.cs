using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;
using System.Linq;

public class MapperWindowEditor : EditorWindow
{
	
	// Data holders
	public static Cell[][][] fullMap;
	public static List<Path> paths = new List<Path> ();
	public static List<Node> mostDanger = null, shortest = null, lengthiest = null, fastest = null, longest = null;
	// Parameters
	public static int startX, startY, maxHeatMap, endX = 27, endY = 27, timeSlice, timeSamples = 800, attemps = 25000, iterations = 2, gridSize = 60, ticksBehind = 5;
	public static bool drawMap = true, drawMoveMap = false, drawMoveUnits = false, drawNeverSeen = false, draw3dExploration = false, drawHeatMap = false, drawHeatMap3d = false, drawPath = true, smoothPath = true, drawShortestPath = false, drawLongestPath = false, drawLengthiestPath = false, drawFastestPath = false, drawMostDangerousPath = false, drawFoVOnly = true, seeByTime = false, seeByLength = false, seeByDanger = false, seeByLoS = false, seeByDanger3 = false, seeByLoS3 = false, seeByDanger3Norm = false, seeByLoS3Norm = false, seeByCrazy = false, seeByVelocity = false;
	public static float stepSize = 1 / 10f, crazySeconds = 5f;
	public static int[,] heatMap;
	public static GameObject start = null, end = null, floor = null, playerPrefab = null;
	public static List<GameObject> waypoints = new List<GameObject> ();
	public static Dictionary<Path, bool> toggleStatus = new Dictionary<Path, bool> ();
	public static Dictionary<Path, GameObject> players = new Dictionary<Path, GameObject> ();
	// Helping stuff
	public static List<Path> arrangedByTime, arrangedByLength, arrangedByDanger, arrangedByLoS, arrangedByDanger3, arrangedByLoS3, arrangedByDanger3Norm, arrangedByLoS3Norm, arrangedByCrazy, arrangedByVelocity;
	private static Vector2 scrollPos = new Vector2 ();
	private int lastTime = timeSlice;
	private static bool simulated = false, playing = false;
	private static float playTime = 0f;
	private Mapper mapper;
	private RRTKDTree rrt = new RRTKDTree ();
	private MapperEditorDrawer drawer;
	
	[MenuItem("Window/Mapper")]
	static void Init ()
	{
		MapperWindowEditor window = (MapperWindowEditor)EditorWindow.GetWindow (typeof(MapperWindowEditor));
		window.title = "Mapper";
		window.ShowTab ();
	}
	
	void OnGUI ()
	{
		// Wait for the floor to be set and initialize the drawer and the mapper
		if (floor != null) {
			if (floor.collider == null) {
				Debug.LogWarning ("Floor has no valid collider, game object ignored.");
				floor = null;
			} else {
				drawer = floor.gameObject.GetComponent<MapperEditorDrawer> ();
				if (drawer == null) {
					drawer = floor.gameObject.AddComponent<MapperEditorDrawer> ();
					drawer.hideFlags = HideFlags.HideInInspector;
				}
			}
			if (mapper == null) {
				mapper = floor.GetComponent<Mapper> ();
				if (mapper == null) {
					mapper = floor.AddComponent<Mapper> ();
				}
			}
		} 
		
		// ----------------------------------
		
		scrollPos = EditorGUILayout.BeginScrollView (scrollPos);
		
		#region 1. Map
		
		EditorGUILayout.LabelField ("1. Map");
		playerPrefab = (GameObject)EditorGUILayout.ObjectField ("Player Prefab", playerPrefab, typeof(GameObject), false);
		
		floor = (GameObject)EditorGUILayout.ObjectField ("Floor", floor, typeof(GameObject), true);
		gridSize = EditorGUILayout.IntSlider ("Grid size", gridSize, 10, 300);

		EditorGUILayout.LabelField ("");
		
		#endregion
		
		// ----------------------------------
		
		#region 2. Units
		
		EditorGUILayout.LabelField ("2. Units");
		
		if (GUILayout.Button ("Store Positions")) {
			StorePositions ();
		}
		EditorGUILayout.LabelField ("");
		
		#endregion
		
		// ----------------------------------
		
		#region 3. Map Computation
		
		EditorGUILayout.LabelField ("3. Map Computation");
		timeSamples = EditorGUILayout.IntSlider ("Time samples", timeSamples, 1, 10000);
		stepSize = EditorGUILayout.Slider ("Step size", stepSize, 0.01f, 1f);
		ticksBehind = EditorGUILayout.IntSlider (new GUIContent ("Ticks behind", "Number of ticks that the FoV will remain seen after the enemy has no visibility on that cell (prevents noise/jitter like behaviours)"), ticksBehind, 0, 100);
		
		if (GUILayout.Button ("Precompute Maps")) {
			
			//Find this is the view
			if (playerPrefab == null) {
				//Debug.Log("No playerPrefab"); 
				//playerPrefab = (GameObject)(Resources.Load( "../Prefab/Player.prefab", typeof(GameObject)));
				playerPrefab = GameObject.Find ("Player"); 
			}
			if (floor == null) {
				floor = (GameObject)GameObject.Find ("Floor");
				
				gridSize = EditorGUILayout.IntSlider ("Grid size", gridSize, 10, 300);
				
				if (mapper == null) {
					mapper = floor.GetComponent<Mapper> ();
					
					if (mapper == null)
						mapper = floor.AddComponent<Mapper> ();
					
				}
				drawer = floor.gameObject.GetComponent<MapperEditorDrawer> ();
				if (drawer == null) {
					drawer = floor.gameObject.AddComponent<MapperEditorDrawer> ();
					drawer.hideFlags = HideFlags.HideInInspector;
				}
			}
			
			if (!simulated) {
				StorePositions ();
				simulated = true;
			}
			fullMap = mapper.PrecomputeMaps (floor.collider.bounds.min, floor.collider.bounds.max, gridSize, gridSize, timeSamples, stepSize, ticksBehind);
			
			drawer.fullMap = fullMap;
			float maxSeenGrid;
			drawer.seenNeverSeen = Analyzer.ComputeSeenValuesGrid (fullMap, out maxSeenGrid);
			drawer.seenNeverSeenMax = maxSeenGrid;
			drawer.tileSize = SpaceState.TileSize;
			drawer.zero.Set (floor.collider.bounds.min.x, floor.collider.bounds.min.z);
			
			ResetAI ();
		} 
		EditorGUILayout.LabelField ("");
		
		#endregion
		
		// ----------------------------------
		
		#region 4. Path
		
		EditorGUILayout.LabelField ("4. Path");
		
		start = (GameObject)EditorGUILayout.ObjectField ("Start", start, typeof(GameObject), true);
		end = (GameObject)EditorGUILayout.ObjectField ("End", end, typeof(GameObject), true);
		attemps = EditorGUILayout.IntSlider ("Attempts", attemps, 1000, 100000);
		iterations = EditorGUILayout.IntSlider ("Iterations", iterations, 1, 1500);
		smoothPath = EditorGUILayout.Toggle ("Smooth path", smoothPath);
		
		// Future work planned, allow the RRT to pass through this safe spots
		/*someBoolean = EditorGUILayout.Foldout (someBoolean, "Passby Waypoints");
		if (someBoolean) {
			for (int i = 0; i < waypoints.Count; i++) {
				EditorGUILayout.BeginHorizontal ();
				
				waypoints [i] = (GameObject)EditorGUILayout.ObjectField ("N:" + (i + 1), waypoints [i], typeof(GameObject), true);
				
				if (GUILayout.Button ("X", GUILayout.MaxWidth (20f))) {
					waypoints.RemoveAt (i);
					i--;
				}
				EditorGUILayout.EndHorizontal ();
			}
			
			GameObject newone = null;
			newone = (GameObject)EditorGUILayout.ObjectField ("N:" + (waypoints.Count + 1), newone, typeof(GameObject), true);
				
			if (newone != null)
				waypoints.Add (newone);
		}*/
		
		if (GUILayout.Button ("Compute Path")) {
			float speed = GameObject.FindGameObjectWithTag ("AI").GetComponent<Player> ().speed;
			
			
			//Check the start and the end and get them from the editor. 
			if (start == null) {
				start = GameObject.Find ("Start");
			}
			if (end == null) {
				end = GameObject.Find ("End");	
			}
			
			paths.Clear ();
			toggleStatus.Clear ();
			arrangedByCrazy = arrangedByDanger = arrangedByDanger3 = arrangedByDanger3Norm = arrangedByLength = arrangedByLoS = arrangedByLoS3 = arrangedByLoS3Norm = arrangedByTime = arrangedByVelocity = null;
			
			startX = (int)((start.transform.position.x - floor.collider.bounds.min.x) / SpaceState.TileSize.x);
			startY = (int)((start.transform.position.z - floor.collider.bounds.min.z) / SpaceState.TileSize.y);
			endX = (int)((end.transform.position.x - floor.collider.bounds.min.x) / SpaceState.TileSize.x);
			endY = (int)((end.transform.position.z - floor.collider.bounds.min.z) / SpaceState.TileSize.y);
			
			rrt.min = floor.collider.bounds.min;
			rrt.tileSizeX = SpaceState.TileSize.x;
			rrt.tileSizeZ = SpaceState.TileSize.y;
			rrt.enemies = SpaceState.Enemies;
			
			List<Node> nodes = null;
			for (int it = 0; it < iterations; it++) {
				nodes = rrt.Compute (startX, startY, endX, endY, attemps, speed, fullMap, smoothPath);
				if (nodes.Count > 0) {
					paths.Add (new Path (nodes));
					toggleStatus.Add (paths.Last (), true);
					paths.Last ().color = new Color (UnityEngine.Random.Range (0.0f, 1.0f), UnityEngine.Random.Range (0.0f, 1.0f), UnityEngine.Random.Range (0.0f, 1.0f));
				}
			}
			heatMap = Analyzer.Compute2DHeatMap (paths, gridSize, gridSize, out maxHeatMap);
			
			Debug.Log ("Paths found: " + paths.Count);
			
			drawer.heatMapMax = maxHeatMap;
			drawer.heatMap = heatMap;
			
			int[] maxHeatMap3d;
			drawer.heatMap3d = Analyzer.Compute3DHeatMap (paths, gridSize, gridSize, timeSamples, out maxHeatMap3d);
			drawer.heatMapMax3d = maxHeatMap3d;
						
			drawer.rrtMap = rrt.explored;
			drawer.tileSize.Set (SpaceState.TileSize.x, SpaceState.TileSize.y);
			shortest = fastest = longest = lengthiest = mostDanger = null;
			
			if(paths.Count == 2)
			{
				toggleStatus.Clear ();
			
				foreach (GameObject obj in players.Values)
					GameObject.DestroyImmediate (obj);
				
				players.Clear ();
				Resources.UnloadUnusedAssets ();
				
				int i = 1;
				foreach (Path p in paths) 
				{
					p.name = "Path " + (i);
				
					if ( i == 1)
						p.color = Color.blue;		
					else
						p.color = Color.red;
					toggleStatus.Add (p, true);

				
				
					p.ZeroValues ();
					i++; 
				}
				
				//Force the display
				
				Analyzer.ComputePathsTimeValues (paths);
				
				arrangedByTime = new List<Path> ();
				arrangedByTime.AddRange (paths);
				arrangedByTime.Sort (new Analyzer.TimeComparer ());
				
				
				seeByTime = EditorGUILayout.Foldout (seeByTime, "Paths by Time");
				if (seeByTime && arrangedByTime != null) 
				{
					for (i = 0; i < arrangedByTime.Count; i++) 
					{
						EditorGUILayout.BeginHorizontal ();
		
						EditorGUILayout.FloatField (arrangedByTime [i].name, arrangedByTime [i].time);
						toggleStatus [arrangedByTime [i]] = EditorGUILayout.Toggle ("", toggleStatus [arrangedByTime [i]], GUILayout.MaxWidth (20f));
						EditorGUILayout.ColorField (arrangedByTime [i].color, GUILayout.MaxWidth (40f));
						
						EditorGUILayout.EndHorizontal ();
					}
				}
			}
		}
		
		
		
		EditorGUILayout.LabelField ("");
		
		
		#endregion
		
		// ----------------------------------
		
		#region 5. Visualization
		
		EditorGUILayout.LabelField ("5. Visualization");
		
		timeSlice = EditorGUILayout.IntSlider ("Time", timeSlice, 0, timeSamples - 1);
		drawMap = EditorGUILayout.Toggle ("Draw map", drawMap);
		drawNeverSeen = EditorGUILayout.Toggle ("- Draw safe places", drawNeverSeen);
		drawFoVOnly = EditorGUILayout.Toggle ("- Draw only fields of view", drawFoVOnly);
		drawHeatMap = EditorGUILayout.Toggle ("- Draw heat map", drawHeatMap);
		drawHeatMap3d = EditorGUILayout.Toggle ("-> Draw heat map 3d", drawHeatMap3d);
		drawPath = EditorGUILayout.Toggle ("Draw path", drawPath);
		drawMoveMap = EditorGUILayout.Toggle ("Move map Y-axis", drawMoveMap);
		drawMoveUnits = EditorGUILayout.Toggle ("Move units Y-axis", drawMoveUnits);
		draw3dExploration = EditorGUILayout.Toggle ("Draw 3D exploration", draw3dExploration);
		
		if (drawer != null) {
			if (drawHeatMap3d)
				drawer.heatMap = null;
			else
				drawer.heatMap = heatMap;
		}
		
		EditorGUILayout.LabelField ("");
		
		if (GUILayout.Button ("Play")) {
			playing = !playing;
		}
		
		EditorGUILayout.LabelField ("");
		
		if (GUILayout.Button ("Batch computation")) {
			BatchComputing ();
		}
		
		
		
		#endregion
		
		// ----------------------------------
		
		#region 6. Paths
		
		EditorGUILayout.LabelField ("6. Paths");
		
		EditorGUILayout.LabelField ("");
		
		crazySeconds = EditorGUILayout.Slider ("Crazy seconds window", crazySeconds, 0f, 10f);
		
		if (GUILayout.Button ("Analyze paths")) {		
			toggleStatus.Clear ();
			
			foreach (GameObject obj in players.Values)
				GameObject.DestroyImmediate (obj);
			
			players.Clear ();
			Resources.UnloadUnusedAssets ();
			
			int i = 1;
			foreach (Path path in paths) {
				path.name = "Path " + (i++);
				path.color = new Color (UnityEngine.Random.Range (0.0f, 1.0f), UnityEngine.Random.Range (0.0f, 1.0f), UnityEngine.Random.Range (0.0f, 1.0f));
				toggleStatus.Add (path, false);
				path.ZeroValues ();
			}
			
			Analyzer.ComputePathsTimeValues (paths);
			Analyzer.ComputePathsLengthValues (paths);
			Analyzer.ComputePathsVelocityValues (paths);
			Analyzer.ComputePathsLoSValues (paths, SpaceState.Enemies, floor.collider.bounds.min, SpaceState.TileSize.x, SpaceState.TileSize.y, fullMap, drawer.seenNeverSeen, drawer.seenNeverSeenMax);
			Analyzer.ComputePathsDangerValues (paths, SpaceState.Enemies, floor.collider.bounds.min, SpaceState.TileSize.x, SpaceState.TileSize.y, fullMap, drawer.seenNeverSeen, drawer.seenNeverSeenMax);
			Analyzer.ComputeCrazyness (paths, fullMap, Mathf.FloorToInt (crazySeconds / stepSize));
			
			arrangedByTime = new List<Path> ();
			arrangedByTime.AddRange (paths);
			arrangedByTime.Sort (new Analyzer.TimeComparer ());
			
			arrangedByLength = new List<Path> ();
			arrangedByLength.AddRange (paths);
			arrangedByLength.Sort (new Analyzer.Length2dComparer ());
			
			arrangedByDanger = new List<Path> ();
			arrangedByDanger.AddRange (paths);
			arrangedByDanger.Sort (new Analyzer.DangerComparer ());
			
			arrangedByDanger3 = new List<Path> ();
			arrangedByDanger3.AddRange (paths);
			arrangedByDanger3.Sort (new Analyzer.Danger3Comparer ());
			
			arrangedByDanger3Norm = new List<Path> ();
			arrangedByDanger3Norm.AddRange (paths);
			arrangedByDanger3Norm.Sort (new Analyzer.Danger3NormComparer ());
			
			arrangedByLoS = new List<Path> ();
			arrangedByLoS.AddRange (paths);
			arrangedByLoS.Sort (new Analyzer.LoSComparer ());
			
			arrangedByLoS3 = new List<Path> ();
			arrangedByLoS3.AddRange (paths);
			arrangedByLoS3.Sort (new Analyzer.LoS3Comparer ());
			
			arrangedByLoS3Norm = new List<Path> ();
			arrangedByLoS3Norm.AddRange (paths);
			arrangedByLoS3Norm.Sort (new Analyzer.LoS3NormComparer ());
			
			arrangedByCrazy = new List<Path> ();
			arrangedByCrazy.AddRange (paths);
			arrangedByCrazy.Sort (new Analyzer.CrazyComparer ());
			
			arrangedByVelocity = new List<Path> ();
			arrangedByVelocity.AddRange (paths);
			arrangedByVelocity.Sort (new Analyzer.VelocityComparer ());
			
			//Put the output of some of them in an XML file for human study.
			if(paths.Count == 2)
			{
				String s = "";
				s += "<Question> \n";
				s += "	<id> </id>\n";
				s += "	<Path1> </Path1>\n";
				s += "	<Path2> </Path2>\n";
				s += "	<P1Colour>Red</P1Colour>\n";
				s += "	<P2Colour>Blue</P2Colour>\n";
				//Different Values	   		
				foreach(Path p in paths)
				{
					s+="	<Danger3>";
					s+= p.danger3.ToString(); 
					s+= "</Danger3>\n";
				}
				foreach(Path p in paths)
				{
					s+="	<LOS3>";
					s+= p.los3.ToString(); 
					s+= "</LOS3>\n";
				}
				foreach(Path p in paths)
				{
					s+="	<Crazy>";
					s+= p.crazy.ToString(); 
					s+= "</Crazy>\n";
				}
				s += "</Question>\n";
				
				//Debug.Log(questionNumber);
				System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(
					@"C:\Users\jtremb59\Dropbox\PhD\2013 Fall\Human Experiment\Output Tool\");
	            int count = dir.GetFiles().Length + 1;
				
				System.IO.File.WriteAllText(
					@"C:\Users\jtremb59\Dropbox\PhD\2013 Fall\Human Experiment\Output Tool\"+
					count.ToString()+".txt", s);
			}
			
		}
		
		seeByTime = EditorGUILayout.Foldout (seeByTime, "Paths by Time");
		if (seeByTime && arrangedByTime != null) {
			for (int i = 0; i < arrangedByTime.Count; i++) {
				EditorGUILayout.BeginHorizontal ();

				EditorGUILayout.FloatField (arrangedByTime [i].name, arrangedByTime [i].time);
				toggleStatus [arrangedByTime [i]] = EditorGUILayout.Toggle ("", toggleStatus [arrangedByTime [i]], GUILayout.MaxWidth (20f));
				EditorGUILayout.ColorField (arrangedByTime [i].color, GUILayout.MaxWidth (40f));
				
				EditorGUILayout.EndHorizontal ();
			}
		}
		
		seeByLength = EditorGUILayout.Foldout (seeByLength, "Paths by Length");
		if (seeByLength && arrangedByLength != null) {
			for (int i = 0; i < arrangedByLength.Count; i++) {
				EditorGUILayout.BeginHorizontal ();

				EditorGUILayout.FloatField (arrangedByLength [i].name, arrangedByLength [i].length2d);
				toggleStatus [arrangedByLength [i]] = EditorGUILayout.Toggle ("", toggleStatus [arrangedByLength [i]], GUILayout.MaxWidth (20f));
				EditorGUILayout.ColorField (arrangedByLength [i].color, GUILayout.MaxWidth (40f));
				
				EditorGUILayout.EndHorizontal ();
			}
		}
		
		seeByDanger = EditorGUILayout.Foldout (seeByDanger, "Paths by Danger (A*)");
		if (seeByDanger && arrangedByDanger != null) {
			for (int i = 0; i < arrangedByDanger.Count; i++) {
				EditorGUILayout.BeginHorizontal ();

				EditorGUILayout.FloatField (arrangedByDanger [i].name, arrangedByDanger [i].danger);
				toggleStatus [arrangedByDanger [i]] = EditorGUILayout.Toggle ("", toggleStatus [arrangedByDanger [i]], GUILayout.MaxWidth (20f));
				EditorGUILayout.ColorField (arrangedByDanger [i].color, GUILayout.MaxWidth (40f));
				
				EditorGUILayout.EndHorizontal ();
			}
		}
		
		seeByDanger3 = EditorGUILayout.Foldout (seeByDanger3, "Paths by Danger 3 (A*)");
		if (seeByDanger3 && arrangedByDanger3 != null) {
			for (int i = 0; i < arrangedByDanger3.Count; i++) {
				EditorGUILayout.BeginHorizontal ();

				EditorGUILayout.FloatField (arrangedByDanger3 [i].name, arrangedByDanger3 [i].danger3);
				toggleStatus [arrangedByDanger3 [i]] = EditorGUILayout.Toggle ("", toggleStatus [arrangedByDanger3 [i]], GUILayout.MaxWidth (20f));
				EditorGUILayout.ColorField (arrangedByDanger3 [i].color, GUILayout.MaxWidth (40f));
				
				EditorGUILayout.EndHorizontal ();
			}
		}
		
		seeByDanger3Norm = EditorGUILayout.Foldout (seeByDanger3Norm, "Paths by Danger 3 (A*) (normalized)");
		if (seeByDanger3Norm && arrangedByDanger3Norm != null) {
			for (int i = 0; i < arrangedByDanger3Norm.Count; i++) {
				EditorGUILayout.BeginHorizontal ();

				EditorGUILayout.FloatField (arrangedByDanger3Norm [i].name, arrangedByDanger3Norm [i].danger3Norm);
				toggleStatus [arrangedByDanger3Norm [i]] = EditorGUILayout.Toggle ("", toggleStatus [arrangedByDanger3Norm [i]], GUILayout.MaxWidth (20f));
				EditorGUILayout.ColorField (arrangedByDanger3Norm [i].color, GUILayout.MaxWidth (40f));
				
				EditorGUILayout.EndHorizontal ();
			}
		}
		
		seeByLoS = EditorGUILayout.Foldout (seeByLoS, "Paths by Line of Sight (FoV)");
		if (seeByLoS && arrangedByLoS != null) {
			for (int i = 0; i < arrangedByLoS.Count; i++) {
				EditorGUILayout.BeginHorizontal ();

				EditorGUILayout.FloatField (arrangedByLoS [i].name, arrangedByLoS [i].los);
				toggleStatus [arrangedByLoS [i]] = EditorGUILayout.Toggle ("", toggleStatus [arrangedByLoS [i]], GUILayout.MaxWidth (20f));
				EditorGUILayout.ColorField (arrangedByLoS [i].color, GUILayout.MaxWidth (40f));
				
				EditorGUILayout.EndHorizontal ();
			}
		}
		
		seeByLoS3 = EditorGUILayout.Foldout (seeByLoS3, "Paths by Line of Sight 3 (FoV)");
		if (seeByLoS3 && arrangedByLoS3 != null) {
			for (int i = 0; i < arrangedByLoS3.Count; i++) {
				EditorGUILayout.BeginHorizontal ();

				EditorGUILayout.FloatField (arrangedByLoS3 [i].name, arrangedByLoS3 [i].los3);
				toggleStatus [arrangedByLoS3 [i]] = EditorGUILayout.Toggle ("", toggleStatus [arrangedByLoS3 [i]], GUILayout.MaxWidth (20f));
				EditorGUILayout.ColorField (arrangedByLoS3 [i].color, GUILayout.MaxWidth (40f));
				
				EditorGUILayout.EndHorizontal ();
			}
		}
		
		seeByLoS3Norm = EditorGUILayout.Foldout (seeByLoS3Norm, "Paths by Line of Sight 3 (FoV) (normalized)");
		if (seeByLoS3Norm && arrangedByLoS3Norm != null) {
			for (int i = 0; i < arrangedByLoS3Norm.Count; i++) {
				EditorGUILayout.BeginHorizontal ();

				EditorGUILayout.FloatField (arrangedByLoS3Norm [i].name, arrangedByLoS3Norm [i].los3Norm);
				toggleStatus [arrangedByLoS3Norm [i]] = EditorGUILayout.Toggle ("", toggleStatus [arrangedByLoS3Norm [i]], GUILayout.MaxWidth (20f));
				EditorGUILayout.ColorField (arrangedByLoS3Norm [i].color, GUILayout.MaxWidth (40f));
				
				EditorGUILayout.EndHorizontal ();
			}
		}
		
		seeByCrazy = EditorGUILayout.Foldout (seeByCrazy, "Paths by Crazyness");
		if (seeByCrazy && arrangedByCrazy != null) {
			for (int i = 0; i < arrangedByCrazy.Count; i++) {
				EditorGUILayout.BeginHorizontal ();

				EditorGUILayout.FloatField (arrangedByCrazy [i].name, arrangedByCrazy [i].crazy);
				toggleStatus [arrangedByCrazy [i]] = EditorGUILayout.Toggle ("", toggleStatus [arrangedByCrazy [i]], GUILayout.MaxWidth (20f));
				EditorGUILayout.ColorField (arrangedByCrazy [i].color, GUILayout.MaxWidth (40f));
				
				EditorGUILayout.EndHorizontal ();
			}
		}
		
		seeByVelocity = EditorGUILayout.Foldout (seeByVelocity, "Paths by Velocity Changes");
		if (seeByVelocity && arrangedByVelocity != null) {
			for (int i = 0; i < arrangedByVelocity.Count; i++) {
				EditorGUILayout.BeginHorizontal ();

				EditorGUILayout.FloatField (arrangedByVelocity [i].name, arrangedByVelocity [i].velocity);
				toggleStatus [arrangedByVelocity [i]] = EditorGUILayout.Toggle ("", toggleStatus [arrangedByVelocity [i]], GUILayout.MaxWidth (20f));
				EditorGUILayout.ColorField (arrangedByVelocity [i].color, GUILayout.MaxWidth (40f));
				
				EditorGUILayout.EndHorizontal ();
			}
		}
		
		
		
		#endregion
		
		// ----------------------------------
		
		#region Voronoi
		/*
		EditorGUILayout.LabelField ("");
		EditorGUILayout.LabelField ("7. Voronoi");
		
		
		
		if (GUILayout.Button ("Calculate Voronoi")) {
			CalculateVoronoi ();
		}
		*/
		#endregion
		
		
		foreach (KeyValuePair<Path, bool> p in toggleStatus) {
			if (p.Value) {
				if (!players.ContainsKey (p.Key)) {
					GameObject player = GameObject.Instantiate (playerPrefab) as GameObject;
					player.transform.position.Set (p.Key.points [0].x, 0f, p.Key.points [0].y);
					players.Add (p.Key, player);
					Material m = new Material (player.renderer.sharedMaterial);
					m.color = p.Key.color;
					player.renderer.material = m;
					Resources.UnloadUnusedAssets ();
				} else {
					players [p.Key].SetActive (true);
				}
			} else {
				if (players.ContainsKey (p.Key)) {
					players [p.Key].SetActive (false);
				}
			}
		}
		
		EditorGUILayout.EndScrollView ();
		
		// ----------------------------------
		
		if (drawer != null) {
			drawer.timeSlice = timeSlice;
			drawer.draw3dExploration = draw3dExploration;
			drawer.drawHeatMap = drawHeatMap;
			drawer.drawMap = drawMap;
			drawer.drawFoVOnly = drawFoVOnly;
			drawer.drawMoveMap = drawMoveMap;
			drawer.drawNeverSeen = drawNeverSeen;
			drawer.drawPath = drawPath;
			drawer.paths = toggleStatus;
			
		}
		
		if (fullMap != null && lastTime != timeSlice) {
			lastTime = timeSlice;
			UpdatePositions (timeSlice, mapper);
		}
		
		SceneView.RepaintAll ();
	}

	void BatchComputing ()
	{
		ResultsRoot root = new ResultsRoot ();
			
		float speed = GameObject.FindGameObjectWithTag ("AI").GetComponent<Player> ().speed;
			
		int gridsize, timesamples, rrtattemps, att = 1;
			
		//for (int att = 1; att <= 31; att++) {
			
		// Grid varying batch tests
			
		using (FileStream stream = new FileStream ("gridvary" + att + ".xml", FileMode.Create)) {
				
			rrtattemps = 30000;
			timesamples = 1200;
				
			for (gridsize = 60; gridsize <= 160; gridsize += 5) {
					
				Debug.Log ("Gridsize attemps " + gridsize + " Memory: " + GC.GetTotalMemory (true) + " Date: " + System.DateTime.Now.ToString ());
							
				fullMap = mapper.PrecomputeMaps (floor.collider.bounds.min, floor.collider.bounds.max, gridSize, gridsize, timesamples, stepSize);
							
				startX = (int)((start.transform.position.x - floor.collider.bounds.min.x) / SpaceState.TileSize.x);
				startY = (int)((start.transform.position.z - floor.collider.bounds.min.z) / SpaceState.TileSize.y);
				endX = (int)((end.transform.position.x - floor.collider.bounds.min.x) / SpaceState.TileSize.x);
				endY = (int)((end.transform.position.z - floor.collider.bounds.min.z) / SpaceState.TileSize.y);

				ResultBatch job = new ResultBatch ();
				job.gridSize = gridsize;
				job.timeSamples = timesamples;
				job.rrtAttemps = rrtattemps;
							
				TimeSpan average = new TimeSpan (0, 0, 0, 0, 0);
				List<Node> path = null;
							
				for (int it = 0; it < 155;) {
					Result single = new Result ();
								
					DateTime before = System.DateTime.Now;
					path = rrt.Compute (startX, startY, endX, endY, rrtattemps, speed, fullMap, false);
					rrt.tree = null;
					DateTime after = System.DateTime.Now;
					TimeSpan delta = after - before;
								
					average += delta;
					single.timeSpent = delta.TotalMilliseconds;
								
					if (path != null && path.Count > 0) {
						it++;
						job.results.Add (single);
					}
					job.totalTries++;
					if (job.totalTries > 100 && job.results.Count == 0)
						it = 200;
				}
					
				// Force the garbage collector to pick this guy before instantiating the next map to avoid memory leak in the Large Object Heap
				fullMap = null;
							
				job.averageTime = (double)average.TotalMilliseconds / (double)job.totalTries;
							
				root.everything.Add (job);
					
			}
				
			Debug.Log ("Serializing 1");
				
			XmlSerializer ser = new XmlSerializer (typeof(ResultsRoot), new Type[] {typeof(ResultBatch), typeof(Result)});
			ser.Serialize (stream, root);
			stream.Flush ();
			stream.Close ();
		}
			
		// Time varying batch tests
			
		using (FileStream stream = new FileStream ("timevary" + att + ".xml", FileMode.Create)) {
			
			root.everything.Clear ();
				
			gridsize = 60;
			rrtattemps = 30000;
				
			for (timesamples = 500; timesamples <= 3500; timesamples += 100) {
					
				Debug.Log ("Timesamples attemps " + timesamples + " Memory: " + GC.GetTotalMemory (true) + " Date: " + System.DateTime.Now.ToString ());
					
				fullMap = mapper.PrecomputeMaps (floor.collider.bounds.min, floor.collider.bounds.max, gridSize, gridSize, timesamples, stepSize);
					
				startX = (int)((start.transform.position.x - floor.collider.bounds.min.x) / SpaceState.TileSize.x);
				startY = (int)((start.transform.position.z - floor.collider.bounds.min.z) / SpaceState.TileSize.y);
				endX = (int)((end.transform.position.x - floor.collider.bounds.min.x) / SpaceState.TileSize.x);
				endY = (int)((end.transform.position.z - floor.collider.bounds.min.z) / SpaceState.TileSize.y);
					
				ResultBatch job = new ResultBatch ();
				job.gridSize = gridsize;
				job.timeSamples = timesamples;
				job.rrtAttemps = rrtattemps;
					
				TimeSpan average = new TimeSpan (0, 0, 0, 0, 0);
				List<Node> path = null;
					
				for (int it = 0; it < 155;) {
					Result single = new Result ();
						
					DateTime before = System.DateTime.Now;
					path = rrt.Compute (startX, startY, endX, endY, rrtattemps, speed, fullMap, false);
					DateTime after = System.DateTime.Now;
					TimeSpan delta = after - before;
						
					average += delta;
					single.timeSpent = delta.TotalMilliseconds;
						
					if (path.Count > 0) {
						it++;
						job.results.Add (single);
					}
					job.totalTries++;
					if (job.totalTries > 100 && job.results.Count == 0)
						it = 200;
				}
				fullMap = null;
					
				job.averageTime = (double)average.TotalMilliseconds / (double)job.totalTries;
					
				root.everything.Add (job);
					
			}
				
			Debug.Log ("Serializing 2");
				
			XmlSerializer ser = new XmlSerializer (typeof(ResultsRoot), new Type[] {typeof(ResultBatch), typeof(Result)});
			ser.Serialize (stream, root);
			stream.Flush ();
			stream.Close ();
		}
			
		// Attemp varying batch tests
			
		using (FileStream stream = new FileStream ("attempvary" + att + ".xml", FileMode.Create)) {
			
			root.everything.Clear ();
				
			gridsize = 60;
			timesamples = 1200;

			fullMap = mapper.PrecomputeMaps (floor.collider.bounds.min, floor.collider.bounds.max, gridSize, gridSize, timesamples, stepSize);
				
			startX = (int)((start.transform.position.x - floor.collider.bounds.min.x) / SpaceState.TileSize.x);
			startY = (int)((start.transform.position.z - floor.collider.bounds.min.z) / SpaceState.TileSize.y);
			endX = (int)((end.transform.position.x - floor.collider.bounds.min.x) / SpaceState.TileSize.x);
			endY = (int)((end.transform.position.z - floor.collider.bounds.min.z) / SpaceState.TileSize.y);
				
			for (rrtattemps = 5000; rrtattemps <= 81000; rrtattemps += 3000) {
					
				Debug.Log ("RRT attemps " + rrtattemps + " Memory: " + GC.GetTotalMemory (true) + " Date: " + System.DateTime.Now.ToString ());

				ResultBatch job = new ResultBatch ();
				job.gridSize = gridsize;
				job.timeSamples = timesamples;
				job.rrtAttemps = rrtattemps;

				TimeSpan average = new TimeSpan (0, 0, 0, 0, 0);
				List<Node> path = null;

				for (int it = 0; it < 155;) {
					Result single = new Result ();
						
					DateTime before = System.DateTime.Now;
					path = rrt.Compute (startX, startY, endX, endY, rrtattemps, speed, fullMap, false);
					DateTime after = System.DateTime.Now;
					TimeSpan delta = after - before;
						
					average += delta;
					single.timeSpent = delta.TotalMilliseconds;
						
					if (path.Count > 0) {
						it++;
						job.results.Add (single);
					}
					job.totalTries++;
					if (job.totalTries > 100 && job.results.Count == 0)
						it = 200;
				}
					
				job.averageTime = (double)average.TotalMilliseconds / (double)job.totalTries;
						
				root.everything.Add (job);
					
			}
				
			fullMap = null;
				
			XmlSerializer ser = new XmlSerializer (typeof(ResultsRoot), new Type[] {typeof(ResultBatch), typeof(Result)});
			ser.Serialize (stream, root);
			stream.Flush ();
			stream.Close ();
		}
			
		//}
	}
	
	void CalculateVoronoi ()
	{
		if (floor == null) {
			floor = GameObject.FindGameObjectWithTag ("Floor");
		}
		
		
		//Get all the point to calculate the voronoi
		GameObject[] gos;
		List<Vector2> pointsVoronoi = new List<Vector2> ();
		gos = GameObject.FindGameObjectsWithTag ("Voronoi").OrderBy (go => go.transform.position.x).ToArray ();
		
		foreach (GameObject g in gos) {
			pointsVoronoi.Add (new Vector2 (g.transform.position.x, g.transform.position.z));
		}
		
		//Debug.DrawLine(gos[0].transform.position,gos[1].transform.position);
		//Triangulator triangulator = new Triangulator();
		//triangulator.CreateInfluencePolygon(pointsVoronoi.ToArray());
	}
	
	public void Update ()
	{
		if (playing) {
			playTime += 1 / 100f;
			if (playTime > stepSize) {
				playTime = 0f;
				timeSlice++;
				if (timeSlice >= timeSamples) {
					timeSlice = 0;
				}
				drawer.timeSlice = timeSlice;
				UpdatePositions (timeSlice, mapper);
			}
		}
	}
	
	// Resets the AI back to it's original position
	public void ResetAI ()
	{
		GameObject[] objs = GameObject.FindGameObjectsWithTag ("AI") as GameObject[];
		foreach (GameObject ob in objs)
			ob.GetComponent<Player> ().ResetSimulation ();
		
		objs = GameObject.FindGameObjectsWithTag ("Enemy") as GameObject[];
		foreach (GameObject ob in objs) 
			ob.GetComponent<Enemy> ().ResetSimulation ();
	}
	
	// Updates everyone's position to the current timeslice
	public void UpdatePositions (int t, Mapper mapper)
	{
		for (int i = 0; i < SpaceState.Enemies.Length; i++) {
			if (SpaceState.Enemies [i] == null)
				continue;
			
			Vector3 pos = SpaceState.Enemies [i].positions [t];
			if (drawMoveUnits)
				pos.y = t;
			SpaceState.Enemies [i].transform.position = pos;
			SpaceState.Enemies [i].transform.rotation = SpaceState.Enemies [i].rotations [t];
		}
		
		/*GameObject[] objs = GameObject.FindGameObjectsWithTag ("AI") as GameObject[];
		for (int i = 0; i < objs.Length; i++) {
			if (path != null && path.Count > 0) {
				Node p = null;
				foreach (Node n in path) {
					if (n.t > t) {
						p = n;
						break;
					}
				}
				if (p != null) {
					Vector3 n2 = p.GetVector3 ();
					Vector3 n1 = p.parent.GetVector3 ();
					Vector3 pos = n1 * (1 - (float)(t - p.parent.t) / (float)(p.t - p.parent.t)) + n2 * ((float)(t - p.parent.t) / (float)(p.t - p.parent.t));
					
					pos.x *= mapper.tileSizeX;
					pos.z *= mapper.tileSizeZ;
					pos.x += floor.collider.bounds.min.x;
					pos.z += floor.collider.bounds.min.z;
					if (!drawMoveUnits) {
						pos.y = 0f;
					}
					objs [i].transform.position = pos;
				}
			
			}
		}*/
		
		foreach (KeyValuePair<Path, GameObject> each in players) {
			if (toggleStatus [each.Key]) {
				Node p = null;
				foreach (Node n in each.Key.points) {
					if (n.t > t) {
						p = n;
						break;
					}
				}
				if (p != null) {
					Vector3 n2 = p.GetVector3 ();
					Vector3 n1 = p.parent.GetVector3 ();
					Vector3 pos = n1 * (1 - (float)(t - p.parent.t) / (float)(p.t - p.parent.t)) + n2 * ((float)(t - p.parent.t) / (float)(p.t - p.parent.t));
					
					pos.x *= SpaceState.TileSize.x;
					pos.z *= SpaceState.TileSize.y;
					pos.x += floor.collider.bounds.min.x;
					pos.z += floor.collider.bounds.min.z;
					if (!drawMoveUnits) {
						pos.y = 0f;
					}
					each.Value.transform.position = pos;
				}
			}
		}
	}
	
	public void StorePositions ()
	{
		GameObject[] objs = GameObject.FindGameObjectsWithTag ("Enemy") as GameObject[];
		for (int i = 0; i < objs.Length; i++) {
			objs [i].GetComponent<Enemy> ().SetInitialPosition ();
		}
		objs = GameObject.FindGameObjectsWithTag ("AI") as GameObject[];
		for (int i = 0; i < objs.Length; i++) {
			objs [i].GetComponent<Player> ().SetInitialPosition ();
		}
	}
	
}

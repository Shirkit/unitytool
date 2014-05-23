


using UnityEngine;
using UnityEditor;
using Vectrosity;
using System.IO;
using System.Xml.Serialization;
using System.Collections;
using System.Collections.Generic;
//using Priority_Queue;
using Mischel.Collections;
using KDTreeDLL;

namespace EditorArea {
	public class PlatformerEditorWindow : EditorWindow  {
		public GameObject heatmap;
		public GameObject players;
		public GameObject models;
		public GameObject posMods;
		public GameObject nodes;
		public GameObject paths;
		public GameObject player;
		public GameObject modelObj;
		public GameObject posModObj;
		public Vector3 startingLoc;
		public Vector3 goalLoc;
		public bool showDeaths;
		public bool drawPaths;
		//public bool markMap;
		public bool playing = false;
		private movementModel mModel;
		private posMovModel pmModel;
		public static int numPlayers = 1;
		public static int numIters = 100;
		public static int depthIter = 3;
		public static int totalFrames = 0;
		public static int realFrame;
		public static int curFrame;
		public static string filename;
		public static string destCount;

		public List<movementModel> mModels;
		public List<posMovModel> pmModels;

		public int count = 0;

		public GameObject nodMarFab = Resources.Load("nodeMarker") as GameObject;
		public GameObject playerFab = Resources.Load ("player") as GameObject;
		public GameObject modelFab = Resources.Load ("modelObject") as GameObject;
		public GameObject posModFab = Resources.Load ("posMod") as GameObject;



		//public static GameObject[] hplats;
		public static HPlatMovement[] hplatmovers;
		public static bool hplatInitialized = false;
		public static bool clean;

		[MenuItem("Window/RRTMapper")]
		static void Init () {
			PlatformerEditorWindow window = (PlatformerEditorWindow)EditorWindow.GetWindow (typeof(PlatformerEditorWindow));
			window.title = "RRTMapper";
			window.ShowTab ();
			clean = false;
		}

		static void initPlat(){
			GameObject hmovplat = GameObject.Find ("HMovingPlatforms");
			//hplats = new GameObject[hmovplat.transform.childCount];
			hplatmovers = new HPlatMovement[hmovplat.transform.childCount];

			int i = 0;
			foreach(Transform child in hmovplat.transform){
				hplatmovers[i] = child.gameObject.GetComponent<HPlatMovement>();
				hplatmovers[i].initialize();
				//hplats[i] = child.gameObject;
				i++;
			}
			hplatInitialized = true;
		}

		void OnGUI () {
			if (GUILayout.Button ("Monte-Carlo Tree Search")) {
				pathsMarked = false;
				realFrame = 0;
				curFrame = 0;
				startingLoc = GameObject.Find ("startingPosition").transform.position;
				goalLoc = GameObject.Find("goalPosition").transform.position;
				multiMCTSearch(startingLoc, goalLoc, new PlayerState(), 0);
				HPlatgoToFrame(0);
			}
			if (GUILayout.Button ("Print Solution")) {
				printSolution();
			}
			numPlayers = EditorGUILayout.IntSlider ("Number of Players", numPlayers, 1, 100);
			numIters = EditorGUILayout.IntSlider ("Iterations Per Player", numIters, 1, 100);
			depthIter = EditorGUILayout.IntSlider ("Max Depth per Iteration", depthIter, 1, 1000);
			maxDistRTNodes = EditorGUILayout.FloatField ("Max Dist RRT Nodes", maxDistRTNodes);
			minDistRTNodes = EditorGUILayout.FloatField ("Min Dist RRT Nodes", minDistRTNodes);
			framesPerStep = EditorGUILayout.IntSlider ("Frames Per Step A Star", framesPerStep, 1, 10);
			maxDepthAStar = EditorGUILayout.IntSlider ("Max Depth A Star", maxDepthAStar, 100, 100000);

			
			
			
			/*playerFab = (GameObject)EditorGUILayout.ObjectField ("player prefab", playerFab, typeof(GameObject), true);
			modelFab = (GameObject)EditorGUILayout.ObjectField ("modelObject prefab", modelFab, typeof(GameObject), true);
			posModFab = (GameObject)EditorGUILayout.ObjectField ("posMod prefab", posModFab, typeof(GameObject), true);
			nodMarFab = (GameObject)EditorGUILayout.ObjectField ("node marker prefab", nodMarFab, typeof(GameObject), true);*/

			showDeaths = EditorGUILayout.Toggle ("Show Deaths", showDeaths);
			drawPaths = EditorGUILayout.Toggle ("Draw Paths", drawPaths);
			//markMap = EditorGUILayout.Toggle ("Mark Map", markMap);

			if (GUILayout.Button ("Clear")) {
				cleanUp();
			}

			curFrame = EditorGUILayout.IntSlider ("frame", curFrame, 0, totalFrames);

			/*if (GUILayout.Button ("Go To Frame")) {
				goToFrame(curFrame);
			}*/

			if (GUILayout.Button (playing ? "Stop" : "Play")) {
				playing = !playing;
			}
			if(GUILayout.Button ("Go To Start")){
				goToStart();
			}

			if (GUILayout.Button ("Export Current Paths")) {
				exportPaths();
			}
			if (GUILayout.Button ("Export Current Paths as PosSets")) {
				exportPathsAsPos();
			}

			filename = EditorGUILayout.TextField("filename: ", filename);
			destCount = EditorGUILayout.TextField("destCount: ", destCount);

			if (GUILayout.Button ("Import Path")) {
				importPath(filename, destCount);
			}
			if (GUILayout.Button ("Import PosSet")) {
				importPos(filename, destCount);
			}


			if (GUILayout.Button ("RRT - MCT")) {
				pathsMarked = false;
				realFrame = 0;
				curFrame = 0;
				RRT(true);
				HPlatgoToFrame(0);
			}
			if (GUILayout.Button ("RRT - AS")) {
				pathsMarked = false;
				realFrame = 0;
				curFrame = 0;
				RRT(false);
				HPlatgoToFrame(0);
			}
			
			if(GUILayout.Button ("AStarSearch")){
				pathsMarked = false;
				realFrame = 0;
				curFrame = 0;
				startingLoc = GameObject.Find ("startingPosition").transform.position;
				goalLoc = GameObject.Find("goalPosition").transform.position;
				AStarSearch(startingLoc, goalLoc, new PlayerState(), 0);
				HPlatgoToFrame(0);
			}

			/*if(GUILayout.Button ("Clean Up Heat Map")){
				cleanUpHMap();
			}*/

			if(GUILayout.Button ("ReInitialize Moving Platforms")){
				initPlat();
			}

			
		}

		void goToStart(){
			goToFrame(0);
			HPlatgoToFrame(0);
			goToFrame(0);
			HPlatgoToFrame(0);
			goToFrame(0);
			HPlatgoToFrame(0);
			goToFrame(0);
			curFrame = 0;
			realFrame = 0;
		}


		bool prevDrawPaths;
		bool pathsMarked;

		public void Update(){
			if(!clean){
				cleanUp();
				clean = true;
			}

			if(prevDrawPaths != drawPaths){
				DestroyImmediate(paths);
				paths = new GameObject("paths");
				paths.transform.parent = players.transform;
				if(drawPaths){
					foreach(movementModel model in mModels){
						if(model != null){
							model.drawPath(paths);
						}
					}
				}
			}
			prevDrawPaths = drawPaths;

			/*if(!pathsMarked && markMap){
				pathsMarked = true;
				if(drawPaths){
					foreach(movementModel model in mModels){
						if(model != null){
							model.markMap(hmapsqrs, hmapbl, hmaptr, hmapinc);
						}
					}
				}
			}*/

			
			if(playing){
				if(realFrame != curFrame){
					//Debug.Log (realFrame);
					//Debug.Log (curFrame);
					goToFrame(curFrame);
					realFrame = curFrame;
				}
				else if(curFrame <= totalFrames){
					curFrame++;
					realFrame = curFrame;
					foreach(movementModel model in mModels){
						if(model != null){
							if(model.updater())
							{
								//model.doAction("wait", 1);
							}
						}
					}
					foreach(posMovModel pModel in pmModels){
						if(pModel != null){
							pModel.goToFrame(curFrame);
						}
					}
					HPlatgoToFrame(curFrame);

				}
				else{
					//Debug.Log ("WTF");
				}
			}
			else{
				if(realFrame != curFrame){
					goToFrame(curFrame);
					realFrame = curFrame;
				}
			}
		}

		private void exportPaths(){
			foreach(movementModel model in mModels){
				if(model != null){
					exportPath(model);
				}
			}
		}

		private void exportPath(movementModel model){
			Debug.Log (model.gameObject.name);
			serializableModel sModel = new serializableModel(model.actions, model.durations, model.numFrames, startingLoc);
			XmlSerializer ser = new XmlSerializer (typeof(serializableModel));
			using (FileStream stream = new FileStream ("path" + model.gameObject.name.Substring(11) + ".xml", FileMode.Create)) {
				ser.Serialize (stream, sModel);
				stream.Flush ();
				stream.Close ();
			}
		}

		private void exportPathsAsPos(){
			foreach(movementModel model in mModels){
				if(model != null){
					exportPathAsPos(model);
				}
			}
		}

		private void exportPathAsPos(movementModel model){
			Debug.Log (model.gameObject.name);
			serializablePosMovModel sModel = model.toPosModel().toSerializable();
			XmlSerializer ser = new XmlSerializer (typeof(serializablePosMovModel));
			using (FileStream stream = new FileStream ("pos" + model.gameObject.name.Substring(11) + ".xml", FileMode.Create)) {
				ser.Serialize (stream, sModel);
				stream.Flush ();
				stream.Close ();
			}
		}


		private void importPos(string filename, string destCount){
			posModObj = Instantiate(posModFab) as GameObject;
			posModObj.name = "posMod" + destCount;
			posModObj.transform.parent = posMods.transform;
			player = Instantiate(playerFab) as GameObject;
			player.name = "player" + destCount;
			player.transform.parent = players.transform;
			pmModel = posModObj.GetComponent<posMovModel>() as posMovModel;
			pmModel.player = player;
			pmModels.Add (pmModel);
			pmModel.color = new Color(Random.Range (0f, 1f), Random.Range (0f, 1f), Random.Range (0f, 1f));
			var tempMaterial = new Material(player.renderer.sharedMaterial);
			tempMaterial.color = pmModel.color;
			player.renderer.sharedMaterial = tempMaterial;

			serializablePosMovModel sModel;
			XmlSerializer ser = new XmlSerializer (typeof(serializablePosMovModel));
			using (FileStream stream = new FileStream (filename, FileMode.Open)) {
				sModel = ser.Deserialize (stream) as serializablePosMovModel;
				stream.Flush ();
				stream.Close ();
			}
			pmModel.positions = sModel.positions;
			player.transform.position = sModel.startLoc;
			totalFrames = Mathf.Max(totalFrames, sModel.numFrames);
			if(drawPaths){
				pmModel.drawPath(paths);
			}
		}

		private void importPath(string filename, string destCount){
			modelObj = Instantiate(modelFab) as GameObject;
			modelObj.name = "modelObject" + destCount;
			modelObj.transform.parent = models.transform;
			player = Instantiate(playerFab) as GameObject;
			player.name = "player" + destCount;
			player.transform.parent = players.transform;
			mModel = modelObj.GetComponent<movementModel>() as movementModel;
			mModel.player = player;
			mModels.Add (mModel);
			mModel.initialize();
			mModel.color = new Color(Random.Range (0f, 1f), Random.Range (0f, 1f), Random.Range (0f, 1f));
			var tempMaterial = new Material(player.renderer.sharedMaterial);
			tempMaterial.color = mModel.color;
			player.renderer.sharedMaterial = tempMaterial;
			
			serializableModel sModel;
			XmlSerializer ser = new XmlSerializer (typeof(serializableModel));
			using (FileStream stream = new FileStream (filename, FileMode.Open)) {
				sModel = ser.Deserialize (stream) as serializableModel;
				stream.Flush ();
				stream.Close ();
			}
			mModel.actions = sModel.actions;
			mModel.durations = sModel.durations;
			player.transform.position = sModel.startLoc;
			totalFrames = Mathf.Max(totalFrames, sModel.numFrames);
			if(drawPaths){
				mModel.drawPath(paths);
			}
		}

		private static void HPlatgoToFrame(int curFrame){
			if(!hplatInitialized){
				initPlat();
			}
			foreach(HPlatMovement mov in hplatmovers){
				if(mov != null){
					mov.goToFrame(curFrame);
				}
			}
		}

		private void goToFrame(int curFrame){
			foreach(movementModel model in mModels){
				if(model != null){
					/*model.aIndex = 0;
					model.player.transform.position = startingLoc;
					if(model.startLocation != null){
						model.player.transform.position = model.startLocation;
					}
					model.state.reset ();
					if(model.startState != null){
						model.resetState();
					}
					model.runFrames(curFrame);*/
					model.goToFrame (curFrame);
				}
			}
			foreach(posMovModel pModel in pmModels){
				if(pModel != null){
					pModel.goToFrame(curFrame);
				}
			}
			HPlatgoToFrame(curFrame);
		}
		
		
		
		private void multiMCTSearch(Vector3 startLoc, Vector3 golLoc, PlayerState state, int frame){
			cleanUp();
			count = 0;

			for(int i = 0; i < numPlayers; i++){
				MCTSearch(startLoc, golLoc, state, frame);
				count++;
			}
		}

		private void cleanUp(){
			DestroyImmediate(GameObject.Find ("players"));
			nodes = new GameObject("nodes");
			players = new GameObject("players");
			models = new GameObject("models");
			paths = new GameObject("paths");
			posMods = new GameObject("posMods");
			paths.transform.parent = players.transform;
			models.transform.parent = players.transform;
			posMods.transform.parent = players.transform;
			nodes.transform.parent = players.transform;
		}

		GameObject[,] hmapsqrs;
		Vector3 hmapbl;
		Vector3 hmaptr;
		float hmapinc;
		private void cleanUpHMap(){
			hmapinc = 0.4f;
			DestroyImmediate(heatmap);
			heatmap = new GameObject("heatmap");
			hmapbl = GameObject.Find ("bottomLeft").transform.position;
			hmaptr = GameObject.Find ("topRight").transform.position;
			float width = hmaptr.x - hmapbl.x;
			float height = hmaptr.y - hmapbl.y;
			int sqrsW = Mathf.CeilToInt(width / 0.4f);
			int sqrsH = Mathf.CeilToInt(height / 0.4f);
			hmapsqrs = new GameObject [sqrsW,sqrsH];

			for(int i = 0; i < sqrsW; i++){
				for(int j = 0; j < sqrsH; j++){
					GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
					cube.transform.parent = heatmap.transform;
					cube.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
					cube.transform.position= new Vector3((hmapbl.x + 0.2f + 0.4f*i), (hmapbl.y + 0.2f + 0.4f*j), 15f);
					var tempMaterial = new Material(cube.renderer.sharedMaterial);
					tempMaterial.color = Color.white;
					cube.renderer.sharedMaterial = tempMaterial;
					hmapsqrs[i,j] = cube;
				}
			}
			Debug.Log (sqrsW * sqrsH);
		}

		private void resetState(movementModel model, PlayerState state){
			model.state.adjustmentVelocity.x = state.adjustmentVelocity.x;
			model.state.adjustmentVelocity.y = state.adjustmentVelocity.y;
			model.state.isOnGround = state.isOnGround;
			model.state.velocity.x = state.velocity.x;
			model.state.velocity.y = state.velocity.y;
			model.state.numJumps = state.numJumps;
		}


		private RTNode MCTSearch(Vector3 startLoc,Vector3 golLoc, PlayerState state, int frame){
			modelObj = Instantiate(modelFab) as GameObject;
			modelObj.name = "modelObject" + count;
			modelObj.transform.parent = models.transform;
			player = Instantiate(playerFab) as GameObject;
			player.name = "player" + count;
			player.transform.parent = players.transform;
			mModel = modelObj.GetComponent<movementModel>() as movementModel;
			mModel.player = player;
			mModels.Add (mModel);
			mModel.startState = state;
			mModel.startLocation = startLoc;
			mModel.startFrame = frame;
			mModel.hplatmovers = hplatmovers;

			int i = 0;
			bool foundAnswer = false;
			while(i < numIters && !foundAnswer){
				foundAnswer = MCTSearchIteration(startLoc, golLoc, state, frame);
				i++;
			}
			if(foundAnswer)
			{
				//Debug.Log ("Success");
				//Debug.Log("foundAnswer-" + mModel.state);
				RTNode toReturn = new RTNode();
				toReturn.position = player.transform.position;
				toReturn.state = mModel.state.clone();
				toReturn.actions = mModel.actions;
				toReturn.durations = mModel.durations;
				toReturn.frame = mModel.numFrames;
				mModel.aIndex = 0;
				player.transform.position = startLoc;
				//mModel.state.reset ();


				resetState(mModel, state);


				if(drawPaths){
					mModel.drawPath(paths);
				}
				return toReturn;
			}
			else{
				//Debug.Log ("Failure");
				if(!showDeaths){
					mModels.Remove(mModel);
					DestroyImmediate(GameObject.Find ("player" + count));
					DestroyImmediate(GameObject.Find ("modelObject" + count));
				}
				else{
					mModel.aIndex = 0;
					player.transform.position = startingLoc;
					//mModel.state.reset ();
					//mModel.state = state.clone ();
					if(drawPaths){
						mModel.drawPath(paths);
					}
				}
				return null;
			}
		}

		private bool MCTSearchIteration(Vector3 startLoc,Vector3 golLoc, PlayerState state, int frame){
			//Debug.Log ("--------------------------------------------------------");
			player.transform.position = startLoc;
			mModel.initializev2();


			mModel.numFrames = 0;
			//resetState(mModel, state);

			mModel.color = new Color(Random.Range (0f, 1f), Random.Range (0f, 1f), Random.Range (0f, 1f));

			var tempMaterial = new Material(player.renderer.sharedMaterial);
			tempMaterial.color = mModel.color;
			player.renderer.sharedMaterial = tempMaterial;

			//
			bool canJump = true;


			int count = 0;
			while(!mModel.dead && count < depthIter){
				int action;
				if(canJump){
					action = Random.Range (0, 6);
				}
				else{
					action = Random.Range (0,3);
				}
				int duration = 1;
				if(action < 3){
					duration  = Random.Range (5, 30);
				}
				mModel.durations.Add (duration);
				switch(action){
				case 0:
					mModel.actions.Add("wait");
					break;
				case 1:
					mModel.actions.Add("Left");
					break;
				case 2:
					mModel.actions.Add("Right");
					break;
				case 3:
					mModel.actions.Add("jump");
					break;
				case 4:
					mModel.actions.Add("jump left");
					break;
				case 5:
					mModel.actions.Add("jump right");
					break;
				default:
					Debug.Log ("-------------------------------------");
					mModel.actions.Add ("wait");
					break;
				}

				/*if(mModel.aIndex != 0){
					mModel.aIndex--;
				}/*
				/*mModel.aIndex = 0;
				mModel.resetState();
				player.transform.position = startLoc;*/

				
				
				//Debug.Log (player.transform.position);
				//Debug.Log (mModel.player.transform.position);
				int frames = mModel.loopUpdate();
				//Debug.Log ("LOOP UPDATE");
				//Debug.Log (player.transform.position);
				//Debug.Log (mModel.player.transform.position);

				if(mModel.state.numJumps < mModel.state.maxJumps){
					canJump = true;
				}
				else{
					canJump = false;
				}

				mModel.numFrames += frames;
				if((player.transform.position - golLoc).magnitude < 0.5){
					//Debug.Log (player.transform.position);
					//player.transform.position = startingLoc;
					totalFrames = Mathf.Max(totalFrames, mModel.numFrames);
					return true;
				}
				else{
				//	Debug.Log (golLoc);
				//	Debug.Log ((player.transform.position - golLoc).magnitude);
				}
				count++;
			}
			//player.transform.position = startingLoc;
			return false;
		}


		public bool asGoalReached;
		public PriorityQueue<RTNode, double> heap;
		public RTNode asRoot;
		public RTNode asGoalNode;

		public static int framesPerStep = 1;
		public static int maxDepthAStar = 100;

		public GameObject astar;
		public int statesExplored;

		private RTNode AStarSearch(Vector3 startLoc, Vector3 golLoc, PlayerState state, int frame){
			statesExplored = 0;
			bool drawWholeThing = true;

			cleanUp();
			if(drawWholeThing){
				astar = new GameObject("ASTAR");
			}
			modelObj = Instantiate(modelFab) as GameObject;
			modelObj.name = "modelObject" + count;
			modelObj.transform.parent = models.transform;
			player = Instantiate(playerFab) as GameObject;
			player.name = "player" + count;
			player.transform.parent = players.transform;
			mModel = modelObj.GetComponent<movementModel>() as movementModel;
			mModel.hplatmovers = hplatmovers;
			mModel.player = player;
			mModels.Add (mModel);
			count++;
			
			asGoalReached = false;
			heap = new PriorityQueue<RTNode, double>();
			asRoot = new RTNode(startLoc, 0, state);
			heap.Enqueue(asRoot, -Vector2.Distance(asRoot.position, golLoc));
			statesExplored++;
			int k = 0;
			while(!asGoalReached && heap.Count > 0 && k < maxDepthAStar){
				k++;

				RTNode cur = heap.Dequeue().Value;
				tryDoAction(cur, "Right", golLoc);
				if(asGoalReached){
					break;
				}
				tryDoAction(cur, "Left", golLoc);
				if(asGoalReached){
					break;
				}
				tryDoAction(cur, "wait", golLoc);
				if(asGoalReached){
					break;
				}
				if(cur.state.numJumps < cur.state.maxJumps){
					tryDoAction(cur, "jump", golLoc);
					if(asGoalReached){
						break;
					}
					tryDoAction(cur, "jump right", golLoc);
					if(asGoalReached){
						break;
					}
					tryDoAction(cur, "jump left", golLoc);
				}
			}
			if(asGoalReached){
				count = 0;
				mModel.startFrame = frame;
				mModel.startState = state;
				mModel.startLocation = startLoc;
				mModel.initializev2();
				mModel.color = new Color(Random.Range (0f, 1f), Random.Range (0f, 1f), Random.Range (0f, 1f));
				Debug.Log ("STATES EXPLORED = " + statesExplored);
				return reCreatePathAS();
			}
			else{
				//Debug.Log("Failed");
				Debug.Log ("STATES EXPLORED = " + statesExplored);
				return null;
			}
		}

		private RTNode reCreatePathAS(){
			loopAddAS(asGoalNode);
			totalFrames = Mathf.Max (mModel.numFrames, totalFrames);
			var tempMaterial = new Material(player.renderer.sharedMaterial);
			tempMaterial.color = mModel.color;
			player.renderer.sharedMaterial = tempMaterial;
			if(drawPaths){
				mModel.drawPath(paths);
			}

			RTNode toReturn = new RTNode(asGoalNode.position, mModel.numFrames, asGoalNode.state);
			toReturn.actions.AddRange (mModel.actions);
			toReturn.durations.AddRange (mModel.durations);
			return toReturn;
		}
			
		private void loopAddAS(RTNode node){
			if(node != asRoot){
				loopAddAS(node.parent);
				mModel.actions.AddRange(node.actions);
				mModel.durations.AddRange(node.durations);
				mModel.numFrames = node.frame;
			}
		}
		private void tryDoAction(RTNode cur, string action, Vector3 golLoc){
			bool drawWholeThing = true;

			RTNode nex = addAction(cur, action, golLoc);
			if(nex != null){
				if(drawWholeThing){
					Color clr;
					switch(action){
					case "wait":
						clr = Color.blue;
						break;
					case "Left":
						clr = Color.green;
						break;
					case "Right":
						clr = Color.magenta;
						break;
					case "jump":
						clr = Color.red;
						break;
					case "jump left":
						clr = Color.yellow;
						break;
					case "jump right":
						clr = Color.white;
						break;
					default:
						clr = Color.cyan;
						break;
					}


					VectorLine line = new VectorLine("AStar", new Vector3[] {cur.position, nex.position}, clr, null, 2.0f);
					line.Draw3D();
					line.vectorObject.transform.parent = astar.transform;
				}
				float dist = Vector2.Distance(nex.position, golLoc);
				if(dist < 0.5){
					asGoalReached = true;
					asGoalNode = nex;
					statesExplored++;
				}
				else{
					heap.Enqueue(nex, -dist);
					statesExplored++;
				}
			}
		}


		private RTNode addAction(RTNode cur, string action, Vector3 golLoc){
			mModel.startState = cur.state;
			mModel.startFrame = cur.frame;
			mModel.startLocation = cur.position;
			mModel.initializev2();

			int frame = framesPerStep;
			mModel.actions.Add (action);
			if(framesPerStep > 1){
				if(action.Equals("Right") || action.Equals ("Left")){
					mModel.durations.Add (framesPerStep);
					int j;
					for(j = 0; j < framesPerStep; j++){

						mModel.runFrames(1);
						float dist = Vector2.Distance(mModel.player.transform.position, golLoc);
						if(dist < 0.5){
							asGoalReached = true;
							mModel.durations[mModel.durations.Count-1] = j+1;
							break;
						}
					}
					if(j == framesPerStep){
						frame = j;
					}
					else{
						frame = j+1;
					}

				}
				else{
					mModel.durations.Add (1);
					mModel.actions.Add ("wait");
					mModel.durations.Add (framesPerStep-1);
					int j;
					for(j = 0; j < framesPerStep; j++){

						mModel.runFrames(1);
						float dist = Vector2.Distance(mModel.player.transform.position, golLoc);
						if(dist < 0.5){

							//Debug.Log (mModel.actions);
							//Debug.Log (mModel.durations);
							asGoalReached = true;
							if(j+1 > 1){
								mModel.durations[mModel.durations.Count-1] = j;
							}
							else{
								mModel.actions.RemoveAt(mModel.actions.Count-1);
								mModel.durations.RemoveAt(mModel.durations.Count-1);
							}
							break;
						}
					}
					if(j == framesPerStep){
						frame = j;
					}
					else{
						frame = j+1;
					}
				}
			}
			else{
				//Debug.Log ("ai");
				mModel.durations.Add (1);
				frame = mModel.loopUpdate();
				/*if(frame != 1){
					Debug.Log ("AIAIAIAIAIA");
					foreach (string act in mModel.actions){
						Debug.Log (act);
					}
					foreach(int dur in mModel.durations){
						Debug.Log (dur);
					}
				}*/

			}

			int fr = 0;
			foreach(int dur in mModel.durations){
				fr += dur;
			}
			if(fr != frame){
				Debug.Log ("SOMETHING IS WRONG" + "-" + fr + "-" + frame);
			}


			if(mModel.dead){
				asGoalReached = false;
				return null;
			}
			else{
				RTNode toReturn = new RTNode(player.transform.position, cur.frame + frame, mModel.state);
				/*
				toReturn.actions.Add (action);
				if(frame > 1){
					if(action.Equals("Right") || action.Equals ("Left")){
						toReturn.durations.Add (frame);
					}
					else{
						toReturn.durations.Add (1);
						toReturn.actions.Add ("wait");
						toReturn.durations.Add (frame-1);
					}
				}
				else{
					toReturn.durations.Add (1);
				}
				*/
				toReturn.actions.AddRange (mModel.actions);
				toReturn.durations.AddRange(mModel.durations);
				toReturn.parent = cur;
				cur.children.Add(toReturn);
				return toReturn;
			}
		}

		/*private RTNode BFSearch(Vector3 startLoc, Vector3 golLoc, PlayerState state){
			modelObj = Instantiate(modelFab) as GameObject;
			modelObj.name = "modelObject" + count;
			modelObj.transform.parent = models.transform;
			player = Instantiate(playerFab) as GameObject;
			player.name = "player" + count;
			player.transform.parent = players.transform;
			mModel = modelObj.GetComponent<movementModel>() as movementModel;
			mModel.player = player;
			mModels.Add (mModel);
			mModel.startState = state;
			mModel.startLocation = startLoc;

			//--------------------------------------------
			player.transform.position = startLoc;
			mModel.initializev2();
			mModel.color = new Color(Random.Range (0f, 1f), Random.Range (0f, 1f), Random.Range (0f, 1f));
			var tempMaterial = new Material(player.renderer.sharedMaterial);
			tempMaterial.color = mModel.color;
			player.renderer.sharedMaterial = tempMaterial;

			bool succeeded = false;

			Queue<List<string>> actionQ = new Queue<List<string>>();
			List<string> t = new List<string>();
			t.Add("wait");
			actionQ.Enqueue(t);
			while(!succeeded){
				t = actionQ.Dequeue();
				if(checkAction(t, golLoc)){
					succeeded = true;
				}
				else{
					List<string> a = new List<string>();
					a.AddRange(t);
					a.Add ("Right");
					actionQ.Enqueue(a);
					List<string> b = new List<string>();
					a.AddRange(t);
					a.Add ("Left");
					actionQ.Enqueue(b);
					List<string> c = new List<string>();
					a.AddRange(t);
					a.Add ("jump");
					actionQ.Enqueue(c);
					List<string> d = new List<string>();
					a.AddRange(t);
					a.Add ("jump right");
					actionQ.Enqueue(d);
					List<string> e = new List<string>();
					a.AddRange(t);
					a.Add ("jump left");
					actionQ.Enqueue(e);
					List<string> f = new List<string>();
					a.AddRange(t);
					a.Add ("wait");
					actionQ.Enqueue(f);
				}
			}
			//--------------------------------------------
			RTNode toReturn = new RTNode();
			toReturn.position = player.transform.position;
			toReturn.state = mModel.state.clone();
			toReturn.actions = mModel.actions;
			toReturn.durations = mModel.durations;
			toReturn.frame = mModel.numFrames;
			mModel.aIndex = 0;
			player.transform.position = startLoc;
			
			resetState(mModel, state);
			
			
			if(drawPaths){
				mModel.drawPath(paths);
			}
			return toReturn;
		}

		private bool checkAction(List<string> pActions, Vector3 golLoc){
			mModel.aIndex = 0;
			mModel.resetState();
			player.transform.position = mModel.startLocation;
			mModel.actions = pActions;
			mModel.durations = new List<int>();
			for(int i =0; i < pActions.Count; i++){
				mModel.durations.Add (1);
			}

			int frames = mModel.loopUpdate ();
			mModel.numFrames = frames;
			if((player.transform.position - golLoc).magnitude < 0.5){
				totalFrames = Mathf.Max (totalFrames, frames);
				return true;
			}
			else{
				return false;
			}
		}*/


		private void printSolution(){
			int i = 0;
			while(i < mModel.durations.Count){
				Debug.Log (mModel.actions[i] + " -- " + mModel.durations[i]);
				i++;
			}
		}

		public bool[] goalReached;
		public static int rrtIters = 10000000;

		//public List<RTNode>[] rrtTrees;
		public KDTree[] rrtTrees;

		public RTNode[] roots;
		public RTNode[] goalNodes;


		public static float maxDistRTNodes = 5;
		public static float minDistRTNodes = 1;

		private void RRT(bool useMCT){
			cleanUp();
			goalReached = new bool[numPlayers];

			//rrtTrees = new List<RTNode>[numPlayers];
			rrtTrees = new KDTree[numPlayers];
			roots = new RTNode[numPlayers];
			goalNodes = new RTNode[numPlayers];

			for(int j = 0; j < numPlayers; j++){

				goalReached[j] = false;
				startingLoc = GameObject.Find ("startingPosition").transform.position;
				goalLoc = GameObject.Find("goalPosition").transform.position;
				Vector3 bl = GameObject.Find ("bottomLeft").transform.position;
				Vector3 tr = GameObject.Find ("topRight").transform.position;
				//rrtTrees[j] = new List<RTNode>();
				rrtTrees[j] = new KDTree(2);
				roots[j] = new RTNode(startingLoc, 0, new PlayerState());
				//rrtTrees[j].Add (roots[j]);
				rrtTrees[j].insert(new double[] {roots[j].position.x, roots[j].position.y} ,roots[j]);
				int q = 0;
				for(int i = 0; i < rrtIters; i++){
					q++;
					float x = Random.Range (bl.x, tr.x);
					float y = Random.Range (bl.y, tr.y);
					if(!tryAddNode(x,y, j, useMCT)){
						i--;
					}
					if(goalReached[j]){
						Debug.Log (i);
						Debug.Log (q);
						break;
					}
				}
			}
			cleanUp();
			reCreatePath();
			//Debug.Log (rrtTrees[0].Count);
			/*for(int j = 0; j< numPlayers; j++){
				if(goalReached[j]){
					Debug.Log ("Success");
					Debug.Log ("nodes added = " + rrtTrees[j].Count);
					//cleanUp();
					reCreatePath();
				}
				else{
					Debug.Log ("Failure");
					foreach(RTNode nod in rrtTree){
						Debug.Log (nod.position);
					}
				}
			}*/
		}

		/*private void reCreatePath2(){
			loopCreate(goalNode);
		}

		private void loopCreate(RTNode node){
			if(node != root){
				loopCreate(node.parent);

				modelObj = Instantiate(modelFab) as GameObject;
				modelObj.name = "modelObject" + count;
				modelObj.transform.parent = models.transform;
				player = Instantiate(playerFab) as GameObject;
				player.name = "player" + count;
				player.transform.parent = players.transform;
				mModel = modelObj.GetComponent<movementModel>() as movementModel;
				mModel.player = player;
				
				mModel.startLocation = node.parent.position;
				mModel.startLocation.z = 10;
				mModel.startState = node.parent.state;
				mModel.initializev2();
				mModels.Add (mModel);

				mModel.actions.AddRange (node.actions);
				mModel.durations.AddRange(node.durations);
				
				count++;


				GameObject nod = Instantiate(nodMarFab, node.position, Quaternion.identity) as GameObject;
				nod.name = "node" + nodesToBeAddedCounterThing;
				nodesToBeAddedCounterThing++;
				//Debug.Log (node.frame);
				
			}
			else{
				nodesToBeAddedCounterThing = 0;
				GameObject nod = Instantiate(nodMarFab, node.position, Quaternion.identity) as GameObject;
				nod.name = "node" + nodesToBeAddedCounterThing;
				nodesToBeAddedCounterThing++;
			}

		}*/


		private void reCreatePath(){
			count = 0;
			for(int j = 0; j < numPlayers; j++){
				if(goalReached[j]){
					Debug.Log ("Attempt " + j + " successful");
					modelObj = Instantiate(modelFab) as GameObject;
					modelObj.name = "modelObject" + count;
					modelObj.transform.parent = models.transform;
					player = Instantiate(playerFab) as GameObject;
					player.name = "player" + count;
					player.transform.parent = players.transform;
					mModel = modelObj.GetComponent<movementModel>() as movementModel;
					mModel.player = player;
					mModel.hplatmovers = hplatmovers;
					mModel.startState = new PlayerState();
					mModel.startFrame = 0;
					mModel.startLocation = startingLoc;
					mModel.initializev2();
					mModels.Add (mModel);

					loopAdd(goalNodes[j], j);
					totalFrames = Mathf.Max(mModel.numFrames, totalFrames);

					mModel.color = new Color(Random.Range (0f, 1f), Random.Range (0f, 1f), Random.Range (0f, 1f));
					
					var tempMaterial = new Material(player.renderer.sharedMaterial);
					tempMaterial.color = mModel.color;
					player.renderer.sharedMaterial = tempMaterial;

					if(drawPaths){
						mModel.drawPath(paths);
					}
					count++;
				}
				else{
					Debug.Log ("Attempt " + j + " failed");
				}
			}

			//mModel.numFrames += 20;
			//Debug.Log (mModel.numFrames);
			
		}

		private int nodesToBeAddedCounterThing;

		private void loopAdd(RTNode node, int j){
			if(node != roots[j]){
				loopAdd(node.parent, j);
				GameObject nod = Instantiate(nodMarFab, node.position, Quaternion.identity) as GameObject;
				nod.transform.parent = nodes.transform;
				nod.name = "node" + nodesToBeAddedCounterThing;
				nodesToBeAddedCounterThing++;
				mModel.actions.AddRange(node.actions);
				mModel.durations.AddRange(node.durations);
				mModel.numFrames = node.frame;
				//Debug.Log (node.frame);

			}
			else{
				nodesToBeAddedCounterThing = 0;
				//mModel.actions.AddRange(node.actions);
				//mModel.durations.AddRange(node.durations);
				//mModel.numFrames = node.frame;
				GameObject nod = Instantiate(nodMarFab, node.position, Quaternion.identity) as GameObject;
				nod.transform.parent = nodes.transform;
				nod.name = "node" + nodesToBeAddedCounterThing;
				nodesToBeAddedCounterThing++;
				//Debug.Log (node.frame);
			}
		}

		private bool tryAddNode(float x, float y, int j, bool useMCT){
			RTNode closest = findClosest(x,y, j);
			if(closest == null){
				//Debug.Log ("Too far away");
				return false;
			}
			else{
				//Debug.Log (closest.state);
				RTNode final;
				if(useMCT){
					final = MCTSearch(new Vector3(closest.position.x, closest.position.y, 10), new Vector3(x, y, 10), closest.state, closest.frame);
				}
				else{
					final = AStarSearch(new Vector3(closest.position.x, closest.position.y, 10), new Vector3(x, y, 10), closest.state, closest.frame);
				}
				if(final != null){
					//Debug.Log ("Node Added");
					final.parent = closest;
					closest.children.Add (final);
					final.frame = closest.frame + final.frame;
					//rrtTrees[j].Add (final);
					rrtTrees[j].insert(new double[] {final.position.x, final.position.y}, final);
					if((new Vector3(final.position.x, final.position.y, 10) - goalLoc).magnitude < 0.5){
						goalReached[j] = true;
						goalNodes[j] = final;
					}
					else{
						goalReached[j] = tryAddGoalNode(final, j, useMCT);
					}




					return true;
				}
				else{
					//Debug.Log ("MCTSearch unsuccesful");
					return true;
				}
			}
		}

		private bool tryAddGoalNode(RTNode node, int j, bool useMCT){
			if(Vector2.Distance (node.position, new Vector2(goalLoc.x, goalLoc.y)) > maxDistRTNodes){
				return false;
			}
			else{
				RTNode final;
				if(useMCT){
					final = MCTSearch(new Vector3(node.position.x, node.position.y, 10), goalLoc, node.state, node.frame);
				}
				else{
					final = AStarSearch(new Vector3(node.position.x, node.position.y, 10), goalLoc, node.state, node.frame);
				}
				if(final != null){
					final.parent = node;
					node.children.Add (final);
					final.frame = node.frame + final.frame;
					//rrtTrees[j].Add (final);
					rrtTrees[j].insert(new double[] {final.position.x, final.position.y}, final);
					goalNodes[j] = final;
					return true;
				}
				else{
					return false;
				}
			}
		}


		private RTNode findClosest(float x,float y, int j){
			/*
			float minDist = maxDistRTNodes;
			float dist;
			RTNode curNode = null;
			foreach(RTNode node in rrtTrees[j]){
				dist = Vector2.Distance(node.position, new Vector2(x,y));
				if(dist < minDistRTNodes){
					return null;
				}
				if(dist < minDist){
					minDist = dist;
					curNode = node;
				}
			}
			*/
			RTNode curNode = (RTNode)rrtTrees[j].nearest(new double[] {x, y});
			float dist = Vector2.Distance(curNode.position, new Vector2(x,y));
			if(dist < minDistRTNodes || dist > maxDistRTNodes){
				return null;
			}
			else{
				return curNode;
			}
		}




	}
}

[XmlRoot("Path")]
public class serializableModel{
	[XmlArray("Actions")]
	[XmlArrayItem("action")]
	public List<string> actions;
	[XmlArray("Durations")]
	[XmlArrayItem("durations")]
	public List<int> durations;
	
	public int numFrames;
	public Vector3 startLoc;


	public serializableModel(){
	}

	public serializableModel(List<string> pActions, List<int> pDurations, int pFrames, Vector3 pSLoc){
		actions = pActions;
		durations = pDurations;
		numFrames = pFrames;
		startLoc = pSLoc;
	}
}
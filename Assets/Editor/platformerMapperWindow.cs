

using UnityEngine;
using UnityEditor;
using Vectrosity;
using System.IO;
using System.Xml.Serialization;
using System.Collections;
using System.Collections.Generic;
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
		public bool playing = false;
		private movementModel mModel;
		public static int numPlayers = 1;
		public static int numIters = 100;
		public static int depthIter = 3;
		public static int totalFrames = 0;
		public static int realFrame;
		public static int curFrame;
		public static string filename;
		public static string destCount;

		public static bool ignoreFrameLimit;

		public static bool debugMode = true;
		public static bool drawWholeThing;
		public List<movementModel> mModels;

		public int count = 0;

		public GameObject nodMarFab = Resources.Load("nodeMarker") as GameObject;
		public GameObject playerFab = Resources.Load ("player") as GameObject;
		public GameObject modelFab = Resources.Load ("modelObject") as GameObject;

		private static Vector2 scrollPos = new Vector2 ();

		public static bool playingKeyboard;

		//public static GameObject[] hplats;
		public static HPlatMovement[] hplatmovers;
		public static VPlatMovement[] vplatmovers;
		public static bool platsInitialized = false;
		public static bool clean;

		public static bool batchComputation;
		public static bool batchComputationRRT;
		public static string batchFilename;

		[MenuItem("Window/RRTMapper")]
		static void Init () {
			PlatformerEditorWindow window = (PlatformerEditorWindow)EditorWindow.GetWindow (typeof(PlatformerEditorWindow));
			window.title = "RRTMapper";
			window.ShowTab ();
			clean = false;
		}

		static void initPlat(){
			GameObject hmovplat = GameObject.Find ("HMovingPlatforms");
			hplatmovers = new HPlatMovement[hmovplat.transform.childCount];

			int i = 0;
			foreach(Transform child in hmovplat.transform){
				hplatmovers[i] = child.gameObject.GetComponent<HPlatMovement>();
				hplatmovers[i].initialize();
				i++;
			}

			GameObject vmovplat = GameObject.Find ("VMovingPlatforms");
			vplatmovers = new VPlatMovement[vmovplat.transform.childCount];
			
			i = 0;
			foreach(Transform child in vmovplat.transform){
				vplatmovers[i] = child.gameObject.GetComponent<VPlatMovement>();
				vplatmovers[i].initialize();
				i++;
			}



			platsInitialized = true;
		}

		void OnGUI () {
			scrollPos = EditorGUILayout.BeginScrollView (scrollPos);




			if (GUILayout.Button ("Monte-Carlo Tree Search")) {
				if(!platsInitialized){
					initPlat();
				}
				pathsMarked = false;
				realFrame = 0;
				curFrame = 0;
				startingLoc = GameObject.Find ("startingPosition").transform.position;
				goalLoc = GameObject.Find("goalPosition").transform.position;
				multiMCTSearch(startingLoc, goalLoc, new PlayerState(), 0);
				PlatsGoToFrame(0);
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



			showDeaths = EditorGUILayout.Toggle ("Show Deaths", showDeaths);
			drawPaths = EditorGUILayout.Toggle ("Draw Paths", drawPaths);
			debugMode = EditorGUILayout.Toggle ("Debug Mode", debugMode);


			if (GUILayout.Button ("Clear")) {
				cleanUp();
				cleanUpRRTDebug();
			}

			curFrame = EditorGUILayout.IntSlider ("frame", curFrame, 0, totalFrames);


			if (GUILayout.Button (playing ? "Stop" : "Play")) {
				playing = !playing;
			}
			ignoreFrameLimit = EditorGUILayout.Toggle ("Ignore Frame Limit", ignoreFrameLimit);

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

			rrtIters = EditorGUILayout.IntField("RRT Nodes: ", rrtIters);
			if (GUILayout.Button ("RRT - MCT")) {
				if(!platsInitialized){
					initPlat();
				}
				pathsMarked = false;
				realFrame = 0;
				curFrame = 0;
				RRT(0);
				PlatsGoToFrame(0);
			}
			if (GUILayout.Button ("RRT - AS")) {
				if(!platsInitialized){
					initPlat();
				}
				pathsMarked = false;
				realFrame = 0;
				curFrame = 0;
				RRT(1);
				PlatsGoToFrame(0);
			}
			if (GUILayout.Button ("RRT - UCT")) {
				if(!platsInitialized){
					initPlat();
				}
				pathsMarked = false;
				realFrame = 0;
				curFrame = 0;
				RRT(2);
				PlatsGoToFrame(0);
			}
			
			if(GUILayout.Button ("AStarSearch")){
				if(debugMode){
					drawWholeThing = true;
				}
				if(!platsInitialized){
					initPlat();
				}
				pathsMarked = false;
				realFrame = 0;
				curFrame = 0;
				startingLoc = GameObject.Find ("startingPosition").transform.position;
				goalLoc = GameObject.Find("goalPosition").transform.position;
				AStarSearch(startingLoc, goalLoc, new PlayerState(), 0);
				PlatsGoToFrame(0);
				drawWholeThing = false;
			}

			if(GUILayout.Button ("UCT Search")){
				if(debugMode){
					drawWholeThing = true;
				}
				if(!platsInitialized){
					initPlat();
				}
				pathsMarked = false;
				realFrame = 0;
				curFrame = 0;
				startingLoc = GameObject.Find ("startingPosition").transform.position;
				goalLoc = GameObject.Find("goalPosition").transform.position;
				UCTSearch(startingLoc, goalLoc, new PlayerState(), 0);
				PlatsGoToFrame(0);
				drawWholeThing = false;
			}

			if(GUILayout.Button ("ReInitialize Moving Platforms")){
				initPlat();
			}


			EditorGUILayout.LabelField ("");
			EditorGUILayout.LabelField ("");
			EditorGUILayout.LabelField ("Batch Computations");
			batchFilename = EditorGUILayout.TextField ("batch Filename", batchFilename);

			EditorGUILayout.LabelField ("A Star Batch");
			minNumFramesAS = EditorGUILayout.IntField ("min Num Frames", minNumFramesAS);
			maxNumFramesAS = EditorGUILayout.IntField ("max Num Frames", maxNumFramesAS);
			incrementFramesAS = EditorGUILayout.IntField ("increment Frames", incrementFramesAS);
			minDepthAS = EditorGUILayout.IntField ("min Depth", minDepthAS);
			maxDepthAS = EditorGUILayout.IntField ("max Depth", maxDepthAS);
			incrementDepthAS = EditorGUILayout.IntField ("increment Depth", incrementDepthAS);
			iterationsPerDataSetAS = EditorGUILayout.IntField ("Iteration Per DataSet", iterationsPerDataSetAS);
			if(GUILayout.Button ("A Star Batch")){
				if(!platsInitialized){
					initPlat();
				}
				batchCompute(1);
			}

			EditorGUILayout.LabelField ("MCT Batch");

			iterationsPerDataSetMCT = EditorGUILayout.IntField("iterations per DataSet", iterationsPerDataSetMCT);
			minIterations = EditorGUILayout.IntField("min iterations", minIterations);
			maxIterations = EditorGUILayout.IntField("max iterations", maxIterations);
			incrementIteration = EditorGUILayout.IntField("increment Iteration", incrementIteration);
			minDepth = EditorGUILayout.IntField("min Depth", minDepth);
			maxDepth = EditorGUILayout.IntField("max Depth", maxDepth);
			incrementDepth = EditorGUILayout.IntField("incrementDepth", incrementDepth);
			if(GUILayout.Button ("MCT Batch")){
				if(!platsInitialized){
					initPlat();
				}
				batchCompute(2);
			}

			EditorGUILayout.LabelField ("RRT Batch");

			iterationsPerDataSet = EditorGUILayout.IntField("iterations per DataSet", iterationsPerDataSet);

			minMinDist = EditorGUILayout.FloatField("min Min Dist", minMinDist);
			maxMinDist = EditorGUILayout.FloatField("max min Dist", maxMinDist);
			incMinDist = EditorGUILayout.FloatField("inc Min Dist", incMinDist);
			
			minMaxDist = EditorGUILayout.FloatField("min Max Dist", minMaxDist);
			maxMaxDist = EditorGUILayout.FloatField("max Max Dist", maxMaxDist);
			incMaxDist = EditorGUILayout.FloatField("inc Max Dist", incMaxDist);
			
			minNodes = EditorGUILayout.IntField("min Nodes", minNodes);
			maxNodes = EditorGUILayout.IntField("max Nodes", maxNodes);
			incNodes = EditorGUILayout.IntField("inc Nodes", incNodes);
			
			MCTIter = EditorGUILayout.IntField("MCT Iter", MCTIter);
			MCTDepth = EditorGUILayout.IntField("MCT Depth", MCTDepth);
			
			ASFrames = EditorGUILayout.IntField("A Star FPS", ASFrames);
			ASDepth = EditorGUILayout.IntField("A Star Depth", ASDepth);
			if(GUILayout.Button ("RRT Batch")){
				if(!platsInitialized){
					initPlat();
				}
				batchCompute(3);
			}
			EditorGUILayout.EndScrollView ();

		}

		void goToStart(){
			goToFrame(0);
			PlatsGoToFrame(0);
			goToFrame(0);
			PlatsGoToFrame(0);
			goToFrame(0);
			PlatsGoToFrame(0);
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


			
			if(playing){
				if(realFrame != curFrame){

					goToFrame(curFrame);
					realFrame = curFrame;
				}
				else if(curFrame <= totalFrames || ignoreFrameLimit){
					curFrame++;
					realFrame = curFrame;
					bool isOne = false;
					foreach(movementModel model in mModels){
						if(model != null){
							isOne = true;
							if(model.updater())
							{
								//model.doAction("wait", 1);
							}
						}
					}
					if(!isOne){
						goToFrame(curFrame);
					}

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

		private static void PlatsGoToFrame(int curFrame){
			if(!platsInitialized){
				initPlat();
			}
			foreach(HPlatMovement mov in hplatmovers){
				if(mov != null){
					mov.goToFrame(curFrame);
				}
			}
			foreach(VPlatMovement mov in vplatmovers){
				if(mov != null){
					mov.goToFrame(curFrame);
				}
			}
		}

		private void goToFrame(int curFrame){
			foreach(movementModel model in mModels){
				if(model != null){
					model.goToFrame (curFrame);
				}
			}

			PlatsGoToFrame(curFrame);
		}		
				
		private void multiMCTSearch(Vector3 startLoc, Vector3 golLoc, PlayerState state, int frame){
			cleanUp();
			count = 0;

			for(int i = 0; i < numPlayers; i++){
				MCTSearch(startLoc, golLoc, state, frame);
				count++;
			}
			if(showDeaths){
				totalFrames = totalFrames + 1000;
			}
		}

		private void cleanUp(){
			DestroyImmediate(astar);
			DestroyImmediate(uct);
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
		private void cleanUpRRTDebug(){
			DestroyImmediate(RRTDebug);

		}



		private RTNode MCTSearch(Vector3 startLoc,Vector3 golLoc, PlayerState state, int frame){
			GameObject modelObj2 = Instantiate(modelFab) as GameObject;
			modelObj2.name = "testmodel";
			GameObject player2 = Instantiate(playerFab) as GameObject;
			player2.name = "testplayer";
			movementModel mModel2 = modelObj2.GetComponent<movementModel>() as movementModel;
			mModel2.player = player2;
			mModel2.startState = state;
			mModel2.startLocation = startLoc;
			mModel2.startFrame = frame;
			mModel2.hplatmovers = hplatmovers;
			mModel2.vplatmovers = vplatmovers;

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
			mModel.vplatmovers = vplatmovers;

			int i = 0;
			bool foundAnswer = false;
			while(i < numIters && !foundAnswer){
				foundAnswer = MCTSearchIteration(startLoc, golLoc, state, frame);
				i++;
			}
			if(batchComputation){
				using (System.IO.StreamWriter file = new System.IO.StreamWriter(batchFilename, true))
				{
					file.WriteLine(("Iterations Used:" + i));
					
				}
				if(foundAnswer){
					return new RTNode();
				}
				else{
					return null;
				}
			}


			if(foundAnswer){
				mModel2.initializev2();
				mModel2.actions.AddRange(mModel.actions);
				mModel2.durations.AddRange(mModel.durations);
				mModel2.loopUpdate();
				if((player2.transform.position - golLoc).magnitude > 0.5){
					foundAnswer = false;
				}
				else{
					foundAnswer = true;
				}

			}
			DestroyImmediate(modelObj2);
			DestroyImmediate(player2);
			if(foundAnswer)
			{


				RTNode toReturn = new RTNode();
				toReturn.position = player.transform.position;
				toReturn.state = mModel.state.clone();
				toReturn.actions = mModel.actions;
				toReturn.durations = mModel.durations;
				toReturn.frame = mModel.numFrames;
				mModel.aIndex = 0;
				player.transform.position = startLoc;



				mModel.resetState();

				if(drawPaths){
					mModel.drawPath(paths);
				}
				return toReturn;
			}
			else{

				if(!showDeaths){
					mModels.Remove(mModel);
					DestroyImmediate(GameObject.Find ("player" + count));
					DestroyImmediate(GameObject.Find ("modelObject" + count));
				}
				else{
					mModel.aIndex = 0;
					player.transform.position = startingLoc;

					if(drawPaths){
						mModel.drawPath(paths);
					}
				}
				return null;
			}
		}

		private bool MCTSearchIteration(Vector3 startLoc,Vector3 golLoc, PlayerState state, int frame){

			mModel.initializev2();
			mModel.numFrames = 0;
			mModel.color = new Color(Random.Range (0f, 1f), Random.Range (0f, 1f), Random.Range (0f, 1f));
			var tempMaterial = new Material(player.renderer.sharedMaterial);
			tempMaterial.color = mModel.color;
			player.renderer.sharedMaterial = tempMaterial;


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

				int frames = mModel.loopUpdate();


				if(mModel.state.numJumps < mModel.state.maxJumps){
					canJump = true;
				}
				else{
					canJump = false;
				}

				mModel.numFrames += frames;
				if((player.transform.position - golLoc).magnitude < 0.5){
					totalFrames = Mathf.Max(totalFrames, mModel.numFrames);
					return true;
				}
				count++;
			}
			return false;
		}


		public bool asGoalReached;
		public PriorityQueue<RTNode, double> heap;
		public RTNode asRoot;
		public RTNode asGoalNode;

		public static int framesPerStep = 10;
		public static int maxDepthAStar = 100;

		public GameObject astar;
		public int statesExplored;

		private RTNode AStarSearch(Vector3 startLoc, Vector3 golLoc, PlayerState state, int frame){
			statesExplored = 0;
			cleanUp();

			if(drawWholeThing){
				astar = new GameObject("ASTAR");
			}
			GameObject modelObj2 = Instantiate(modelFab) as GameObject;
			modelObj2.name = "testmodel";
			GameObject player2 = Instantiate(playerFab) as GameObject;
			player2.name = "testplayer";
			movementModel mModel2 = modelObj2.GetComponent<movementModel>() as movementModel;
			mModel2.player = player2;
			mModel2.startState = state;
			mModel2.startLocation = startLoc;
			mModel2.startFrame = frame;
			mModel2.hplatmovers = hplatmovers;
			mModel2.vplatmovers = vplatmovers;






			modelObj = Instantiate(modelFab) as GameObject;
			modelObj.name = "modelObject" + count;
			modelObj.transform.parent = models.transform;
			player = Instantiate(playerFab) as GameObject;
			player.name = "player" + count;
			player.transform.parent = players.transform;
			mModel = modelObj.GetComponent<movementModel>() as movementModel;
			mModel.hplatmovers = hplatmovers;
			mModel.vplatmovers = vplatmovers;
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
			if(batchComputation){
				using (System.IO.StreamWriter file = new System.IO.StreamWriter(batchFilename, true))
				{
					file.WriteLine(("States Explored:" + statesExplored));

				}
				if(asGoalReached){
					return new RTNode();
				}
				else{
					return null;
				}
			}
			if(asGoalReached){
				count = 0;
				mModel.startFrame = frame;
				mModel.startState = state;
				mModel.startLocation = startLoc;
				mModel.initializev2();
				mModel.color = new Color(Random.Range (0f, 1f), Random.Range (0f, 1f), Random.Range (0f, 1f));
				if(drawWholeThing){
					Debug.Log ("STATES EXPLORED = " + statesExplored);
				}

				RTNode toReturn = reCreatePathAS();

				mModel2.initializev2();
				mModel2.actions.AddRange(toReturn.actions);
				mModel2.durations.AddRange(toReturn.durations);
				mModel2.loopUpdate();
				if((player2.transform.position - golLoc).magnitude > 0.5){
					DestroyImmediate(modelObj2);
					DestroyImmediate(player2);
					return null;
				}
				else{
					DestroyImmediate(modelObj2);
					DestroyImmediate(player2);
					return toReturn;
				}
			}
			else{
				DestroyImmediate(modelObj2);
				DestroyImmediate(player2);
				if(drawWholeThing){
					Debug.Log ("STATES EXPLORED = " + statesExplored);
				}
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
				mModel.durations.Add (1);
				frame = mModel.loopUpdate();


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

				toReturn.actions.AddRange (mModel.actions);
				toReturn.durations.AddRange(mModel.durations);
				toReturn.parent = cur;
				cur.children.Add(toReturn);
				return toReturn;
			}
		}



		private void printSolution(){
			int i = 0;
			while(i < mModel.durations.Count){
				Debug.Log (mModel.actions[i] + " -- " + mModel.durations[i]);
				i++;
			}
		}

		public bool[] goalReached;
		public static int rrtIters = 100;

		public KDTree[] rrtTrees;

		public RTNode[] roots;
		public RTNode[] goalNodes;


		public static float maxDistRTNodes = 10;
		public static float minDistRTNodes = 1;

		public static GameObject RRTDebug;

		private bool RRT(int useMCT){
			cleanUp();
			if(debugMode){

				//TODO: Put it in a clean place. The RRT Gameobject is never 
				//clean up before creating a new one.
				DestroyImmediate(GameObject.Find ("RRT"));
				RRTDebug = new GameObject("RRT");

			}

			goalReached = new bool[numPlayers];

			rrtTrees = new KDTree[numPlayers];
			roots = new RTNode[numPlayers];
			goalNodes = new RTNode[numPlayers];
			int i = 0;
			int q = 0;
			for(int j = 0; j < numPlayers; j++){

				goalReached[j] = false;
				startingLoc = GameObject.Find ("startingPosition").transform.position;
				goalLoc = GameObject.Find("goalPosition").transform.position;
				Vector3 bl = GameObject.Find ("bottomLeft").transform.position;
				Vector3 tr = GameObject.Find ("topRight").transform.position;
				rrtTrees[j] = new KDTree(2);
				roots[j] = new RTNode(startingLoc, 0, new PlayerState());
				rrtTrees[j].insert(new double[] {roots[j].position.x, roots[j].position.y} ,roots[j]);
				q = 0;

				
				float xMin = startingLoc.x - maxDistRTNodes;
				float xMax = startingLoc.x + maxDistRTNodes;
				float yMin = startingLoc.y - maxDistRTNodes;
				float yMax = startingLoc.y + maxDistRTNodes;
				
				//Check if the bounds of the level are reached
				if (xMin < bl.x)
					xMin = bl.x; 
				if (xMax > tr.x)
					xMax = tr.x; 
				if (yMin < bl.y)
					yMin = bl.y; 
				if (yMax < tr.y)
					yMax = tr.y; 
				


				for(i = 0; i < rrtIters; i++){
					q++;

					//TODO sample from limits reachable 
					//Use 4 heaps. 
					float x = Random.Range (xMin, xMax);
					float y = Random.Range (yMin, yMax);

					//TODO: 
					//Add a control for that one
					if(UnityEngine.Random.Range(0,100)>70)
					{
						RaycastHit2D returnCast = Physics2D.Raycast(new Vector3(x,y),- Vector3.up,8f);

						if(returnCast.collider != null && returnCast.collider.tag == "Floor")
						{
							//VectorLine line = new VectorLine("linecast", new Vector3[] {new Vector3(x,y), 
							//	returnCast.point}, Color.blue, null, 1.0f);
							//line.Draw3D();
							//line.vectorObject.transform.parent = RRTDebug.transform;


							//Draw lines

							//Debug.Log(returnCast.collider.name); 
							y = returnCast.point.y+0.5f; 
						}

					}

					//Adding blue sphere if debugging
					//Placing it where we tried to place a node
					if(debugMode)
					{

						//Added the sphere for better display
						GameObject g = GameObject.Find("RRT");
						GameObject o = GameObject.CreatePrimitive(PrimitiveType.Sphere);
						o.transform.parent = g.transform; 
						o.name = "node"+i;
						o.transform.position = new Vector3(x,y); 
						o.transform.localScale = new Vector3(0.33f,0.33f,0.33f);
						//Add interpolation between colours to know when it was added. 
					}

					if(!tryAddNode(x,y, j, useMCT))
					{
						i--;
					}
					else
					{
						//The node was added. 
						//Updating the random bounds

						if (x - maxDistRTNodes < xMin)
							xMin = x - maxDistRTNodes; 							
						if (x + maxDistRTNodes > xMax)
							xMax = x + maxDistRTNodes; 							
						if (y - maxDistRTNodes < yMin)
							yMin = y - maxDistRTNodes; 
						if (y + maxDistRTNodes > yMax)
							yMax = y + maxDistRTNodes; 

						//Check if the bounds of the level are reached
						if (xMin < bl.x)
							xMin = bl.x; 
						if (xMax > tr.x)
							xMax = tr.x; 
						if (yMin < bl.y)
							yMin = bl.y; 
						if (yMax < tr.y)
							yMax = tr.y; 
							
					}

					
					if(q > rrtIters*10)
					{
						break;
					}
					
					if(goalReached[j])
					{
						break;
					}
				}
			}
			if(batchComputationRRT){
				using (System.IO.StreamWriter file = new System.IO.StreamWriter(batchFilename, true))
				{
					file.WriteLine(i + "," + q);
				}
				if(goalReached[0]){
					return true;
				}
				else{
					return false;
				}
			}
			cleanUp();
			reCreatePath();
			if(goalReached[0]){
				return true;
			}
			else{
				return false;
			}

		}



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
					mModel.vplatmovers = vplatmovers;
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

			}
			else{
				nodesToBeAddedCounterThing = 0;

				GameObject nod = Instantiate(nodMarFab, node.position, Quaternion.identity) as GameObject;
				nod.transform.parent = nodes.transform;
				nod.name = "node" + nodesToBeAddedCounterThing;
				nodesToBeAddedCounterThing++;
			}
		}

		private bool tryAddNode(float x, float y, int j, int useMCT){
			RTNode closest = findClosest(x,y, j);
			if(closest == null){

				return false;
			}
			else{

				RTNode final;
				if(useMCT == 0)
				{
					final = MCTSearch(new Vector3(closest.position.x, closest.position.y, 10), new Vector3(x, y, 10), closest.state, closest.frame);
				}
				else if (useMCT == 1)
				{
					final = AStarSearch(new Vector3(closest.position.x, closest.position.y, 10), new Vector3(x, y, 10), closest.state, closest.frame);
				}
				else{
					final = UCTSearch(new Vector3(closest.position.x, closest.position.y, 10), new Vector3(x, y, 10), closest.state, closest.frame);
				}
				if(final != null)
				{
					if(debugMode)
					{
						VectorLine line = new VectorLine("RRT", new Vector3[] {closest.position, final.position}, Color.red, null, 2.0f);
						line.Draw3D();
						line.vectorObject.transform.parent = RRTDebug.transform;
						
						//Added the sphere for better display
						GameObject g = GameObject.Find("RRT");
						GameObject o = GameObject.CreatePrimitive(PrimitiveType.Sphere);
						o.transform.parent = g.transform; 
						o.name = "node";
						o.transform.position = closest.position; 
						o.transform.localScale = new Vector3(0.33f,0.33f,0.33f);

						//end nodes 
						o = GameObject.CreatePrimitive(PrimitiveType.Sphere);
						o.transform.parent = g.transform; 
						o.name = "node";
						o.transform.position = final	.position; 
						o.transform.localScale = new Vector3(0.33f,0.33f,0.33f);
					}
					
					final.parent = closest;
					closest.children.Add (final);
					final.frame = closest.frame + final.frame;
					rrtTrees[j].insert(new double[] {final.position.x, final.position.y}, final);
					
					if((new Vector3(final.position.x, final.position.y, 10) - goalLoc).magnitude < 0.5)
					{
						goalReached[j] = true;
						goalNodes[j] = final;
					}
					else
					{
						goalReached[j] = tryAddGoalNode(final, j, useMCT);
					}

					return true;
				}
				else
				{
					return true;
				}
			}
		}

		private bool tryAddGoalNode(RTNode node, int j, int useMCT){
			if(Vector2.Distance (node.position, new Vector2(goalLoc.x, goalLoc.y)) > maxDistRTNodes){
				return false;
			}
			else{
				RTNode final;
				if(useMCT == 0){
					final = MCTSearch(new Vector3(node.position.x, node.position.y, 10), goalLoc, node.state, node.frame);
				}
				else if (useMCT == 1){
					final = AStarSearch(new Vector3(node.position.x, node.position.y, 10), goalLoc, node.state, node.frame);
				}
				else{
					final = UCTSearch(new Vector3(node.position.x, node.position.y, 10), goalLoc, node.state, node.frame);
				}
				if(final != null){
					if(debugMode){
						VectorLine line = new VectorLine("RRT", new Vector3[] {node.position, final.position}, Color.red, null, 2.0f);
						line.Draw3D();
						line.vectorObject.transform.parent = RRTDebug.transform;
					}
					final.parent = node;
					node.children.Add (final);
					final.frame = node.frame + final.frame;
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

			RTNode curNode = (RTNode)rrtTrees[j].nearest(new double[] {x, y});
			float dist = Vector2.Distance(curNode.position, new Vector2(x,y));
			if(dist < minDistRTNodes || dist > maxDistRTNodes){
				return null;
			}
			else{
				return curNode;
			}
		}

		//Batch Computations Code

		//ASTAR
		public static int iterationsPerDataSetAS;
		public static int minNumFramesAS;
		public static int maxNumFramesAS;
		public static int incrementFramesAS;
		public static int minDepthAS;
		public static int maxDepthAS;
		public static int incrementDepthAS;

		//MCT
		public static int iterationsPerDataSetMCT;
		public static int minIterations;
		public static int maxIterations;
		public static int incrementIteration;
		public static int minDepth;
		public static int maxDepth;
		public static int incrementDepth;


		//RRT
		public static int iterationsPerDataSet;
		public static float minMinDist;
		public static float maxMinDist;
		public static float incMinDist;
		
		public static float minMaxDist;
		public static float maxMaxDist;
		public static float incMaxDist;
		
		public static int minNodes;
		public static int maxNodes;
		public static int incNodes;
		
		public static int MCTIter;
		public static int MCTDepth;
		
		public static int ASFrames;
		public static int ASDepth;

		void batchCompute(int number){
			initPlat();
			numPlayers = 1;
			pathsMarked = false;
			startingLoc = GameObject.Find ("startingPosition").transform.position;
			goalLoc = GameObject.Find("goalPosition").transform.position;
			showDeaths = false;
			drawPaths = false;
			
			if(number == 1){
				batchComputation = true;

				//A Star Batch Computation
				using (System.IO.StreamWriter file = new System.IO.StreamWriter(batchFilename))
				{
					file.WriteLine("A Star Batch Computation Results");
				}
				for(framesPerStep = minNumFramesAS; framesPerStep <= maxNumFramesAS; framesPerStep += incrementFramesAS){
					for(maxDepthAStar = minDepthAS; maxDepthAStar <= maxDepthAS; maxDepthAStar += incrementDepthAS){
						for(int i = 0; i < iterationsPerDataSetAS; i++){
							realFrame = 0;
							curFrame = 0;
							PlatsGoToFrame(0);
							string toWrite = framesPerStep + "," + maxDepthAStar + ",";
							System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
							stopwatch.Start();
							RTNode tmp = AStarSearch(startingLoc, goalLoc, new PlayerState(), 0);
							stopwatch.Stop();
							if(tmp == null){
								toWrite += "false,";
							}	
							else{
								toWrite += "true,";
							}
							toWrite += stopwatch.Elapsed;
							
							
							using (System.IO.StreamWriter file = new System.IO.StreamWriter(batchFilename, true))
							{
								file.WriteLine(toWrite);
							}
						}
					}
				}
				
			}
			else if (number == 2){
				batchComputation = true;

				//Monte Carlo Batch Computation
				using (System.IO.StreamWriter file = new System.IO.StreamWriter(batchFilename))
				{
					file.WriteLine("Monte Carlo Batch Computation Results");
				}
				
				
				for(numIters = minIterations; numIters <= maxIterations; numIters += incrementIteration){
					for(depthIter = minDepth; depthIter <= maxDepth; depthIter += incrementDepth){
						for(int i = 0; i < iterationsPerDataSetMCT; i++){
							realFrame = 0;
							curFrame = 0;
							PlatsGoToFrame(0);
							string toWrite = numIters + "," + depthIter + ",";
							System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
							cleanUp();
							stopwatch.Start();
							RTNode tmp = MCTSearch(startingLoc, goalLoc, new PlayerState(), 0);
							stopwatch.Stop();
							if(tmp == null){
								toWrite += "false,";
							}	
							else{
								toWrite += "true,";
							}
							toWrite += stopwatch.Elapsed;
							
							
							using (System.IO.StreamWriter file = new System.IO.StreamWriter(batchFilename, true))
							{
								file.WriteLine(toWrite);
							}
						}
					}
				}
			}
			else if (number == 3){
				batchComputationRRT = true;
				
				framesPerStep = ASFrames;
				maxDepthAStar = ASDepth;
				
				numIters = MCTIter;
				depthIter = MCTDepth;
				
				for(minDistRTNodes = minMinDist; minDistRTNodes <= maxMinDist; minDistRTNodes += incMinDist){
					for(maxDistRTNodes = minMaxDist; maxDistRTNodes <= maxMaxDist; maxDistRTNodes += incMaxDist){
						for(rrtIters = minNodes; rrtIters <= maxNodes; rrtIters += incNodes){
							for(int i = 0; i <= iterationsPerDataSet; i++){
								string toWrite = "MCT" + minDistRTNodes + "," + maxDistRTNodes + ",";
								bool success = false;
								realFrame = 0;
								curFrame = 0;
								PlatsGoToFrame(0);

								System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
								stopwatch.Start ();
								success = RRT(0);
								stopwatch.Stop();
								if(success){
									toWrite += "true,";
								}
								else{
									toWrite += "false,";
								}
								toWrite += stopwatch.Elapsed;
								using (System.IO.StreamWriter file = new System.IO.StreamWriter(batchFilename, true))
								{
									file.WriteLine(toWrite);
								}

								toWrite = "AStar" + minDistRTNodes + "," + maxDistRTNodes + ",";
								success = false;
								realFrame = 0;
								curFrame = 0;
								PlatsGoToFrame(0);
								stopwatch = new System.Diagnostics.Stopwatch();
								stopwatch.Start ();
								success = RRT(1);
								stopwatch.Stop();
								if(success){
									toWrite += "true,";
								}
								else{
									toWrite += "false,";
								}
								toWrite += stopwatch.Elapsed;
								using (System.IO.StreamWriter file = new System.IO.StreamWriter(batchFilename, true))
								{
									file.WriteLine(toWrite);
								}
							}
						}
					}
				}
			}
			batchComputation = false;
			batchComputationRRT = false;
			goToFrame(0);
		}

		void initializeKeyboard(){
			if(!platsInitialized){
				initPlat();
			}
			modelObj = Instantiate(modelFab) as GameObject;
			modelObj.name = "modelObject" + "P";
			modelObj.transform.parent = models.transform;
			player = Instantiate(playerFab) as GameObject;
			player.name = "player" + "P";
			player.transform.parent = players.transform;
			mModel = modelObj.GetComponent<movementModel>() as movementModel;
			mModel.hplatmovers = hplatmovers;
			mModel.vplatmovers = vplatmovers;
			mModel.player = player;
			mModels.Add (mModel);
			mModel.startState = new PlayerState();
			mModel.startLocation = startingLoc;
			mModel.initializev2();
		}


		string getControlInput(Event e){
			//Event e = Event.current;
			if(e == null){
				return "wait";
			}
			else{
				Debug.Log ("SOMETHOING");
			}
			if(e.keyCode == KeyCode.A){
				return "Left";
			}
			else if(e.keyCode == KeyCode.D){
				return "Right";
			}
			else if(e.keyCode == KeyCode.W){
				return "jump";
			}
			else{
				return "wait";
			}
			/*
			if(Input.GetKey (KeyCode.LeftArrow) || Input.GetKey (KeyCode.A)){
				if(Input.GetKey (KeyCode.UpArrow) || Input.GetKey (KeyCode.W)){
					Debug.Log ("jump left");
					return "jump left";
				}
				else{
					Debug.Log ("left");
					return "Left";
				}
			}
			else if(Input.GetKey (KeyCode.RightArrow) || Input.GetKey (KeyCode.D)){
				if(Input.GetKey (KeyCode.UpArrow) || Input.GetKey (KeyCode.W)){
					Debug.Log ("jump right");
					return "jump right";
				}
				else{
					Debug.Log ("right");
					return "Right";
				}
			}
			else if(Input.GetKey (KeyCode.UpArrow) || Input.GetKey (KeyCode.W)){
				Debug.Log ("jump");
				return "jump";
			}
			else{
				Debug.Log ("wait");
				return "wait";
			}*/
		}
	
	


	//TODO Finish
	//UCTSEARCH

		public float Cp = 1 / Mathf.Sqrt(2);
		public GameObject uct;

		private RTNode UCTSearch(Vector3 startLoc, Vector3 golLoc, PlayerState state, int frame){
			cleanUp();

			if(drawWholeThing){
				uct = new GameObject("UCT");
			}
			
			modelObj = Instantiate(modelFab) as GameObject;
			modelObj.name = "modelObject" + count;
			modelObj.transform.parent = models.transform;
			player = Instantiate(playerFab) as GameObject;
			player.name = "player" + count;
			player.transform.parent = players.transform;
			mModel = modelObj.GetComponent<movementModel>() as movementModel;
			mModel.hplatmovers = hplatmovers;
			mModel.vplatmovers = vplatmovers;
			mModel.player = player;
			mModels.Add (mModel);
			count++;
			
			
			
			UCTNode root = new UCTNode();
			root.rt.state = state;
			root.rt.frame = frame;
			root.rt.position = startLoc;
			int budget = maxDepthAStar;
			UCTNode v = null;

			int i = 0;
			bool success = false;
			while(i < budget){
				i++;
				v = TreePolicy(root, golLoc);
				double delta = DefaultPolicy(v.rt, golLoc);
				if(v.dead){
					delta = -10;
					v.delta = 0;
				}
				Backup(v, delta);
				if(delta > 999.5){
					Debug.Log ("SUCCESS");
					success = true;
					break;
				}	
			}

			if(success || showDeaths){
				mModel.startState = state;
				mModel.startFrame = frame;
				mModel.startLocation = startLoc;
				mModel.initializev2();
				mModel.color = new Color(Random.Range (0f, 1f), Random.Range (0f, 1f), Random.Range (0f, 1f));
				
				return reCreatePathUCT(v, root);
			}
			else{
				return null;
			}
		}
		
		public UCTNode TreePolicy(UCTNode v, Vector3 golLoc){
			//TODO: instead of while true, should be while !terminal.
			while(true){
				if(v.unusedActions.Count > 0){
					return Expand(v, golLoc);
				}
				else{
					v = BestChild(v, Cp);
				}
			}
			return v;
		}
		
		
		//Returns Null if bestChild value is < 0, which should not happen I think?
		public UCTNode BestChild(UCTNode v,double c){
			UCTNode maxNode = null;
			double maxVal = 0;
			foreach(UCTNode child in v.children){
				double val = (child.delta/child.visits) + c * Mathf.Sqrt(2 * Mathf.Log(v.visits) / child.visits);
				if(val > maxVal){
					maxVal = val;
					maxNode = child;
				}
			}
			return maxNode;
		}

		//This is maybe, possibly right?
		public float DefaultPolicy(RTNode s, Vector3 golLoc){
			return 1000 - ((Vector2)golLoc - s.position).magnitude;
		}
		
		
		
		public UCTNode Expand(UCTNode v, Vector3 golLoc){
			int actInt = Random.Range(0, v.unusedActions.Count);
			string action = v.unusedActions[actInt];
			v.unusedActions.RemoveAt(actInt);
			UCTNode vn = expandWith(v, action, golLoc);

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
				
				
				VectorLine line = new VectorLine("UCT", new Vector3[] {v.rt.position, vn.rt.position}, clr, null, 2.0f);
				line.Draw3D();
				line.vectorObject.transform.parent = uct.transform;
			}
			


			return vn;
		}
		
		
		public void Backup(UCTNode v, double delta){
			while(v != null){
				v.visits++;
				v.delta += delta;
				v = v.parent;
			}
		}

		private UCTNode expandWith(UCTNode v, string action, Vector3 golLoc){
			mModel.startState = v.rt.state;
			mModel.startLocation = v.rt.position;
			mModel.startFrame = v.rt.frame;
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
				mModel.durations.Add (1);
				frame = mModel.loopUpdate();
			}			
			
		
			RTNode toReturnRT = new RTNode(player.transform.position, v.rt.frame + frame, mModel.state);
				
			toReturnRT.actions.AddRange (mModel.actions);
			toReturnRT.durations.AddRange(mModel.durations);
			toReturnRT.parent = v.rt;
			v.rt.children.Add(toReturnRT);

			UCTNode toReturn  = new UCTNode();
			toReturn.rt = toReturnRT;
			toReturn.parent = v;
			v.children.Add(toReturn);
			if(mModel.dead){
					toReturn.dead = true;
			}
			return toReturn;
			};
		}
		
		
		
		private RTNode reCreatePathUCT(UCTNode v, UCTNode root){

			loopAddUCT(v.rt, root.rt);
			totalFrames = Mathf.Max (mModel.numFrames, totalFrames);
			var tempMaterial = new Material(player.renderer.sharedMaterial);
			tempMaterial.color = mModel.color;
			player.renderer.sharedMaterial = tempMaterial;
			if(drawPaths){
				mModel.drawPath(paths);
			}
			
			RTNode toReturn = new RTNode(v.rt.position, mModel.numFrames, v.rt.state);
			toReturn.actions.AddRange (mModel.actions);
			toReturn.durations.AddRange (mModel.durations);
			return toReturn;
		}
		
		private void loopAddUCT(RTNode node, RTNode root){
			if(node != root){
				loopAddUCT(node.parent, root);
				mModel.actions.AddRange(node.actions);
				mModel.durations.AddRange(node.durations);
				mModel.numFrames = node.frame;
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
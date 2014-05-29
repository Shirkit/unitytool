

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
		private posMovModel pmModel;
		public static int numPlayers = 1;
		public static int numIters = 100;
		public static int depthIter = 3;
		public static int totalFrames = 0;
		public static int realFrame;
		public static int curFrame;
		public static string filename;
		public static string destCount;

		public static bool drawWholeThing;
		public List<movementModel> mModels;
		public List<posMovModel> pmModels;

		public int count = 0;

		public GameObject nodMarFab = Resources.Load("nodeMarker") as GameObject;
		public GameObject playerFab = Resources.Load ("player") as GameObject;
		public GameObject modelFab = Resources.Load ("modelObject") as GameObject;
		public GameObject posModFab = Resources.Load ("posMod") as GameObject;

		private static Vector2 scrollPos = new Vector2 ();
		
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
			drawWholeThing = EditorGUILayout.Toggle ("Debug Mode", drawWholeThing);


			if (GUILayout.Button ("Clear")) {
				cleanUp();
			}

			curFrame = EditorGUILayout.IntSlider ("frame", curFrame, 0, totalFrames);


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

			rrtIters = EditorGUILayout.IntField("RRT Nodes: ", rrtIters);
			if (GUILayout.Button ("RRT - MCT")) {
				if(!platsInitialized){
					initPlat();
				}
				pathsMarked = false;
				realFrame = 0;
				curFrame = 0;
				RRT(true);
				PlatsGoToFrame(0);
			}
			if (GUILayout.Button ("RRT - AS")) {
				if(!platsInitialized){
					initPlat();
				}
				pathsMarked = false;
				realFrame = 0;
				curFrame = 0;
				RRT(false);
				PlatsGoToFrame(0);
			}
			
			if(GUILayout.Button ("AStarSearch")){
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
					PlatsGoToFrame(curFrame);

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
			foreach(posMovModel pModel in pmModels){
				if(pModel != null){
					pModel.goToFrame(curFrame);
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



		private void resetState(movementModel model, PlayerState state){
			model.state.isOnGround = state.isOnGround;
			model.state.velocity.x = state.velocity.x;
			model.state.velocity.y = state.velocity.y;
			model.state.numJumps = state.numJumps;
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



				resetState(mModel, state);


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

			player.transform.position = startLoc;
			mModel.initializev2();


			mModel.numFrames = 0;


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

		public static int framesPerStep = 1;
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

				return toReturn;
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


		public static float maxDistRTNodes = 5;
		public static float minDistRTNodes = 1;

		private bool RRT(bool useMCT){
			cleanUp();
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
				for(i = 0; i < rrtIters; i++){
					q++;
					float x = Random.Range (bl.x, tr.x);
					float y = Random.Range (bl.y, tr.y);
					if(!tryAddNode(x,y, j, useMCT)){
						i--;
					}
					if(q > rrtIters*10){
						break;
					}
					if(goalReached[j]){
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

		private bool tryAddNode(float x, float y, int j, bool useMCT){
			RTNode closest = findClosest(x,y, j);
			if(closest == null){

				return false;
			}
			else{

				RTNode final;
				if(useMCT){
					final = MCTSearch(new Vector3(closest.position.x, closest.position.y, 10), new Vector3(x, y, 10), closest.state, closest.frame);
				}
				else{
					final = AStarSearch(new Vector3(closest.position.x, closest.position.y, 10), new Vector3(x, y, 10), closest.state, closest.frame);
				}
				if(final != null){
					final.parent = closest;
					closest.children.Add (final);
					final.frame = closest.frame + final.frame;
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
								success = RRT(true);
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
								success = RRT(false);
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
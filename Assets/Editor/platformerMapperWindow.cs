

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
		
		#region var defs
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
		#endregion var defs

		#region heatmap

		public static GameObject hmap;
		public static GameObject hmapText;
		public static Texture2D hmapTex;
		public static int[,] hmapDensity;
		public static int hmapGridX = 100;
		public static int hmapGridY = 100;
		public static int maxHDensity;
		public static Vector3 blH;
		public static Vector3 trH;

		public static bool colHmapU;
		public static bool colHmapL;

		public static void initHeatMapper(){
			DestroyImmediate(GameObject.Find ("hmap"));

			blH = GameObject.Find ("bottomLeft").transform.position;
			trH = GameObject.Find ("topRight").transform.position;
			hmapGridX = 25;
			hmapGridY = 25;


			hmapDensity = new int[hmapGridX, hmapGridY];
			maxHDensity = 0;



			

			hmap = new GameObject("hmap");
			hmapText = new GameObject("hmapText");
			hmapText.transform.position = blH + (0.5f * (trH - blH));
			hmapText.transform.position += new Vector3(0,0,15);
			hmapText.transform.localScale = new Vector3((100f*(trH.x - blH.x)/((float)hmapGridX)), (100f*(trH.y - blH.y)/((float)hmapGridY)), 1);
			hmapText.AddComponent<SpriteRenderer>();
			hmapTex = new Texture2D(hmapGridX, hmapGridY);
			hmapText.transform.parent = hmap.transform;
		}
		public static void colorHeatMapper(){
			for(int k = 0; k < hmapGridX; k++){
				for(int l = 0; l < hmapGridY; l++){

					Color col;
					if((float)hmapDensity[k, l] > 0.75f * ((float)maxHDensity)){
						col  = Color.red;
					}
					else{

						Color initCol = Color.red;
						initCol.a = 0f;
						
						float maxLerp = 0.75f * ((float)maxHDensity);

						col = Color.Lerp(initCol, Color.red, (((float)hmapDensity[ k, l])/maxLerp)); 
					}


					hmapTex.SetPixel(k, l, col);
				}
			}
			hmapTex.Apply();
			
			byte[] bytes = hmapTex.EncodeToPNG();
			File.WriteAllBytes(Application.dataPath + 
			                   "/Levels/Plateformer/Graphics/HMAPDensity.png", bytes);
			
			
			SpriteRenderer r = hmapText.GetComponent<SpriteRenderer>(); 
			
			
			
			Sprite s = AssetDatabase.LoadAssetAtPath(
				"Assets/Levels/Plateformer/Graphics/HMAPDensity.png", typeof(Sprite)) as Sprite;
			//Debug.Log ("s-" + s);
			//Debug.Log ("r-" + r);
			
			
			r.sprite = s;  
			
		}
		public static void updateDensity(float x, float y){


			int xIndex = Mathf.FloorToInt((x - blH.x) / ((trH.x - blH.x) / (float)hmapGridX));
			int yIndex = Mathf.FloorToInt((y - blH.y) / ((trH.y - blH.y) / (float)hmapGridY));

			if(xIndex >= hmapGridX || yIndex >= hmapGridY || xIndex < 0 || yIndex < 0){
				return;
			}


			try{
			hmapDensity[xIndex, yIndex]++;
			}
			catch(System.Exception e){
				Debug.Log (xIndex);
				Debug.Log (yIndex);
				Debug.Log (hmapDensity.Length);
				Debug.Log (hmapDensity.Rank);
				throw e;
			}
			maxHDensity = Mathf.Max (maxHDensity, hmapDensity[xIndex, yIndex]);
		}
		public static void colorPath(movementModel model){
			while(!model.updater()){
				float x = model.player.transform.position.x;
				float y = model.player.transform.position.y;
				updateDensity(x, y);
			}
		}


		#endregion heatmap

		#region Inits

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

		#endregion Inits

		#region Update/Gui

		void OnGUI () {
			scrollPos = EditorGUILayout.BeginScrollView (scrollPos);

			if(GUILayout.Button("Init Heat Mapper")){
				initHeatMapper();
			}
			if(GUILayout.Button ("colorHeatMapper")){
				colorHeatMapper();
			}
			colHmapU = EditorGUILayout.Toggle("ColourHMap", colHmapU);






			if(GUILayout.Button ("Draw Moving Platform Lines")){
				drawArrows();
			}


			if (GUILayout.Button ("Monte-Carlo Tree Search")) {
				cleanUpRRTDebug();
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
			framesPerStep = EditorGUILayout.IntSlider ("Frames Per Step A Star", framesPerStep, 1, 15);
			maxDepthAStar = EditorGUILayout.IntSlider ("Max Depth A Star", maxDepthAStar, 1, 50000);



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

			threedee = EditorGUILayout.Toggle ("Astar3d", threedee);
			minDist = EditorGUILayout.FloatField("Min Dist Astar", minDist);

			if(GUILayout.Button ("AStarSearch")){
				if(colHmapU){
					colHmapL =  true;
				}
				cleanUpRRTDebug();
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
				colHmapL = false;
			}

			if(GUILayout.Button ("UCT Search")){
				if(colHmapU){
					colHmapL =  true;
				}
				cleanUpRRTDebug();
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
				colHmapL = false;
			}

			if(GUILayout.Button ("ReInitialize Moving Platforms")){
				initPlat();
			}

			#region LevelTest
			EditorGUILayout.LabelField ("");
			EditorGUILayout.LabelField ("");
			EditorGUILayout.LabelField ("Level Test");
			
			testFilename = EditorGUILayout.TextField ("Test Filename", testFilename);
			iters = EditorGUILayout.IntField ("Iteration Per DataSet", iters);
			
			EditorGUILayout.LabelField ("A Star Test");
			NumFramesAS = EditorGUILayout.IntField (" Num Frames", NumFramesAS);

			DepthAS = EditorGUILayout.IntField ("Depth", DepthAS);

			EditorGUILayout.LabelField ("UCT Test");
			NumFramesUCT = EditorGUILayout.IntField (" Num Frames", NumFramesUCT);
			
			DepthUCT = EditorGUILayout.IntField ("Depth", DepthUCT);

			EditorGUILayout.LabelField ("MCT Test");
			
			Iterations = EditorGUILayout.IntField("iterations", Iterations);
			Depth = EditorGUILayout.IntField("min Depth", Depth);

			EditorGUILayout.LabelField ("RRT Test ASTAR");
			
			MinDistAS = EditorGUILayout.FloatField("Min Dist", MinDistAS);

			MaxDistAS = EditorGUILayout.FloatField("Max Dist", MaxDistAS);

			NodesAS = EditorGUILayout.IntField("Nodes", NodesAS);

			ASFramesTST = EditorGUILayout.IntField("A Star FPS", ASFramesTST);
			ASDepthTST = EditorGUILayout.IntField("A Star Depth", ASDepthTST);


			EditorGUILayout.LabelField ("RRT Test MCT");
			
			MinDistMCT = EditorGUILayout.FloatField("Min Dist", MinDistMCT);
			
			MaxDistMCT = EditorGUILayout.FloatField("Max Dist", MaxDistMCT);
			
			Nodes = EditorGUILayout.IntField("Nodes", Nodes);
			
			MCTIterTST = EditorGUILayout.IntField("MCT Iter", MCTIterTST);
			MCTDepthTST = EditorGUILayout.IntField("MCT Depth", MCTDepthTST);

			EditorGUILayout.LabelField ("RRT Test UCT");
			
			MinDistUCT = EditorGUILayout.FloatField("Min Dist", MinDistUCT);
			
			MaxDistUCT = EditorGUILayout.FloatField("Max Dist", MaxDistUCT);
			
			NodesUCT = EditorGUILayout.IntField("Nodes", NodesUCT);

			UCTFramesTST = EditorGUILayout.IntField("UCT FPS", UCTFramesTST);
			UCTDepthTST = EditorGUILayout.IntField("UCT Depth", UCTDepthTST);
			as2B = EditorGUILayout.Toggle("astar2d", as2B);
			as3B = EditorGUILayout.Toggle("astar3d", as3B);
			mctB = EditorGUILayout.Toggle("mct", mctB);
			uctB = EditorGUILayout.Toggle("uct", uctB);
			rrtasB = EditorGUILayout.Toggle("rrt-astar", rrtasB);
			rrtmctB = EditorGUILayout.Toggle("rrt-mct", rrtmctB);
			rrtuctB = EditorGUILayout.Toggle("rrt-uct", rrtuctB);



			if(GUILayout.Button ("Test Level")){
				if(!platsInitialized){
					initPlat();
				}
				startingLoc = GameObject.Find ("startingPosition").transform.position;
				goalLoc = GameObject.Find("goalPosition").transform.position;
				testLevel();
			}

			#endregion LevelTest




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

		#endregion Update/Gui

		#region ImportExport

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

		#endregion ImportExport

		#region UtilityMethods

		
		public static void drawArrows(){
			DestroyImmediate(GameObject.Find("arrows"));
			GameObject arws = new GameObject ("arrows");
			Texture2D frontTex = Resources.Load ("arrowStart") as Texture2D;
			Material arrowMat = Resources.Load ("Arrow") as Material;
			VectorLine.SetEndCap ("Arrow", EndCap.None);
			VectorLine.SetEndCap("Arrow", EndCap.Mirror, arrowMat , frontTex);
			
			
			foreach(HPlatMovement hplat in hplatmovers){
				Vector3 tmp = new Vector3(hplat.lmostX, hplat.gameObject.transform.position.y, -5);
				Vector3 tmp2 = new Vector3(hplat.rmostX, hplat.gameObject.transform.position.y, -5);
				
				VectorLine line = new VectorLine("line", new Vector3[] {tmp, tmp2} , Color.magenta, arrowMat, 7f, LineType.Continuous);
				line.vectorObject.transform.parent = arws.transform;
				line.endCap = "Arrow";
				line.Draw3D();
			}
			foreach(VPlatMovement vplat in vplatmovers){
				Vector3 tmp = new Vector3(vplat.gameObject.transform.position.x, vplat.bmostY, -5);
				Vector3 tmp2 = new Vector3(vplat.gameObject.transform.position.x, vplat.tmostY, -5);
				
				VectorLine line = new VectorLine("line", new Vector3[] {tmp, tmp2} , Color.magenta, arrowMat, 7f, LineType.Continuous);
				line.vectorObject.transform.parent = arws.transform;
				line.endCap = "Arrow";
				line.Draw3D();
			}
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
			while(GameObject.Find ("RRT") != null){
				DestroyImmediate(GameObject.Find ("RRT"));
			}
				            

		}

		#endregion UtilityMethods

		#region MCT
		
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

		#endregion MCT

		#region AStar

		public bool asGoalReached;
		public PriorityQueue<RTNode, double> heap;
		public RTNode asRoot;
		public RTNode asGoalNode;

		public static int framesPerStep = 10;
		public static int maxDepthAStar = 4000;

		public GameObject astar;
		public int statesExploredAS;

		public KDTree asClosed;
		public bool asKDNonEmpty;
		public bool threedee;

		private RTNode AStarSearch(Vector3 startLoc, Vector3 golLoc, PlayerState state, int frame){

			statesExploredAS = 0;
			cleanUp();
			if(threedee){
				asClosed = new KDTree(3);
			}
			else{
				asClosed = new KDTree(2);
			}
			asKDNonEmpty = false;

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
			heap.Enqueue(asRoot, -Vector2.Distance(asRoot.position, golLoc) -((float)asRoot.frame)/10f);

			statesExploredAS++;
			int k = 0;
			while(!asGoalReached && heap.Count > 0 && k < maxDepthAStar){
				k++;

				RTNode cur = heap.Dequeue().Value;


				//Check to expend it or not
				//TODO: Make a 2d kd-tree . 
				if(asKDNonEmpty)
				{
					RTNode closest;
					Vector3 pos1;
					Vector3 pos2;

					if(threedee){
						closest = asClosed.nearest(new double[]{cur.position.x, cur.position.y, cur.frame}) as RTNode;

						pos1 = new Vector3(cur.position.x, cur.position.y,((float)cur.frame)/10f);
						pos2 = new Vector3(closest.position.x, closest.position.y,((float)closest.frame)/10f);
					}
					else{
						closest = asClosed.nearest(new double[]{cur.position.x, cur.position.y}) as RTNode;
						
						pos1 = new Vector3(cur.position.x, cur.position.y,0);
						pos2 = new Vector3(closest.position.x, closest.position.y,0);
					}
					
					if(Vector3.Distance(pos1, pos2) < minDist)
					{
						continue;
					}
					else{
						if(threedee){
							asClosed.insert (new double[]{cur.position.x, cur.position.y, cur.frame}, cur);
						}
						else{
							asClosed.insert (new double[]{cur.position.x, cur.position.y}, cur);
						}	          
						asKDNonEmpty = true;
					}
				}
				else{
					if(threedee){
						asClosed.insert (new double[]{cur.position.x, cur.position.y, cur.frame}, cur);
					}
					else{
						asClosed.insert (new double[]{cur.position.x, cur.position.y}, cur);
					}	          
					asKDNonEmpty = true;
				}

				if(drawWholeThing)
				{
					//GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
					//g.transform.parent = astar.transform; 
					//g.transform.position = new Vector3(cur.position.x, cur.position.y,cur.frame); 
				}

				/*
				try{					
					asClosed.insert (new double[]{cur.position.x, cur.position.y, cur.frame}, cur);
					asKDNonEmpty = true;
				}
				catch (KeyDuplicateException e){
					continue;
				}*/

				//Have to expend the node
				
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
					file.WriteLine(("States Explored:" + statesExploredAS));

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
					Debug.Log ("STATES EXPLORED = " + statesExploredAS);
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
					if(colHmapL){
						colorPath(mModel);
					}

					return toReturn;
				}
			}
			else{
				DestroyImmediate(modelObj2);
				DestroyImmediate(player2);
				if(drawWholeThing){
					Debug.Log ("STATES EXPLORED = " + statesExploredAS);
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


			//Debug.Log (" Number of frames : " + mModel.numFrames);
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

		public float minDist = 1.5f;

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


				if(dist < 0.5f){
					asGoalReached = true;
					asGoalNode = nex;
					statesExploredAS++;
				}
				else{
					//Check if in the close list. 
					if(asKDNonEmpty){
						RTNode closest;
						Vector3 pos1;
						Vector3 pos2;
						if(threedee){
							closest = asClosed.nearest(new double[]{nex.position.x, nex.position.y, nex.frame}) as RTNode;
							pos1 = new Vector3(nex.position.x, nex.position.y,((float)nex.frame)/10f);
							pos2 = new Vector3(closest.position.x, closest.position.y,((float)closest.frame)/10f);
						}
						else{
							closest = asClosed.nearest(new double[]{nex.position.x, nex.position.y}) as RTNode;
							pos1 = new Vector3(nex.position.x, nex.position.y,0);
							pos2 = new Vector3(closest.position.x, closest.position.y,0);
						}
						if(Vector3.Distance(pos1, pos2) > minDist)
						{
							heap.Enqueue(nex, -dist -((float)nex.frame)/10f);
							statesExploredAS++;
						}

					}
					else{
						heap.Enqueue(nex, -dist -((float)nex.frame)/10f);
						statesExploredAS++;
					}
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

		#endregion AStar

		private void printSolution(){
			int i = 0;
			while(i < mModel.durations.Count){
				Debug.Log (mModel.actions[i] + " -- " + mModel.durations[i]);
				i++;
			}
		}


		#region RRT

		public bool[] goalReached;
		public static int rrtIters = 100;

		public KDTree[] rrtTrees;

		public RTNode[] roots;
		public RTNode[] goalNodes;

		public static float maxDistRTNodes = 10;
		public static float minDistRTNodes = 1;

		public static GameObject RRTDebug;

		public static bool addedNode = false;
		public static float addedX;
		public static float addedY;

		public static int statesExploredRRT;

		private bool RRT(int useMCT){
			cleanUp();
			statesExploredRRT = 0;

			if(debugMode){

				//TODO: Put it in a clean place. The RRT Gameobject is never 
				//clean up before creating a new one.
				DestroyImmediate(RRTDebug);
				RRTDebug = new GameObject("RRT");

			}



			goalReached = new bool[numPlayers];

			rrtTrees = new KDTree[numPlayers];
			roots = new RTNode[numPlayers];
			goalNodes = new RTNode[numPlayers];
			int i = 0;
			int q = 0;

			int counterNode = 0; 

			for(int j = 0; j < numPlayers; j++){

				maxDensity[j] = 0;

				goalReached[j] = false;
				startingLoc = GameObject.Find ("startingPosition").transform.position;
				goalLoc = GameObject.Find("goalPosition").transform.position;
				Vector3 bl = GameObject.Find ("bottomLeft").transform.position;
				Vector3 tr = GameObject.Find ("topRight").transform.position;
				rrtTrees[j] = new KDTree(2);
				roots[j] = new RTNode(startingLoc, 0, new PlayerState());
				try{
				rrtTrees[j].insert(new double[] {roots[j].position.x, roots[j].position.y} ,roots[j]);
				}
				catch(System.Exception e){
					Debug.Log ("LOCATION 1");
					throw e;
				}
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
				if (yMax > tr.y)
					yMax = tr.y; 
				


				for(i = 0; i < rrtIters; i++){
					q++;

					//TODO sample from limits reachable 
					//Use 4 heaps. 
					float x = Random.Range (xMin, xMax);
					float y = Random.Range (yMin, yMax);

					//TODO: 
					//Add a control for that one
					if(UnityEngine.Random.Range(0,100)>0)
					{
						RaycastHit2D returnCast = Physics2D.Raycast(new Vector3(x,y),- Vector3.up, 200f);

						if(returnCast.collider != null )//&& returnCast.collider.tag == "Floor")
						{
							if(returnCast.collider.gameObject.tag == "Lethals"){
								i--;
								continue; 
							}
							if(Vector2.Distance(returnCast.point,new Vector3(x,y))>5f)
							{
								y = returnCast.point.y+0.5f; 
							}
							//VectorLine line = new VectorLine("linecast", new Vector3[] {new Vector3(x,y), 
							//	returnCast.point}, Color.blue, null, 1.0f);
							//line.Draw3D();
							//line.vectorObject.transform.parent = RRTDebug.transform;


							//Draw lines

							//Debug.Log(returnCast.collider.name); 
						}

					}

					//Adding blue sphere if debugging
					//Placing it where we tried to place a node
					if(debugMode)
					{

						//Added the sphere for better display
						GameObject o = GameObject.CreatePrimitive(PrimitiveType.Sphere);
						o.transform.parent = RRTDebug.transform; 
						o.name = "node"+i;
						o.transform.position = new Vector3(x,y); 
						o.transform.localScale = new Vector3(0.33f,0.33f,0.33f);

						float t = ((float)i)/((float)rrtIters);
						if(t <= 0.5f){
							var tempMaterial = new Material(o.renderer.sharedMaterial);
							tempMaterial.color = Color.Lerp(Color.green, Color.blue, (t*2f));
							o.renderer.sharedMaterial = tempMaterial;
						}
						else{
							var tempMaterial = new Material(o.renderer.sharedMaterial);
							tempMaterial.color = Color.Lerp(Color.blue, Color.red, ((t-0.5f)*2f));
							o.renderer.sharedMaterial = tempMaterial;
						}

						//Add interpolation between colours to know when it was added. 
					}

					if(!tryAddNode(x,y, j, useMCT))
					{
						i--;
					}
					else if(addedNode)
					{
						addedNode = false;

						//The node was added. 
						//Updating the random bounds
						counterNode ++; 

						if (addedX - maxDistRTNodes < xMin)
							xMin = addedX - maxDistRTNodes; 							
						if (addedX + maxDistRTNodes > xMax)
							xMax = addedX + maxDistRTNodes; 							
						if (addedY - maxDistRTNodes < yMin)
							yMin = addedY - maxDistRTNodes; 
						if (addedY + maxDistRTNodes > yMax)
							yMax = addedY + maxDistRTNodes; 

						//Check if the bounds of the level are reached
						if (xMin < bl.x)
							xMin = bl.x; 
						if (xMax > tr.x)
							xMax = tr.x; 
						if (yMin < bl.y)
							yMin = bl.y; 
						if (yMax > tr.y)
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
			if(debugMode){
				Debug.Log (statesExploredRRT);
			}

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
					mModel.numFrames = 0;

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
					if(colHmapU){
						colorPath(mModel);
					}

				}
				else{
					//Debug.Log ("Attempt " + j + " failed");
				}
			}

			//Debug.Log ("Num Frames : " + mModel.numFrames);
			
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
			if(closest == null)
			{

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
					statesExploredRRT += statesExploredAS;
				}
				else
				{
					final = UCTSearch(new Vector3(closest.position.x, closest.position.y, 10), new Vector3(x, y, 10), closest.state, closest.frame);
					statesExploredRRT += statesExploredUCT;
				}
				if(final != null)
				{


					RTNode newClosest = rrtTrees[j].nearest(new double[] {final.position.x, final.position.y}) as RTNode;
					//Check how close it is to the parent. 
					if( Vector2.Distance(final.position, newClosest.position) < minDistRTNodes)// ||(final.position - closest.position).magnitude > maxDistRTNodes )
						return true; 

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





					if(rrtTrees[j].search(new double[] {final.position.x, final.position.y}) == null)
					{
						addedNode = true;
						addedX = final.position.x;
						addedY = final.position.y;

						final.parent = closest;
						closest.children.Add (final);
						if(useMCT == 1){
							final.frame = closest.frame + final.frame;
						}
						try{
						rrtTrees[j].insert(new double[] {final.position.x, final.position.y}, final);
						}
						catch(System.Exception e){
							Debug.Log ("LOCATION 2");
							throw e;
						}
						
						if((new Vector3(final.position.x, final.position.y, 10) - goalLoc).magnitude < 0.5)
						{
							Debug.Log (goalLoc);
							goalReached[j] = true;
							goalNodes[j] = final;
						}
						else if((new Vector3(final.position.x, final.position.y, 10) - goalLoc).magnitude < 5f)
						{
							goalReached[j] = tryAddGoalNode(final, j, useMCT);
						}
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
				if(useMCT == 0)
				{
					final = MCTSearch(new Vector3(node.position.x, node.position.y, 10), goalLoc, node.state, node.frame);
				}
				else if (useMCT == 1){
					final = AStarSearch(new Vector3(node.position.x, node.position.y, 10), goalLoc, node.state, node.frame);
					statesExploredRRT += statesExploredAS;
				}
				else{
					final = UCTSearch(new Vector3(node.position.x, node.position.y, 10), goalLoc, node.state, node.frame);
					statesExploredRRT += statesExploredUCT;
				}
				if(final != null && Vector2.Distance (final.position, goalLoc) < 0.5f){



					if(debugMode){
						VectorLine line = new VectorLine("RRT", new Vector3[] {node.position, final.position}, Color.red, null, 2.0f);
						line.Draw3D();
						line.vectorObject.transform.parent = RRTDebug.transform;
					}
					final.parent = node;
					node.children.Add (final);
					final.frame = node.frame + final.frame;
					try{
					rrtTrees[j].insert(new double[] {final.position.x, final.position.y}, final);
					}
					catch(System.Exception e){
						Debug.Log ("LOCATION 3");
						throw e;
					}
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

		#endregion RRT

		#region Batch

		//Batch Computations Code

		#region batchCompute vars
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
		#endregion batchCompute vars
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

		#endregion Batch

		#region failedKeyboardAttempt
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

		#endregion failedKeyboardAttempt

		#region UCT

	//UCTSEARCH

		public double Cp = 1 / Mathf.Sqrt(2);
		public GameObject uct;
		public int[, ,] uctDensity;
		public int[] maxDensity;
		public int uctGridX;
		public int uctGridY;
		public GameObject uctText;
		public Texture2D uctTex;

		public int statesExploredUCT;

		public UCTNode cls;

		private RTNode UCTSearch(Vector3 startLoc, Vector3 golLoc, PlayerState state, int frame){
			cleanUp();
			statesExploredUCT = 0;

			uctGridX = 25;
			uctGridY = 10;
			//TODO: Replace 1 with numPlayers
			maxDensity = new int[1];
			uctDensity = new int[1, uctGridX, uctGridY];
			Vector3 bl = GameObject.Find ("bottomLeft").transform.position;
			Vector3 tr = GameObject.Find ("topRight").transform.position;

			if(drawWholeThing){
				uct = new GameObject("UCT");
				uctText = new GameObject("uctTexture");
				uctText.transform.position = bl + (0.5f * (tr - bl));
				uctText.transform.position += new Vector3(0,0,15);
				uctText.transform.localScale = new Vector3((100f*(tr.x - bl.x)/((float)uctGridX)), (100f*(tr.y - bl.y)/((float)uctGridY)), 1);
				uctText.AddComponent<SpriteRenderer>();
				uctTex = new Texture2D(uctGridX, uctGridY);
				uctText.transform.parent = uct.transform;
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
			mModel.numFrames = 0;
			mModels.Add (mModel);
			count++;
			
			
			
			UCTNode root = new UCTNode();
			root.rt.state = state;
			root.rt.frame = frame;
			root.rt.position = startLoc;
			cls = root;
			int budget = maxDepthAStar;
			UCTNode v = root;

			int i = 0;
			bool success = false;
			while(i < budget){
				i++;

				v = TreePolicy(root, golLoc, bl, tr);
				double delta = DefaultPolicy(v, golLoc, startLoc);


				if(v == null){
					break;
				}



				//RRT Density Grid Stuf
				float x = v.rt.position.x;
				float y = v.rt.position.y;
				int xIndex = Mathf.FloorToInt((x - bl.x) / ((tr.x - bl.x) / (float)uctGridX));
				int yIndex = Mathf.FloorToInt((y - bl.y) / ((tr.y - bl.y) / (float)uctGridY));
				/*if(xIndex >= uctGridX){
					Debug.Log ("XTOOBIG" + x + "-" + y);
				}
				else if(yIndex >= uctGridY){
					Debug.Log ("YTOOBIG" + x + "-" + y);
				}
				else{*/
				try
				{
					uctDensity[0, xIndex, yIndex]++;
					maxDensity[0] = Mathf.Max(maxDensity[0], uctDensity[0, xIndex, yIndex]);
					v.densityPenalty = ((float)uctDensity[0, xIndex, yIndex] )/ ((float)maxDensity[0]);
				}
				catch
				{
				}

				//}
				if(maxDensity[0] > 25){
					if(v.densityPenalty > 0.22f){
						v.dead = true;
					}
				}

				if(v.dead)
				{
					delta = -100000;
					
					v.delta = int.MinValue;
				}

				Backup(v, delta);


				if(((Vector2)golLoc -v.rt.position).magnitude < 0.5f){
					//Debug.Log ("SUCCESS");
					success = true;
					break;
				}
			}

			if(drawWholeThing){
				for(int k = 0; k < uctGridX; k++){
					for(int l = 0; l < uctGridY; l++){
						Color initialCol = Color.red;
						initialCol.a = 0f;

					

						Color col = Color.Lerp(initialCol, Color.red, (((float)uctDensity[0, k, l])/((float)maxDensity[0]))); 
						//col.a = 0.8f;




						uctTex.SetPixel(k, l, col);
					}
				}
				uctTex.Apply();

				byte[] bytes = uctTex.EncodeToPNG();
				File.WriteAllBytes(Application.dataPath + 
				                   "/Levels/Plateformer/Graphics/UCTDensity.png", bytes);
				
				
				SpriteRenderer r = uctText.GetComponent<SpriteRenderer>(); 
				
				
				
				Sprite s = AssetDatabase.LoadAssetAtPath(
					"Assets/Levels/Plateformer/Graphics/UCTDensity.png", typeof(Sprite)) as Sprite;
				//Debug.Log ("s-" + s);
				//Debug.Log ("r-" + r);


				r.sprite = s;  
				//Debug.Log ("rspr-" + r.sprite);
				//Debug.Log (uctText.GetComponent<SpriteRenderer>().sprite);

				Debug.Log("States Explored: " + statesExploredUCT);

			}

			if(success || showDeaths){
			
				mModel.startState = state;
				mModel.startFrame = frame;
				mModel.startLocation = startLoc;
				mModel.initializev2();
				mModel.color = new Color(Random.Range (0f, 1f), Random.Range (0f, 1f), Random.Range (0f, 1f));
				RTNode toReturn = reCreatePathUCT(v, root);
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

					if(colHmapL){
						colorPath(mModel);
					}
					return toReturn;
				}

			}
			else{
				DestroyImmediate(modelObj2);
				DestroyImmediate(player2);
				//Debug.Log (mModel.numFrames);

				return null;

				if(cls.dead)
				{
					return null;
				}
				else
				{
					mModel.startState = state;
					mModel.startFrame = frame;
					mModel.startLocation = startLoc;
					mModel.initializev2();
					mModel.color = new Color(Random.Range (0f, 1f), Random.Range (0f, 1f), Random.Range (0f, 1f));
					return reCreatePathUCT(cls, root);
				}
			}
		}
		
		public UCTNode TreePolicy(UCTNode v, Vector3 golLoc, Vector3 bl, Vector3 tr){
			//TODO: instead of while true, should be while !terminal.
			while(v != null){

				if(v.dead){
					break;
				}

				if(v.unusedActions.Count > 0){
					return Expand(v, golLoc);
				}
				else{
					v = BestChild(v, Cp, bl, tr);
				}


			}
			return v;
		}
				
		//Returns Null if bestChild value is < 0, which should not happen I think?
		public UCTNode BestChild(UCTNode v,double c, Vector3 bl, Vector3 tr){


			UCTNode maxNode = null;
			double maxVal = double.MinValue;
			foreach(UCTNode child in v.children){

				double val = (child.delta/child.visits) + c * Mathf.Sqrt(2 * Mathf.Log(v.visits) / child.visits) - child.densityPenalty * 50;
				if(val > maxVal){
					maxVal = val;
					maxNode = child;
				}
			}
			if(maxNode == null){
				Debug.Log ("FAILED");
				foreach(UCTNode child in v.children){
					double val = (child.delta/child.visits) + c * Mathf.Sqrt(2 * Mathf.Log(v.visits) / child.visits);
					Debug.Log (val);
				}
			}
			return maxNode;
		}

		//This is maybe, possibly right?
		public float DefaultPolicy(UCTNode u, Vector3 golLoc, Vector3 startLoc){
			RTNode s = u.rt;

			float dist = (golLoc - startLoc).magnitude;
			float dist2 = ((Vector2)golLoc - s.position).magnitude;

			if(cls == null || dist2 < ((Vector2)golLoc - cls.rt.position).magnitude){
				cls = u;
			}

			//return (((dist - dist2) / dist) * 50);
			return (1/((Vector2)golLoc - s.position).sqrMagnitude )*100;
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
				if(v.parent != null){
					v.parent.densityPenalty = Mathf.Max(v.densityPenalty-0.05f, v.parent.densityPenalty);
				}
				v = v.parent;
			}
		}

		private UCTNode expandWith(UCTNode v, string action, Vector3 golLoc){
			statesExploredUCT++;

			mModel.startState = v.rt.state;
			mModel.startLocation = v.rt.position;
			mModel.startFrame = v.rt.frame;
			//mModel.numFrames = 0;
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

			//Debug.Log ("Frame numbr is: " + mModel.numFrames);
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
		
		#endregion UCT

		#region LevelTest  

	//LevelTest
		public static bool as2B;
		public static bool as3B;
		public static bool mctB;
		public static bool uctB;
		public static bool rrtasB;
		public static bool rrtmctB;
		public static bool rrtuctB;

		public static int NumFramesAS = 10;
		public static int DepthAS = 4000;
		public static int NumFramesUCT = 10;
		public static int DepthUCT = 8000;
		public static int Iterations = 25;
		public static int Depth= 1000;
		public static float MinDistAS = 1f;
		public static float MaxDistAS = 10f;
		public static int NodesAS = 400;
		public static int ASFramesTST = 10;
		public static int ASDepthTST = 25;
		public static float MinDistMCT = 1f;
		public static float MaxDistMCT = 10f;
		public static int Nodes = 400;
		public static int MCTIterTST = 8;
		public static int MCTDepthTST = 20;
		public static float MinDistUCT = 1f;
		public static float MaxDistUCT = 10f;
		public static int NodesUCT = 400;
		public static int UCTFramesTST = 10;
		public static int UCTDepthTST = 80;


		public static int iters = 2;
		public static string testFilename = "test.csv";

		private void testLevel(){

			using (System.IO.StreamWriter file = new System.IO.StreamWriter(testFilename, true))
			{
				file.WriteLine("Type,Iteration,Success,Time,Frames,KeyPresses,StatesExplored");
			}
			threedee = false;
			if(as2B){
				for(int i = 0; i < iters; i++){

				//Astar

				framesPerStep = NumFramesAS;
				maxDepthAStar = DepthAS;
				
				realFrame = 0;
				curFrame = 0;
				PlatsGoToFrame(0);
				string toWrite = "AStar2," + i + ",";
				System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
				stopwatch.Start();
				RTNode tmp = AStarSearch(startingLoc, goalLoc, new PlayerState(), 0);
				stopwatch.Stop();
				if(tmp == null){
					toWrite += "0,";
				}
				else if( Vector2.Distance(tmp.position, goalLoc) > 0.5f){
					toWrite += "3,";
				}
				else{
					toWrite += "1,";
				}
				toWrite += stopwatch.ElapsedMilliseconds;
				toWrite += "," + mModel.numFrames;
				toWrite += "," + retrieveInputLength(mModel);
				toWrite += "," + statesExploredAS;

				using (System.IO.StreamWriter file = new System.IO.StreamWriter(testFilename, true))
				{
					file.WriteLine(toWrite);
				}
			}
			}
			threedee = true;
			if(as3B){
				for(int i = 0; i < iters; i++){
					
					//Astar
					
					framesPerStep = NumFramesAS;
					maxDepthAStar = DepthAS;
					
					realFrame = 0;
					curFrame = 0;
					PlatsGoToFrame(0);
					string toWrite = "AStar3," + i + ",";
					System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
					stopwatch.Start();
					RTNode tmp = AStarSearch(startingLoc, goalLoc, new PlayerState(), 0);
					stopwatch.Stop();
					if(tmp == null || Vector2.Distance(tmp.position, goalLoc) > 0.5f){
						toWrite += "0,";
					}	
					else{
						toWrite += "1,";
					}
					toWrite += stopwatch.ElapsedMilliseconds;
					toWrite += "," + mModel.numFrames;
					toWrite += "," + retrieveInputLength(mModel);
					toWrite += "," + statesExploredAS;

					using (System.IO.StreamWriter file = new System.IO.StreamWriter(testFilename, true))
					{
						file.WriteLine(toWrite);
					}
				}
			}
			threedee = false;
			if(mctB){
				for(int i = 0; i < iters; i++){

				//MCT


				numIters = Iterations;
				depthIter = Depth;
				
				realFrame = 0;
				curFrame = 0;
				PlatsGoToFrame(0);
				string toWrite = "MCT," + i + ",";
				System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
				cleanUp();
				stopwatch.Start();
				RTNode tmp = MCTSearch(startingLoc, goalLoc, new PlayerState(), 0);
				stopwatch.Stop();
				if(tmp == null || Vector2.Distance(tmp.position, goalLoc) > 0.5f){
					toWrite += "0,";
				}	
				else{
					toWrite += "1,";
				}
				toWrite += stopwatch.ElapsedMilliseconds;
				toWrite += "," + mModel.numFrames;
				toWrite += "," + retrieveInputLength(mModel);
				
				using (System.IO.StreamWriter file = new System.IO.StreamWriter(testFilename, true))
				{
					file.WriteLine(toWrite);
				}
			}
			}
			if(uctB){
				for(int i = 0; i < iters; i++){
				//UCT


				framesPerStep = NumFramesUCT;
				maxDepthAStar = DepthUCT;
				
				realFrame = 0;
				curFrame = 0;
				PlatsGoToFrame(0);
				string toWrite = "UCT," +  i + ",";
				System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
				cleanUp();
				stopwatch.Start();
				RTNode tmp = UCTSearch(startingLoc, goalLoc, new PlayerState(), 0);
				stopwatch.Stop();
				if(tmp == null || Vector2.Distance(tmp.position, goalLoc) > 0.5f){
					toWrite += "0,";
				}	
				else{
					toWrite += "1,";
				}
				toWrite += stopwatch.ElapsedMilliseconds;
				toWrite += "," + mModel.numFrames;
				toWrite += "," + retrieveInputLength(mModel);
				toWrite += "," + statesExploredUCT;

				using (System.IO.StreamWriter file = new System.IO.StreamWriter(testFilename, true))
				{
					file.WriteLine(toWrite);
				}

			}
			}

			if(rrtasB){
				for(int i = 0; i < iters; i++){
				//RRT - Astar



				minDistRTNodes = MinDistAS;
				maxDistRTNodes = MaxDistAS;
				framesPerStep = ASFramesTST;
				maxDepthAStar = ASDepthTST;
				rrtIters = NodesAS;


				string toWrite = "RRTASTAR," +  i + ",";
				bool success = false;
				realFrame = 0;
				curFrame = 0;
				PlatsGoToFrame(0);
				System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
				stopwatch.Start ();
				success = RRT(1);
				stopwatch.Stop();
				if(success){
					toWrite += "1,";
				}
				else{
					toWrite += "0,";
				}
				toWrite += stopwatch.ElapsedMilliseconds;
				toWrite += "," + mModel.numFrames;
				toWrite += "," + retrieveInputLength(mModel);
				toWrite += "," + statesExploredRRT;

				using (System.IO.StreamWriter file = new System.IO.StreamWriter(testFilename, true))
				{
					file.WriteLine(toWrite);
				}


			}
			}

			if(rrtmctB){
				for(int i = 0; i < iters; i++){
				//RRT - MCT




				
				minDistRTNodes = MinDistMCT;
				maxDistRTNodes = MaxDistMCT;
				numIters = MCTIterTST;
				depthIter = MCTDepthTST;
				rrtIters = Nodes;


				
				string toWrite = "RRTMCT," +  i + ",";
				bool success = false;
				realFrame = 0;
				curFrame = 0;
				PlatsGoToFrame(0);
				System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
				stopwatch.Start ();
				success = RRT(0);
				stopwatch.Stop();
				if(success){
					toWrite += "1,";
				}
				else{
					toWrite += "0,";
				}
				toWrite += stopwatch.ElapsedMilliseconds;
				toWrite += "," + mModel.numFrames;
				toWrite += "," + retrieveInputLength(mModel);
				
				using (System.IO.StreamWriter file = new System.IO.StreamWriter(testFilename, true))
				{
					file.WriteLine(toWrite);
				}
			}
			}
			if(rrtuctB){
				for(int i = 0; i < iters; i++){
				//RRT - UCT


				
				minDistRTNodes = MinDistUCT;
				maxDistRTNodes = MaxDistUCT;
				framesPerStep = UCTFramesTST;
				maxDepthAStar = UCTDepthTST;
				rrtIters = NodesUCT;

				string toWrite = "RRTUCT," +  i + ",";
				bool success = false;
				realFrame = 0;
				curFrame = 0;
				PlatsGoToFrame(0);
				System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
				stopwatch.Start ();
				success = RRT(2);
				stopwatch.Stop();
				if(success){
					toWrite += "1,";
				}
				else{
					toWrite += "0,";
				}
				toWrite += stopwatch.ElapsedMilliseconds;
				toWrite += "," + mModel.numFrames;
				toWrite += "," + retrieveInputLength(mModel);
				toWrite += "," + statesExploredRRT;

				using (System.IO.StreamWriter file = new System.IO.StreamWriter(testFilename, true))
				{
					file.WriteLine(toWrite);
				}
			}
			}
			
		}
		
		
		
		private int retrieveInputLength(movementModel model){
			int numKeyPress = 0;
			int index = 0;
			string prevAction = "";
			string curAction;
			while(index < model.actions.Count){
				curAction = model.actions[index];
				if(curAction.Equals(prevAction)){
					index++;
				}
				else{
					numKeyPress++;
					index++;

					if (curAction.Equals("jump left")){
						if(!prevAction.Contains("eft")){
							numKeyPress++;
						}
						prevAction = "Left";
					}
					if (curAction.Equals("jump right")){
						if(!prevAction.Contains("ight")){
							numKeyPress++;
						}
						prevAction = "Right";
					}
					else{
						prevAction = curAction;
					}
				}

			}
			return numKeyPress;

		}

		#endregion LevelTest
		
		


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
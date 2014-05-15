


using UnityEngine;
using UnityEditor;
using Vectrosity;
using System.IO;
using System.Xml.Serialization;
using System.Collections;
using System.Collections.Generic;

namespace EditorArea {
	public class PlatformerEditorWindow : EditorWindow  {
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

		public List<movementModel> mModels;
		public List<posMovModel> pmModels;

		public int count = 0;

		public GameObject nodMarFab = Resources.Load("nodeMarker") as GameObject;
		public GameObject playerFab = Resources.Load ("player") as GameObject;
		public GameObject modelFab = Resources.Load ("modelObject") as GameObject;
		public GameObject posModFab = Resources.Load ("posMod") as GameObject;

		[MenuItem("Window/RRTMapper")]
		static void Init () {
			PlatformerEditorWindow window = (PlatformerEditorWindow)EditorWindow.GetWindow (typeof(PlatformerEditorWindow));
			window.title = "RRTMapper";
			window.ShowTab ();
		}

		void OnGUI () {
			if (GUILayout.Button ("Monte-Carlo Tree Search")) {
				realFrame = 0;
				curFrame = 0;
				startingLoc = GameObject.Find ("startingPosition").transform.position;
				goalLoc = GameObject.Find("goalPosition").transform.position;
				multiMCTSearch(startingLoc, goalLoc, new PlayerState());
			}
			if (GUILayout.Button ("Print Solution")) {
				printSolution();
			}
			numPlayers = EditorGUILayout.IntSlider ("Number of Players", numPlayers, 1, 100);
			numIters = EditorGUILayout.IntSlider ("Iterations Per Player", numIters, 1, 100);
			depthIter = EditorGUILayout.IntSlider ("Max Depth per Iteration", depthIter, 1, 1000);
			maxDistRTNodes = EditorGUILayout.FloatField ("Max Dist RRT Nodes", maxDistRTNodes);

			
			
			
			/*playerFab = (GameObject)EditorGUILayout.ObjectField ("player prefab", playerFab, typeof(GameObject), true);
			modelFab = (GameObject)EditorGUILayout.ObjectField ("modelObject prefab", modelFab, typeof(GameObject), true);
			posModFab = (GameObject)EditorGUILayout.ObjectField ("posMod prefab", posModFab, typeof(GameObject), true);
			nodMarFab = (GameObject)EditorGUILayout.ObjectField ("node marker prefab", nodMarFab, typeof(GameObject), true);*/

			showDeaths = EditorGUILayout.Toggle ("Show Deaths", showDeaths);
			drawPaths = EditorGUILayout.Toggle ("Draw Paths", drawPaths);

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


			if (GUILayout.Button ("RRT")) {
				realFrame = 0;
				curFrame = 0;
				RRT();
			}

		}

		public void Update(){
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
		}



		private void multiMCTSearch(Vector3 startLoc, Vector3 golLoc, PlayerState state){
			cleanUp();
			count = 0;

			for(int i = 0; i < numPlayers; i++){
				MCTSearch(startLoc, golLoc, state);
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

		private void resetState(movementModel model, PlayerState state){
			model.state.adjustmentVelocity.x = state.adjustmentVelocity.x;
			model.state.adjustmentVelocity.y = state.adjustmentVelocity.y;
			model.state.isOnGround = state.isOnGround;
			model.state.velocity.x = state.velocity.x;
			model.state.velocity.y = state.velocity.y;
			model.state.numJumps = state.numJumps;
		}


		private RTNode MCTSearch(Vector3 startLoc,Vector3 golLoc, PlayerState state){
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


			int i = 0;
			bool foundAnswer = false;
			while(i < numIters && !foundAnswer){
				foundAnswer = MCTSearchIteration(startLoc, golLoc, state);
				i++;
			}
			if(foundAnswer)
			{
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

		private bool MCTSearchIteration(Vector3 startLoc,Vector3 golLoc, PlayerState state){
			//Debug.Log ("--------------------------------------------------------");
			player.transform.position = startLoc;
			mModel.initializev2();

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
				mModel.aIndex = 0;
				//mModel.state.reset();

				//resetState(mModel, state);
				mModel.resetState();


				player.transform.position = startLoc;
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

				mModel.numFrames = frames;
				if((player.transform.position - golLoc).magnitude < 0.5){
					//Debug.Log (player.transform.position);
					//player.transform.position = startingLoc;
					totalFrames = Mathf.Max(totalFrames, frames);
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

		private void printSolution(){
			int i = 0;
			while(i < mModel.durations.Count){
				Debug.Log (mModel.actions[i] + " -- " + mModel.durations[i]);
				i++;
			}
		}

		public bool[] goalReached;
		public int rrtIters = 10000000;
		public List<RTNode>[] rrtTrees;
		public RTNode[] roots;
		public RTNode[] goalNodes;


		public static float maxDistRTNodes = 5;

		private void RRT(){
			cleanUp();
			goalReached = new bool[numPlayers];
			rrtTrees = new List<RTNode>[numPlayers];
			roots = new RTNode[numPlayers];
			goalNodes = new RTNode[numPlayers];

			for(int j = 0; j < numPlayers; j++){

				goalReached[j] = false;
				startingLoc = GameObject.Find ("startingPosition").transform.position;
				goalLoc = GameObject.Find("goalPosition").transform.position;
				Vector3 bl = GameObject.Find ("bottomLeft").transform.position;
				Vector3 tr = GameObject.Find ("topRight").transform.position;
				rrtTrees[j] = new List<RTNode>();
				roots[j] = new RTNode(startingLoc, 0, new PlayerState());
				rrtTrees[j].Add (roots[j]);

				for(int i = 0; i < rrtIters; i++){
					float x = Random.Range (bl.x, tr.x);
					float y = Random.Range (bl.y, tr.y);
					tryAddNode(x,y, j);
					if(goalReached[j]){
						break;
					}
				}
			}
			cleanUp();
			reCreatePath();
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
					mModel.startState = new PlayerState();
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

		private bool tryAddNode(float x, float y, int j){
			RTNode closest = findClosest(x,y, j);
			if(closest == null){
				//Debug.Log ("Too far away");
				return false;
			}
			else{
				//Debug.Log (closest.state);
				RTNode final = MCTSearch(new Vector3(closest.position.x, closest.position.y, 10), new Vector3(x, y, 10), closest.state);

				if(final != null){
					//Debug.Log ("Node Added");
					final.parent = closest;
					closest.children.Add (final);
					final.frame = closest.frame + final.frame;
					rrtTrees[j].Add (final);
					if((new Vector3(final.position.x, final.position.y, 10) - goalLoc).magnitude < 0.5){
						goalReached[j] = true;
						goalNodes[j] = final;
					}
					else{
						goalReached[j] = tryAddGoalNode(final, j);
					}




					return true;
				}
				else{
					//Debug.Log ("MCTSearch unsuccesful");
					return false;
				}
			}
		}

		private bool tryAddGoalNode(RTNode node, int j){
			if(Vector2.Distance (node.position, new Vector2(goalLoc.x, goalLoc.y)) > maxDistRTNodes){
				return false;
			}
			else{
				RTNode final = MCTSearch(new Vector3(node.position.x, node.position.y, 10), goalLoc, node.state);
				if(final != null){
					final.parent = node;
					node.children.Add (final);
					final.frame = node.frame + final.frame;
					rrtTrees[j].Add (final);
					goalNodes[j] = final;
					return true;
				}
				else{
					return false;
				}
			}
		}


		private RTNode findClosest(float x,float y, int j){
			float minDist = maxDistRTNodes;
			float dist;
			RTNode curNode = null;
			foreach(RTNode node in rrtTrees[j]){
				dist = Vector2.Distance(node.position, new Vector2(x,y));
				if(dist < minDist){
					minDist = dist;
					curNode = node;
				}
			}
			return curNode;
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
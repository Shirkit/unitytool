


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
		public static int numIters = 10;
		public static int depthIter = 250;
		public static int totalFrames = 0;
		public static int curFrame;
		public static string filename;
		public static string destCount;

		public List<movementModel> mModels;
		public List<posMovModel> pmModels;

		public int count = 0;


		public GameObject playerFab;
		public GameObject modelFab;
		public GameObject posModFab;

		[MenuItem("Window/RRTMapper")]
		static void Init () {
			PlatformerEditorWindow window = (PlatformerEditorWindow)EditorWindow.GetWindow (typeof(PlatformerEditorWindow));
			window.title = "RRTMapper";
			window.ShowTab ();
		}

		void OnGUI () {
			if (GUILayout.Button ("Monte-Carlo Tree Search")) {
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


			playerFab = (GameObject)EditorGUILayout.ObjectField ("player prefab", playerFab, typeof(GameObject), true);
			modelFab = (GameObject)EditorGUILayout.ObjectField ("modelObject prefab", modelFab, typeof(GameObject), true);
			posModFab = (GameObject)EditorGUILayout.ObjectField ("posMod prefab", posModFab, typeof(GameObject), true);

			showDeaths = EditorGUILayout.Toggle ("Show Deaths", showDeaths);
			drawPaths = EditorGUILayout.Toggle ("Draw Paths", drawPaths);

			if (GUILayout.Button ("Clear")) {
				cleanUp();
			}

			curFrame = EditorGUILayout.IntSlider ("frame", curFrame, 0, totalFrames);

			if (GUILayout.Button ("Go To Frame")) {
				goToFrame(curFrame);
			}

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

		}

		public void Update(){
			if(playing){
				if(curFrame <= totalFrames){
					curFrame++;
					foreach(movementModel model in mModels){
						if(model != null){
							if(model.updater())
							{
								model.doAction("wait", 1);
							}
						}
					}
					foreach(posMovModel pModel in pmModels){
						if(pModel != null){
							pModel.goToFrame(curFrame);
						}
					}
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
					model.aIndex = 0;
					model.player.transform.position = startingLoc;
					model.state.reset ();
					model.runFrames(curFrame);
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
			players = new GameObject("players");
			models = new GameObject("models");
			paths = new GameObject("paths");
			posMods = new GameObject("posMods");
			paths.transform.parent = players.transform;
			models.transform.parent = players.transform;
			posMods.transform.parent = players.transform;

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
			//mModel.state = state.clone ();
			mModels.Add (mModel);
			int i = 0;
			bool foundAnswer = false;
			while(i < numIters && !foundAnswer){
				foundAnswer = MCTSearchIteration(startLoc, golLoc, state);
				i++;
			}
			if(foundAnswer)
			{
				RTNode toReturn = new RTNode();
				toReturn.position = player.transform.position;
				toReturn.state = mModel.state;
				toReturn.actions = mModel.actions;
				toReturn.durations = mModel.durations;
				toReturn.frame = mModel.numFrames;
				mModel.aIndex = 0;
				player.transform.position = startLoc;
				mModel.state.reset ();
				//mModel.state = state.clone ();
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
			mModel.initialize();
			mModel.color = new Color(Random.Range (0f, 1f), Random.Range (0f, 1f), Random.Range (0f, 1f));

			var tempMaterial = new Material(player.renderer.sharedMaterial);
			tempMaterial.color = mModel.color;
			player.renderer.sharedMaterial = tempMaterial;

			int count = 0;
			while(!mModel.dead && count < depthIter){
				int action = Random.Range (0, 6);
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
				mModel.state.reset();
				mModel.state.reset();
				player.transform.position = startLoc;
				Debug.Log (player.transform.position);
				Debug.Log (mModel.player.transform.position);
				int frames = mModel.loopUpdate();
				Debug.Log ("LOOP UPDATE");
				Debug.Log (player.transform.position);
				Debug.Log (mModel.player.transform.position);
				mModel.numFrames = frames;
				if((player.transform.position - golLoc).magnitude < 0.5){
					//Debug.Log (player.transform.position);
					//player.transform.position = startingLoc;
					totalFrames = Mathf.Max(totalFrames, frames);
					return true;
				}
				else{
					Debug.Log (golLoc);
					Debug.Log ((player.transform.position - golLoc).magnitude);
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

		public bool goalReached = false;
		public int rrtIters = 1000;
		public List<RTNode> rrtTree;
		public RTNode root;

		public float maxDistRTNodes;

		private void RRT(){
			startingLoc = GameObject.Find ("startingPosition").transform.position;
			goalLoc = GameObject.Find("goalPosition").transform.position;
			Vector3 bl = GameObject.Find ("bottomLeft").transform.position;
			Vector3 tr = GameObject.Find ("topRight").transform.position;
			rrtTree = new List<RTNode>();
			root = new RTNode(startingLoc, 0, new PlayerState());
			rrtTree.Add (root);

			for(int i = 0; i < rrtIters; i++){
				tryAddNode(Random.Range (bl.x, tr.x), Random.Range (bl.y, tr.y));
				if(goalReached){
					break;
				}
			}
			if(goalReached){
				Debug.Log ("Success");
			}
			else{
				Debug.Log ("Failure");
			}
		}

		private bool tryAddNode(float x, float y){
			RTNode closest = findClosest(x,y);
			if(closest == null){
				return false;
			}
			else{
				RTNode final = MCTSearch(new Vector3(closest.position.x, closest.position.y, 10), new Vector3(x, y, 10), closest.state);
				if(final != null){
					final.parent = closest;
					closest.children.Add (final);
					final.frame = closest.frame + final.frame;
					rrtTree.Add (final);
					if((new Vector3(final.position.x, final.position.y, 10) - goalLoc).magnitude < 0.5){
						goalReached = true;
					}
					return true;
				}
				else{
					return false;
				}
			}
		}

		private RTNode findClosest(float x,float y){
			float minDist = maxDistRTNodes;
			float dist;
			RTNode curNode = null;
			foreach(RTNode node in rrtTree){
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
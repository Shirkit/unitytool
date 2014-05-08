


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
		public GameObject paths;
		//public GameObject pathFab;
		public GameObject player;
		public GameObject modelObj;
		public Vector3 startingLoc;
		public Vector3 goalLoc;
		public bool showDeaths;
		public bool drawPaths;
		public bool playing = false;
		private movementModel mModel;
		public static int numPlayers = 1;
		public static int numIters = 10;
		public static int depthIter = 250;
		public static int totalFrames = 0;
		public static int curFrame;
		public static string filename;
		public static string destCount;

		public List<movementModel> mModels;

		public int count = 0;


		public GameObject playerFab;
		public GameObject modelFab;

		[MenuItem("Window/RRTMapper")]
		static void Init () {
			PlatformerEditorWindow window = (PlatformerEditorWindow)EditorWindow.GetWindow (typeof(PlatformerEditorWindow));
			window.title = "RRTMapper";
			window.ShowTab ();
		}

		void OnGUI () {
			if (GUILayout.Button ("Monte-Carlo Tree Search")) {
				multiMCTSearch();
			}
			if (GUILayout.Button ("Print Solution")) {
				printSolution();
			}
			numPlayers = EditorGUILayout.IntSlider ("Number of Players", numPlayers, 1, 100);
			numIters = EditorGUILayout.IntSlider ("Iterations Per Player", numIters, 1, 100);
			depthIter = EditorGUILayout.IntSlider ("Max Depth per Iteration", depthIter, 1, 1000);


			playerFab = (GameObject)EditorGUILayout.ObjectField ("player prefab", playerFab, typeof(GameObject), true);
			modelFab = (GameObject)EditorGUILayout.ObjectField ("modelObject prefab", modelFab, typeof(GameObject), true);
			//pathFab = (GameObject)EditorGUILayout.ObjectField ("path prefab", pathFab, typeof(GameObject), true);

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

			filename = EditorGUILayout.TextField("filename: ", filename);
			destCount = EditorGUILayout.TextField("destCount: ", destCount);

			if (GUILayout.Button ("Import Path")) {
				importPath(filename, destCount);
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
				drawPath();
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



		private void multiMCTSearch(){
			cleanUp();
			count = 0;
			for(int i = 0; i < numPlayers; i++){
				MCTSearch();
				count++;
			}
		}

		private void cleanUp(){
			DestroyImmediate(GameObject.Find ("players"));
			players = new GameObject("players");
			models = new GameObject("models");
			paths = new GameObject("paths");
			paths.transform.parent = players.transform;
			models.transform.parent = players.transform;
		}

		private void MCTSearch(){
			modelObj = Instantiate(modelFab) as GameObject;
			modelObj.name = "modelObject" + count;
			modelObj.transform.parent = models.transform;
			player = Instantiate(playerFab) as GameObject;
			player.name = "player" + count;
			player.transform.parent = players.transform;
			mModel = modelObj.GetComponent<movementModel>() as movementModel;
			mModel.player = player;
			mModels.Add (mModel);
			startingLoc = GameObject.Find ("startingPosition").transform.position;
			goalLoc = GameObject.Find("goalPosition").transform.position;
			int i = 0;
			bool foundAnswer = false;
			while(i < numIters && !foundAnswer){
				foundAnswer = MCTSearchIteration();
				i++;
			}
			if(foundAnswer)
			{
				/*Vector3 oldPos = player.transform.position;
				mModel.aIndex = 0;
				mModel.state.reset();
				player.transform.position = startingLoc;
				mModel.loopUpdate();
				if((player.transform.position - goalLoc).magnitude < 0.5){
				}
				else{
				Debug.Log ("old" + oldPos);
				Debug.Log ("??????????????" + player.transform.position);
				}*/

				mModel.aIndex = 0;
				player.transform.position = startingLoc;
				mModel.state.reset ();
				if(drawPaths){
					drawPath();
				}

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
					mModel.state.reset ();
					if(drawPaths){
						drawPath();
					}
				}
			}

		}

		private bool MCTSearchIteration(){
			//Debug.Log ("--------------------------------------------------------");
			player.transform.position = startingLoc;
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
				player.transform.position = startingLoc;
				int frames = mModel.loopUpdate();
				mModel.numFrames = frames;
				if((player.transform.position - goalLoc).magnitude < 0.5){
					//Debug.Log (player.transform.position);
					//player.transform.position = startingLoc;
					totalFrames = Mathf.Max(totalFrames, frames);
					return true;
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

		private void drawPath(){
			/*GameObject path = Instantiate(pathFab) as GameObject;
			path.transform.parent = paths.transform;
			path.name = "path" + count;
			LineRenderer pathRend = path.GetComponent<LineRenderer>();

			var tempMaterial = new Material(pathRend.sharedMaterial);
			tempMaterial.color = mModel.color;
			pathRend.sharedMaterial = tempMaterial;

			//pathRend.material.color = mModel.color;*/
			//pathRend.SetPosition(0, player.transform.position);

			List<Vector3> pointsList = new List<Vector3>();
			pointsList.Add(new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z));
			//int numVerts = 1;
			bool finished = false;
			while(!finished){
				finished = mModel.runFrames(5);
				//numVerts++;
				pointsList.Add(new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z));
				//pathRend.SetVertexCount(numVerts);
				//pathRend.SetPosition (numVerts-1, player.transform.position);
			}

			Vector3[] pointsArray = new Vector3[pointsList.Count];
			int i = 0;
			foreach(Vector3 point in pointsList){
				pointsArray[i] = point;
				i++;
			}

			VectorLine line = new VectorLine("path" + count, pointsArray, mModel.color, null, 2.0f, LineType.Continuous);
			line.Draw3D();
			line.vectorObject.transform.parent = paths.transform;
			mModel.aIndex = 0;
			player.transform.position = startingLoc;
			mModel.state.reset ();
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
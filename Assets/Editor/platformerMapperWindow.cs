using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace EditorArea {
	public class PlatformerEditorWindow : EditorWindow  {

		public GameObject player;
		public GameObject modelObj;
		public Vector3 startingLoc;
		public Vector3 goalLoc;
		public bool showDeaths;
		private movementModel mModel;
		public static int numPlayers;

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

			playerFab = (GameObject)EditorGUILayout.ObjectField ("player prefab", playerFab, typeof(GameObject), true);
			modelFab = (GameObject)EditorGUILayout.ObjectField ("modelObject prefab", modelFab, typeof(GameObject), true);
			showDeaths = EditorGUILayout.Toggle ("Show Deaths", showDeaths);
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
			for(int i = 0; i <= count; i++){
				DestroyImmediate(GameObject.Find ("player" + i));
				DestroyImmediate(GameObject.Find ("modelObject" + i));
			}
		}

		private void MCTSearch(){
			modelObj = Instantiate(modelFab) as GameObject;
			modelObj.name = "modelObject" + count;
			player = Instantiate(playerFab) as GameObject;
			player.name = "player" + count;
			mModel = modelObj.GetComponent<movementModel>() as movementModel;
			mModel.player = player;
			startingLoc = GameObject.Find ("startingPosition").transform.position;
			goalLoc = GameObject.Find("goalPosition").transform.position;
			int i = 0;
			bool foundAnswer = false;
			while(i < 10 && !foundAnswer){
				foundAnswer = MCTSearchIteration();
				i++;
			}
			if(foundAnswer)
			{
				//movementModel playerMModel = player.GetComponent<movementModel>() as movementModel;
				//playerMModel.actions = mModel.actions;
				//playerMModel.durations = mModel.durations;
				//Debug.Log (goalLoc);
				//for(int j = 0; j < 10; j++){

					Vector3 oldPos = player.transform.position;
					mModel.aIndex = 0;
					mModel.state.reset();
					player.transform.position = startingLoc;
					mModel.loopUpdate();
					if((player.transform.position - goalLoc).magnitude < 0.5){
					}
					else{
						//Debug.Log ("ATTEMPT" + j + "------");
						Debug.Log ("old" + oldPos);
						Debug.Log ("??????????????" + player.transform.position);
					}

				//}

				//Debug.Log ("Found a solution" + i);
				mModel.aIndex = 0;
				player.transform.position = startingLoc;
				mModel.state.reset ();
				//playerMovement pMov = player.GetComponent(typeof(playerMovement)) as playerMovement;
				//pMov.model = mModel;
				//Debug.Log(pMov.model);
				//Debug.Log ((player.GetComponent(typeof(playerMovement)) as playerMovement).model);
				//Debug.Log ((player.GetComponent(typeof(playerMovement)) as playerMovement).setModel(mModel));
			}
			else{
				//Debug.Log ("No solution found" + i);
				//Debug.Log (player.transform.position);
				if(!showDeaths){
					DestroyImmediate(GameObject.Find ("player" + count));
					DestroyImmediate(GameObject.Find ("modelObject" + count));
				}
				else{
					player.transform.position = startingLoc;
				}
			}

		}

		private bool MCTSearchIteration(){
			//Debug.Log ("--------------------------------------------------------");
			player.transform.position = startingLoc;
			mModel.initialize();
			while(!mModel.dead){
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
				mModel.loopUpdate();
				if((player.transform.position - goalLoc).magnitude < 0.5){
					//Debug.Log (player.transform.position);
					//player.transform.position = startingLoc;
					return true;
				}
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



	}
}
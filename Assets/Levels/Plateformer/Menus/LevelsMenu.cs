using UnityEngine;
using System.Collections;

//This should be level 1.

public class LevelsMenu : MonoBehaviour {

	void OnGUI() {
		//Main menu button
		if(GUI.Button(new Rect(Screen.width*.4f, Screen.height*.8f, Screen.width*.2f, Screen.height*.12f), "Main Menu")) {
			Application.LoadLevel(3);
		}
		//Level 1
		if(GUI.Button(new Rect(Screen.width*.2f, Screen.height*.28f, Screen.width*.2f, Screen.height*.12f), "Level 1")) {
			PlayerInfo.curLevel = 1;
			Application.LoadLevel(5);
		}
		//Level 2
		if(GUI.Button(new Rect(Screen.width*.4f, Screen.height*.28f, Screen.width*.2f, Screen.height*.12f), "Level 2")) {
			PlayerInfo.curLevel = 2;
			Application.LoadLevel(6);
		}
		//Level 3
		if(GUI.Button(new Rect(Screen.width*.6f, Screen.height*.28f, Screen.width*.2f, Screen.height*.12f), "Level 3")) {
			PlayerInfo.curLevel = 3;
			Application.LoadLevel(7);
		}
		//Level 1
		if(GUI.Button(new Rect(Screen.width*.2f, Screen.height*.4f, Screen.width*.2f, Screen.height*.12f), "Level 4")) {
			PlayerInfo.curLevel = 4;
			Application.LoadLevel(8);
		}
		//Level 2
		if(GUI.Button(new Rect(Screen.width*.4f, Screen.height*.4f, Screen.width*.2f, Screen.height*.12f), "Level 5")) {
			PlayerInfo.curLevel = 5;
			Application.LoadLevel(9);
		}
		//Level 3
		if(GUI.Button(new Rect(Screen.width*.6f, Screen.height*.4f, Screen.width*.2f, Screen.height*.12f), "Level 6")) {
			PlayerInfo.curLevel = 6;
			Application.LoadLevel(10);
		}
		//Level 1
		if(GUI.Button(new Rect(Screen.width*.2f, Screen.height*.52f, Screen.width*.2f, Screen.height*.12f), "Level 7")) {
			PlayerInfo.curLevel = 7;
			Application.LoadLevel(11);
		}
		//Level 2
		if(GUI.Button(new Rect(Screen.width*.4f, Screen.height*.52f, Screen.width*.2f, Screen.height*.12f), "Level 8")) {
			PlayerInfo.curLevel = 8;
			Application.LoadLevel(12);
		}
		//Level 3
		if(GUI.Button(new Rect(Screen.width*.6f, Screen.height*.52f, Screen.width*.2f, Screen.height*.12f), "Level 9")) {
			PlayerInfo.curLevel = 9;
			Application.LoadLevel(13);
		}
		GUI.Label(new Rect(Screen.width*.43f, Screen.height*.1f, Screen.width*.14f, Screen.height*.2f), "<size=" + Screen.width*.025 +">Select Level</size>");
	}
}

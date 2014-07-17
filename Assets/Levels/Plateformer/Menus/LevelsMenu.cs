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
		if(GUI.Button(new Rect(Screen.width*.2f, Screen.height*.4f, Screen.width*.2f, Screen.height*.12f), "Level 1")) {
			PlayerInfo.curLevel = 1;
			Application.LoadLevel(5);
		}
		//Level 2
		if(GUI.Button(new Rect(Screen.width*.4f, Screen.height*.4f, Screen.width*.2f, Screen.height*.12f), "Level 2")) {
			PlayerInfo.curLevel = 2;
			Application.LoadLevel(6);
		}
		//Level 3
		if(GUI.Button(new Rect(Screen.width*.6f, Screen.height*.4f, Screen.width*.2f, Screen.height*.12f), "Level 3")) {
			PlayerInfo.curLevel = 3;
			Application.LoadLevel(7);
		}
		GUI.Label(new Rect(Screen.width*.43f, Screen.height*.1f, Screen.width*.14f, Screen.height*.2f), "<size=" + Screen.width*.025 +">Select Level</size>");
	}
}

using UnityEngine;
using System.Collections;

public class LevelDifficulty : MonoBehaviour {
	
	void OnGUI(){
		
		GUI.Label(new Rect(Screen.width * .4f, Screen.height * .2f, Screen.width * .25f, Screen.height * .1f), "Please rate the difficulty of the level from 1 to 5");
		
		if(GUI.Button(new Rect(Screen.width * .4f, Screen.height * .3f, Screen.width * .25f, Screen.height * .1f), "Difficulty Level: 1 - Simple")) {
			PlayerInfo.diffs[PlayerInfo.curLevel-2] = 1;
			if(PlayerInfo.curLevel == 10){
				Application.LoadLevel(3);
			}
			Application.LoadLevel(PlayerInfo.curLevel+5);
		}
		if(GUI.Button(new Rect(Screen.width * .4f, Screen.height * .4f, Screen.width * .25f, Screen.height * .1f), "Difficulty Level: 2")) {
			PlayerInfo.diffs[PlayerInfo.curLevel-2] = 2;
			if(PlayerInfo.curLevel == 10){
				Application.LoadLevel(3);
			}
			Application.LoadLevel(PlayerInfo.curLevel+5);
		}
		if(GUI.Button(new Rect(Screen.width * .4f, Screen.height * .5f, Screen.width * .25f, Screen.height * .1f), "Difficulty Level: 3")) {
			PlayerInfo.diffs[PlayerInfo.curLevel-2] = 3;
			if(PlayerInfo.curLevel == 10){
				Application.LoadLevel(3);
			}
			Application.LoadLevel(PlayerInfo.curLevel+5);
		}
		if(GUI.Button(new Rect(Screen.width * .4f, Screen.height * .6f, Screen.width * .25f, Screen.height * .1f), "Difficulty Level: 4")) {
			PlayerInfo.diffs[PlayerInfo.curLevel-2] = 4;
			if(PlayerInfo.curLevel == 10){
				Application.LoadLevel(3);
			}
			Application.LoadLevel(PlayerInfo.curLevel+5);
		}
		if(GUI.Button(new Rect(Screen.width * .4f, Screen.height * .7f, Screen.width * .25f, Screen.height * .1f), "Difficulty Level: 5 - Difficult")) {
			PlayerInfo.diffs[PlayerInfo.curLevel-2] = 5;
			if(PlayerInfo.curLevel == 10){
				Application.LoadLevel(3);
			}
			Application.LoadLevel(PlayerInfo.curLevel+5);
		}
		
		
	}
}

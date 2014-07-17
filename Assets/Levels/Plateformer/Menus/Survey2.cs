using UnityEngine;
using System.Collections;

public class Survey2 : MonoBehaviour {

	void OnGUI(){
		
		GUI.Label(new Rect(Screen.width * .4f, Screen.height * .2f, Screen.width * .2f, Screen.height * .1f), "Please rate your problem-solving ability from 1 to 5");
		
		if(GUI.Button(new Rect(Screen.width * .4f, Screen.height * .3f, Screen.width * .2f, Screen.height * .1f), "Skill Level: 1 - Pathetic")) {
			PlayerInfo.ansQ2 = 1;
			Application.LoadLevel(3);
		}
		if(GUI.Button(new Rect(Screen.width * .4f, Screen.height * .4f, Screen.width * .2f, Screen.height * .1f), "Skill Level: 2")) {
			PlayerInfo.ansQ2 = 2;
			Application.LoadLevel(3);
		}
		if(GUI.Button(new Rect(Screen.width * .4f, Screen.height * .5f, Screen.width * .2f, Screen.height * .1f), "Skill Level: 3")) {
			PlayerInfo.ansQ2 = 3;
			Application.LoadLevel(3);
		}
		if(GUI.Button(new Rect(Screen.width * .4f, Screen.height * .6f, Screen.width * .2f, Screen.height * .1f), "Skill Level: 4")) {
			PlayerInfo.ansQ2 = 4;
			Application.LoadLevel(3);
		}
		if(GUI.Button(new Rect(Screen.width * .4f, Screen.height * .7f, Screen.width * .2f, Screen.height * .1f), "Skill Level: 5 - Masterful")) {
			PlayerInfo.ansQ2 = 5;
			Application.LoadLevel(3);
		}
		
		
	}
}

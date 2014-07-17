using UnityEngine;
using System.Collections;

public class SurveyEntry : MonoBehaviour {

	public string username = "username";
	void OnGUI(){

		username = GUI.TextField(new Rect(Screen.width * .4f, Screen.height * .5f, Screen.width * .2f, Screen.height * .1f), username, 25);

		if(GUI.Button(new Rect(Screen.width * .4f, Screen.height * .4f, Screen.width * .2f, Screen.height * .1f), "Enter Username and Click Here")) {
			if(username.Length > 1 && !username.Equals("username")){
				PlayerInfo.username = username;
				Application.LoadLevel(1);
			}
		}


	}

}

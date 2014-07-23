using UnityEngine;
using System.Collections;

//This should be in level 0

public class MainMenu : MonoBehaviour {

	public string comments = "If you have comments, please enter them here\n Note: You will be taken back to this page after you have played all of the levels.";

	void OnGUI() {
		if(GUI.Button(new Rect(Screen.width * .4f, Screen.height * .4f, Screen.width * .2f, Screen.height * .1f), "Play")) {
			Application.LoadLevel(4);
		}
		if(GUI.Button (new Rect(Screen.width*.35f, Screen.height*.55f, Screen.width*.3f, Screen.height*.1f), "Click Here to Submit Comments")){
			submit();
		}

		comments = GUI.TextArea(new Rect(Screen.width *.2f, Screen.height*.65f, Screen.width*.6f, Screen.height*.3f), comments);

	}

	private void submit(){

	}
}

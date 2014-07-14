using UnityEngine;
using System.Collections;

//This should be in level 0

public class MainMenu : MonoBehaviour {

	void OnGUI() {
		if(GUI.Button(new Rect(Screen.width * .4f, Screen.height * .4f, Screen.width * .2f, Screen.height * .1f), "Play")) {
			Application.LoadLevel(1);
		}
	}

}

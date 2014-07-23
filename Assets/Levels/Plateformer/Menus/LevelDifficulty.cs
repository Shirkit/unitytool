using UnityEngine;
using System.Collections;

public class LevelDifficulty : MonoBehaviour {
	
	void OnGUI(){
		
		GUI.Label(new Rect(Screen.width * .4f, Screen.height * .2f, Screen.width * .25f, Screen.height * .1f), "Please rate the difficulty of the level from 1 to 5");
		
		if(GUI.Button(new Rect(Screen.width * .4f, Screen.height * .3f, Screen.width * .25f, Screen.height * .1f), "Difficulty Level: 1 - Simple")) {
			PlayerInfo.diffs[PlayerInfo.curLevel-2] = 1;
			sendData(userMovement.title + "," + PlayerInfo.diffs[PlayerInfo.curLevel-2], userMovement.data);
			if(PlayerInfo.curLevel == 10){
				Application.LoadLevel(3);
			}
			Application.LoadLevel(PlayerInfo.curLevel+5);
		}
		if(GUI.Button(new Rect(Screen.width * .4f, Screen.height * .4f, Screen.width * .25f, Screen.height * .1f), "Difficulty Level: 2")) {
			PlayerInfo.diffs[PlayerInfo.curLevel-2] = 2;
			sendData(userMovement.title + "," + PlayerInfo.diffs[PlayerInfo.curLevel-2], userMovement.data);
			if(PlayerInfo.curLevel == 10){
				Application.LoadLevel(3);
			}
			Application.LoadLevel(PlayerInfo.curLevel+5);
		}
		if(GUI.Button(new Rect(Screen.width * .4f, Screen.height * .5f, Screen.width * .25f, Screen.height * .1f), "Difficulty Level: 3")) {
			PlayerInfo.diffs[PlayerInfo.curLevel-2] = 3;
			sendData(userMovement.title + "," + PlayerInfo.diffs[PlayerInfo.curLevel-2], userMovement.data);
			if(PlayerInfo.curLevel == 10){
				Application.LoadLevel(3);
			}
			Application.LoadLevel(PlayerInfo.curLevel+5);
		}
		if(GUI.Button(new Rect(Screen.width * .4f, Screen.height * .6f, Screen.width * .25f, Screen.height * .1f), "Difficulty Level: 4")) {
			PlayerInfo.diffs[PlayerInfo.curLevel-2] = 4;
			sendData(userMovement.title + "," + PlayerInfo.diffs[PlayerInfo.curLevel-2], userMovement.data);
			if(PlayerInfo.curLevel == 10){
				Application.LoadLevel(3);
			}
			Application.LoadLevel(PlayerInfo.curLevel+5);
		}
		if(GUI.Button(new Rect(Screen.width * .4f, Screen.height * .7f, Screen.width * .25f, Screen.height * .1f), "Difficulty Level: 5 - Difficult")) {
			PlayerInfo.diffs[PlayerInfo.curLevel-2] = 5;
			sendData(userMovement.title + "," + PlayerInfo.diffs[PlayerInfo.curLevel-2], userMovement.data);
			if(PlayerInfo.curLevel == 10){
				Application.LoadLevel(3);
			}
			Application.LoadLevel(PlayerInfo.curLevel+5);
		}
		
		
	}

	private IEnumerator sendData(string title, string data){
		string URL ="http://cgi.cs.mcgill.ca/~aborod3/writeResults.php";
		WWWForm form = new WWWForm();
		form.AddField ( "name", title);
		form.AddField ( "data", data);
		
		var headers = form.headers;
		
		if (!headers.Contains("Content-Type"))
		{
			headers.Add("Content-Type", "application/x-www-form-urlencoded");
		}
		
		WWW w = new WWW(URL, form.data, headers);
		yield return w;		

		//StartCoroutine(WaitForRequest(w));
		
		
	}
	
	
	
	IEnumerator WaitForRequest(WWW w){
		yield return w;		
		if(!string.IsNullOrEmpty(w.error)){
			Debug.Log ("WWW Error:" + w.error);
		}
		else{
			Debug.Log ("WWW Success" + w.text);
		}
	}


}

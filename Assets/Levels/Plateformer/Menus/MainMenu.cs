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

		if(display){
			if(GUI.Button(new Rect(Screen.width * .1f, Screen.height * .1f, Screen.width * .8f, Screen.height * .3f), webResults)) {
				
			}
		}
	}

	private string webResults;
	public bool display = false;

	private void submit(){
		string title = PlayerInfo.userid + "," + System.DateTime.Now + "\n";
		sendData(title, comments);


	}



	private void sendData(string title, string data){
		string URL ="http://cgi.cs.mcgill.ca/~aborod3/writeComment.php";
		WWWForm form = new WWWForm();
		form.AddField ( "name", title);
		form.AddField ( "data", data);
		
		var headers = form.headers;
		
		if (!headers.Contains("Content-Type"))
		{
			headers.Add("Content-Type", "application/x-www-form-urlencoded");
		}
		
		WWW w = new WWW(URL, form.data, headers);
		
		StartCoroutine(WaitForRequest(w));
		
		
	}
	
	
	
	IEnumerator WaitForRequest(WWW w){
		yield return w;
		
		if(!string.IsNullOrEmpty(w.error)){
			Debug.Log ("WWW Error:" + w.error);
			webResults = "WWW Error:" + w.error;
			display = true;
		}
		else{
			Debug.Log ("WWW Success" + w.text);
			webResults = "WWW Success" + w.text;
			display = true;
		}
	}
}

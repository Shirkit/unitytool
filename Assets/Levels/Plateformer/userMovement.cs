using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//using WWW;

public class userMovement : MonoBehaviour {

	public string action;
	public movementModel mov;
	public Vector3 goalLocation;
	public bool won;
	public bool died = false;
	public List<Vector2> path;
	//step, jump, die, win
	public  AudioClip sound1;
	public  AudioClip sound2;
	public  AudioClip sound3;
	public  AudioClip sound4;


	// Use this for initialization
	void Start () {
		mov = gameObject.GetComponent<movementModel>();
		mov.player = gameObject;
		mov.startLocation = gameObject.transform.position;
		mov.startState = mov.state;
		goalLocation  = GameObject.Find("goalPosition").transform.position;
		won = false;
		path = new List<Vector2>();
	}

	private int num = 0;

	// Update is called once per frame
	void Update () {


		if(!mov.dead){
			if((Input.GetKey (KeyCode.RightArrow) || Input.GetKey (KeyCode.D))){
				if((Input.GetKeyDown (KeyCode.UpArrow) || Input.GetKeyDown (KeyCode.W))){
					action = "jump right";
					audio.PlayOneShot (sound2);
					num = 0;
				}
				else{
					action = "Right";
					if(mov.state.isOnGround){
						num++;
						num = num % 8;
						if(num == 1){
							audio.PlayOneShot (sound1);
						}
					}
				}
			}
			else if((Input.GetKey (KeyCode.LeftArrow) || Input.GetKey (KeyCode.A))){
				if((Input.GetKeyDown (KeyCode.UpArrow) || Input.GetKeyDown (KeyCode.W))){
					action = "jump left";
					audio.PlayOneShot (sound2);
					num = 0;
				}
				else{
					action = "Left";
					if(mov.state.isOnGround){
						num++;
						num = num % 8;
						if(num == 1){
							audio.PlayOneShot (sound1);
						}
					}
				}
			}
			else if((Input.GetKeyDown (KeyCode.UpArrow) || Input.GetKeyDown (KeyCode.W))){
				action = "jump";
				audio.PlayOneShot (sound2);
				num = 0;
			}
			else{
				action = "wait";
				num = 0;
			}

			mov.doAction(action, 1);
			mov.movePlayer();
			mov.doCollisions();

			if(Vector3.Distance(gameObject.transform.position, goalLocation) < 0.5f){
				if(!won){
					audio.PlayOneShot (sound4);
					win();
				}
			}
			path.Add (gameObject.transform.position);
		
		}

		if(gameObject.transform.position.y < -1){
			mov.dead = true;
		}

		if(mov.dead){
			if(!died){
				audio.PlayOneShot (sound3);
				death();
			}
		}
	

		if(Input.GetKeyDown (KeyCode.R)){
			reset();
		}

		if(Input.GetKeyDown (KeyCode.Escape)){
			Application.LoadLevel (4);
		}

	}

	private void reset(){
		died = false;
		mov.dead = false;
		gameObject.transform.position =  mov.startLocation;
		mov.state = mov.startState;
		won = false;
		path = new List<Vector2>();
	}

	private void death(){
		died = true;
		string data = "";
		foreach(Vector2 pos in path){
			data += pos;
			data += "\n";
		}
		string title = System.DateTime.Now + "," + PlayerInfo.toStringIncr() + ",0";
		sendData(title, data);
	}

	private void win(){
		won  = true;
		//Record path somewhere....
		string data = "";
		foreach(Vector2 pos in path){
			data += pos;
			data += "\n";
		}
		string title = System.DateTime.Now + "," + PlayerInfo.toStringIncr() + ",1";
		sendData(title, data);
	}

	private string webResults;
	public int countDisplayed = 0;
	public bool display = false;

	private void sendData(string title, string data){
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


	void OnGUI(){
		if(mov.dead){
			if(GUI.Button(new Rect(Screen.width * .4f, Screen.height * .4f, Screen.width * .3f, Screen.height * .1f), "You Died, Click Here, or Press R to Restart")) {
				reset();
			}
		}
		else if(won){
			if(GUI.Button(new Rect(Screen.width * .4f, Screen.height * .4f, Screen.width * .3f, Screen.height * .1f), "You Won, Click Here, or Press R to Restart")) {
				reset();
			}
		}

		/*if(display){
			if(GUI.Button(new Rect(Screen.width * .1f, Screen.height * .1f, Screen.width * .8f, Screen.height * .3f), webResults)) {

			}
		}*/
		

	}

}

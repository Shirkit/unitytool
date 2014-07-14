using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class userMovement : MonoBehaviour {

	public string action;
	public movementModel mov;
	public Vector3 goalLocation;
	public bool won;
	public List<Vector2> path;

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
	
	// Update is called once per frame
	void Update () {
		if(!mov.dead){
			if((Input.GetKey (KeyCode.RightArrow) || Input.GetKey (KeyCode.D))){
				if((Input.GetKeyDown (KeyCode.UpArrow) || Input.GetKeyDown (KeyCode.W))){
					action = "jump right";
				}
				else{
					action = "Right";
				}
			}
			else if((Input.GetKey (KeyCode.LeftArrow) || Input.GetKey (KeyCode.A))){
				if((Input.GetKeyDown (KeyCode.UpArrow) || Input.GetKeyDown (KeyCode.W))){
					action = "jump left";
				}
				else{
					action = "Left";
				}
			}
			else if((Input.GetKeyDown (KeyCode.UpArrow) || Input.GetKeyDown (KeyCode.W))){
				action = "jump";
			}
			else{
				action = "wait";
			}

			mov.doAction(action, 1);
			mov.movePlayer();
			mov.doCollisions();

			if(Vector3.Distance(gameObject.transform.position, goalLocation) < 0.5f){
				if(!won){
					win();
				}
			}
			path.Add (gameObject.transform.position);
		
		}

		if(gameObject.transform.position.y < -1){
			mov.dead = true;
		}
	

		if(Input.GetKeyDown (KeyCode.R)){
			reset();
		}

		if(Input.GetKeyDown (KeyCode.Escape)){
			Application.LoadLevel (1);
		}

	}

	private void reset(){
		mov.dead = false;
		gameObject.transform.position =  mov.startLocation;
		mov.state = mov.startState;
		won = false;
		path = new List<Vector2>();
	}

	private void win(){
		won  = true;
		//Record path somewhere....
		foreach(Vector2 pos in path){
			Debug.Log (pos);
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

	}

}

using UnityEngine;
using System.Collections;

public class userMovement : MonoBehaviour {

	public string action;
	public movementModel mov;

	// Use this for initialization
	void Start () {
		mov = gameObject.GetComponent<movementModel>();
		mov.player = gameObject;
		mov.startLocation = gameObject.transform.position;
	}
	
	// Update is called once per frame
	void Update () {
		if(Input.GetKey (KeyCode.RightArrow)){
			if(Input.GetKeyDown (KeyCode.UpArrow)){
				action = "jump right";
			}
			else{
				action = "Right";
			}
		}
		else if(Input.GetKey (KeyCode.LeftArrow)){
			if(Input.GetKeyDown (KeyCode.UpArrow)){
				action = "jump left";
			}
			else{
				action = "Left";
			}
		}
		else if(Input.GetKeyDown (KeyCode.UpArrow)){
			action = "jump";
		}
		else{
			action = "wait";
		}

		mov.doAction(action, 1);
		mov.movePlayer();
		mov.doCollisions();

		if(gameObject.transform.position.y < -1){
			mov.dead = true;
		}

		if(mov.dead){
			mov.dead = false;
			gameObject.transform.position =  mov.startLocation;
			mov.state = new PlayerState();
		}

	}
}

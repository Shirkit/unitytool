using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class movementModel : MonoBehaviour {
	
	private int aIndex;
	private List<string> actions;
	private List<int> durations;
	private Dictionary<string, AbsAction> actionTypes;
	private PlayerState state;
	private Vector2 blCorner;
	private Vector2 trCorner;
	
	// Use this for initialization
	void Start () {
		state = new PlayerState();

		actionTypes = new Dictionary<string, AbsAction>();
		actions = new List<string>();
		durations = new List<int>();
		//Initialize action types
		pressLeftAction pla = new pressLeftAction(gameObject, state);
		pressRightAction pra = new pressRightAction(gameObject, state);
		pressNothingAction pna = new pressNothingAction(gameObject, state);
		pressUpAction pua = new pressUpAction(gameObject, state);
		pressUpLeftAction pula = new pressUpLeftAction(gameObject, state);
		pressUpRightAction pura = new pressUpRightAction(gameObject, state);
		actionTypes.Add ("jump left", pula);
		actionTypes.Add ("jump right", pura);
		actionTypes.Add("jump", pua);
		actionTypes.Add("wait", pna);
		actionTypes.Add("Left", pla);
		actionTypes.Add("Right", pra);
		

		//Initialize actions to be performed.
		actions.Add("Left");
		durations.Add(12);
		actions.Add("Left");
		durations.Add(20);
		actions.Add("Right");
		durations.Add(28);
		actions.Add("wait");
		durations.Add(30);
		actions.Add("jump");
		durations.Add(15);
		actions.Add("Left");
		durations.Add(36);
		actions.Add("jump left");
		durations.Add(1);
		actions.Add("wait");
		durations.Add(10);
		actions.Add("Right");
		durations.Add(16);
		actions.Add("wait");
		durations.Add(30);
		actions.Add("jump");
		durations.Add(15);		
		actions.Add("jump right");
		durations.Add(1);
		actions.Add("Right");
		durations.Add(12);
		actions.Add("Left");
		durations.Add(32);
		actions.Add("Right");
		durations.Add(60);
		actions.Add ("Right");
		durations.Add(60);
		actions.Add ("jump right");
		durations.Add (1);
		actions.Add ("Right");
		durations.Add (60);
		actions.Add ("jump right");
		durations.Add (1);
		actions.Add ("Right");
		durations.Add (60);
		actions.Add ("jump right");
		durations.Add (1);
		actions.Add ("Right");
		durations.Add (60);
		actions.Add ("jump right");
		durations.Add (1);
		actions.Add ("Right");
		durations.Add (90);
		actions.Add ("jump right");
		durations.Add (1);
		actions.Add ("Right");
		durations.Add (60);
		actions.Add ("jump right");
		durations.Add (1);
		actions.Add ("Right");
		durations.Add (60);
		actions.Add ("jump right");
		durations.Add (1);
		actions.Add ("Right");
		durations.Add (60);
		actions.Add ("wait");
		durations.Add (1);
		actions.Add ("jump");
		durations.Add (1);
		actions.Add ("wait");
		durations.Add (10);
		actions.Add ("jump");
		durations.Add (1);
		
		






		
		//Start first action
		aIndex = 0;		

	}
	
	// Update is called once per frame
	void FixedUpdate () {
		//loopUpdate();
		if(updater()){
			doAction("wait", 1);
		}



	}

	void loopUpdate (){
		while(!updater()){};
	}

	bool updater(){
		bool toReturn;
		if(aIndex < actions.Count){
			//Debug.Log("Performing action: " + actions[aIndex] + " for duration: " + durations[aIndex]);
			doAction(actions[aIndex], durations[aIndex]);
			toReturn = false;
		}
		else{
			toReturn = true;
		}

		movePlayer();
		doCollisions();

		return toReturn;

	}

	private void doAction(string aName, int aDuration){
		AbsAction action = actionTypes[aName];
		if(action.execute(aDuration)){
			aIndex++;
		}
	}

	private void movePlayer(){
		if(!state.isOnGround){
			state.velocity += state.gravity;
			gameObject.transform.position += (Vector3)state.velocity;
			gameObject.transform.position += (Vector3)state.adjustmentVelocity;
		}
		else{
			state.numJumps = 0;
			state.velocity.y = 0;
			gameObject.transform.position += (Vector3)state.velocity;
		}
	}

	private void doCollisions(){
		updateCorners();
		Collider2D coll = Physics2D.OverlapArea(blCorner, trCorner);
		/*if(coll != null){
			Debug.Log (coll.gameObject.name);
		}*/
		if(coll != null && coll.gameObject != gameObject){
			//Assume collision with ground and you are falling from on top of it.
			if(!state.isOnGround){
				state.isOnGround = true;
				gameObject.transform.position = new Vector3(gameObject.transform.position.x, (coll.gameObject.transform.position.y + coll.gameObject.transform.localScale.y*0.5f + gameObject.transform.localScale.y*0.5f + 0.01f), gameObject.transform.position.z);
			}
		}
	}

	private void updateCorners(){

		blCorner = new Vector2((gameObject.transform.position.x - 0.5f*gameObject.transform.localScale.x), (gameObject.transform.position.y - 0.5f*gameObject.transform.localScale.y));
		trCorner = new Vector2((gameObject.transform.position.x + 0.5f*gameObject.transform.localScale.x), (gameObject.transform.position.y + 0.5f*gameObject.transform.localScale.y));
	}

}

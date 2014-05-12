using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class movementModel : MonoBehaviour{
	
	public int aIndex;
	public List<string> actions;
	public List<int> durations;
	public Dictionary<string, AbsAction> actionTypes;
	public PlayerState state;
	public Vector2 blCorner;
	public Vector2 trCorner;
	public bool dead;
	public GameObject player;

	public movementModel(GameObject pPlayer){
		player = pPlayer;

		initialize();

	}

	void Awake(){
		//Debug.Log ("Awake");
		state = new PlayerState();
		aIndex = 0;
		dead = false;
		actionTypes = new Dictionary<string, AbsAction>();
		pressLeftAction pla = new pressLeftAction(player, state);
		pressRightAction pra = new pressRightAction(player, state);
		pressNothingAction pna = new pressNothingAction(player, state);
		pressUpAction pua = new pressUpAction(player, state);
		pressUpLeftAction pula = new pressUpLeftAction(player, state);
		pressUpRightAction pura = new pressUpRightAction(player, state);
		actionTypes.Add ("jump left", pula);
		actionTypes.Add ("jump right", pura);
		actionTypes.Add("jump", pua);
		actionTypes.Add("wait", pna);
		actionTypes.Add("Left", pla);
		actionTypes.Add("Right", pra);
	}

	public void initialize(){
		aIndex = 0;	
		state = new PlayerState();
		dead = false;
		actionTypes = new Dictionary<string, AbsAction>();
		actions = new List<string>();
		durations = new List<int>();
		//Initial Wait Time
		actions.Add ("wait");
		durations.Add (30);
		//Initialize action types
		pressLeftAction pla = new pressLeftAction(player, state);
		pressRightAction pra = new pressRightAction(player, state);
		pressNothingAction pna = new pressNothingAction(player, state);
		pressUpAction pua = new pressUpAction(player, state);
		pressUpLeftAction pula = new pressUpLeftAction(player, state);
		pressUpRightAction pura = new pressUpRightAction(player, state);
		actionTypes.Add ("jump left", pula);
		actionTypes.Add ("jump right", pura);
		actionTypes.Add("jump", pua);
		actionTypes.Add("wait", pna);
		actionTypes.Add("Left", pla);
		actionTypes.Add("Right", pra);
		player.transform.position = GameObject.Find("startingPosition").transform.position;
	}

	public void loopUpdate (){
		while(!updater()){};
	}



	public bool updater(){
		bool toReturn;
		if(aIndex < actions.Count){
			//Debug.Log (aIndex);
			//Debug.Log (actions.Count);
			//Debug.Log (durations.Count);
			//Debug.Log("Performing action: " + actions[aIndex] + " for duration: " + durations[aIndex]);
			doAction(actions[aIndex], durations[aIndex]);
			toReturn = false;
		}
		else{
			toReturn = true;
			//doAction("wait", 1);
		}

		movePlayer();
		doCollisions();
		if(player.transform.position.y < -1){
			dead = true;
		}
		return toReturn;

	}

	public void doAction(string aName, int aDuration){
		AbsAction action = actionTypes[aName];
		if(action.execute(aDuration)){
			aIndex++;
			//Debug.Log (aName + "--" + aDuration);
			//Debug.Log (player.transform.position);
		}
	}

	private void movePlayer(){
		if(!state.isOnGround){
			state.velocity += state.gravity;
			player.transform.position += (Vector3)state.velocity;
			player.transform.position += (Vector3)state.adjustmentVelocity;
		}
		else{
			state.velocity.y = 0;
			player.transform.position += (Vector3)state.velocity;
		}
	}

	private void doCollisions(){
		updateCorners();
		Collider2D coll = Physics2D.OverlapArea(blCorner, trCorner);

		if(coll != null && coll.gameObject != player){
			if(!state.isOnGround){

				if((state.velocity.y < 0.1f) && (coll.gameObject.transform.position.y + coll.gameObject.transform.localScale.y*0.5f + player.transform.localScale.y*0.5f + state.velocity.y + state.adjustmentVelocity.y) < player.transform.position.y + 0.1f){
					state.isOnGround = true;
					state.numJumps = 0;
					player.transform.position = new Vector3(player.transform.position.x, (coll.gameObject.transform.position.y + coll.gameObject.transform.localScale.y*0.5f + player.transform.localScale.y*0.5f - 0.1f), player.transform.position.z);
					state.velocity.y = 0;
				}
			}
		}
		else{
			state.isOnGround = false;
		}
	}

	private void updateCorners(){

		blCorner = new Vector2((player.transform.position.x - 0.5f*player.transform.localScale.x)-0.1f, (player.transform.position.y - 0.5f*player.transform.localScale.y)-0.1f);
		trCorner = new Vector2((player.transform.position.x + 0.5f*player.transform.localScale.x)+0.1f, (player.transform.position.y + 0.5f*player.transform.localScale.y)+0.1f);
	}

}

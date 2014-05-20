using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Vectrosity;

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
	public Color color;
	public int numFrames;
	public bool pathComputed = false;
	public Vector3[] pointsArray;

	public PlayerState startState;
	public Vector3 startLocation;

	public movementModel(){
	}

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

	public void resetState(){
		state.adjustmentVelocity.x = startState.adjustmentVelocity.x;
		state.adjustmentVelocity.y = startState.adjustmentVelocity.y;
		state.isOnGround = startState.isOnGround;
		state.velocity.x = startState.velocity.x;
		state.velocity.y = startState.velocity.y;
		state.numJumps = startState.numJumps;
	}
		
	public void initializev2(){
		aIndex = 0;	
		state = new PlayerState();
		state.adjustmentVelocity.x = startState.adjustmentVelocity.x;
		state.adjustmentVelocity.y = startState.adjustmentVelocity.y;
		state.isOnGround = startState.isOnGround;
		state.velocity.x = startState.velocity.x;
		state.velocity.y = startState.velocity.y;
		state.numJumps = startState.numJumps;
		
		
		
		
		
		dead = false;
		actionTypes = new Dictionary<string, AbsAction>();
		actions = new List<string>();
		durations = new List<int>();
		//Initial Wait Time
		//actions.Add ("wait");
		//durations.Add (30);
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


		player.transform.position = startLocation;
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

	public int loopUpdate (){
		int toReturn  = 0;
		while(!updater()){
			toReturn++;
		};
		return toReturn;
	}

	public bool runFrames(int num){
		bool toReturn = false;
		for(int i = 0; i < num; i++){
			toReturn = updater();
		}
		return toReturn;
	}

	public bool goToFrame(int fr){
		if(startState != null){
			resetState();
		}
		else{
			state.reset();
		}
		aIndex = 0;
		if(startLocation != null){
			player.transform.position = startLocation;
		}
		else{
			player.transform.position = GameObject.Find ("startingPosition").transform.position;
		}
		return runFrames(fr);
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
			//toReturn = true;

			return true;
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

	public void computePath(){
		List<Vector3> pointsList = new List<Vector3>();
		pointsList.Add(new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z));
		bool finished = false;
		while(!finished){
			finished = runFrames(5);
			pointsList.Add(new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z));
		}
		
		pointsArray = new Vector3[pointsList.Count];
		int i = 0;
		foreach(Vector3 point in pointsList){
			pointsArray[i] = point;
			i++;
		}
		aIndex = 0;
		player.transform.position = GameObject.Find("startingPosition").transform.position;
		state.reset ();
		pathComputed = true;
	}
	public void drawPath(GameObject paths){
		if(!pathComputed){
			computePath();
		}
		VectorLine line = new VectorLine("path" + gameObject.name.Substring(11), pointsArray, color, null, 2.0f, LineType.Continuous);
		line.Draw3D();
		line.vectorObject.transform.parent = paths.transform;
	}

	public void markMap(GameObject [,] hmapsqrs,Vector3 hmapbl,Vector3 hmaptr,float hmapinc){
		if(!pathComputed){
			computePath();
		}
		int x;
		int y;
		GameObject cube;
		Vector3 pt;
		foreach(Vector3 pnt in pointsArray){
			pt = pnt - hmapbl;
			x = Mathf.FloorToInt(pt.x/hmapinc);
			y = Mathf.FloorToInt (pt.y/hmapinc);
			cube = hmapsqrs[x,y];
			var tempMaterial = new Material(cube.renderer.sharedMaterial);
			tempMaterial.color = Color.black;
			cube.renderer.sharedMaterial = tempMaterial;
		}
	}

	public posMovModel toPosModel(){
		List<Vector3> positions = new List<Vector3>();
		player.transform.position = GameObject.Find("startingPosition").transform.position;
		state.reset();
		positions.Add (player.transform.position);
		while(!runFrames(5)){
			positions.Add (player.transform.position);
		}
		player.transform.position = GameObject.Find("startingPosition").transform.position;
		state.reset();
		return new posMovModel(player, positions);
	}

}

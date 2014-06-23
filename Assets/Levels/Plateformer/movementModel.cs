using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using Vectrosity;

public class movementModel : MonoBehaviour{
	
	public int aIndex;
	public int frame;
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
	public int startFrame;

	public HPlatMovement[] hplatmovers;
	public VPlatMovement[] vplatmovers;

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
		state.isOnGround = startState.isOnGround;
		state.velocity.x = startState.velocity.x;
		state.velocity.y = startState.velocity.y;
		state.platformVelocity.x = startState.platformVelocity.x;
		state.platformVelocity.y = startState.platformVelocity.y;
		state.numJumps = startState.numJumps;
	}
		
	public void initializev2(){
		aIndex = 0;	
		state = new PlayerState();
		state.isOnGround = startState.isOnGround;
		state.velocity.x = startState.velocity.x;
		state.velocity.y = startState.velocity.y;
		state.platformVelocity.x = startState.platformVelocity.x;
		state.platformVelocity.y = startState.platformVelocity.y;
		state.numJumps = startState.numJumps;
		frame = startFrame;
		
		
		
		
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
		frame = startFrame;
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
		frame++;
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

	public void movePlayer(){
		if(!state.isOnGround){
			state.velocity += state.gravity;
		}
		else{
			state.velocity.y = 0;
		}
		player.transform.position += (Vector3)state.velocity;
		player.transform.position += (Vector3)state.platformVelocity;
	}

	private LayerMask floor;// = 1 << LayerMask.NameToLayer("Floor");
	private LayerMask walls;// = 1 << LayerMask.NameToLayer("Walls");
	private LayerMask hplats;// = 1 << LayerMask.NameToLayer("HMovingPlatforms");
	private LayerMask vplats;// = 1 << LayerMask.NameToLayer("VMovingPlatforms");
	private LayerMask lethals;// = 1 << LayerMask.NameToLayer ("Lethals");
	private bool layerMasksLoaded = false;

	public void doCollisions(){
		if(!layerMasksLoaded){
			floor = 1 << LayerMask.NameToLayer("Floor");
			walls = 1 << LayerMask.NameToLayer("Walls");
			hplats = 1 << LayerMask.NameToLayer("HMovingPlatforms");
			vplats = 1 << LayerMask.NameToLayer("VMovingPlatforms");
			lethals = 1 << LayerMask.NameToLayer ("Lethals");
		}

		updateCorners();
		PlatsGoToFrame(frame);
		Collider2D collF= Physics2D.OverlapArea(blCorner, trCorner, floor);
		Collider2D collW = Physics2D.OverlapArea(blCorner, trCorner, walls);
		Collider2D collH = Physics2D.OverlapArea (blCorner, trCorner, hplats);
		Collider2D collV = Physics2D.OverlapArea(blCorner, trCorner, vplats);
		Collider2D collD = Physics2D.OverlapArea (blCorner, trCorner, lethals);
		if(collD != null){
			if(collD.tag.Equals("Lethals")){
				dead = true;
				return;
			}
		}

		if(collH != null){
			if(collH.tag.Equals("HMovingPlatforms")){
				if(!state.isOnGround){
					
					if((state.velocity.y < 0.1f) && (collH.gameObject.transform.position.y + collH.gameObject.transform.localScale.y*0.5f + player.transform.localScale.y*0.5f + state.velocity.y) < player.transform.position.y + 0.1f){
						state.isOnGround = true;
						state.numJumps = 0;
						player.transform.position = new Vector3(player.transform.position.x, (collH.gameObject.transform.position.y + collH.gameObject.transform.localScale.y*0.5f + player.transform.localScale.y*0.5f - 0.1f), player.transform.position.z);
						state.velocity.y = 0;
					}
					if((state.velocity.y > 0.1f) && ((collH.gameObject.transform.position.y) > (player.transform.position.y - state.velocity.y + 0.1f))){
						player.transform.position = new Vector3(player.transform.position.x, (collH.gameObject.transform.position.y - collH.gameObject.transform.localScale.y*0.5f - player.transform.localScale.y*0.5f - 0.1f), player.transform.position.z);
						state.velocity.y = 0;
					}
					if(Mathf.Approximately(player.transform.position.y, (collH.gameObject.transform.position.y + collH.transform.localScale.y*0.5f + player.transform.localScale.y*0.5f - 0.1f))){
						if(collH.gameObject.GetComponent<HPlatMovement>().isGoingLeft(frame)){
							state.platformVelocity.x = -0.09f;
						}
						else{
							state.platformVelocity.x = 0.09f;
						}
					}
				}
			}
		}

		
		if(collV != null){
			if(collV.tag.Equals("VMovingPlatforms")){
				if(!state.isOnGround){
					
					if((state.velocity.y < 0.1f) && (collV.gameObject.transform.position.y + collV.gameObject.transform.localScale.y*0.5f + player.transform.localScale.y*0.5f + state.velocity.y) < player.transform.position.y + 0.1f){
						state.isOnGround = true;
						state.numJumps = 0;
						player.transform.position = new Vector3(player.transform.position.x, (collV.gameObject.transform.position.y + collV.gameObject.transform.localScale.y*0.5f + player.transform.localScale.y*0.5f - 0.1f), player.transform.position.z);
						state.velocity.y = 0;
					}
					if((state.velocity.y > 0.1f) && ((collF.gameObject.transform.position.y) > (player.transform.position.y - state.velocity.y + 0.1f))){
						player.transform.position = new Vector3(player.transform.position.x, (collF.gameObject.transform.position.y - collF.gameObject.transform.localScale.y*0.5f - player.transform.localScale.y*0.5f - 0.1f), player.transform.position.z);
						state.velocity.y = 0;
					}
					if(Mathf.Approximately(player.transform.position.y, (collV.gameObject.transform.position.y + collV.transform.localScale.y*0.5f + player.transform.localScale.y*0.5f - 0.1f))){
						if(collV.gameObject.GetComponent<VPlatMovement>().isGoingDown(frame)){
							state.platformVelocity.y = -0.09f;							
						}
						else{
							state.platformVelocity.y = 0.09f;
						}
					}
				}
			}
		}


		if(collF != null){
			if(collF.tag.Equals("Floor")){
				if(!state.isOnGround){

					if((state.velocity.y + state.platformVelocity.y < 0.1f) && (collF.gameObject.transform.position.y + collF.gameObject.transform.localScale.y*0.5f + player.transform.localScale.y*0.5f + state.velocity.y) < player.transform.position.y + 0.1f){
						state.isOnGround = true;
						state.numJumps = 0;
						player.transform.position = new Vector3(player.transform.position.x, (collF.gameObject.transform.position.y + collF.gameObject.transform.localScale.y*0.5f + player.transform.localScale.y*0.5f - 0.1f), player.transform.position.z);
						state.velocity.y = 0;
					}
					if((state.velocity.y + state.platformVelocity.y > 0.1f) && ((collF.gameObject.transform.position.y) > (player.transform.position.y - state.velocity.y + 0.1f))){
						player.transform.position = new Vector3(player.transform.position.x, (collF.gameObject.transform.position.y - collF.gameObject.transform.localScale.y*0.5f - player.transform.localScale.y*0.5f - 0.1f), player.transform.position.z);
						state.velocity.y = 0;
					}
				}
			}
		}


		if(collH == null && collF == null && collV == null){
			state.isOnGround = false;
			state.platformVelocity.x = 0;
			state.platformVelocity.y = 0;
			if(state.numJumps < 1){
				state.numJumps = 1;
			}
		}
			


		
		if(collW != null){
			 if(collW.tag.Equals ("Wall")){
				player.transform.position = new Vector3((player.transform.position.x - state.velocity.x*1.03f - state.platformVelocity.x*1.03f), player.transform.position.y, player.transform.position.z);
				state.velocity.x = 0;
				state.platformVelocity.x = 0;
			}
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

	private void PlatsGoToFrame(int curFrame){
		foreach(HPlatMovement mov in hplatmovers){
			if(mov != null){
				mov.goToFrame(curFrame);
			}
		}
		foreach(VPlatMovement mov in vplatmovers){
			if(mov != null){
				mov.goToFrame(curFrame);
			}
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

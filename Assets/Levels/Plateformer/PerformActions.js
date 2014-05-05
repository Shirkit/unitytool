#pragma strict
import System.Collections.Generic;

class PerformActions extends MonoBehaviour{

private var aIndex : int;
private var actions : List.<String>;
private var durations : List.<int>;
private var actionTypes : Hashtable;

	function Start () {
		actionTypes = new Hashtable();
		actions = new List.<String>();
		durations = new List.<int>();
		//Initialize action types
		var moveLeftAction = new moveLeftAction(gameObject);
		var moveRightAction = new moveRightAction(gameObject);
		var wait = new AbstractAction(gameObject);
		var jumpAction = new jumpAction(gameObject);
		actionTypes.Add("jump", jumpAction);
		actionTypes.Add("wait", wait);
		actionTypes.Add("moveLeft", moveLeftAction);
		actionTypes.Add("moveRight", moveRightAction);
		
		
		//Initialize actions to be performed.
		actions.Add("moveLeft");
		durations.Add(12);
		actions.Add("moveLeft");
		durations.Add(20);
		actions.Add("moveRight");
		durations.Add(28);
		actions.Add("wait");
		durations.Add(30);
		actions.Add("jump");
		durations.Add(15);
		actions.Add("moveLeft");
		durations.Add(36);
		actions.Add("moveRight");
		durations.Add(16);
		actions.Add("wait");
		durations.Add(30);
		actions.Add("jump");
		durations.Add(15);
		actions.Add("moveRight");
		durations.Add(12);
		actions.Add("moveLeft");
		durations.Add(32);
		actions.Add("moveRight");
		durations.Add(60);
		
		//Start first action
		aIndex = 0;		

	}

	function FixedUpdate () {
		if(aIndex < actions.Count){
			Debug.Log("Performing action: " + actions[aIndex] + " for duration: " + durations[aIndex]);
			doAction(actions[aIndex], durations[aIndex]);
		}
		else{
			Debug.Log("Finished");
		}
	}
	
	function doAction(aName : String, aDuration : int){
		var action : AbstractAction = actionTypes[aName];
		if(action.execute(aDuration)){
			aIndex++;
		}
	}

}
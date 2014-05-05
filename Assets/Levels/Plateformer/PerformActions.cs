using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PerformActions : MonoBehaviour {

	private int aIndex;
	private List<string> actions;
	private List<int> durations;
	private Dictionary<string, AbstractAction> actionTypes;

	// Use this for initialization
	void Start () {
		actionTypes = new Dictionary<string, AbstractAction>();
		actions = new List<string>();
		durations = new List<int>();
		//Initialize action types
		moveLeftAction mla = new moveLeftAction(gameObject);
		moveRightAction mra = new moveRightAction(gameObject);
		waitAction wait = new waitAction(gameObject);
		jumpAction ja = new jumpAction(gameObject);
		actionTypes.Add("jump", ja);
		actionTypes.Add("wait", wait);
		actionTypes.Add("moveLeft", mla);
		actionTypes.Add("moveRight", mra);
		
		
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
	
	// Update is called once per frame
	void FixedUpdate () {
		if(aIndex < actions.Count){
			Debug.Log("Performing action: " + actions[aIndex] + " for duration: " + durations[aIndex]);
			doAction(actions[aIndex], durations[aIndex]);
		}
		else{
			Debug.Log("Finished");
		}
	}

	void doAction(string aName, int aDuration){
		AbstractAction action = actionTypes[aName];
		if(action.execute(aDuration)){
			aIndex++;
		}
	}
}

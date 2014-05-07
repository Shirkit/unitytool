using UnityEngine;
using System.Collections;

public class playerMovement : MonoBehaviour {
	public static int curIndex;
	public int index; 

	public movementModel model;

	public playerMovement(){
		
	}


	void Awake () {
		index = curIndex;
		curIndex++;
		model = GameObject.Find ("modelObject" + index).GetComponent<movementModel>() as movementModel;
	}
	
	// Update is called once per frame
	void Update () {
		if(model.updater()){
			model.doAction("wait", 1);
		}
	}


}

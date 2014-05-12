using UnityEngine;
using System.Collections;

public class playerMovement : MonoBehaviour {
	public int index; 
	private int waited;

	public movementModel model;

	public playerMovement(){
		
	}


	void Awake () {
		string num = gameObject.name.Substring(6);
		int.TryParse(num, out index);
		model = GameObject.Find("modelObject" + index).GetComponent<movementModel>() as movementModel;

		gameObject.renderer.material.color = new Color(Random.Range (0f, 1f), Random.Range (0f, 1f), Random.Range (0f, 1f));
		waited = 0;
	}
	
	// Update is called once per frame
	void FixedUpdate () 
	{
		if(waited < 60){
			waited++;
		}
		else{
			if(model.updater())
			{
				model.doAction("wait", 1);
			}
		}
	}


}

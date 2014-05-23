using UnityEngine;
using System.Collections;

public class HPlatMovement : MonoBehaviour {

	private float movementSpeed;
	private float lmostX;
	private float rmostX;
	private bool initialized = false;
	private int totalFrames;

	public void initialize(){
		initialized = true;
		movementSpeed = 0.075f;
		lmostX = transform.FindChild("lmost").position.x;
		rmostX = transform.FindChild("rmost").position.x;
		totalFrames = Mathf.CeilToInt((rmostX - lmostX) / movementSpeed) * 2;
	}

	public void goToFrame(int frame){
		if(!initialized){
			initialize();
		}
		
		int fr = frame % totalFrames;
		//Debug.Log ("fr - " + fr);
		//Debug.Log (totalFrames/2);
		if(fr > (totalFrames/2)){

			transform.position = new Vector3((rmostX - (fr - totalFrames/2)*movementSpeed), transform.position.y, transform.position.z);
		}
		else{

			transform.position = new Vector3((lmostX + fr*movementSpeed), transform.position.y, transform.position.z);
		}
	}
	public bool isGoingLeft(int frame){
		if(frame % totalFrames > totalFrames/2){
			return true;
		}
		else{
			return false;
		}
	}

}

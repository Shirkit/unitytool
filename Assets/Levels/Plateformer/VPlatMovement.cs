using UnityEngine;
using System.Collections;

public class VPlatMovement : MonoBehaviour {
	
	private float movementSpeed;
	private float bmostY;
	private float tmostY;
	private bool initialized = false;
	private int totalFrames;
	
	public void initialize(){
		initialized = true;
		movementSpeed = 0.075f;
		bmostY = transform.FindChild("bmost").position.y;
		tmostY = transform.FindChild("tmost").position.y;
		totalFrames = Mathf.CeilToInt((tmostY - bmostY) / movementSpeed) * 2;
		desPos = new Vector3(transform.position.x, transform.position.y, transform.position.z);
	}
	
	private Vector3 desPos;
	private Vector3 difPos;
	
	public void goToFrame(int frame){
		if(!initialized){
			initialize();
		}
		
		int fr = frame % totalFrames;
		//Debug.Log ("fr - " + fr);
		//Debug.Log (totalFrames/2);
		if(fr > (totalFrames/2)){
			
			desPos.y = (tmostY - (fr - totalFrames/2)*movementSpeed);
			difPos = desPos - transform.position;
			transform.position += difPos;
		}
		else{
			desPos.y = (bmostY + fr*movementSpeed);
			difPos = desPos - transform.position;
			transform.position += difPos;
		}
	}
	public bool isGoingDown(int frame){
		if(frame % totalFrames > totalFrames/2){
			return true;
		}
		else{
			return false;
		}
	}
	
}

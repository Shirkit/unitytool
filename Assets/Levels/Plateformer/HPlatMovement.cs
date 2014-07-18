using UnityEngine;
using System.Collections;

public class HPlatMovement : MonoBehaviour {

	private float movementSpeed;
	public float lmostX;
	public float rmostX;
	private bool initialized = false;
	public int totalFrames;

	public int curFrame;

	public void Awake(){
		initialize();
		curFrame = 0;
	}

	void Update(){
		curFrame++;
		goToFrame(curFrame);
	}

	public void initialize(){
		initialized = true;
		movementSpeed = 0.09f;
		lmostX = transform.FindChild("lmost").position.x;
		rmostX = transform.FindChild("rmost").position.x;
		totalFrames = Mathf.CeilToInt((rmostX - lmostX) / movementSpeed) * 2;
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

			desPos.x = (rmostX - (fr - totalFrames/2)*movementSpeed);
			difPos = desPos - transform.position;
			transform.position += difPos;
		}
		else{
			desPos.x = (lmostX + fr*movementSpeed);
			difPos = desPos - transform.position;
			transform.position += difPos;
		}
	}
	public bool isGoingLeft(int frame){
		if((frame % totalFrames) > (totalFrames/2)){
			return true;
		}
		else{
			return false;
		}
	}

}

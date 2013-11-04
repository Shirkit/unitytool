using UnityEngine;
using System.Collections;

public class CameraControl : MonoBehaviour {

public enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2, None = 3 }
	public RotationAxes axes = RotationAxes.MouseXAndY;
	public float sensitivityX = 15F;
	public float sensitivityY = 15F;

	public float minimumX = -360F;
	public float maximumX = 360F;

	public float minimumY = -90F;
	public float maximumY = 90F;

	float rotationY = 0F;

	void Update ()
	{
		if (axes == RotationAxes.MouseXAndY)
		{
			float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityX;
			
			rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
			rotationY = Mathf.Clamp (rotationY, minimumY, maximumY);
			
			transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
		}
		else if (axes == RotationAxes.MouseX)
		{
			transform.Rotate(0, Input.GetAxis("Mouse X") * sensitivityX, 0);
		}
		else if (axes == RotationAxes.MouseY) 
		{
			rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
			rotationY = Mathf.Clamp (rotationY, minimumY, maximumY);
			
			transform.localEulerAngles = new Vector3(-rotationY, transform.localEulerAngles.y, 0);
		}
		else
		{
		}
		
		
		if (axes != RotationAxes.None) {
			float xAxisValue = Input.GetAxis("Horizontal");
    		float zAxisValue = Input.GetAxis("Vertical");    		
			transform.Translate(new Vector3(xAxisValue, 0.0f, zAxisValue));
		}    	
		
	}
	
	void Start ()
	{
		// Make the rigid body not change rotation
		if (rigidbody)
			rigidbody.freezeRotation = true;
	}
}

using UnityEngine;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
	public float speed;
	private Vector3 initialPosition;
	private Quaternion initialRotation;
	
	public void SetInitialPosition () {
		initialPosition = transform.position;
		initialRotation = transform.rotation;
	}
	
	public void ResetSimulation ()
	{
		transform.position = initialPosition;
		transform.rotation = initialRotation;
	}
	
}

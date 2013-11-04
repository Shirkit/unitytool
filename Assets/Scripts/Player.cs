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
	
	public void Update()
	{
		float ah = Input.GetAxis("Horizontal");
		float av = Input.GetAxis("Vertical");
		Vector3 d = new Vector3(ah, 0f, av).normalized;
		d.x *= speed * Time.deltaTime;
		d.z *= speed * Time.deltaTime;
		
		transform.Translate(d);
		
		Vector2 pos = new Vector2 ((transform.position.x - SpaceState.FloorMin.x) / SpaceState.TileSize.x, (transform.position.z - SpaceState.FloorMin.z) / SpaceState.TileSize.y);
		int mapX = (int) pos.x;
		int mapY = (int) pos.y;
		
		if 	(SpaceState.FullMap != null) {
			if (SpaceState.FullMap[SpaceState.TimeSlice][mapX][mapY].seen)
				Debug.Log("Game Over");
			else if (SpaceState.FullMap[SpaceState.TimeSlice][mapX][mapY].blocked)
				transform.Translate(-d);
		}
	}
	
}

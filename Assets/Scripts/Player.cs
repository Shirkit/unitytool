using UnityEngine;
using System.Collections.Generic;
using Common;

namespace Objects {
	public class Player : MonoBehaviour {
		public float speed;
		public float maxHp;
		private Vector3 initialPosition;
		private Quaternion initialRotation;
		
		public void SetInitialPosition () {
			initialPosition = transform.position;
			initialRotation = transform.rotation;
		}
		
		public void ResetSimulation () {
			transform.position = initialPosition;
			transform.rotation = initialRotation;
		}
		
		public void Update () {
			float ah = Input.GetAxis ("Horizontal");
			float av = Input.GetAxis ("Vertical");
			Vector3 d = new Vector3 (ah, 0f, av);
			d.x *= speed * Time.deltaTime;
			d.z *= speed * Time.deltaTime;
			transform.Translate (d);
			
			Vector2 pos = new Vector2 ((transform.position.x - SpaceState.Running.floorMin.x) / SpaceState.Running.tileSize.x, (transform.position.z - SpaceState.Running.floorMin.z) / SpaceState.Running.tileSize.y);
			int mapX = (int)pos.x;
			int mapY = (int)pos.y;

			if (SpaceState.Running.fullMap != null) {
				if (SpaceState.Running.fullMap [SpaceState.Running.timeSlice - 1] [mapX] [mapY].goal)
					state = 1;
				else if (SpaceState.Running.fullMap [SpaceState.Running.timeSlice - 1] [mapX] [mapY].seen)
					state = 2;
				else
					state = 0;
				
				if (SpaceState.Running.fullMap [SpaceState.Running.timeSlice - 1] [mapX] [mapY].blocked)
					transform.Translate (-d);
			}
		}
		
		private short state = 0;
		
		void OnGUI () {
			GUIStyle s = new GUIStyle ();
			s.fontSize = 144;
			if (state == 1)
				GUI.Box (new Rect (10, 10, 200, 50), "Win", s);
			else if (state == 2)
				GUI.Box (new Rect (10, 10, 200, 50), "Lose", s);
		}
	}
}
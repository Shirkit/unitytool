using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace Objects {
	public class Waypoint : MonoBehaviour {
		
		public Waypoint next;
		public static bool debug = true;
	
		// Use this for initialization
		void Start () {
		}
		
		// Update is called once per frame
		void Update () {
		}
		
		void OnDrawGizmos () {
			Gizmos.color = Color.white;
			if (debug)
				Gizmos.DrawSphere(transform.position, 0.1f);
		}
	}
}
/*public class RotationWaypoint : Waypoint {
	
	public Vector3 lookDir;

}

public class WaitingWaypoint : Waypoint {
	
	public float waitingTime;
	
	[HideInInspector]
	public Dictionary<int, float> times = new Dictionary<int, float>();
	
}*/
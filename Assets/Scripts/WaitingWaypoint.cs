using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

public class WaitingWaypoint : Waypoint {
	
	public float waitingTime;
	
	[HideInInspector]
	public Dictionary<int, float> times = new Dictionary<int, float>();
	
}
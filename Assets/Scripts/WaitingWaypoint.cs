#if !UNITY_WEBPLAYER

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace Objects {
	public class WaitingWaypoint : Waypoint {
		
		public float waitingTime;
		[HideInInspector]
		public Dictionary<int, float>
			times = new Dictionary<int, float> ();
		
	}
}

#endif
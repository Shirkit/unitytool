#if !UNITY_WEBPLAYER

using System;
using UnityEngine;
using Objects;

namespace Extra {
	public class EnemyMover {
		
		public static void Solve (int id, Vector3 position, Quaternion rotation, float moveSpeed, float rotationSpeed, float tpf, Waypoint waypoint, float collisionRadius, out Vector3 outPosition, out Quaternion outRotation, out Waypoint outWaypoint) {
			if (waypoint is RotationWaypoint) {
				
				RotationWaypoint r = (RotationWaypoint)waypoint;
				
				Quaternion lookDir = Quaternion.LookRotation (r.lookDir);
				
				if ((lookDir.eulerAngles - rotation.eulerAngles).magnitude < 0.1f) {
					Solve (id, position, rotation, moveSpeed, rotationSpeed, tpf, waypoint.next, collisionRadius, out outPosition, out outRotation, out outWaypoint);
					return;
				}
				
				outRotation = Quaternion.RotateTowards (rotation, lookDir, rotationSpeed * tpf);
				outPosition = position;
				outWaypoint = waypoint;
			} else if (waypoint is WaitingWaypoint) {
				
				WaitingWaypoint w = (WaitingWaypoint)waypoint;
				
				float t;
				
				if (w.times.TryGetValue (id, out t)) {
					if (t > w.waitingTime) {
						w.times [id] = 0f;
						Solve (id, position, rotation, moveSpeed, rotationSpeed, tpf, waypoint.next, collisionRadius, out outPosition, out outRotation, out outWaypoint);
						return;
					}
					
				} else {
					w.times [id] = 0;
				}
				
				w.times [id] += tpf;
				
				outPosition = position;
				outWaypoint = waypoint;
				outRotation = rotation;
			} else {
				if (Dist (position, waypoint.transform.position) <= collisionRadius) {
					Solve (id, position, rotation, moveSpeed, rotationSpeed, tpf, waypoint.next, collisionRadius, out outPosition, out outRotation, out outWaypoint);
					return;
				}
				
				outPosition = position;
				outRotation = rotation;
				Vector3 dir = waypoint.transform.position - position;
				dir.y = 0f;
				dir.Normalize ();
				dir *= moveSpeed * tpf;
			
				outPosition = position + dir;
				outRotation = Quaternion.LookRotation (dir);
				outWaypoint = waypoint;
			}
		}
		
		private static float Dist (Vector3 source, Vector3 goal) {
			Vector3 v1 = new Vector3 (), v2 = new Vector3 ();
			v1.x = source.x;
			v1.y = source.z;
			v2.x = goal.x;
			v2.y = goal.z;
			return Vector2.Distance (v1, v2);
		}
		
	}
}

#endif
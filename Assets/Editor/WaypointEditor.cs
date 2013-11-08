using UnityEditor;
using UnityEngine;
using Objects;

namespace EditorArea {
	[CustomEditor(typeof(Waypoint))]
	public class WaypointEditor : Editor {
		// Custom Editors are only called when the object is selected.
		// To do Gizmos draws, the code should be placed in the inspected class
		
		private static bool debug = true;
		private int i;
		
		public override void OnInspectorGUI() {
			DrawDefaultInspector();
			debug = EditorGUILayout.Toggle("Draw debug:", debug);
			Waypoint.debug = debug;
			SceneView.RepaintAll();
		}
		
		public void OnSceneGUI() {
			Waypoint wp = (Waypoint) target;
			
			//Handles.Label(wp.transform.position + Vector3.up * (wp.transform.localScale.y*2/3), "Test!");
			if (debug && wp.next != null) {
				//wp.transform.LookAt(wp.next.transform);
				Quaternion q = new Quaternion(wp.transform.rotation.x, wp.transform.rotation.y, wp.transform.rotation.z, wp.transform.rotation.w);
				q.SetLookRotation(wp.next.transform.position - wp.transform.position);
				Handles.ArrowCap(0, wp.transform.position, q, HandleUtility.GetHandleSize(wp.transform.position));
				Handles.DrawLine(wp.transform.position, wp.next.transform.position);
			}
		}
	}
}
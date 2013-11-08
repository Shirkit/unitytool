using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Common;

namespace EditorArea {
	[CustomEditor(typeof(Mapper))]
	public class MapperEditor : Editor
	{
		
		public static bool editGrid = false, didUpdate = false;
		
		public override void OnInspectorGUI ()
		{
			Mapper mapper = (Mapper)target;
			
			DrawDefaultInspector ();
			
			MapperEditorDrawer drawer = mapper.gameObject.GetComponent<MapperEditorDrawer> ();
			if (drawer == null) {
				drawer = mapper.gameObject.AddComponent<MapperEditorDrawer> ();
				drawer.hideFlags = HideFlags.HideInInspector;
			}
			
			editGrid = EditorGUILayout.Toggle ("Edit grid", editGrid);
			drawer.editGrid = editGrid;
			if (editGrid) {
				OnSceneGUI();
				drawer.editingGrid = grid;
				if (!didUpdate) {
					mapper.ComputeTileSize (SpaceState.Editor, mapper.collider.bounds.min, mapper.collider.bounds.max, MapperWindowEditor.gridSize, MapperWindowEditor.gridSize);
					drawer.tileSize.Set (SpaceState.Editor.tileSize.x, SpaceState.Editor.tileSize.y);
					drawer.zero.Set (SpaceState.Editor.floorMin.x, SpaceState.Editor.floorMin.z);
					didUpdate = true;
				}
			} else {
				didUpdate = false;
			}
			
			SceneView.RepaintAll ();
		}
		
		public static Cell[][] grid;
	
		public void OnSceneGUI ()
		{
			if (!editGrid)
				return;
			
			//Mapper mapper = (Mapper)target;
			if (grid == null || grid.Length != MapperWindowEditor.gridSize) {
				grid = new Cell[MapperWindowEditor.gridSize][];
				for (int i = 0; i < MapperWindowEditor.gridSize; i++)
					grid [i] = new Cell[MapperWindowEditor.gridSize];
			}
			// Disabled part to do raycasting to identify the cell which the user clicked
			Event current = Event.current;
			switch (current.type) {
			case EventType.KeyDown:
				switch (current.keyCode) {
				case KeyCode.Alpha1:
				case KeyCode.Alpha2:
				case KeyCode.Alpha3:
				case KeyCode.Alpha4:
				case KeyCode.Alpha5:
				case KeyCode.Alpha0:
					Ray ray = HandleUtility.GUIPointToWorldRay (current.mousePosition);
					RaycastHit hit;
					if (Physics.Raycast (ray, out hit, Mathf.Infinity, 1 << LayerMask.NameToLayer ("Floor"))) {
						if (hit.transform.CompareTag ("Floor")) {
							Vector2 pos = new Vector2 ((hit.point.x - hit.collider.bounds.min.x) / SpaceState.Editor.tileSize.x, (hit.point.z - hit.collider.bounds.min.x) / SpaceState.Editor.tileSize.y);
							Cell c = grid [(int)pos.x] [(int)pos.y];
							if (c == null) {
								c = new Cell ();
								grid [(int)pos.x] [(int)pos.y] = c;
							}
							if (current.keyCode == KeyCode.Alpha1)
							{
								c.safe = true;
							}
							else if (current.keyCode == KeyCode.Alpha2)
								c.blocked = true;
							else if (current.keyCode == KeyCode.Alpha3)
								c.noisy = true;
							else if (current.keyCode == KeyCode.Alpha4)
								c.waypoint = true;
							else if (current.keyCode == KeyCode.Alpha5)
								c.cluster = true;
							else if (current.keyCode == KeyCode.Alpha0)
								grid [(int)pos.x] [(int)pos.y] = null;
						
							SceneView.RepaintAll();
						}
					}
					break;
				}
				break;
			}
		}
	}
}
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

namespace EditorArea
{
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
				OnSceneGUI ();
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
			if (current != null && EventType.KeyDown == current.type) {
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
						
						switch (current.keyCode) {
						case KeyCode.Alpha0:
							grid [(int)pos.x] [(int)pos.y] = null;
							break;
							
						case KeyCode.Alpha1:
							c.safe = true;
							break;
						
						case KeyCode.Alpha2:
							c.blocked = true;
							break;
						
						case KeyCode.Alpha3:
							c.noisy = true;
							break;
						
						case KeyCode.Alpha4:
							c.waypoint = true;
							break;
							
						case KeyCode.Keypad0:
							c.cluster = 0;
							break;
						
						case KeyCode.Keypad1:
							c.cluster |= 1;
							break;
						
						case KeyCode.Keypad2:
							c.cluster |= 2;
							break;
						
						case KeyCode.Keypad3:
							c.cluster |= 4;
							break;
						
						case KeyCode.Keypad4:
							c.cluster |= 8;
							break;
						
						case KeyCode.Keypad5:
							c.cluster |= 16;
							break;
						
						case KeyCode.Keypad6:
							c.cluster |= 32;
							break;
						
						case KeyCode.Keypad7:
							c.cluster |= 64;
							break;
						
						case KeyCode.Keypad8:
							c.cluster |= 128;
							break;
						
						case KeyCode.Keypad9:
							c.cluster |= 256;
							break;
						}
						
						SceneView.RepaintAll ();
					}
				}
			}
		} // OnSceneGui
	} // Class
} // NS
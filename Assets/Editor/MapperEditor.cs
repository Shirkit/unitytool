#if !UNITY_WEBPLAYER

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
	public class MapperEditor : Editor {
		
		public static Cell[][] grid;
		//
		public static bool editGrid = false, didUpdate = false;
		//
		private Mapper mapper;
		private MapperEditorDrawer drawer;
	
		public void OnSceneGUI () {			
			// Update drawer
			if (drawer != null)
				drawer.editGrid = editGrid;
			
			// Stop if needed
			if (!editGrid) {
				didUpdate = false;
				return;
			}
			
			// Create grid
			if (grid == null || grid.Length != MapperWindowEditor.gridSize) {
				grid = new Cell[MapperWindowEditor.gridSize][];
				for (int i = 0; i < MapperWindowEditor.gridSize; i++)
					grid [i] = new Cell[MapperWindowEditor.gridSize];
			}
			
			// Prepare holders
			if (mapper == null) {
				mapper = (Mapper)target;
			}
			if (drawer == null) {
				drawer = mapper.gameObject.GetComponent<MapperEditorDrawer> ();
			}
			
			drawer.editingGrid = grid;
			
			// Update Scene
			if (!didUpdate) {
				mapper.ComputeTileSize (SpaceState.Editor, mapper.collider.bounds.min, mapper.collider.bounds.max, MapperWindowEditor.gridSize, MapperWindowEditor.gridSize);
				drawer.tileSize.Set (SpaceState.Editor.tileSize.x, SpaceState.Editor.tileSize.y);
				drawer.zero.Set (SpaceState.Editor.floorMin.x, SpaceState.Editor.floorMin.z);
				didUpdate = true;
			}
			
			// Raycast
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
							
						case KeyCode.U:
							c.cluster = 0;
							break;
						
						
						case KeyCode.Alpha6:
							c.cluster |= 1;
							break;
						
						case KeyCode.Alpha7:
							c.cluster |= 2;
							break;
						
						case KeyCode.Alpha8:
							c.cluster |= 4;
							break;
						
						case KeyCode.Alpha9:
							c.cluster |= 8;
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

#endif
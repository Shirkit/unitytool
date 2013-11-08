using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Serialization;
using UnityEngine;

namespace EditorArea {
[Serializable]
public class MapperEditorHolder
	{
		/*public List<List<List<Cell>>> fullMap = new List<List<List<Cell>>>();
		public List<Node> path;
		public List<List<Node>> paths = new List<List<Node>> ();
		public int startX, startY, endX = 37, endY = 39, timeSlice, timeSamples = 500, attemps = 20000, iterations = 1;
		public bool drawMap = true, drawMoveMap = false, drawMoveUnits = false, drawNeverSeen = false, draw3dExploration = false, drawHeatMap = true, drawPath = false;
		public float stepSize = 1 / 10f;
	
		public MapperEditorHolder ()
		{
		}
		
		public void CopyFrom (MapperEditor obj)
		{
			foreach (FieldInfo f in this.GetType().GetFields()) {
				if (f.Name != "fullMap") {
					FieldInfo ff = typeof(MapperEditor).GetField (f.Name);
					f.SetValue (this, ff.GetValue (obj));
				}
			}
			
			if (MapperEditor.fullMap != null)
				for (int t = 0; t < MapperEditor.fullMap.GetLength(0); t++) {
					List<List<Cell>> currentTime = new List<List<Cell>> ();
					for (int x = 0; x < MapperEditor.fullMap[t].GetLength(0); x++) {
						List<Cell> currentX = new List<Cell> ();
						for (int y = 0; y < MapperEditor.fullMap[t].GetLength(1); y++) {
							currentX.Add (MapperEditor.fullMap [t] [x, y]);
						}
						currentTime.Add (currentX);
					}
					fullMap.Add (currentTime);
				}
		}
		
		public void SaveTo (MapperEditor obj)
		{
			foreach (FieldInfo f in this.GetType().GetFields()) {
				if (f.Name != "fullMap") {
					FieldInfo ff = typeof(MapperEditor).GetField (f.Name);
					ff.SetValue (obj, f.GetValue (this));
				}
			}
			
			Cell[][,] saving = new Cell[fullMap.Count][,];
			for (int t = 0; t < fullMap.Count; t++) {
				saving[t] = new Cell[fullMap[t].Count, fullMap[t][0].Count];
				for (int x = 0; x < fullMap.Count; x++) {
					for (int y = 0; y < fullMap.Count; y++) {
						saving[t][x,y] = fullMap[t][x][y];
					}
				}
			}
			
			MapperEditor.fullMap = saving;
		}*/
	}
}
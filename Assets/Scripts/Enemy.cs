using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Extra;
using System;
using Common;

namespace Objects {
	public class Enemy : MonoBehaviour {
		
		public Waypoint target;
		public float moveSpeed;
		public float rotationSpeed;
		public float fovAngle = 33;
		public float fovDistance = 5;
		public float radius = 0.5f;
		public float dps = 2;
		public float maxHealth = 100;
		// The first index is always the time span you want to peek
		[HideInInspector]
		public Vector3[] positions;
		[HideInInspector]
		public Vector3[] forwards;
		[HideInInspector]
		public Quaternion[] rotations;
		[HideInInspector]
		public Vector2[][] cells; // The second index goes from 0 to the amount of seen cells in that time span
		[HideInInspector]
		public Cell[][][] seenCells;
		//
		private Waypoint dummyTarget;
		private Vector3 dummyPosition;
		private Quaternion dummyRotation;
		//
		private Vector3 initialPosition;
		private Quaternion initialRotation;
		private Waypoint initialTarget;
		//
		private Vector3 currentPosition;
		private Quaternion currentRotation;
		public Color LineForFOV = new Color (1.0f, 0.3f, 0.0f, 1f);
		
		// This moves the enemy in the game running environment
		void Update () {
			Vector3 outPos;
			Quaternion outRot;
			Waypoint outWay;
			
			EnemyMover.Solve (gameObject.GetHashCode (), transform.position, transform.rotation, moveSpeed, rotationSpeed, Time.deltaTime, target, 0.25f, out outPos, out outRot, out outWay);
			
			transform.position = outPos;
			transform.rotation = outRot;
			target = outWay;
			
			currentPosition = transform.position;
			currentRotation = transform.rotation;
		}
		
		// Reset back the dummy and actual gameobject back to the initial position
		public void ResetSimulation () {
			transform.position = initialPosition;
			transform.rotation = initialRotation;
			target = initialTarget;
			
			dummyPosition = transform.position;
			dummyRotation = transform.rotation;
			dummyTarget = target;
		}
		
		// Sets the initial position with the current transform coordinates
		public void SetInitialPosition () {
			initialTarget = target;
			initialPosition = transform.position;
			initialRotation = transform.rotation;
			
			ResetSimulation ();
		}
		
		// This siumulates the enemy's movement based on the actual enemy movement
		public void Simulate (float time) {
			Vector3 outPos;
			Quaternion outRot;
			Waypoint outWay;
			
			EnemyMover.Solve (gameObject.GetHashCode (), dummyPosition, dummyRotation, moveSpeed, rotationSpeed, time, dummyTarget, 0.25f, out outPos, out outRot, out outWay);
			
			dummyPosition = outPos;
			dummyRotation = outRot;
			dummyTarget = outWay;
			
			currentPosition = dummyPosition;
			currentRotation = dummyRotation;
		}
		
		public void OnDrawGizmos () {
			return; 
			/*if (transform.FindChild("FOV") != null)
			{
				GameObject FOV = transform.FindChild("FOV").gameObject;
				
				Mesh mesh = FOV.GetComponent<MeshFilter>().sharedMesh; 
				if (mesh == null)
				{
					//Create Mesh
					mesh = new Mesh(); 
					
					List<Vector3> vertices = new List<Vector3>();
					
					Quaternion q = Quaternion.Euler(new Vector3(0f, fovAngle, 0f));
					Vector3 dir = q * transform.forward;
					vertices.Add(dir * fovDistance);
					
					dir = (Quaternion.Inverse(q)) * transform.forward;
					vertices.Add(dir * fovDistance);
					
					vertices.Add(Vector3.zero);
					
					mesh.vertices = vertices.ToArray();
					mesh.uv = new Vector2[] {new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1)};
		        	mesh.triangles = new int[] {2, 1, 0};
					
					//Normal
					mesh.RecalculateNormals();
					mesh.RecalculateBounds();
					mesh.Optimize(); 
				}
				FOV.GetComponent<MeshFilter>().mesh = mesh;	
				
			}
			else
			{	
				Quaternion q = Quaternion.Euler(new Vector3(0f, fovAngle, 0f));
				Vector3 dir = q * transform.forward;
				Gizmos.color = LineForFOV; 
				Gizmos.DrawLine(transform.position, transform.position + dir * fovDistance);
				dir = (Quaternion.Inverse(q)) * transform.forward;
				Gizmos.DrawLine(transform.position, transform.position + dir * fovDistance);
			}	
		
			*/
			
			
		}
		
		public Vector3 GetSimulationPosition () {
			return currentPosition;
		}
		
		public Vector3 GetSimulatedForward () {
			return currentRotation * Vector3.forward;
		}
		
		public Quaternion GetSimulatedRotation () {
			return currentRotation;
		}

		public void ComputeSeenCells(Cell[][][] fullmap) {
			seenCells = new Cell[fullmap.Length][][];

			for (int t = 0; t < fullmap.Length; t++) {
				seenCells[t] = new Cell[fullmap[0].Length][];

				for (int x = 0; x < fullmap[0][0].Length; x++) {
					seenCells[t][x] = new Cell[fullmap[0][0].Length];
				}
			}

			for (int t = 0; t < this.cells.Length; t++) {
				foreach (Vector2 p in this.cells[t]) {
					seenCells[t][(int)p.x][(int)p.y] = fullmap[t][(int)p.x][(int)p.y];
				}
			}
		}
	}
}
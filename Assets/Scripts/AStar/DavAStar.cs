using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Common;
using Objects;
using System;

namespace Exploration {
	public class DavAStar : System.Collections.IComparer
	{
		Node start, end;
		public Enemy[] enemies;
		public List<Node> enemyPath;
		private double[,] probs;

		public int Compare (object x, object y)
		{
			return f ((Node)x) > f ((Node)y) ? 1 : -1;
		}
		public DavAStar ()
		{
		}
		private double[,] enemyPathProb(Node[][] matrix){
			double[,] values= new double[matrix.Length,matrix[0].Length];
			foreach (Node n in enemyPath){
				for(int x=-2; x<=2;x++){
					for(int y=-2;y<=2;y++){
						if(n.x+x<0 || n.y+y<0 || n.x+x>=matrix.Length || n.y+y>=matrix[0].Length) 
							continue;
						values[n.x+x,n.y+y]++;
					}
				}

				//add decaying FOV prob
			}
			double max = 0;
			int maxX=0;
			int maxY=0;
			for(int x=0; x<values.GetLength (0);x++){
				for(int y=0; y<values.GetLength(1);y++){
					values[x,y]++;
					if(values[x,y]>max){
						max = values[x,y];
						maxX=x;
						maxY=y;
					}
				}
			}
			this.probs=values;
			return values;
		}
		private List<Node> getAdjacent (Node n, Node[][] matrix)
		{
			List<Node> adj = new List<Node> ();
			for (int xx = n.x - 1; xx <= n.x +1; xx++) {
				for (int yy = n.y - 1; yy <= n.y +1; yy++) {
					if (xx <= n.x + 1 && xx >= 0 && xx < matrix.Length && yy <= n.y + 1 && yy >= 0 && yy < matrix [0].Length) {
						Node c = matrix [xx] [yy];
						if (!((Cell)c.cell).blocked) {
							adj.Add (c);
						}
					}
				}
			}
			adj.Remove (n);
			return adj;
		}

		private float h (Node current, Node to, ref double temp)
		{
			if (to == end){
				return getProb(current,ref temp)*Vector2.Distance (new Vector2 (to.x, to.y), new Vector2 (current.x, current.y));
			}
			return getProb(to, ref temp)*Vector2.Distance (new Vector2 (to.x, to.y), new Vector2 (current.x, current.y));
		}	

		private float f (Node current)
		{
			double tmp = 1;
			return (g (current) +  h (current, end, ref tmp)) ;
		}
		private float getProb(Node current, ref double temperature){
			return (float)probs[current.x, current.y];
			/*foreach (Enemy en in enemies){
				foreach (Vector2 v2 in en.cells[current.t]){
					//if( Math.Abs(v2.x - current.x) <1 && Math.Abs(v2.y - current.y) <1){
					if (v2.x == current.x && v2.y == current.y){
						//Debug.Log ("collision");
						temperature = temperature /1.5;
						return 1000;
					}
						
				}
			}
			return 1;*/
		}
		// current moving cost
		private float g (Node current)
		{
			double tmp = 1.0;
			if (current.parent == null)
				return 0;
			else
				return h (current.parent, current ,ref tmp) + g (current.parent);
		}
		
		public List<Node> Compute (int startX, int startY, int endX, int endY, Cell[][][] matrix, int time, bool prob)
		{
			try{
			Priority_Queue.IPriorityQueue<Node> heap2 = new Priority_Queue.HeapPriorityQueue<Node>(600);
			
			// Initialize our version of the matrix (can we skip this?)
			Node[][] newMatrix = new Node[matrix[0].Length][];
			for (int x = 0; x < matrix [0].Length; x++) {
				newMatrix [x] = new Node[matrix [0] [x].Length];
				for (int y = 0; y < matrix [0] [x].Length; y++) {
					newMatrix [x] [y] = new Node ();
					newMatrix [x] [y].parent = null;
					newMatrix [x] [y].cell = matrix [0] [x] [y];
					newMatrix [x] [y].x = x;
					newMatrix [x] [y].y = y;
				}
			}
			enemyPathProb(newMatrix);
			// Do the work for the first cell before firing the algorithm
			start = newMatrix [startX] [startY];
			end = newMatrix [endX] [endY];
			start.parent=null;
			start.visited=true;
		
			
			foreach (Node n in getAdjacent(start, newMatrix)) {
				n.t=time;
				n.parent = start;
				float fVal = f (n);
				n.Priority = fVal;
				heap2.Enqueue(n,fVal);
			}
			while(heap2.Count != 0){

				Node first = heap2.Dequeue();
				if(first == end)
					break;
				first.visited=true;
				double temprt = 1;
				List<Node> adjs = getAdjacent(first,newMatrix);
				
				foreach(Node m in adjs){
					float currentG = (float)(g (first) + h (first,m, ref temprt));
					if(m.visited){
							if(g (m)>currentG){
								m.parent=first;
								m.t = first.t+time;
							}
						}
					else{
						if( !heap2.Contains(m)){
							m.parent = first;
							m.t= first.t +time;
							m.Priority = (float)temprt*f(m);
							heap2.Enqueue(m, m.Priority);
						}
						else
						{
							float gVal = g (m);
							if(gVal>currentG){
								m.parent= first;
								m.t = first.t+time;
								m.Priority= (float)temprt *f (m);
								heap2.UpdatePriority(m, m.Priority);
							}
						}
					}
				}
			}
				// Creates the result list
				Node l = end;
				List<Node> points = new List<Node> ();
				while (l != null) {
					points.Add (l);
					l = l.parent;
				}
				points.Reverse ();
				
				// If we didn't find a path
				if (points.Count == 1)
				points.Clear ();
				return points;
			}catch(System.Exception e){
								Debug.Log (e.Message);
								Debug.Log (e.StackTrace);
								Debug.Log ("ERROR 2");
								return null;
						}
			}
			
		}
}


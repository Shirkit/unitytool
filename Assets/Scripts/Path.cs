using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using Exploration;
using Objects;
using Extra;

namespace Common {
	public class Path {
		public String name;
		public Color color;
		public List<Node> points;
		// Metrics
		public float time, length2d, length3d, danger, los, danger3, los3, danger3Norm, los3Norm, crazy, velocity;
		
		/// <summary>
		/// Serialization only.
		/// </summary>
		public Path () {
		}
		
		public Path (List<Node> points) {
			this.points = points;
			if (points == null)
				throw new ArgumentNullException ("Points can't be null");
		}
		
		public void ZeroValues () {
			time = length2d = length3d = danger = los = danger3 = los3 = danger3Norm = los3Norm = crazy = velocity = 0f;
		}
	}
	
	// Export / Import paths area

	[XmlRoot("bulk"), XmlType("bulk")]
	public class PathBulk {
		public List<Path> paths;
		
		public PathBulk () {
			paths = new List<Path> ();
		}
		
		public static void SavePathsToFile (string file, List<Path> paths) {
			XmlSerializer ser = new XmlSerializer (typeof(PathBulk));
			
			PathBulk bulk = new PathBulk ();
			bulk.paths.AddRange (paths);
			
			using (FileStream stream = new FileStream (file, FileMode.Create)) {
				ser.Serialize (stream, bulk);
				stream.Flush ();
				stream.Close ();
			}
		}
		
		public static List<Path> LoadPathsFromFile (string file) {
			XmlSerializer ser = new XmlSerializer (typeof(PathBulk));
			
			PathBulk loaded = null;
			using (FileStream stream = new FileStream (file, FileMode.Open)) {
				loaded = (PathBulk)ser.Deserialize (stream);
				stream.Close ();
			}
			
			// Setup parenting
			foreach (Path p in loaded.paths) {
				for (int i = p.points.Count - 1; i > 0; i--) {
					p.points [i].parent = p.points [i - 1];
				}
			}
			
			return loaded.paths;
		}
	}
	
	public class PathML : NodeProvider {
		
		public List<TimeStamp> times = new List<TimeStamp> ();
		private SpaceState state;

		/// <summary>
		/// Serialization only.
		/// </summary>
		public PathML () {
		}

		public PathML (SpaceState state) {
			this.state = state;
		}
		
		public void SavePathsToFile (string file, List<Vector3> points) {

			for (int i = 0; i < points.Count; i++) {
				
				TimeStamp ts = new TimeStamp ();
				ts.t = i;
				ts.playerPos = points [i];
				
				for (int k = 0; k < SpaceState.Running.enemies.Length; k++) {

					EnemyStamp es = new EnemyStamp ();
					es.id = k;
					es.position = SpaceState.Running.enemies [k].positions [i];
					
					int mapPX = (int)((ts.playerPos.x - SpaceState.Running.floorMin.x) / SpaceState.Running.tileSize.x);
					int mapPY = (int)((ts.playerPos.z - SpaceState.Running.floorMin.z) / SpaceState.Running.tileSize.y);
					
					int mapEX = (int)((es.position.x - SpaceState.Running.floorMin.x) / SpaceState.Running.tileSize.x);
					int mapEY = (int)((es.position.z - SpaceState.Running.floorMin.z) / SpaceState.Running.tileSize.y);
					
					Node n1 = new Node ();
					n1.x = mapPX;
					n1.t = ts.t;
					n1.y = mapPY;
					n1.cell = SpaceState.Running.fullMap [n1.t] [n1.x] [n1.y];
					
					Node n2 = new Node ();
					n2.x = mapEX;
					n2.t = ts.t;
					n2.y = mapEY;
					n2.cell = SpaceState.Running.fullMap [n2.t] [n2.x] [n2.y];
					
					es.angle = Vector3.Angle (SpaceState.Running.enemies [k].forwards [i], (ts.playerPos - es.position).normalized);
					
					//es.los = ! CheckCollision (n1, n2, 0);
					es.los = ! Extra.Collision.CheckCollision (n1, n2, this, state);
					
					ts.enemies.Add (es);
				}
				
				times.Add (ts);
			}
			
			XmlSerializer ser = new XmlSerializer (typeof(PathML));
			
			using (FileStream stream = new FileStream (file, FileMode.Create)) {
				ser.Serialize (stream, this);
				stream.Flush ();
				stream.Close ();
			}
		}

		public Node GetNode (int t, int x, int y) {
			Node n3 = new Node ();
			n3.cell = state.fullMap [t] [x] [y];
			n3.x = x;
			n3.y = y;
			n3.t = t;
			return n3;
		}
		
	}
	
	public class TimeStamp {
		
		[XmlAttribute]
		public int
			t;
		public Vector3 playerPos;
		public List<EnemyStamp> enemies = new List<EnemyStamp> ();
		
	}
	
	public class EnemyStamp {
		
		[XmlAttribute]
		public int
			id;
		public Vector3 position;
		public bool los;
		public float angle;
		
	}
}
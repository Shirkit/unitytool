using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using Exploration;

namespace Common
{
	public class Path
	{
		public String name;
		public Color color;
		public List<Node> points;
		// Metrics
		public float time, length2d, length3d, danger, los, danger3, los3, danger3Norm, los3Norm, crazy, velocity;
		
		/// <summary>
		/// Serialization only.
		/// </summary>
		public Path ()
		{
		}
		
		public Path (List<Node> points)
		{
			this.points = points;
			if (points == null)
				throw new ArgumentNullException ("Points can't be null");
		}
		
		public void ZeroValues ()
		{
			time = length2d = length3d = danger = los = danger3 = los3 = danger3Norm = los3Norm = crazy = velocity = 0f;
		}
		
	}
	
	[XmlRoot("bulk"), XmlType("bulk")]
	public class PathBulk
	{
		public List<Path> paths;
		
		public PathBulk ()
		{
			paths = new List<Path> ();
		}
		
		public static void SavePathsToFile (string file, List<Path> paths)
		{
			XmlSerializer ser = new XmlSerializer (typeof(PathBulk));
			
			PathBulk bulk = new PathBulk ();
			bulk.paths.AddRange (paths);
			
			using (FileStream stream = new FileStream (file, FileMode.Create)) {
				ser.Serialize (stream, bulk);
				stream.Flush ();
				stream.Close ();
			}
		}
		
		public static List<Path> LoadPathsFromFile (string file)
		{
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
}
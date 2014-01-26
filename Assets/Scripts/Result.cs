using Extra;
using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;

namespace Common {
	[Serializable]
	public class ResultsRoot {
		public List<ResultBatch> everything = new List<ResultBatch> ();
	}
	
	[Serializable]
	public class ResultBatch {
		public int timeSamples = 0;
		public int gridSize = 0;
		public int rrtAttemps = 0;
		public List<Result> results = new List<Result> ();
		public double averageTime = 0f;
		public int totalTries = 0;
	}
	
	[Serializable]
	public class Result {
		public double timeSpent = 0f;
	}
	
	[Serializable, XmlRoot("clusters")]
	public class ClustersRoot {
		[XmlElement("cluster")]
		public List<MetricsRoot> everything = new List<MetricsRoot> ();
	}
	
	[Serializable, XmlRoot("paths")]
	public class MetricsRoot {
		[XmlAttribute]
		public string name;
		[XmlAttribute]
		public string number;
		[XmlElement("path")]
		public List<PathResults> everything = new List<PathResults> ();
	}
	
	[Serializable, XmlRoot("timestamp")]
	public class PathResults {
		[XmlAttribute("name")]
		public string name;
		[XmlElement("total-results")]
		public List<Value> totalPerPath;
		[XmlArray("timestamp-results")]
		public List<PathValue> values;
		
		// Serialization only
		public PathResults () {
		}
		
		public PathResults (Path path, float[][] input) {
			this.name = path.name;
			this.values = new List<PathValue> ();
			this.totalPerPath = new List<Value> ();
			for (int ttime = 0; ttime < path.points[path.points.Count - 1].t; ttime++)
				if (input != null)
					this.values.Add (new PathValue (input, ttime));
			{
				this.totalPerPath.Add (new Value ("Velocity", path.velocity));
				this.totalPerPath.Add (new Value ("Crazyness", path.crazy));
				this.totalPerPath.Add (new Value ("Danger", path.danger));
				this.totalPerPath.Add (new Value ("Danger3", path.danger3));
				this.totalPerPath.Add (new Value ("Danger3Norm", path.danger3Norm));
				this.totalPerPath.Add (new Value ("Los", path.los));
				this.totalPerPath.Add (new Value ("Los3", path.los3));
				this.totalPerPath.Add (new Value ("Los3Norm", path.los3Norm));
			}
		}
	}
	
	[Serializable]
	public class PathValue {
		[XmlAttribute("time")]
		public int time;
		[XmlElement("metric")]
		public List<Value> values;
		
		// Serialization only
		public PathValue () {
		}
		
		public PathValue (float[][] entry, int time) {
			this.time = time;
			this.values = new List<Value> ();
			for (int i = 0; i < entry.Length; i++) {
				this.values.Add (new Value (Enum.GetName (typeof(Analyzer.Heuristic), i), entry [i] [time]));
			}
		}
		
		public int Timestamp {
			get { return this.time; }
		}
	}
	
	[Serializable]
	public class Value {
		[XmlAttribute("name")]
		public string metricName;
		[XmlText]
		public float metricvalue;
		
		// Serialization only
		public Value () {
		}
		
		public Value (string name, float v) {
			this.metricName = name;
			this.metricvalue = v;
		}
	}
	
}
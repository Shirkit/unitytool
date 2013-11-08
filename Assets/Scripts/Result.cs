using Extra;
using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;

namespace Common {
	[Serializable]
	public class ResultsRoot
	{
		public List<ResultBatch> everything = new List<ResultBatch>();
	}
	
	[Serializable]
	public class ResultBatch
	{
		public int timeSamples = 0;
		public int gridSize = 0;
		public int rrtAttemps = 0;
		
		public List<Result> results = new List<Result>();
		
		public double averageTime = 0f;
		public int totalTries = 0;
	}
	
	[Serializable]
	public class Result
	{
		public double timeSpent = 0f;
	}
	
	[Serializable, XmlRoot("paths")]
	public class MetricsRoot
	{
		[XmlElement("path")]
		public List<PathResults> everything = new List<PathResults>();
	}
	
	[Serializable]
	public class PathResults
	{
		[XmlAttribute("name")]
		public string name;
		[XmlElement("results")]
		public List<PathValue> values;
		public float[][] total;
		
		// Serialization only
		public PathResults() {
		}
		
		public PathResults(Path path, float[][] input) {
			this.name = path.name;
			this.values = new List<PathValue>();
			for (int ttime = 0; ttime < path.points[path.points.Count - 1].t; ttime++)
				this.values.Add(new PathValue(input, ttime));
			this.total = input;
		}
	}
	
	[Serializable]
	public class PathValue
	{
		[XmlAttribute("time")]
		public int time;
		[XmlElement("metric")]
		public List<Value> values;
		
		// Serialization only
		public PathValue() {
		}
		
		public PathValue(float[][] entry, int time) {
			this.time = time;
			this.values = new List<Value>();
			for (int i = 0; i < entry.Length; i++) {
				this.values.Add(new Value(Enum.GetName(typeof(Analyzer.Heuristic), i), entry[i][time]));
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
		public Value() {
		}
		
		public Value(string name, float v) {
			this.metricName = name;
			this.metricvalue = v;
		}
	}
	
}
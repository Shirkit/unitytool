using System;
using System.Collections.Generic;

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

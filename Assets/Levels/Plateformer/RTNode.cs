using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//using Priority_Queue;


public class RTNode {
	public RTNode(Vector2 position, int frame, PlayerState state){
		this.position = position;
		this.frame = frame;
		this.state = state;
		children = new List<RTNode>();
		actions = new List<string>();
		durations = new List<int>();
	}
	public RTNode(){
		children = new List<RTNode>();
		actions = new List<string>();
		durations = new List<int>();
	}
	
	public Vector2 position;
	public int frame;
	public PlayerState state;
	public RTNode parent;
	public List<RTNode> children;
	public List<string> actions;
	public List<int> durations;
	public float h;
	public float f; 
	public float g; 
	public int statesExplored;
}

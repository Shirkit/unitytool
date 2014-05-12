using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RTNode {
	public RTNode(Vector2 position, float frame, PlayerState state){
		this.position = position;
		this.frame = frame;
		this.state = state;
	}
	public RTNode(){
	}
	public Vector2 position;
	public float frame;
	public PlayerState state;
	public RTNode parent;
	public List<RTNode> children;
	public List<string> actions;
	public List<int> durations;
}

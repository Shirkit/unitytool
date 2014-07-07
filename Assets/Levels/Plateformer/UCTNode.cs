using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UCTNode{
	public List<string> unusedActions;
	public RTNode rt;
	public int visits;
	public double delta;
	public bool dead;
	public float densityPenalty;

	public UCTNode parent;
	public List<UCTNode> children;


	public UCTNode(){
		densityPenalty = 0f;
		initUAct();
		rt = new RTNode();
		children = new List<UCTNode>();
		visits = 0;
		delta = 0;
		dead = false;
	}

	private void initUAct(){
		unusedActions = new List<string>();
		unusedActions.Add ("Left");
		unusedActions.Add ("Right");
		unusedActions.Add ("jump");
		unusedActions.Add ("jump left");
		unusedActions.Add ("jump right");
	}
}

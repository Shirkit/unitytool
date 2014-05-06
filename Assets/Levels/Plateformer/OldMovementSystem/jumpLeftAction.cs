﻿using UnityEngine;
using System.Collections;

public class jumpLeftAction : AbstractAction {
	
	private bool reachedPeak;
	
	public jumpLeftAction(GameObject pPlayer) : base(pPlayer){
		reachedPeak = false;
	}
	
	public override bool execute(int duration) {
		if(reachedPeak){
			if(curTime < duration){
				player.transform.position -= new Vector3(0.05f, 0.1f, 0);
				curTime++;
				return false;
			}
			else{
				curTime = 0;
				reachedPeak = false;
				return true;
			}
		}
		else{
			if(curTime < duration){
				player.transform.position += new Vector3(-0.05f, 0.1f, 0);
				curTime++;
				return false;
			}
			else{
				curTime = 0;
				reachedPeak = true;
				return false;
			}
		}
	}
	
}
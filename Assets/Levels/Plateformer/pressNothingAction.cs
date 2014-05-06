﻿using UnityEngine;
using System.Collections;

public class pressNothingAction : AbsAction {
	public pressNothingAction(GameObject pPlayer, PlayerState pState) : base(pPlayer, pState){
	}
	
	public override bool execute (int duration)
	{
		if(curTime < duration){
			if(state.isOnGround){
				state.velocity.x = 0;
			}
			state.adjustmentVelocity.x = 0;
			curTime++;
			return false;
			
		}
		else{
			curTime = 0;
			return true;
		}
	}
	
	
}

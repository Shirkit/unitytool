using UnityEngine;
using System.Collections;

public class pressRightAction : AbsAction {
	public pressRightAction(GameObject pPlayer, PlayerState pState) : base(pPlayer, pState){
	}

	public override bool execute (int duration)
	{
		if(curTime < duration){
			if(state.isOnGround){
				state.velocity.x = state.movementSpeed;
			}
			else{
				state.adjustmentVelocity.x = state.movementSpeed/2;
			}
			curTime++;
			if(curTime == duration){
				curTime = 0;
				return true;
			}
			return false;
			
		}
		else{
			curTime = 0;
			return true;
		}
	}


}

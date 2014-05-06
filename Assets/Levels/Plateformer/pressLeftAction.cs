using UnityEngine;
using System.Collections;

public class pressLeftAction : AbsAction {
	public pressLeftAction(GameObject pPlayer, PlayerState pState) : base(pPlayer, pState){
	}
	
	public override bool execute (int duration)
	{
		if(curTime < duration){
			if(state.isOnGround){
				state.velocity.x = -state.movementSpeed;
			}
			else{
				state.adjustmentVelocity.x = -state.movementSpeed/2;
			}
			curTime++;
			return false;
			
		}
		else{
			curTime = 0;
			return true;
		}
	}
	
	
}

using UnityEngine;
using System.Collections;

public class pressLeftAction : AbsAction {
	public pressLeftAction(GameObject pPlayer, PlayerState pState) : base(pPlayer, pState){
	}
	
	public override bool execute (int duration)
	{
		if(curTime < duration){
			state.velocity.x = -state.movementSpeed;
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

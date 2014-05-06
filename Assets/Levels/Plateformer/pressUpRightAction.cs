using UnityEngine;
using System.Collections;

public class pressUpRightAction : AbsAction {
	public pressUpRightAction(GameObject pPlayer, PlayerState pState) : base(pPlayer, pState){
	}
	
	public override bool execute (int duration)
	{
		if(state.numJumps < state.maxJumps){
			state.velocity.x = state.movementSpeed;
			state.velocity.y = state.jumpPower;
			state.numJumps++;
			state.isOnGround = false;
		}
		return true;
	}
	
	
}

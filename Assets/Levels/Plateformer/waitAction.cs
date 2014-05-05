using UnityEngine;
using System.Collections;

public class waitAction : AbstractAction {
	
	public waitAction(GameObject pPlayer) : base(pPlayer){
		
	}
	
	public override bool execute(int duration) {
		if(curTime < duration){
			curTime++;
			return false;
		}
		else{
			curTime = 0;
			return true;
		}
	}
	
}
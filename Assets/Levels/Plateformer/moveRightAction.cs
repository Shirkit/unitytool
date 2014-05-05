using UnityEngine;
using System.Collections;

public class moveRightAction : AbstractAction {
	
	public moveRightAction(GameObject pPlayer) : base(pPlayer){
		
	}

	public override bool execute(int duration) {
		if(curTime < duration){
			player.transform.position += new Vector3(0.05f, 0, 0);
			curTime++;
			return false;
		}
		else{
			curTime = 0;
			return true;
		}
	}
	
}
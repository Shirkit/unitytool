using UnityEngine;
using System.Collections;

public abstract class AbstractAction{

	protected GameObject player;
	protected int curTime;

	public AbstractAction(GameObject pPlayer){
		player = pPlayer;
		curTime = 0;
	}

	public abstract bool execute(int duration);
}

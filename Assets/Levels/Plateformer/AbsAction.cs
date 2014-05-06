using UnityEngine;
using System.Collections;

public abstract class AbsAction{

	protected GameObject player;
	protected PlayerState state;
	protected int curTime;

	public AbsAction(GameObject pPlayer, PlayerState pState){
		player = pPlayer;
		state = pState;
		curTime = 0;
	}

	public abstract bool execute(int duration);

}

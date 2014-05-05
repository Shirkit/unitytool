#pragma strict

public class AbstractAction {

	protected var player : GameObject;
	protected var curTime : int;

	public function AbstractAction(pPlayer : GameObject){
		player = pPlayer;
		curTime  = 0;
	}

	public function execute(duration : int) : boolean{
		while(curTime < duration){
			curTime++;
			return false;
		}
		curTime = 0;
		return true;
		
	}
}
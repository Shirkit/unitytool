#pragma strict

public class moveLeftAction extends AbstractAction {

	public function moveLeftAction(pPlayer : GameObject){
		super(pPlayer);
	}

	public function execute(duration : int) : boolean{
		while(curTime < duration){
			player.transform.position.x -= 0.05;
			curTime++;
			return false;
		}
		curTime = 0;
		return true;
		
	}
}
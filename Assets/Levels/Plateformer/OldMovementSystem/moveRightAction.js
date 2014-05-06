#pragma strict

public class moveRightAction extends AbstractAction {

	public function moveRightAction(pPlayer : GameObject){
		super(pPlayer);
	}

	public function execute(duration : int) : boolean{
		while(curTime < duration){
			player.transform.position.x += 0.05;
			curTime++;
			return false;
		}
		curTime = 0;
		return true;
		
	}
}
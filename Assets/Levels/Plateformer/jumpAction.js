#pragma strict

public class jumpAction extends AbstractAction {

private var reachedPeak : boolean;

	public function jumpAction(pPlayer : GameObject){
		super(pPlayer);
		reachedPeak = false;
	}

	public function execute(duration : int) : boolean{
		if(reachedPeak){
			while(curTime < duration){
				player.transform.position.y -= 0.1;
				curTime++;
				return false;
			}
		curTime = 0;
		reachedPeak = false;
		return true;
		}
		
		
		else{
			while(curTime < duration){
				player.transform.position.y += 0.1;
				curTime++;
				return false;
			}
		curTime = 0;
		reachedPeak = true;
		return false;
		}
		

		
	}
}
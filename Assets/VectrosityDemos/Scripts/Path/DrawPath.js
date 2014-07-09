import Vectrosity;

var lineMaterial : Material;
var maxPoints = 500;
var continuousUpdate = true;
var ballPrefab : Rigidbody;
var force = 16.0;

private var pathLine : VectorLine;
private var pathIndex = 0;
private var pathPoints : Vector3[];

function Start () {
	pathPoints = new Vector3[maxPoints];
	pathLine = new VectorLine("Path", pathPoints, lineMaterial, 12.0, LineType.Continuous);
	
	var ball = Instantiate(ballPrefab, Vector3(-2.25, -4.4, -1.9), Quaternion.Euler(300.0, 70.0, 310.0)) as Rigidbody;
	ball.useGravity = true;
	ball.AddForce (ball.transform.forward * force, ForceMode.Impulse);
	
	SamplePoints (ball.transform);
}

function SamplePoints (thisTransform : Transform) {
	var running = true;
	while (running) {
		pathPoints[pathIndex] = thisTransform.position;
		if (++pathIndex == maxPoints) {
			running = false;
		}
		yield WaitForSeconds(.05);
		
		if (continuousUpdate) {
			DrawPath();
		}
	}
}

function OnGUI () {
	if (!continuousUpdate && GUI.Button(Rect(10, 10, 100, 30), "Draw Path")) {
		DrawPath();
	}
}

function DrawPath () {
	if (pathIndex < 2) return;
	pathLine.maxDrawIndex = pathIndex-1;
	pathLine.Draw();
	pathLine.SetTextureScale (1.0);
}
import Vectrosity;

var lineThickness = 10.0;
var extraThickness = 2;
private var line : VectorLine;
private var points : Vector2[];
private var wasSelected = false;
private var index = 0;

function Start () {
	points = new Vector2[10];
	line = new VectorLine("SelectLine", points, null, lineThickness, LineType.Continuous, Joins.Fill);
	SetPoints();
}

function SetPoints () {
	for (var i = 0; i < points.Length; i++) {
		points[i] = Vector2(Random.Range(0, Screen.width), Random.Range(0, Screen.height-20));
	}
	line.Draw();
}

function Update () {
	if (line.Selected (Input.mousePosition, extraThickness, index)) {
		if (!wasSelected) {
			line.SetColor (Color.green);
			wasSelected = true;
		}
		if (Input.GetMouseButtonDown(0)) {
			SetPoints();
		}
	}
	else {
		if (wasSelected) {
			wasSelected = false;
			line.SetColor (Color.white);
		}
	}
}

function OnGUI () {
	GUI.Label (Rect(10, 10, 800, 30), "Click the line to make a new line");
}
import Vectrosity;

private var color1 = Color.green;
private var color2 = Color.blue;
private var line : VectorLine;
private var lineColors : Color[];
private var linePoints : Vector3[];
private var numberOfPoints = 350;
var lineMaterial : Material;
var lineWidth = 14;

function Start () {
	linePoints = new Vector3[numberOfPoints];
	lineColors = new Color[numberOfPoints-1];
	line = new VectorLine("Line", linePoints, lineColors, lineMaterial, lineWidth, LineType.Continuous);
	LineSetup (false);
}

function LineSetup (resize : boolean) {
	if (resize) {
		linePoints = new Vector3[numberOfPoints];
		lineColors = new Color[numberOfPoints-1];
		line.Resize (linePoints);
	}
	for (var i = 0; i < linePoints.Length; i++) {
		linePoints[i] = Vector3(Random.Range(-5.0, 5.0), Random.Range(-5.0, 5.0), Random.Range(-5.0, 5.0));
	}
	SetColors();
}

function SetColors () {
	for (var i = 0; i < lineColors.Length; i++) {
		lineColors[i] = Color.Lerp(color1, color2, (i+0.0)/lineColors.Length);
	}
	line.SetColors (lineColors);
}

function LateUpdate () {
	line.Draw (transform);
}

function OnGUI() {
	GUI.Label(Rect(20, 10, 250, 30), "Zoom with scrollwheel or arrow keys");
	if (GUI.Button(Rect(20, 50, 100, 30), "Change colors")) {
		var component1 = Random.Range(0, 3);
		var component2 = Random.Range(0, 3);
		while (component2 == component1) {
			component2 = Random.Range(0, 3);
		}
		// Make sure colors are different, since different color components will be set to 0
		color1 = RandomColor(color1, component1);
		color2 = RandomColor(color2, component2);
		SetColors();
	}
	GUI.Label(Rect(20, 100, 150, 30), "Number of points: " + numberOfPoints);
	numberOfPoints = GUI.HorizontalSlider(Rect(20, 130, 120, 30), numberOfPoints, 50, 1000);
	if (GUI.Button(Rect(160, 120, 40, 30), "Set")) {
		LineSetup (true);
	}
}

function RandomColor (color : Color, component : int) : Color {
	for (var i = 0; i < 3; i++) {
		if (i == component) {
			color[i] = Random.value*.25;
		}
		else {
			color[i] = Random.value*.5 + .5;
		}
	}
	return color;
}
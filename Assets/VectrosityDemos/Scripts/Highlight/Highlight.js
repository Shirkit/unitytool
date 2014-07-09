import Vectrosity;

var lineMaterial : Material;
var lineMaterial2 : Material;
var lineWidth = 5;
var energyLineWidth = 4;
var selectionSize = .5;
var force = 20.0;
var pointsInEnergyLine = 100;

private var line : VectorLine;
private var energyLine : VectorLine;
private var linePoints : Vector2[];
private var energyLinePoints : Vector2[];
private var hit : RaycastHit;
private var selectIndex = 0;
private var energyLevel = 0.0;
private var canClick : boolean;
private var spheres : GameObject[];
private var timer : double = 0.0;
private var maxSelections : int;
private var oldWidth : int;
private var ignoreLayer : int;
private var defaultLayer : int;
private var fading = false;

function Start () {
	maxSelections = (GetComponent(MakeSpheres) as MakeSpheres).numberOfSpheres;
	spheres = new GameObject[maxSelections];
	oldWidth = Screen.width;
	ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");
	defaultLayer = LayerMask.NameToLayer("Default");

	// Set up the two lines
	linePoints = new Vector2[10 * maxSelections];
	line = new VectorLine("Line", linePoints, lineMaterial, lineWidth);
	line.capLength = lineWidth*.5;
	energyLinePoints = new Vector2[pointsInEnergyLine];
	energyLine = new VectorLine("Energy", energyLinePoints, lineMaterial2, energyLineWidth, LineType.Continuous);
	SetEnergyLinePoints();
}

function SetEnergyLinePoints () {
	for (var i = 0; i < energyLinePoints.Length; i++) {
		var xPoint = Mathf.Lerp(70, Screen.width-20, (i+0.0)/energyLinePoints.Length);
		energyLinePoints[i] = Vector2(xPoint, Screen.height*.1);
	}
}

function Update () {
	// Don't allow clicking in the left-most 50 pixels (where the slider is), or if the spheres are currently fading
	if (Input.GetMouseButtonDown(0) && Input.mousePosition.x > 50 && !fading) {
		// If neither shift key is down, reset selection
		if (!(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && selectIndex > 0) {
			ResetSelection();
		}
		// See if we clicked on an object (the room is set to the IgnoreRaycast layer, so we can't select it)
		if (selectIndex < maxSelections && Physics.Raycast (Camera.main.ScreenPointToRay(Input.mousePosition), hit, 100.0)) {
			spheres[selectIndex] = hit.collider.gameObject;
			spheres[selectIndex].layer = ignoreLayer;	// So it can't be clicked again (unless reset)
			selectIndex++;
		}
	}
	
	// Draw a square for each selected object
	for (var i = 0; i < selectIndex; i++) {
		// Make the size of the square larger or smaller depending on the object's Z distance from the camera
		var squareSize = (Screen.height * selectionSize) / Camera.main.transform.InverseTransformPoint(spheres[i].transform.position).z;
		var screenPoint = Camera.main.WorldToScreenPoint(spheres[i].transform.position);
		var thisSquare = Rect(screenPoint.x-squareSize, screenPoint.y+squareSize, squareSize*2, squareSize*2);
		line.MakeRect (thisSquare, i*10);
		// Make a line connecting from the midpoint of the square's left edge to the energyLevel slider position
		linePoints[i*10 + 8] = Vector2(thisSquare.x - lineWidth*.25, thisSquare.y - squareSize);
		linePoints[i*10 + 9] = Vector2(35, Mathf.Lerp(65, Screen.height-25, energyLevel));
		// Change color of selected objects
		spheres[i].renderer.material.SetColor("_Emission", Color(energyLevel, energyLevel, energyLevel));
		spheres[i].renderer.material.color.a = energyLevel * .25;
	}
	
	// Redo energy line points if screen resolution changes
	if (Screen.width != oldWidth) {
		oldWidth = Screen.width;
		SetEnergyLinePoints();
	}
}

function FixedUpdate () {
	// Move y position of all points to the left by one
	for (var i = 0; i < energyLinePoints.Length-1; i++) {
		energyLinePoints[i].y = energyLinePoints[i+1].y;
	}
	// Calculate new point based on the energy level and time
	timer += Time.deltaTime * Mathf.Lerp(5.0, 20.0, energyLevel);
	energyLinePoints[i].y = Screen.height * (.1 + Mathf.Sin(timer) * .08 * energyLevel);
}

function LateUpdate () {
	line.Draw();
	energyLine.Draw();
}

function ResetSelection () {
	// Fade sphere colors back to normal
	if (energyLevel > 0.0) {
		FadeColor();
	}
	// Reset the selection index and erase all squares and lines that might have been made
	selectIndex = 0;
	energyLevel = 0.0;
	line.ZeroPoints();
	// Reset sphere layers so they can be clicked again
	for (sphere in spheres) {
		if (sphere) sphere.layer = defaultLayer;
	}
}

function FadeColor () {
	// Do the fade
	fading = true;
	var startColor = Color(energyLevel, energyLevel, energyLevel, 0.0);
	var startAlpha = energyLevel*.25;
	var thisIndex = selectIndex;	// Since selectIndex is set back to 0 this frame
	for (var t = 0.0; t < 1.0; t += Time.deltaTime) {
		for (var i = 0; i < thisIndex; i++) {
			spheres[i].renderer.material.SetColor("_Emission", Color.Lerp(startColor, Color.black, t));
			spheres[i].renderer.material.color.a = Mathf.Lerp(startAlpha, 0.0, t);
		}
		yield;
	}
	fading = false;
}

function OnGUI () {
	GUI.Label(Rect(60, 20, 600, 40), "Click to select sphere, shift-click to select multiple spheres\nThen change energy level slider and click Go");
	energyLevel = GUI.VerticalSlider(Rect(30, 20, 10, Screen.height-80), energyLevel, 1.0, 0.0);
	// Prevent energy slider from working if nothing is selected
	if (selectIndex == 0) {
		energyLevel = 0.0;
	}
	if (GUI.Button(Rect(20, Screen.height-40, 32, 20), "Go")) {
		for (var i = 0; i < selectIndex; i++) {
			spheres[i].rigidbody.AddRelativeForce(Vector3.forward * force * energyLevel, ForceMode.VelocityChange);
		}
		ResetSelection();
	}
}
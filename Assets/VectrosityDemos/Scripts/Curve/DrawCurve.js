import Vectrosity;

var lineMaterial : Material;
var dottedLineMaterial : Material;
var segments = 60;

var anchorPoint : GameObject;
var controlPoint : GameObject;

private var numberOfCurves = 1;
private var curvePoints : Vector2[];

private var linePoints : Vector2[];
private var line : VectorLine;

private var controlLine : VectorLine;

private var pointIndex = 0;
static var use : DrawCurve;
static var cam : Camera;
private var anchorObject : GameObject;
private var oldWidth : int;
private var useDottedLine = false;
private var oldDottedLineSetting = false;
private var listPoints = false;

function Start () {
	use = this;	// Reference to this script, so FindObjectOfType etc. are not needed
	cam = Camera.main;
	oldWidth = Screen.width;

	// Set up initial curve points (also used for drawing the green lines that connect control points to anchor points)
	curvePoints = new Vector2[4];
	curvePoints[0] = Vector2(Screen.width*.25, Screen.height*.25);
	curvePoints[1] = Vector2(Screen.width*.125, Screen.height*.5);
	curvePoints[2] = Vector2(Screen.width-Screen.width*.25, Screen.height-Screen.height*.25);
	curvePoints[3] = Vector2(Screen.width-Screen.width*.125, Screen.height*.5);
	
	// Make the GUITexture objects for anchor and control points (two anchor points and two control points)
	AddControlObjects();
	AddControlObjects();	
	
	// Make the control lines
	controlLine = new VectorLine("Control Line", curvePoints, Color(0.0, .75, .1, .6), lineMaterial, 2.0);
	controlLine.Draw();
	
	// Make the line object for the curve
	linePoints = new Vector2[segments+1];
	SetLine();
	// Create a curve in the VectorLine object
	line.MakeCurve (curvePoints[0], curvePoints[1], curvePoints[2], curvePoints[3], segments);
	
	DrawLine();
}

function SetLine () {
	if (useDottedLine) {
		line = new VectorLine("Curve", linePoints, dottedLineMaterial, 8.0, LineType.Continuous, Joins.Weld);
		line.depth = 1;
	}
	else {
		line = new VectorLine("Curve", linePoints, lineMaterial, 5.0, LineType.Continuous);
		line.depth = 1;		
	}
}

function DrawLine () {
	line.Draw();
	if (useDottedLine) {
		line.SetTextureScale(1.0);
	}
}

function AddControlObjects () {
	anchorObject = Instantiate(anchorPoint, cam.ScreenToViewportPoint(curvePoints[pointIndex]), Quaternion.identity);
	(anchorObject.GetComponent(CurvePointControl) as CurvePointControl).objectNumber = pointIndex++;
	var controlObject : GameObject = Instantiate(controlPoint, cam.ScreenToViewportPoint(curvePoints[pointIndex]), Quaternion.identity);
	(controlObject.GetComponent(CurvePointControl) as CurvePointControl).objectNumber = pointIndex++;
	// Make the anchor object have a reference to the control object, so they can move together
	// Having control objects be children of anchor objects would be easier, but parent/child doesn't really work with GUITextures
	(anchorObject.GetComponent(CurvePointControl) as CurvePointControl).controlObject = controlObject;
}

function UpdateLine (objectNumber : int, pos : Vector2, go : GameObject) {
	var oldPos = curvePoints[objectNumber];	// Get previous position, so we can make the control point move with the anchor point
	curvePoints[objectNumber] = pos;
	var curveNumber : int = objectNumber / 4;
	var curveIndex = curveNumber * 4;
	line.MakeCurve (curvePoints[curveIndex], curvePoints[curveIndex+1], curvePoints[curveIndex+2], curvePoints[curveIndex+3], segments,
		curveNumber * (segments+1));
		
	// If it's an anchor point...
	if (objectNumber % 2 == 0) {
		// Move control point also
		curvePoints[objectNumber+1] += pos-oldPos;
		(go.GetComponent(CurvePointControl) as CurvePointControl).controlObject.transform.position = cam.ScreenToViewportPoint(curvePoints[objectNumber+1]);
		// If it's not an end anchor point, move the next anchor/control points as well, and update the next curve
	 	if (objectNumber > 0 && objectNumber < curvePoints.Length-2) {
			curvePoints[objectNumber+2] = pos;
			curvePoints[objectNumber+3] += pos-oldPos;
			(go.GetComponent(CurvePointControl) as CurvePointControl).controlObject2.transform.position = cam.ScreenToViewportPoint(curvePoints[objectNumber+3]);
			line.MakeCurve (curvePoints[curveIndex+4], curvePoints[curveIndex+5], curvePoints[curveIndex+6], curvePoints[curveIndex+7], segments,
				(curveNumber+1) * (segments+1));
		}
	}
	
	DrawLine();
	controlLine.Draw();	
}

function OnGUI () {
	if (GUI.Button(Rect(20, 20, 100, 30), "Add Point")) {
		AddPoint();
	}
	
	GUI.Label(Rect(20, 59, 200, 30), "Curve resolution: " + segments);
	segments = GUI.HorizontalSlider(Rect(20, 80, 150, 30), segments, 3, 60);
	if (GUI.changed) {
		ChangeSegments();
	}
	
	useDottedLine = GUI.Toggle(Rect(20, 105, 80, 20), useDottedLine, " Dotted line");
	if (oldDottedLineSetting != useDottedLine) {
		oldDottedLineSetting = useDottedLine;
		VectorLine.Destroy (line);
		SetLine();
		DrawLine();
	}
	
	GUILayout.BeginArea(Rect(20, 150, 150, 800));
	if (GUILayout.Button(listPoints? "Hide points" : "List points", GUILayout.Width(100)) ) {
		listPoints = !listPoints;
	}
	if (listPoints) {
		var idx = 0;
		for (var i = 0; i < curvePoints.Length; i += 2) {
			GUILayout.Label("Anchor " + idx + ": (" + parseInt(curvePoints[i].x) + ", " + parseInt(curvePoints[i].y) + ")");
			GUILayout.Label("Control " + idx++ + ": (" + parseInt(curvePoints[i+1].x) + ", " + parseInt(curvePoints[i+1].y) + ")");
		}
	}
	GUILayout.EndArea();
}

function AddPoint () {
	// Don't do anything if adding a new point would exceed the max number of vertices per mesh
	if (line.points2.Length + segments > 16384) return;
	
	System.Array.Resize.<Vector2>(linePoints, (segments+1) * ++numberOfCurves);
	System.Array.Resize.<Vector2>(curvePoints, numberOfCurves*4);
	
	// Make the first anchor and control points of the new curve be the same as the second anchor/control points of the previous curve
	curvePoints[pointIndex] = curvePoints[pointIndex-2];
	curvePoints[pointIndex+1] = curvePoints[pointIndex-1];
	// Make the second anchor/control points of the new curve be offset a little ways from the first
	var offset = (curvePoints[pointIndex-2] - curvePoints[pointIndex-4]) * .25;
	curvePoints[pointIndex+2] = curvePoints[pointIndex-2] + offset;
	curvePoints[pointIndex+3] = curvePoints[pointIndex-1] + offset;
	// If that made the new anchor point go off the screen, offset them the opposite way
	if (curvePoints[pointIndex+2].x > Screen.width || curvePoints[pointIndex+2].y > Screen.height ||
			curvePoints[pointIndex+2].x < 0 || curvePoints[pointIndex+2].y < 0) {
		curvePoints[pointIndex+2] = curvePoints[pointIndex-2] - offset;
		curvePoints[pointIndex+3] = curvePoints[pointIndex-1] - offset;
	}
	// For the next control point, make the initial position offset from the anchor point the opposite way as the second control point in the curve
	var controlPointPos = curvePoints[pointIndex-1] + (curvePoints[pointIndex] - curvePoints[pointIndex-1])*2;
	pointIndex++;	// Skip the next anchor point, since we want the second anchor point of one curve and the first anchor point of the next curve
					// to move together (this is handled in UpdateLine)
	curvePoints[pointIndex] = controlPointPos;
	// Make another control point
	var controlObject : GameObject = Instantiate(controlPoint, cam.ScreenToViewportPoint(controlPointPos), Quaternion.identity);
	(controlObject.GetComponent(CurvePointControl) as CurvePointControl).objectNumber = pointIndex++;
	// For the last anchor object that was made, make a reference to this control point so they can move together
	(anchorObject.GetComponent(CurvePointControl) as CurvePointControl).controlObject2 = controlObject;
	// Then make another anchor/control point group
	AddControlObjects();
	
	// Update the control lines
	controlLine.Resize(curvePoints);
	controlLine.Draw();
	
	// Update the curve with the new points
	line.Resize(linePoints);
	line.MakeCurve (curvePoints[pointIndex-4], curvePoints[pointIndex-3], curvePoints[pointIndex-2], curvePoints[pointIndex-1], segments,
		(segments+1) * (numberOfCurves-1));
	DrawLine();
}

function ChangeSegments () {
	// Don't do anything if the requested segments would make the curve exceed the max number of vertices per mesh
	if (segments*4*numberOfCurves > 65534) return;
	
	linePoints = new Vector2[(segments+1) * numberOfCurves];
	line.Resize(linePoints);
	for (var i = 0; i < numberOfCurves; i++) {
		line.MakeCurve (curvePoints[i*4], curvePoints[i*4+1], curvePoints[i*4+2], curvePoints[i*4+3], segments, (segments+1)*i);
	}
	DrawLine();
}

function Update () {
	if (Screen.width != oldWidth) {
		oldWidth = Screen.width;
		ChangeResolution();
	}
}

function ChangeResolution () {
	VectorLine.SetCamera();
	controlLine.Draw();
	DrawLine();
	var controlPointObjects = GameObject.FindGameObjectsWithTag("GameController");
	for (obj in controlPointObjects) {
		obj.transform.position = cam.ScreenToViewportPoint(curvePoints[(obj.GetComponent(CurvePointControl) as CurvePointControl).objectNumber]);
	}
}
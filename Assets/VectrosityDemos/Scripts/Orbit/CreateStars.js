import Vectrosity;

var numberOfStars = 2000;
var stars : VectorPoints;

private var oldWidth : int;

function Start () {
	// Make a bunch of points in a spherical distribution
	var starPoints = new Vector3[numberOfStars];
	for (var i = 0; i < numberOfStars; i++) {
		starPoints[i] = Random.onUnitSphere * 100.0;
	}
	// Make each star have a size of 1 or 2
	var starSizes = new float[numberOfStars];
	for (i = 0; i < numberOfStars; i++) {
		starSizes[i] = Random.Range(1, 3);
	}
	// Make each star have a random shade of grey
	var starColors = new Color[numberOfStars];
	for (i = 0; i < numberOfStars; i++) {
		var thisValue = Random.value * .75 + .25;
		starColors[i] = Color(thisValue, thisValue, thisValue);
	}
	
	SetCam();
	
	stars = new VectorPoints("Stars", starPoints, starColors, null, 1.0);
	stars.SetWidths (starSizes);
	
	oldWidth = Screen.width;
}

function LateUpdate () {
	stars.Draw();
	// Handle resolution changes at runtime (such as going fullscreen in the webplayer)
	if (Screen.width != oldWidth) {
		oldWidth = Screen.width;
		SetCam();
	}
}

function SetCam () {
	// We want the stars to be drawn behind everything else, like a skybox. So we set the vector camera
	// to have a lower depth than the main camera, and make it have a solid black background
	var vectorCam = VectorLine.SetCamera(CameraClearFlags.SolidColor);
	vectorCam.backgroundColor = Color.black;
	vectorCam.depth = Camera.main.depth - 1;
	// Set the main camera's clearFlags to depth only, so the vector cam shows through the background
	Camera.main.clearFlags = CameraClearFlags.Depth;	
}
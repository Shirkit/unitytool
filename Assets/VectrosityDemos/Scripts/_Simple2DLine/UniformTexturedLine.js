import Vectrosity;

var lineMaterial : Material;
var lineWidth = 8.0;
var textureScale = 1.0;

function Start () {
	// Make Vector2 array with 2 elements...
	var linePoints = [Vector2(0, Random.Range(0, Screen.height/2)),				// ...one on the left side of the screen somewhere
					  Vector2(Screen.width-1, Random.Range(0, Screen.height))];	// ...and one on the right
	
	// Make a VectorLine object using the above points and material
	var line = new VectorLine("Line", linePoints, lineMaterial, lineWidth);
	
	// Draw the line and set the texture scale
	line.Draw();
	line.SetTextureScale (textureScale);
}
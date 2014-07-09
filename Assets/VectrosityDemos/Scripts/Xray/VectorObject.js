import Vectrosity;

enum Shape {Cube = 0, Sphere = 1}
var shape = Shape.Cube;

function Start () {
	var line = new VectorLine ("Shape", XrayLineData.use.shapePoints[shape], Color.green, XrayLineData.use.lineMaterial, XrayLineData.use.lineWidth);
	VectorManager.ObjectSetup (gameObject, line, Visibility.Always, Brightness.None);
}
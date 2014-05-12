using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using Vectrosity;

public class posMovModel : MonoBehaviour {

	public GameObject player;
	public int curFrame;
	public List<Vector3> positions;
	public Color color;
	public bool pathComputed = false;
	public Vector3[] pointsArray;


	public posMovModel(){

	}

	public posMovModel(GameObject pPlayer, List<Vector3> pPositions){
		player = pPlayer;
		positions = pPositions;
		curFrame = 0;
	}


	public bool runFrames(int num){
		bool toReturn = false;
		for(int i = 0; i < num; i++){
			toReturn = updater();
		}
		return toReturn;
	}
	
	public bool updater(){
		curFrame++;
		bool toReturn;
		if(curFrame%5 < positions.Count){
			player.transform.position = positions[curFrame%5];
			toReturn = false;
		}
		else{
			toReturn = true;
		}
		return toReturn;		
	}

	public bool goToFrame(int frame){
		curFrame = frame - 1;
		return updater();
	}

	private void computePath(GameObject paths){
		pointsArray = new Vector3[positions.Count];
		int i = 0;
		foreach(Vector3 point in positions){
			pointsArray[i] = point;
			i++;
		}
		
		VectorLine line = new VectorLine("path" + gameObject.name.Substring(6), pointsArray, color, null, 2.0f, LineType.Continuous);
		line.Draw3D();
		line.vectorObject.transform.parent = paths.transform;
		pathComputed = true;
	}



	public void drawPath(GameObject paths){
		if(!pathComputed){
			computePath(paths);
		}
		else{
			VectorLine line = new VectorLine("path" + gameObject.name.Substring(6), pointsArray, color, null, 2.0f, LineType.Continuous);
			line.Draw3D();
			line.vectorObject.transform.parent = paths.transform;
		}
	}

	public serializablePosMovModel toSerializable(){
		return new serializablePosMovModel(positions, positions.Count*5, positions[0]);
	}
}



[XmlRoot("Pos")]
public class serializablePosMovModel{
	[XmlArray("Positions")]
	[XmlArrayItem("position")]
	public List<Vector3> positions;
	
	public int numFrames;
	public Vector3 startLoc;
	
	
	public serializablePosMovModel(){
	}
	
	public serializablePosMovModel(List<Vector3> pPositions,int pFrames, Vector3 pSLoc){
		positions = pPositions;
		numFrames = pFrames;
		startLoc = pSLoc;
	}
}
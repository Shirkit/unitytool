using UnityEngine;
using System.Collections;

public class PlayerInfo {
	public static int userid = 0;
	public static int ansQ1 = 0;
	public static int ansQ2 = 0;
	public static int curLevel = 0;	
	public static int pathNumber = 0;
	public static bool mute = false;

	public static string toStringIncr(){
		pathNumber++;
		return userid + "," + ansQ1 +"," + ansQ2 +"," + curLevel + "," + pathNumber;
	}
}

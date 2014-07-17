using UnityEngine;
using System.Collections;

public class PlayerInfo {
	public static string username = "";
	public static int ansQ1 = 0;
	public static int ansQ2 = 0;
	public static int curLevel = 0;	
	public static int pathNumber = 0;

	public static void toStringIncr(){
		pathNumber++;
		return username + "," + ansQ1 +"," + ansQ2 +"," + curLevel + "," + pathNumber;
	}
}

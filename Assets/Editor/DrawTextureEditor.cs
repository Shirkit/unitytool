using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(DrawTexture))]

public class DrawTextureEditor : Editor 
{

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector(); 

		if(GUILayout.Button("Draw Texture"))
		{
			GameObject g = GameObject.Find("DrawTexture");
			if(g!= null)
			{
				DrawTexture t = g.GetComponent<DrawTexture>(); 
				t.DrawSomething(); 
			}
		}
	}

}

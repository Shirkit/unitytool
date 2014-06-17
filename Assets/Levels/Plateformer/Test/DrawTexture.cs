using UnityEngine;
using System.Collections;
using System.IO;
using UnityEditor;

[ExecuteInEditMode]
public class DrawTexture : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void DrawSomething()
	{
		//Create a white sprite
		Texture2D texture = new Texture2D(128,128);
//		

		int y = 0;
		while (y < texture.height) 
		{
			int x = 0;

			while (x < texture.width) 
			{
				Color color = ((x == y) ? Color.white : Color.black);
				texture.SetPixel(x, y, color);
				++x;
			}
			++y;
		}

		texture.Apply(); 

		byte[] bytes = texture.EncodeToPNG();
		File.WriteAllBytes(Application.dataPath + 
			"/Levels/Plateformer/Graphics/SavedScreen.png", bytes);


		SpriteRenderer r = gameObject.GetComponent<SpriteRenderer>(); 



		Sprite s = AssetDatabase.LoadAssetAtPath(
			"Assets/Levels/Plateformer/Graphics/SavedScreen.png", typeof(Sprite)) as Sprite;

		r.sprite = s;  

	}	
}

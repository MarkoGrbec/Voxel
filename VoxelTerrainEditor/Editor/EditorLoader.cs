using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEditor.SceneManagement;

namespace VoxelTerrain
{
	public class EditorLoader : EditorWindow
	{

		// Add menu item named "Voxel" to the Window menu
		[MenuItem("Window/Voxel/VoxelEditorLoader")]
		public static void ShowWindow()
		{
			//Show existing window instance. If one doesn't exist, make one.
			EditorWindow.GetWindow(typeof(EditorLoader));
			//EditorWindow.GetWindow(typeof(EditorLoader)).minSize=new Vector2(300,200);
		}



		void OnGUI()
		{
			string[] levelList = SaveLoad.getSavedMaps();
			//GUILayout.Label("Click Prepare Terrain Before Loading Terrain");
			//if(GUILayout.Button("Prepare Terrain")){
			// DestroyImmediate(GameObject.Find("VTerrain").GetComponent<VoxTerrain>());
			// GameObject.Find("VTerrain").AddComponent<VoxTerrain>();
			//}

			GUILayout.Label("select Level to load", EditorStyles.boldLabel);

			foreach (string levelName in levelList)
			{
				if (GUILayout.Button(levelName))
				{
					VoxTerrain.LoadFromFile(Path.GetFileName(levelName));
				}
			}

		}
		public void Update()
		{
			// Update Voxel Terrain in Editor
			//Repaint(); VoxTerrain.Instance.Update();
		}

	}
}
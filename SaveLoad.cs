using UnityEngine;
using System.IO;

namespace VoxelTerrain
{
	// this class can save and load terrains to binary file
	public class SaveLoad : MonoBehaviour
	{
		// convert 3d array to 1d array, so we can save it to binary file easy
		public static byte[] mapTo1D(byte[,,] _map, int width, int height, int depth)
		{
			byte[] map1d = new byte[(width + 1) * (height + 1) * (depth + 1)];
			int counter = 0;
			for (int x = 0; x < width + 1; x++)
			{
				for (int y = 0; y < height + 1; y++)
				{
					for (int z = 0; z < depth + 1; z++)
					{
						map1d[counter] = _map[x, y, z];
						counter++;
					}

				}


			}
			return map1d;
		}
		// converts 1d array to 3d array
		public static byte[,,] mapTo3D(byte[] map1d, int width, int height, int depth)
		{
			byte[,,] _map = new byte[width + 1, height + 1, depth + 1];
			int counter = 0;
			for (int x = 0; x < width + 1; x++)
			{
				for (int y = 0; y < height + 1; y++)
				{
					for (int z = 0; z < depth + 1; z++)
					{
						_map[x, y, z] = map1d[counter];
						counter++;
					}

				}
			}
			return _map;
		}
		// convert 3d color array to 1d color array, so we can save it to binary file easy
		public static short[] colorsTo1D(short[,,] _colors, int width, int height, int depth)
		{
			short[] colors1d = new short[(width + 1) * (height + 1) * (depth + 1)];
			int counter = 0;
			for (int x = 0; x < width + 1; x++)
			{
				for (int y = 0; y < height + 1; y++)
				{
					for (int z = 0; z < depth + 1; z++)
					{
						colors1d[counter] = _colors[x, y, z];
						counter++;
					}

				}


			}
			return colors1d;
		}

		// converts color 1d array to color 3d array
		public static short[,,] colorsTo3D(short[] colors1d, int width, int height, int depth)
		{
			short[,,] _colors = new short[width + 1, height + 1, depth + 1];
			int counter = 0;
			for (int x = 0; x < width + 1; x++)
			{
				for (int y = 0; y < height + 1; y++)
				{
					for (int z = 0; z < depth + 1; z++)
					{
						_colors[x, y, z] = colors1d[counter];
						counter++;
					}

				}


			}
			return _colors;
		}
		//save map to binary file
		public static void saveMap(byte[,,] _map, short[,,] _colors, int width, int height, int depth, string fileName, Material _material)
		{
			byte[] map1d = mapTo1D(_map, width, height, depth);
			short[] colors1d = colorsTo1D(_colors, width, height, depth);

			string path = Application.streamingAssetsPath + "/VoxelLevels" + "/";
			// Mobile devices can not write to the streamingAssetsPath so we use persistentDataPath instead.
			if (Application.isMobilePlatform)
				path = Application.persistentDataPath + "/VoxelLevels" + "/";

			if (Directory.Exists(path) == false)
				Directory.CreateDirectory(path);

			using (BinaryWriter writer = new BinaryWriter(File.Open(path + fileName + ".bytes", FileMode.Create)))
			{
				writer.Write(map1d.Length);
				writer.Write(width);
				writer.Write(height);
				writer.Write(depth);
				writer.Write(_material.name);
				writer.Write(map1d);
				writer.Write(colors1d.Length);
				for (int i = 0; i < colors1d.Length; i++)
				{
					writer.Write(colors1d[i]); //textureID

				}
				writer.Close();
			}


		}
		//load map to binary file
		public static void loadMap(string fileName, out int width, out int height, out int depth, out byte[,,] _map, out short[,,] _colors, out Material _material)
		{
			string path = Application.streamingAssetsPath + "/VoxelLevels" + "/" + fileName + ".bytes";
			// Mobile devices can not write to the streamingAssetsPath so we use persistentDataPath instead.
			if (Application.isMobilePlatform)
				path = Application.persistentDataPath + "/VoxelLevels" + "/" + fileName + ".bytes";

			Stream s = new MemoryStream(File.ReadAllBytes(path));
			BinaryReader reader = new BinaryReader(s);
			int arrayLength = reader.ReadInt32();
			width = reader.ReadInt32();
			height = reader.ReadInt32();
			depth = reader.ReadInt32();
			_material = Resources.Load("VoxelResources/"+reader.ReadString()) as Material;
			byte[] map1d = reader.ReadBytes(arrayLength);
			_map = mapTo3D(map1d, width, height, depth);
			int colorsCount = reader.ReadInt32();
			short[] colors1d = new short[colorsCount];
			for (int i = 0; i < colorsCount; i++)
			{
				colors1d[i] = reader.ReadInt16(); // textureID
			}
			_colors = colorsTo3D(colors1d, width, height, depth);
			reader.Close();
		}

		// get list of saved files
		public static string[] getSavedMaps()
		{
			//return System.IO.Directory.GetFiles(@""+Application.persistentDataPath+"/", "*.bytes");
			string path = Application.streamingAssetsPath + "/VoxelLevels";
			// Mobile devices can not write to the streamingAssetsPath so we use persistentDataPath instead.
			if (Application.isMobilePlatform)
				path = Application.persistentDataPath + "/VoxelLevels";

			FileInfo[] fileInfo = new FileInfo(path).Directory.GetFiles("*.bytes", SearchOption.AllDirectories);
			string[] levels = new string[fileInfo.Length];
			int index = 0;
			foreach (FileInfo file in fileInfo) { string levelName = file.Name; levelName = file.Name.Split('.')[0]; levels[index++] = levelName; }
			return levels;
		}
	}
}

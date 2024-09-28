using UnityEngine;
using System.Collections.Generic;
using VoxelTerrain;

namespace VoxelTerrain
{
	[ExecuteInEditMode]
	public class VoxTerrain : MonoBehaviour
	{
		// brushes
		public enum OBJ
		{
			SPHERE,
			CUBE,
			RANDOM,
			PLANE,
			HEIGHTMAP

		}
		// brush effects
		public enum EFFECT
		{
			ADD,
			SUB,
			PAINT
		}

		public static VoxTerrain Instance { get; private set; }

		[Header("Terrain file name")]
		[Header("If terrain with specified name do not exist, new one will be created")]
		public string TerrainName;

		[Header("To change size, create a new terrain (Assign new TerrainName)", order = 1)]

		//size of the terrain cube
		[Header("Width of the terrain cube", order = 2)]
		public int width = 128;

		[Header("Height of the terrain cube", order = 2)]
		public int height = 128;

		[Header("Depth of the terrain cube", order = 2)]
		public int depth = 128;

		[Space(20)]
		[Header("Chunk Resolution")]
		[Tooltip("Number of x, y, z in one cube if RES=25 and cube size=100^3 then terrain will be separated on 4^3 chunks ")]
		public int RES = 32;

		private bool firstLoad = false;

		public static Texture2D heightmap = null;
		public static float heightmapDepth = 0.20f;

		public List<Cube> _cubes;               // All cubes
		private List<Cube> _reBuild;            // Cubes for rebuild
		private List<Cube> _reBuildCollider;    // Cube Colliders for rebuild
		private int _reBuildColliderCount;      // Number of colliders to rebuild

		public List<Cube> _cubesClient;
		private List<Cube> _reBuildClient;
		private List<Cube> _reBuildColliderClient;
		private int _reBuildColliderCountClient;

		// 3d arrays
		public byte[,,] _map;
		public short[,,] _colors;  // contains texture indexes, RGBA colors.
		[HideInInspector]
		public Material _material;

		GameObject handler;
		GameObject handlerClient;

		[Space(20)]
		//public static string levelToLoad;
		public Color[] textureChannels = new Color[]{
			new Color(1F,0F,0F,0F), //texture 1
			new Color(0F,1F,0F,0F), //texture 2
			new Color(0F,0F,1F,0F), //texture 3
			new Color(0F,0F,0F,1F)  //White or no texture
		};

		public void Start()
		{
			if (string.IsNullOrEmpty(TerrainName))
			{
				Debug.Log("TerrainName is null. Variable TerrainName need to be set.");
				return;
			}

			if (GameObject.Find("Terrain") != null) { DestroyImmediate(GameObject.Find("Terrain")); }
			handler = new GameObject(); handler.name = "Terrain";

			if (GameObject.Find("TerrainClient") != null) { DestroyImmediate(GameObject.Find("TerrainClient")); }
			handlerClient = new GameObject(); handlerClient.name = "TerrainClient";

			//if(_instance==null)
			Instance = this;

			_cubes = new List<Cube>();
			_reBuild = new List<Cube>();
			_reBuildCollider = new List<Cube>();
			_reBuildColliderCount = 0;

			_cubesClient = new List<Cube>();
			_reBuildClient = new List<Cube>();
			_reBuildColliderClient= new List<Cube>();
			_reBuildColliderCountClient = 0;

			// Load terrain if exist, or load empty world if not.
			bool loadedFromFile = LoadMapFromFile();

			var _renderer = GetComponent<MeshRenderer>();
			if (_renderer == null)
			{
				_renderer = gameObject.AddComponent<MeshRenderer>();
			}

			if (_material == null)
			{
				_material = _renderer.sharedMaterial;
			}

			_renderer.sharedMaterial = _material;

			// Instantiate all cubes
			for (int x = 0; x < width / RES; x++)
				for (int y = 0; y < height / RES; y++)
					for (int z = 0; z < depth / RES; z++)
					{
						Bounds cubeBounds = new Bounds();
						cubeBounds.min = new Vector3(x, y, z) * RES;
						cubeBounds.max = cubeBounds.min + new Vector3(RES, RES, RES);
						Cube cube = new Cube(false, cubeBounds, this);
						_cubes.Add(cube);

						Bounds cubeBoundsClient = new Bounds();
						cubeBoundsClient.min = new Vector3(x, y, z) * RES;
						cubeBoundsClient.max = cubeBoundsClient.min + new Vector3(RES, RES, RES);
						Cube cubeClient = new Cube(true, cubeBoundsClient, this);
						_cubesClient.Add(cubeClient);
					}

			if (_map == null || _colors == null)
			{
				_map = new byte[width + 1, height + 1, depth + 1];
				_colors = new short[width + 1, height + 1, depth + 1];
				// Initialise all point in map
				ResetMap();
			}

			if (loadedFromFile)
			{
				DrawLoadedMap();
			}

			if (heightmap != null)
			{
				/*if you want to load terrain from heightmap then change heightmap to 
				  heightmap=Resources.Load("terrain you want to load") as Texture2D; */

				//resize texture to equals terrain size
				heightmap = ScaleTexture(heightmap, width, depth);
				DrawTerrainFromHeightmap();
			}
			ReBuildCollider();
			ReBuildColliderClient();
		}

		// build one cube per frame and collider when ReBuildCollider is call
		public void Update()
		{
			//_instance=this; 
			// Build mesh
			if (_reBuild != null)
			{
				if (_reBuild.Count != 0)
				{
					_reBuild[0].ReBuild();
					_reBuild.RemoveAt(0);
				}
			}

			// Build collider
			if (_reBuildColliderCount != 0)
			{
				// Build all cube in the last ReBuildCollider() call
				_reBuildColliderCount--;
				_reBuildCollider[0].ReBuildCollider();
				_reBuildCollider.RemoveAt(0);
			}


			if (_reBuildClient != null)
			{
				if (_reBuildClient.Count != 0)
				{
					_reBuildClient[0].ReBuild();
					_reBuildClient.RemoveAt(0);
				}
			}

			// Build collider
			if (_reBuildColliderCountClient != 0)
			{
				// Build all cube in the last ReBuildCollider() call
				_reBuildColliderCountClient--;
				_reBuildColliderClient[0].ReBuildCollider();
				_reBuildColliderClient.RemoveAt(0);
			}
		}

		#region of Draw3D

		//sculping modifying,call Draw3D to draw/sculpt things
		public void Draw3D(Vector3 position, Vector3 scale, OBJ obj, EFFECT BrushEffect, short textureID, bool doSculpt, bool doPaint)
		{

			// Get point in terrain location
			Matrix4x4 matrix = transform.worldToLocalMatrix;
			position = matrix.MultiplyPoint(position);
			scale = matrix.MultiplyVector(scale);
			Bounds bounds = new Bounds(position, scale);

			int sX = (int)scale.x / 2 + 1,
				sY = (int)scale.y / 2 + 1,
				sZ = (int)scale.z / 2 + 1,
				bX = Mathf.Max((int)position.x - sX, 1),
				bY = Mathf.Max((int)position.y - sY, 1),
				bZ = Mathf.Max((int)position.z - sZ, 1),
				eX = Mathf.Min((int)position.x + sX + 2, width - 2),
				eY = Mathf.Min((int)position.y + sY + 2, height - 2),
				eZ = Mathf.Min((int)position.z + sZ + 2, depth - 2);

			// inverse effect for subtraction
			if (BrushEffect == EFFECT.SUB)
				for (int x = bX; x < eX; x++)
					for (int y = bY; y < eY; y++)
						for (int z = bZ; z < eZ; z++)
							_map[x, y, z] = (byte)(255 - _map[x, y, z]);

			// Begin of effect

			switch (BrushEffect)
			{
				case EFFECT.ADD:
				case EFFECT.SUB:
					switch (obj)
					{
						case OBJ.CUBE: AddCube(bounds, bX, bY, bZ, eX, eY, eZ, textureID, doSculpt, doPaint); break;
						case OBJ.SPHERE: AddSphere(bounds, bX, bY, bZ, eX, eY, eZ, textureID, doSculpt, doPaint); break;
						case OBJ.RANDOM: AddRandom(bounds, bX, bY, bZ, eX, eY, eZ, textureID, doSculpt, doPaint); break;
						case OBJ.PLANE: AddPlane(bounds, bX, bY, bZ, eX, eY, eZ, textureID, doSculpt, doPaint); break;
						case OBJ.HEIGHTMAP: addHeightMap(bounds, bX, bY, bZ, eX, eY, eZ, textureID); break;
					}
					break;

			}

			// End effect

			// inverse effect for subtraction
			if (BrushEffect == EFFECT.SUB)
				for (int x = bX; x < eX; x++)
					for (int y = bY; y < eY; y++)
						for (int z = bZ; z < eZ; z++)
							_map[x, y, z] = (byte)(255 - _map[x, y, z]);

			// rebuild map in this bounds
			bounds.SetMinMax(new Vector3(bX, bY, bZ), new Vector3(eX, eY, eZ));
			ReBuild(bounds);
			ReBuildClient(bounds);
		}

		// separate Draw3D function called from addHeightmap
		public void HeightMapDraw3D(Vector3 position, Vector3 scale, short textureID)
		{

			Bounds bounds = new Bounds(position, scale);

			int size = (int)scale.x / 2 + 1;
			int bX = Mathf.Max((int)position.x - size, 1),
				bY = Mathf.Max((int)position.y - size, 1),
				bZ = Mathf.Max((int)position.z - size, 1),
				eX = Mathf.Min((int)position.x + size + 2, width - 2),
				eY = Mathf.Min((int)position.y + size + 2, height - 2),
				eZ = Mathf.Min((int)position.z + size + 2, depth - 2);

			// draw spheres to represent 3d pixels
			Vector3 center = bounds.center;
			//float radius = Mathf.Min(Mathf.Min(bounds.size.x, bounds.size.y), bounds.size.z)/2F;
			float radius = scale.x / 2;
			for (int x = bX; x < eX; x++)
				for (int y = bY; y < eY; y++)
					for (int z = bZ; z < eZ; z++)
					{
						// get distance for marching cubes
						float dist = (Vector3.Distance(new Vector3(x, y, z), center) - radius) * 255;
						// sphere equattion
						byte bVal = (byte)(dist > 255 ? 255 : (dist < 0 ? 0 : dist));
						if (bVal < _map[x, y, z])
							_map[x, y, z] = bVal;
						// paint sphere
						_colors[x, y, z] = textureID;
					}


		}
		#endregion of Draw3D

		#region of load Save Terrain
		// save level
		public void saveMap(string fileName)
		{
			SaveLoad.saveMap(_map, _colors, width, height, depth, fileName, _material);
		}
		// load level
		public void loadMap(string fileName)
		{
			// call loadMap to get saved terrain
			SaveLoad.loadMap(fileName, out width, out height, out depth, out _map, out _colors, out _material);
		}

		public bool LoadMapFromFile()
		{
			string path = Application.streamingAssetsPath + "/VoxelLevels" + "/" + TerrainName + ".bytes";
			// Mobile devices can not write to the streamingAssetsPath so we use persistentDataPath instead.
			if (Application.isMobilePlatform)
				path = Application.persistentDataPath + "/VoxelLevels" + "/" + TerrainName + ".bytes";

			if (System.IO.File.Exists(path))
			{
				VoxTerrain.Instance.loadMap(TerrainName);//try{} 
														 //catch{UnityEngine.Debug.LogWarning ("Error while loading Terrain at path : (" + path + "). Loading empty world");}
				return true;
			}
			else
			{
				if (!firstLoad) UnityEngine.Debug.LogWarning("Terrain at path : (" + path + ") not exist. Loading empty world");
				return false;
			}
		}

		public void DrawLoadedMap()
		{
			// rebuild loaded terrain
			Vector3 scale = new Vector3(width, height, depth) * 2;
			// Get point in terrain location
			Matrix4x4 matrix = transform.worldToLocalMatrix;
			Vector3 position = new Vector3(0, 0, 0);
			position = matrix.MultiplyPoint(position);
			scale = matrix.MultiplyVector(scale);
			Bounds bounds = new Bounds(position, scale);
			int sX = (int)scale.x / 2 + 1,
			sY = (int)scale.y / 2 + 1,
			sZ = (int)scale.z / 2 + 1,
			bX = Mathf.Max((int)position.x - sX, 1),
			bY = Mathf.Max((int)position.y - sY, 1),
			bZ = Mathf.Max((int)position.z - sZ, 1),
			eX = Mathf.Min((int)position.x + sX + 2, width - 2),
			eY = Mathf.Min((int)position.y + sY + 2, height - 2),
			eZ = Mathf.Min((int)position.z + sZ + 2, depth - 2);
			// rebuild map in this bounds
			bounds.SetMinMax(new Vector3(bX, bY, bZ), new Vector3(eX, eY, eZ));
			ReBuild(bounds);
			ReBuildClient(bounds);
		}

		public static VoxTerrain CreateNewTerrain(string levelName, int width, int height, int depth, int chunkRes)
		{
			VoxTerrain oldTerrain = FindObjectOfType<VoxTerrain>();

			GameObject loadedTerrain = new GameObject();
			VoxTerrain voxTerrain = loadedTerrain.AddComponent<VoxTerrain>();
			voxTerrain.TerrainName = levelName;
			voxTerrain.gameObject.name = "VTerrain";
			voxTerrain.width = width;
			voxTerrain.height = height;
			voxTerrain.depth = depth;
			voxTerrain.RES = chunkRes;
			voxTerrain.firstLoad = true;

			if (oldTerrain != null)
			{
				if (Application.isPlaying)
				{
					Destroy(oldTerrain.gameObject);
				}
				else
				{
					DestroyImmediate(oldTerrain.gameObject);
				}
			}

			return voxTerrain;
		}

		public static VoxTerrain LoadFromFile(string levelName)
		{
			VoxTerrain oldTerrain = FindObjectOfType<VoxTerrain>();

			GameObject loadedTerrain = new GameObject();
			VoxTerrain voxTerrain = loadedTerrain.AddComponent<VoxTerrain>();
			voxTerrain.TerrainName = levelName;
			voxTerrain.gameObject.name = "VTerrain";

			if (oldTerrain != null)
			{
				if (Application.isPlaying)
				{
					Destroy(oldTerrain.gameObject);
				}
				else
				{
					DestroyImmediate(oldTerrain.gameObject);
				}
			}

			return voxTerrain;
		}

		public static void SaveToFile(VoxTerrain voxTerrain, string fileName = "")
		{
			if (fileName == "")
			{
				fileName = voxTerrain.TerrainName;
			}
			else
			{
				voxTerrain.TerrainName = fileName;
			}

			SaveLoad.saveMap(voxTerrain._map, voxTerrain._colors, voxTerrain.width, voxTerrain.height, voxTerrain.depth, fileName, voxTerrain._material);
		}

		#endregion of load Save Terrain

		#region of add Objects
		// Effects
		private void AddPlane(Bounds bounds, int bX, int bY, int bZ, int eX, int eY, int eZ, short textureID, bool doSculpt, bool doPaint)
		{
			// Average height between min and max
			int height = (bY + eY) / 2;

			if (doSculpt)
			{
				for (int x = bX; x < eX; x++)
						for (int z = bZ; z < eZ; z++)
						{
							_map[x, height, z] = 0;
						}
			}

			if(doPaint)
            {
				for (int x = bX - 1; x < eX; x++)
					for (int y = height - 1; y <= height; y++)
						for (int z = bZ - 1; z < eZ; z++)
						{
							_colors[x, y, z] = textureID;
						}
			}

		}

		private void AddCube(Bounds bounds, int bX, int bY, int bZ, int eX, int eY, int eZ, short textureID, bool doSculpt, bool doPaint)
		{
			if (doSculpt && doPaint)
			{
				for (int x = bX; x < eX; x++)
					for (int y = bY; y < eY; y++)
						for (int z = bZ; z < eZ; z++)
						{
							_map[x, y, z] = 0;
						}

				for (int x = bX - 1; x < eX; x++)
					for (int y = bY - 1; y < eY; y++)
						for (int z = bZ - 1; z < eZ; z++)
						{
							_colors[x, y, z] = textureID;
						}
			}
			else if (doSculpt && doPaint == false)
			{
				for (int x = bX; x < eX; x++)
					for (int y = bY; y < eY; y++)
						for (int z = bZ; z < eZ; z++)
							_map[x, y, z] = 0;
			}
			else if (doSculpt == false && doPaint)
			{
				for (int x = bX; x < eX; x++)
					for (int y = bY; y < eY; y++)
						for (int z = bZ; z < eZ; z++)
							_colors[x, y, z] = textureID;
			}
		}
		private void AddSphere(Bounds bounds, int bX, int bY, int bZ, int eX, int eY, int eZ, short textureID, bool doSculpt, bool doPaint)
		{
			Vector3 center = bounds.center;
			float sculptRadius = Mathf.Min(Mathf.Min(bounds.size.x, bounds.size.y), bounds.size.z) / 2F;
			float paintRadius = sculptRadius + 0.5f;
			float paintOnlyRadius = sculptRadius - 1f;

			// Add extra space for more accurate check
			bX -= 2; if (bX < 1) bX = 1;
			bY -= 2; if (bY < 1) bY = 1;
			bZ -= 2; if (bZ < 1) bZ = 1;

			eX += 2; if (eX > width-2) eX = width-2;
			eY += 2; if (eY > height-2) eY = height-2;
			eZ += 2; if (eZ > depth-2) eZ = depth-2;


			if (doSculpt && doPaint)
			{
				for (int x = bX; x < eX; x++)
					for (int y = bY; y < eY; y++)
						for (int z = bZ; z < eZ; z++)
						{
							// get distance for marching cubes
							float distToCenter = Vector3.Distance(new Vector3(x, y, z), center);
							float dist = (distToCenter - sculptRadius) * 255;
							byte bVal = (byte)(dist > 255 ? 255 : (dist < 0 ? 0 : dist));
							byte xyz = _map[x, y, z];
							if (bVal < xyz)
								_map[x, y, z] = bVal;

							dist = (distToCenter - paintRadius) * 255;
							// paint sphere
							if (dist < 255)
								_colors[x, y, z] = textureID;
						}
			}
			else if (doSculpt && doPaint == false)
			{
				for (int x = bX; x < eX; x++)
					for (int y = bY; y < eY; y++)
						for (int z = bZ; z < eZ; z++)
						{
							// get distance for marching cubes
							float distToCenter = Vector3.Distance(new Vector3(x, y, z), center);
							float dist = (distToCenter - sculptRadius) * 255;
							byte bVal = (byte)(dist > 255 ? 255 : (dist < 0 ? 0 : dist));
							byte xyz = _map[x, y, z];
							if (bVal < xyz)
								_map[x, y, z] = bVal;
						}
			}
			else if (doSculpt == false && doPaint)
			{
				for (int x = bX; x < eX; x++)
					for (int y = bY; y < eY; y++)
						for (int z = bZ; z < eZ; z++)
						{
							// get distance for marching cubes
							float distToCenter = Vector3.Distance(new Vector3(x, y, z), center);
							float dist = (distToCenter - paintOnlyRadius) * 255;

							// paint sphere
							if (dist < 255)
								_colors[x, y, z] = textureID;
						}
			}
		}

		private void AddRandom(Bounds bounds, int bX, int bY, int bZ, int eX, int eY, int eZ, short textureID, bool doSculpt, bool doPaint)
		{
			Vector3 center = bounds.center;
			float sculptRadius = Mathf.Min(Mathf.Min(bounds.size.x, bounds.size.y), bounds.size.z) / 2F;
			float paintRadius = sculptRadius + 1f;
			float paintOnlyRadius = sculptRadius - 1f;

			// Add extra space for more accurate check
			bX -= 2; if (bX < 1) bX = 1;
			bY -= 2; if (bY < 1) bY = 1;
			bZ -= 2; if (bZ < 1) bZ = 1;

			eX += 2; if (eX > width - 2) eX = width - 2;
			eY += 2; if (eY > height - 2) eY = height - 2;
			eZ += 2; if (eZ > depth - 2) eZ = depth - 2;

			if (doSculpt && doPaint)
			{
				for (int x = bX; x < eX; x++)
					for (int y = bY; y < eY; y++)
						for (int z = bZ; z < eZ; z++)
						{
							// get distance for marching cubes
							float distToCenter = Vector3.Distance(new Vector3(x, y, z), center);
							float dist = (Vector3.Distance(new Vector3(x, y, z), center) - sculptRadius) * 0.5F;
							dist = (dist + Random.value) * 255;
							byte bVal = (byte)(dist > 255 ? 255 : (dist < 0 ? 0 : dist));
							byte xyz = _map[x, y, z];
							if (bVal < xyz)
							{
								_map[x, y, z] = bVal;
							}

							dist = (distToCenter - paintRadius) * 255;
							if (dist < 255)
							{
								_colors[x, y, z] = textureID;
							}
						}
			}
			if (doSculpt && doPaint == false)
			{
				for (int x = bX; x < eX; x++)
					for (int y = bY; y < eY; y++)
						for (int z = bZ; z < eZ; z++)
						{
							// get distance for marching cubes
							float dist = (Vector3.Distance(new Vector3(x, y, z), center) - sculptRadius) * 0.5F;
							dist = (dist + Random.value) * 255;
							byte bVal = (byte)(dist > 255 ? 255 : (dist < 0 ? 0 : dist));
							if (bVal < _map[x, y, z])
								_map[x, y, z] = bVal;
						}
			}
			if (doSculpt == false && doPaint)
			{
				for (int x = bX; x < eX; x++)
					for (int y = bY; y < eY; y++)
						for (int z = bZ; z < eZ; z++)
						{
							// get distance for marching cubes
							float dist = (Vector3.Distance(new Vector3(x, y, z), center) - paintOnlyRadius) * 0.5F;
							dist = (dist + Random.value) * 255;
							if (dist < 255)
								_colors[x, y, z] = textureID;
						}
			}
		}

		private void addHeightMap(Bounds bounds, int bX, int bY, int bZ, int eX, int eY, int eZ, short textureID)
		{
			Vector3 pixelSize = new Vector3(13f, 13f, 13f);
			float yDepth = depth * heightmapDepth;
			for (int x = 0; x < height; x++)
				for (int z = 0; z < width; z++)
				{
					int ySize = (int)(getAvgHeight(x, z) * yDepth);
					for (int y = 0; y < ySize; y++)
					{
						if (y > ySize - 2)
						{
							Vector3 pos = new Vector3(x, y, z);
							HeightMapDraw3D(pos, pixelSize, textureID);
						}
						if (((x < 2 || z < 2 || x > width - 2 || z > height - 2)))
						{
							Vector3 pos = new Vector3(x, y, z);
							HeightMapDraw3D(pos, pixelSize, textureID);
						}

					}
				}

		}
		// used for heightmap
		float getAvgHeight(int x, int z)
		{
			if ((x < 2 || z < 2 || x > width - 2 || z > height - 2)) { return heightmap.GetPixel(x, z).a; }
			//float one = heightmap.GetPixel(x,z).a;
			float two = heightmap.GetPixel(x + 1, z).a;
			float three = heightmap.GetPixel(x, z + 1).a;
			float four = heightmap.GetPixel(x - 1, z).a;
			float five = heightmap.GetPixel(x, z - 1).a;
			//float six = heightmap.GetPixel(x+1,z+1).a;
			//float seven = heightmap.GetPixel(x-1,z-1).a;
			//float eight = heightmap.GetPixel(x+1,z-1).a;
			//float nine = heightmap.GetPixel(x-1,z+1).a;
			//return (one+two+three+four+five+six+seven+eight+nine)/9;
			return (two + three + four + five) / 4;

		}



		#endregion of add Objects

		// Initialise all point in map to val
		public void ResetMap()
		{
			for (int x = 0; x < width + 1; x++)
				for (int y = 0; y < height + 1; y++)
					for (int z = 0; z < depth + 1; z++)
					{
						_map[x, y, z] = 255;
					}

			for (int x = 10; x < width + 1; x += 10)
				for (int y = 10; y < height + 1; y += 10)
					for (int z = 10; z < depth + 1; z += 10)
					{
						for (int ix = x - 10; ix < x; ix++)
							for (int iy = y - 10; iy < y; iy++)
								for (int iz = z - 10; iz < z; iz++)
								{
									_colors[ix, iy, iz] = 3;
								}
					}
			ReBuild();
			ReBuildClient();
		}
		// Rebuild this cube
		public void ReBuild(Cube cube)
		{
			// Add mesh to re build list
			if (!_reBuild.Contains(cube))
				_reBuild.Add(cube);

			// Add mesh collider to rebuild list
			if (!_reBuildCollider.Contains(cube))
				_reBuildCollider.Add(cube);
		}
		public void ReBuildClient(Cube cube)
		{
			// Add mesh to re build list
			if (!_reBuildClient.Contains(cube))
				_reBuildClient.Add(cube);

			// Add mesh collider to rebuild list
			if (!_reBuildColliderClient.Contains(cube))
				_reBuildColliderClient.Add(cube);
		}

		// Rebuild the cube contains point
		public void ReBuild(Vector3 point)
		{
			foreach (Cube cube in _cubes)
				if (cube.bounds.Contains(point))
				{
					ReBuild(cube);
					return;
				}
		}
		public void ReBuildClient(Vector3 point)
		{
			foreach (Cube cube in _cubesClient)
				if (cube.bounds.Contains(point))
				{
					ReBuildClient(cube);
					return;
				}
		}

		// Rebuild all cube in bounds of effect
		public void ReBuild(Bounds bounds)
		{
			foreach (Cube cube in _cubes)
				if (bounds.Intersects(cube.bounds))
					ReBuild(cube);
		}
		public void ReBuildClient(Bounds bounds)
		{
			foreach (Cube cube in _cubesClient)
				if (bounds.Intersects(cube.bounds))
					ReBuildClient(cube);
		}
		// Rebuild all cube
		public void ReBuild()
		{
			foreach (Cube cube in _cubes)
				ReBuild(cube);
		}
		public void ReBuildClient()
		{
			foreach (Cube cube in _cubesClient)
				ReBuildClient(cube);
		}

		// Rebuild collider after Draw3D
		public void ReBuildCollider()
		{
			// Get the count for ReBuild, for reduction of time process
			_reBuildColliderCount = _reBuildCollider.Count;
		}
		public void ReBuildColliderClient()
		{
			// Get the count for ReBuild, for reduction of time process
			_reBuildColliderCountClient = _reBuildColliderClient.Count;
		}

		// called once to draw empty plane(default terrain)
		public void DrawPlaneTerrain()
		{

			//load empty terrain (plane)
			Draw3D(new Vector3(width / 2, 1, depth / 2), new Vector3(width, 1, depth), OBJ.PLANE, EFFECT.ADD, 0, true, true);
			// move camera right to get space from all sides (first voxel is on (0,0,0) pisition
			Camera.main.transform.position = new Vector3(width / 2, 25, -25);
			//Camera.main.transform.RotateAround(new Vector3(125,0,20), Vector3.right, 45f);

		}
		//called one to draw terrain from heightmap
		public void DrawTerrainFromHeightmap()
		{


			//load terrain (heightmap)
			Draw3D(new Vector3(width / 2, 0, height / 2), new Vector3(width, depth, height), OBJ.HEIGHTMAP, EFFECT.ADD, 0, true, true);
			DrawPlaneTerrain();
			// move camera right to get space from all sides (first voxel is on (0,0,0) pisition
			Camera.main.transform.position = new Vector3(width / 2, 25, -25);
			Camera.main.transform.RotateAround(new Vector3(125, 0, 20), Vector3.right, 45f);

		}
		// add terrain chunk
		public GameObject AddObject(bool client)
		{
			GameObject obj;
			if(!client){
				obj = new GameObject("Cube" + _cubes.Count);
				obj.transform.parent = handler.transform;
			}
			else{
				obj = new GameObject("CubeClient" + _cubesClient.Count);
				obj.transform.parent = handlerClient.transform;
			}
			obj.AddComponent<MeshRenderer>().materials = GetComponent<Renderer>().sharedMaterials; // renderer.sharedMaterials;
			obj.AddComponent<MeshFilter>();
			obj.AddComponent<MeshCollider>();
			return obj;
		}
		private Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
		{
			Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, true);
			Color[] rpixels = result.GetPixels(0);
			float incX = ((float)1 / source.width) * ((float)source.width / targetWidth);
			float incY = ((float)1 / source.height) * ((float)source.height / targetHeight);
			for (int px = 0; px < rpixels.Length; px++)
			{
				rpixels[px] = source.GetPixelBilinear(incX * ((float)px % targetWidth),
								  incY * ((float)Mathf.Floor(px / targetWidth)));
			}
			result.SetPixels(rpixels, 0);
			result.Apply();
			return result;
		}

		void OnDestroy()
		{
			if (Application.isPlaying)
			{
				/*if (_saveOnDestroy){
					print("Saving terrain on destroy");
					VoxTerrain.Instance.saveMap(TerrainName);
				}*/
			}
		}
	}
}
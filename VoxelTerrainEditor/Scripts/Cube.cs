using UnityEngine;
using System.Collections.Generic;

namespace VoxelTerrain
{
	public class Cube
	{
		
		public GameObject _object;
		private Mesh _mesh;
		private Bounds _bounds;
		private int _minX, _minY, _minZ, _maxX, _maxY, _maxZ;
		private VoxTerrain _terrain;
		public Bounds bounds{get{return _bounds;}}
		public Mesh mesh { get { return _mesh; } }

		// Create new mesh zone
		public Cube(bool client, Bounds bounds, VoxTerrain terrain)
		{
			// Limit to this zone
			_minX = (int) bounds.min.x;
			_minY = (int) bounds.min.y;
			_minZ = (int) bounds.min.z;
			_maxX = (int) bounds.max.x;
			_maxY = (int) bounds.max.y;
			_maxZ = (int) bounds.max.z;
			_bounds = bounds;
			
			// Limit to all zone
			_terrain = terrain;
			_object = terrain.AddObject(client); _object.GetComponent<MeshFilter>().sharedMesh=new Mesh();
			_mesh = _object.GetComponent<MeshFilter>().sharedMesh; 
			_mesh.bounds = _bounds;
			if(client){
				_object.transform.position += new Vector3(256,256,256);
			}
			_object.layer = 7;
			//visuals
			_object.tag =  "GroundVisuals";
		}
		
		public void ReBuild()
		{
			int w = _terrain.width;
			int h = _terrain.height;
			int d = _terrain.depth;
			
			Vector3[] vertices;
			int[] triangles;
			Vector2[] uvY;
			Vector2[] uvZ;
			Color[] colors;
			
			// Delete old vertices and triangles mesh
			_mesh.Clear();
			
			MarchingCubes.Polygonize(_terrain._map,
			                            _minX, _minY, _minZ,
			                            _maxX, _maxY, _maxZ,
			                            out vertices,
			                            out triangles);
			
			//uvX = new Vector2[vertices.Length];
			uvY = new Vector2[vertices.Length];
			uvZ = new Vector2[vertices.Length];
			colors = new Color[vertices.Length];

			// Generate uv array
			for(int i=0; i<vertices.Length; i++)
			{
				// Planar with global position
				Vector3 v = vertices[i];
				
				// For top planar
				uvY[i] = new Vector2(v.x/w, v.z/d);
				
				// For side planar
				uvZ[i] = new Vector2(v.x/w, v.y/h);

				int x = (int)v.x;
				if (x == 0) x = 1;

				int y = (int)v.y;
				if (y == 0) y = 1;

				int z = (int)v.z;
				if (z == 0) z = 1;

				// Index of texture
				colors[i] =  VoxTerrain.Instance.textureChannels[_terrain._colors[x, y, z]];
			}
			// Apply mesh
			_mesh.vertices = vertices;
			_mesh.triangles = triangles;
			
			_mesh.uv  = uvY;
			_mesh.uv2 = uvZ;
			_mesh.colors = colors;
			
			_mesh.RecalculateNormals();
			//computeVertexNormals();
		}
		
		// Rebuild collider after paint
		public void ReBuildCollider()
		{
			MeshCollider meshCollider = _object.GetComponent<MeshCollider>();
			meshCollider.sharedMesh = null;
			if(_mesh.vertexCount > 0)
				meshCollider.sharedMesh = _mesh;
		}
		// not used
		void averageNormals(Vector3 normal1,Vector3 normal2){
			Vector3 average = (normal1 + normal2) *.5f;
            normal1 = average;
            normal2 = average;
		}
		
		//not used
		void computeVertexNormals(){
			
			Vector3[] normalBuffer=new Vector3[_mesh.vertices.Length];
			List<Vector3>[] vertexNormalNeighbors = new List<Vector3>[_mesh.vertices.Length];
			for(int i=0;i<_mesh.vertices.Length;i++){ vertexNormalNeighbors[i] = new List<Vector3>();}
			
			int[] indexBuffer = _mesh.triangles;
			 for (int i = 0; i < indexBuffer.Length-3; i+=3)
                {
                        Vector3 normal =  (_mesh.vertices[indexBuffer[i+2]]-_mesh.vertices[indexBuffer[i]]);
				        normal = Vector3.Cross(normal,((_mesh.vertices[indexBuffer[i+1]]-_mesh.vertices[indexBuffer[i]])));
                               
                        normal.Normalize();
                        vertexNormalNeighbors[indexBuffer[i]].Add(normal);
                        vertexNormalNeighbors[indexBuffer[i+1]].Add(normal);
                        vertexNormalNeighbors[indexBuffer[i+2]].Add(normal);
                }
                for (int i = 0; i < normalBuffer.Length; i++)
                {
                        normalBuffer[i] = new Vector3(0,0,0);
                        for (int n = 0; n < vertexNormalNeighbors[i].Count; n++)
                                normalBuffer[i] += vertexNormalNeighbors[i][n];
                        normalBuffer[i].Normalize();
                }
			_mesh.normals=normalBuffer;
		}	
	}	
}
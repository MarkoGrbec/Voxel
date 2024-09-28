using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using VoxelTerrain;

namespace VoxelTerrain
{
	public class VVertices
	{
		private List<Vector3> _vertices;
		private Dictionary<Vector3,int> _find;
		private int _index;
		
		Vector3 n1,n2,n3;
		
		public VVertices()
		{
			_vertices = new List<Vector3>();
			_find = new Dictionary<Vector3, int>();
			_index=0;
		}
		
		public int GetIndex(Vector3 vertex)
		{
			int i;
			if(_find.TryGetValue(vertex, out i))
				return i;
			_vertices.Add(vertex);
			_find.Add(vertex, _index);
			return _index++;
		}
		
		public Vector3[] ToArray()
		{
			return _vertices.ToArray();
		}
		
	}
}
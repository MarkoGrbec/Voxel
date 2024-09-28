using UnityEditor;

namespace VoxelTerrain
{
    [InitializeOnLoad]
    class UpdaterEditor
    {
        static UpdaterEditor()
        {
            EditorApplication.update += Update;
        }

        static void Update()
        {
            try { VoxTerrain.Instance.Update(); } catch { }
        }
    }
}

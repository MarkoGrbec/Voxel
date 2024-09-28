using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;
using UnityEditor.SceneManagement;

namespace VoxelTerrain
{
    public class ExportToObj : ScriptableObject
    {
        private static int vertexOffset = 0;
        private static int normalOffset = 0;
        private static int uvOffset = 0;

        private static string MeshToString(MeshFilter mf, Dictionary<string, objMaterial> materialList)
        {
            Mesh m = mf.sharedMesh;
            Material[] mats = mf.GetComponent<Renderer>().sharedMaterials;

            StringBuilder sb = new StringBuilder();

            sb.Append("g ").Append(mf.name).Append("\n");
            foreach (Vector3 lv in m.vertices)
            {
                Vector3 wv = mf.transform.TransformPoint(lv);

                sb.Append(string.Format("v {0} {1} {2}\n", -wv.x, wv.y, wv.z));
            }
            sb.Append("\n");

            foreach (Vector3 lv in m.normals)
            {
                Vector3 wv = mf.transform.TransformDirection(lv);

                sb.Append(string.Format("vn {0} {1} {2}\n", -wv.x, wv.y, wv.z));
            }
            sb.Append("\n");

            foreach (Vector3 v in m.uv)
            {
                sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
            }
            sb.Append("\n");
            foreach (Vector2 v in m.uv2)
            {
                sb.Append(string.Format("vt1 {0} {1}\n", v.x, v.y));
            }
            sb.Append("\n");
            foreach (Vector2 v in m.uv2)
            {
                sb.Append(string.Format("vt2 {0} {1}\n", v.x, v.y));
            }
            sb.Append("\n");
            foreach (Color c in m.colors)
            {
                sb.Append(string.Format("vc {0} {1} {2} {3}\n", c.r, c.g, c.b, c.a));
            }

            for (int material = 0; material < m.subMeshCount; material++)
            {
                sb.Append("\n");
                sb.Append("usemtl ").Append(mats[material].name).Append("\n");
                sb.Append("usemap ").Append(mats[material].name).Append("\n");

                //See if this material is already in the materiallist.
                try
                {
                    objMaterial objMaterial = new objMaterial();

                    objMaterial.name = mats[material].name;

                    if (mats[material].mainTexture)
                        objMaterial.textureName = AssetDatabase.GetAssetPath(mats[material].mainTexture);
                    else
                        objMaterial.textureName = null;

                    materialList.Add(objMaterial.name, objMaterial);
                }
                catch (ArgumentException)
                {
                    //Already in the dictionary
                }


                int[] triangles = m.GetTriangles(material);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    //Because we inverted the x-component, we also needed to alter the triangle winding.
                    sb.Append(string.Format("f {1}/{1}/{1} {0}/{0}/{0} {2}/{2}/{2}\n",
                        triangles[i] + 1 + vertexOffset, triangles[i + 1] + 1 + normalOffset, triangles[i + 2] + 1 + uvOffset));
                }
            }

            vertexOffset += m.vertices.Length;
            normalOffset += m.normals.Length;
            uvOffset += m.uv.Length;

            return sb.ToString();
        }

        private static void Clear()
        {
            vertexOffset = 0;
            normalOffset = 0;
            uvOffset = 0;
        }

        private static Dictionary<string, objMaterial> PrepareFileWrite()
        {
            Clear();

            return new Dictionary<string, objMaterial>();
        }

        private static void MaterialsToFile(Dictionary<string, objMaterial> materialList, string folder, string filename)
        {
            using (StreamWriter sw = new StreamWriter(folder + "/" + filename + ".mtl"))
            {
                foreach (KeyValuePair<string, objMaterial> kvp in materialList)
                {
                    sw.Write("\n");
                    sw.Write("newmtl {0}\n", kvp.Key);
                    sw.Write("Ka  0.6 0.6 0.6\n");
                    sw.Write("Kd  0.6 0.6 0.6\n");
                    sw.Write("Ks  0.9 0.9 0.9\n");
                    sw.Write("d  1.0\n");
                    sw.Write("Ns  0.0\n");
                    sw.Write("illum 2\n");

                    if (kvp.Value.textureName != null)
                    {
                        string destinationFile = kvp.Value.textureName;


                        int stripIndex = destinationFile.LastIndexOf('/');//FIXME: Should be Path.PathSeparator;

                        if (stripIndex >= 0)
                            destinationFile = destinationFile.Substring(stripIndex + 1).Trim();


                        string relativeFile = destinationFile;

                        destinationFile = folder + "/" + destinationFile;


                        try
                        {
                            File.Copy(kvp.Value.textureName, destinationFile);
                        }
                        catch
                        {

                        }


                        sw.Write("map_Kd {0}", relativeFile);
                    }

                    sw.Write("\n\n\n");
                }
            }
        }

        private static void MeshToFile(MeshFilter mf, string folder, string filename)
        {
            Dictionary<string, objMaterial> materialList = PrepareFileWrite();

            using (StreamWriter sw = new StreamWriter(folder + "/" + filename + ".obj"))
            {
                sw.Write("mtllib ./" + filename + ".mtl\n");

                sw.Write(MeshToString(mf, materialList));
            }

            MaterialsToFile(materialList, folder, filename);
        }

        private static void MeshesToFile(MeshFilter[] mf, string folder, string filename)
        {
            Dictionary<string, objMaterial> materialList = PrepareFileWrite();

            using (StreamWriter sw = new StreamWriter(folder + "/" + filename + ".obj"))
            {
                sw.Write("mtllib ./" + filename + ".mtl\n");

                for (int i = 0; i < mf.Length; i++)
                {
                    sw.Write(MeshToString(mf[i], materialList));
                }
            }

            MaterialsToFile(materialList, folder, filename);
        }

        public static string targetFolder = "Assets/ExportedObjs";
        private static bool createObjFolder()
        {
            try
            {
                System.IO.Directory.CreateDirectory(targetFolder);
            }
            catch
            {
                EditorUtility.DisplayDialog("Error!", "Failed to create ExportedObjs folder!", "");
                return false;
            }

            return true;
        }

        [MenuItem("Window/Voxel/Export/Export selection to OBJ (Read doc.pdf)")]
        static void Export()
        {
            if (!createObjFolder())
                return;


            Transform[] selection = Selection.GetTransforms(SelectionMode.Editable | SelectionMode.ExcludePrefab);

            if (selection.Length == 0)
            {
                EditorUtility.DisplayDialog("No Objects Selected", "", "ok");
                return;
            }

            int exportedObjects = 0;

            ArrayList mfList = new ArrayList();

            for (int i = 0; i < selection.Length; i++)
            {
                Component[] meshfilter = selection[i].GetComponentsInChildren(typeof(MeshFilter));

                for (int m = 0; m < meshfilter.Length; m++)
                {
                    exportedObjects++;
                    mfList.Add(meshfilter[m]);
                }
            }

            if (exportedObjects > 0)
            {
                MeshFilter[] mf = new MeshFilter[mfList.Count];

                for (int i = 0; i < mfList.Count; i++)
                {
                    mf[i] = (MeshFilter)mfList[i];
                }

                string filename = EditorSceneManager.GetActiveScene().name + "Obj";

                int stripIndex = filename.LastIndexOf('/');

                if (stripIndex >= 0)
                    filename = filename.Substring(stripIndex + 1).Trim();

                MeshesToFile(mf, targetFolder, filename);


                EditorUtility.DisplayDialog(" Exported to" + filename, "", "ok");
            }
            else
                EditorUtility.DisplayDialog("Error Exporting Objects", "", "ok");
        }

        struct objMaterial
        {
            public string name;
            public string textureName;
        }
    }
}
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace TLab.SFU.Interact.Editor
{
    public class OutlineProcessor : EditorWindow
    {
        [SerializeField, HideInInspector] private GameObject[] m_targets;
        [SerializeField, HideInInspector] private Shader m_outline;

        [SerializeField, HideInInspector] private string m_meshSavePath;
        [SerializeField, HideInInspector] private string m_materialSavePath;

        [MenuItem("TLab/SFU/Outline Processor")]
        private static void Init()
        {
            // Get existing open window or if none, make a new one:
            var window = (OutlineProcessor)GetWindow(typeof(OutlineProcessor));
            window.Show();
        }

        private const float ERROR = 1e-8f;

        private void OnGUI()
        {
            var @object = new SerializedObject(this);
            var propTargets = @object.FindProperty("m_targets");
            EditorGUILayout.PropertyField(propTargets, new GUIContent("Targets"), true);
            var propShader = @object.FindProperty("m_outline");
            EditorGUILayout.PropertyField(propShader, new GUIContent("Shader"), true);
            @object.ApplyModifiedProperties();

            if (GUILayout.Button("Process Mesh"))
            {
                if (SelectPath(ref m_meshSavePath, "Save Path"))
                    ProcessMesh();

                EditorUtility.SetDirty(this);
            }

            if (GUILayout.Button("Create Outline"))
            {
                if (SelectPath(ref m_materialSavePath, "Save Path"))
                    CreateOutline();

                EditorUtility.SetDirty(this);
            }

            if (GUILayout.Button("Create Outline"))
            {
                CreatePointable();

                EditorUtility.SetDirty(this);
            }
        }

#if UNITY_EDITOR
        // https://blog.syn-sophia.co.jp/articles/2022/10/17/outline_rendering_01

        public void SaveMesh(Mesh mesh, MeshFilter meshFilter)
        {
            var path = m_meshSavePath + "/" + mesh.name + ".asset";
            var copyMesh = GameObject.Instantiate(mesh);
            var copyMeshName = copyMesh.name.ToString();
            copyMesh.name = copyMeshName.Substring(0, copyMeshName.Length - "(Clone)".Length);
            var asset = AssetDatabase.LoadAssetAtPath<Mesh>(path);

            if (asset != null)
            {
                EditorUtility.CopySerialized(copyMesh, asset);
                meshFilter.sharedMesh = asset;
            }
            else
            {
                AssetDatabase.CreateAsset(copyMesh, path);
                meshFilter.sharedMesh = copyMesh;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Saved Process Mesh: " + path);
        }

        public void SaveManaterial(Material outline, ref Material[] newMaterials, MeshRenderer meshRenderer)
        {
            var path = m_materialSavePath + "/" + outline.name + ".mat";
            var prevMat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (prevMat != null)
            {
                EditorUtility.CopySerialized(outline, prevMat);
                newMaterials[newMaterials.Length - 1] = prevMat;
                meshRenderer.sharedMaterials = newMaterials;
            }
            else
            {
                AssetDatabase.CreateAsset(outline, path);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Saved Material: " + path);
        }

        public void ProcessMesh(GameObject obj)
        {
            var meshFilters = obj.GetComponents<MeshFilter>();

            foreach (var meshFilter in meshFilters)
            {
                var mesh = meshFilter.sharedMesh;

                var normals = mesh.normals;
                var vertices = mesh.vertices;
                var vertexCount = mesh.vertexCount;

                var softEdges = new Color[normals.Length];

                for (int i = 0; i < vertexCount; i++)
                {
                    var softEdge = Vector3.zero;

                    for (int j = 0; j < vertexCount; j++)
                    {
                        var v = vertices[i] - vertices[j];

                        if (v.sqrMagnitude < ERROR)
                            softEdge += normals[j];
                    }

                    softEdge.Normalize();
                    softEdges[i] = new Color(softEdge.x, softEdge.y, softEdge.z, 0);
                }

                mesh.name = obj.name;

                mesh.colors = softEdges;
                meshFilter.sharedMesh = mesh;
                EditorUtility.SetDirty(meshFilter);

                SaveMesh(mesh, meshFilter);
            }

            EditorUtility.SetDirty(obj);
        }

        private void AddOutlineMaterial(GameObject obj)
        {
            var meshRenderer = obj.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                var prevMaterials = meshRenderer.sharedMaterials;

                var newMaterialList = new List<Material>();
                for (int i = 0; i < prevMaterials.Length; i++)
                    if (prevMaterials[i] != null && prevMaterials[i].shader != m_outline)
                        newMaterialList.Add(prevMaterials[i]);

                var outline = new Material(m_outline);
                outline.name = obj.name + "#Outline";
                newMaterialList.Add(outline);

                var newMaterials = newMaterialList.ToArray();
                meshRenderer.sharedMaterials = newMaterials;

                EditorUtility.SetDirty(meshRenderer);

                SaveManaterial(outline, ref newMaterials, meshRenderer);
            }
        }

        private void AddPointableOutline(GameObject obj)
        {
            var meshRenderer = obj.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                var selectable = obj.RequireComponent<PointableOutline>();
                selectable.outlineMat = meshRenderer.sharedMaterials[meshRenderer.sharedMaterials.Length - 1];

                EditorUtility.SetDirty(selectable);
            }
        }

        public void CreateOutline(GameObject obj)
        {
            AddOutlineMaterial(obj);

            EditorUtility.SetDirty(obj);
        }

        public void CreatePointable(GameObject obj)
        {
            AddPointableOutline(obj);

            EditorUtility.SetDirty(obj);
        }

        public void ProcessMesh()
        {
            foreach (var target in m_targets)
                ProcessMesh(target);
        }

        public void CreateOutline()
        {
            foreach (var target in m_targets)
                CreateOutline(target);
        }

        public void CreatePointable()
        {
            foreach (var target in m_targets)
                CreatePointable(target);
        }

        private static string GetDiskPath(string assetPath) => Directory.GetCurrentDirectory() + assetPath.Substring("Assets".Length - 1);

        private static bool FileExists(string assetPath)
        {
            if (assetPath.Length < "Assets".Length - 1)
                return false;

            return File.Exists(GetDiskPath(assetPath));
        }

        public static bool SelectPath(ref string currentPath, string panelTitle)
        {
            var initialDir = currentPath != null && FileExists(currentPath) ? currentPath : "Assets";
            var path = EditorUtility.SaveFolderPanel(panelTitle, initialDir, "");
            if (path == "")
            {
                return false;
            }
            var fullPath = Directory.GetCurrentDirectory();
            currentPath = path.Remove(0, fullPath.Length + 1);
            return true;
        }
#endif
    }
}

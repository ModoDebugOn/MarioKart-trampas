using System.IO;
using UnityEditor;
using UnityEngine;

namespace KartRace.Editor
{
    /// <summary>What the builders share: materials, folders, prefabs and primitives.</summary>
    static class BuildTools
    {
        /// <summary>Loads or creates {folder}/{name}.mat (URP Lit) and sets its colour. Keeps the
        /// GUID when regenerating, so everything that uses it stays linked.</summary>
        public static Material Mat(string folder, string name, Color color, float smoothness)
        {
            string path = $"{folder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>A primitive without collider (decoration). With `mirror`, a second copy on the
        /// other side (x flipped).</summary>
        public static GameObject Piece(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 euler,
                                       Vector3 size, Material material, bool mirror = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            if (mirror)
                Piece(type, name, parent, new Vector3(-position.x, position.y, position.z),
                      new Vector3(euler.x, -euler.y, -euler.z), size, material);
            return go;
        }

        public static GameObject SavePrefab(GameObject root, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}

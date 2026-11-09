using System;
using KartRace.Domain.Items;
using KartRace.Infrastructure;
using KartRace.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static KartRace.Editor.BuildTools;

namespace KartRace.Editor
{
    /// <summary>
    /// Everything the item boxes need, and plugs it into the race:
    /// - Prefabs/Items/ItemBox: a see-through cube with a "?" on every face (Sprites/Default, so it
    ///   can be tinted and is lit by nobody) and the ItemBoxView that moves it.
    /// - ScriptableObjects/ItemIcons: the icon of every item, for the roulette and the odds chart.
    /// - On the race: the box prefab for the bootstrap, and the roulette and the odds chart on the
    ///   object of the race HUD.
    /// </summary>
    static class ItemBoxBuilder
    {
        const string Root = "Assets/_Project";
        const string PrefabFolder = Root + "/Prefabs/Items", PrefabPath = PrefabFolder + "/ItemBox.prefab";
        const string MaterialFolder = Root + "/Materials/Items", MaterialPath = MaterialFolder + "/ItemBox.mat";
        const string IconsFolder = Root + "/ScriptableObjects", IconsPath = IconsFolder + "/ItemIcons.asset";
        const string IconFolder = Root + "/Textures/Items";

        [MenuItem("Tools/Kart Race/Build Item Boxes")]
        static void BuildMenu()
        {
            var race = UnityEngine.Object.FindAnyObjectByType<RaceBootstrap>();
            if (race != null)
            {
                Assign(race);
                EditorSceneManager.SaveScene(race.gameObject.scene);
            }
            else Build(out _);
            Debug.Log($"[ItemBoxBuilder] {PrefabPath} and {IconsPath}" + (race != null ? ", plugged into the race." : "."));
        }

        /// <summary>Builds the prefab and the icons; returns the prefab.</summary>
        public static ItemBoxView Build(out ItemIcons icons)
        {
            foreach (string folder in new[] { PrefabFolder, MaterialFolder, IconsFolder }) EnsureFolder(folder);
            icons = Icons();

            ItemIconBuilder.BuildBoxFace();
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Sprites/Default"));
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ItemIconBuilder.BoxFacePath);
            EditorUtility.SetDirty(material);

            var root = new GameObject("ItemBox");
            var view = root.AddComponent<ItemBoxView>();
            GameObject cube = Piece(PrimitiveType.Cube, "Cube", root.transform, new Vector3(0f, 1.3f, 0f), Vector3.zero,
                                    Vector3.one * 1.5f, material);
            cube.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var so = new SerializedObject(view);
            so.FindProperty("cube").objectReferenceValue = cube.GetComponent<Renderer>();
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = SavePrefab(root, PrefabPath);
            AssetDatabase.SaveAssets();
            return prefab.GetComponent<ItemBoxView>();
        }

        /// <summary>
        /// The box prefab for the bootstrap, and the roulette and the odds chart next to the race HUD
        /// (one object per player's HUD, so player 2 gets a copy of all of it).
        /// </summary>
        public static void Assign(RaceBootstrap race)
        {
            ItemBoxView prefab = Build(out ItemIcons icons);
            var so = new SerializedObject(race);
            GameObject go = HudObject(race, so);
            ItemRouletteHud roulette = go.GetComponent<ItemRouletteHud>();
            if (roulette == null) roulette = go.AddComponent<ItemRouletteHud>();
            ItemOddsChart odds = go.GetComponent<ItemOddsChart>();
            if (odds == null) odds = go.AddComponent<ItemOddsChart>();
            foreach (Component component in new Component[] { roulette, odds })
            {
                var view = new SerializedObject(component);
                view.FindProperty("icons").objectReferenceValue = icons;
                view.ApplyModifiedPropertiesWithoutUndo();
            }

            so.FindProperty("itemBoxPrefab").objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(race);
        }

        /// <summary>
        /// The object of the race HUD. Scenes built before two players had the HUD (and the item
        /// views) on the race's own object, which cannot be copied: they get a "HUD" child instead.
        /// </summary>
        static GameObject HudObject(RaceBootstrap race, SerializedObject so)
        {
            SerializedProperty property = so.FindProperty("hud");
            var hud = property.objectReferenceValue as RaceHud;
            if (hud != null && hud.gameObject != race.gameObject) return hud.gameObject;

            foreach (var old in new Component[] { hud, race.GetComponent<ItemRouletteHud>(), race.GetComponent<ItemOddsChart>() })
                if (old != null) UnityEngine.Object.DestroyImmediate(old);
            var go = new GameObject("HUD");
            go.transform.SetParent(race.transform, false);
            property.objectReferenceValue = go.AddComponent<RaceHud>();
            return go;
        }

        /// <summary>Loads (or creates) the icons asset and fills it from Textures/Items, drawing them if missing.</summary>
        static ItemIcons Icons()
        {
            Item[] items = (Item[])Enum.GetValues(typeof(Item));
            if (AssetDatabase.LoadAssetAtPath<Sprite>($"{IconFolder}/{items[0]}.png") == null) ItemIconBuilder.BuildIcons();

            var icons = AssetDatabase.LoadAssetAtPath<ItemIcons>(IconsPath);
            if (icons == null)
            {
                icons = ScriptableObject.CreateInstance<ItemIcons>();
                AssetDatabase.CreateAsset(icons, IconsPath);
            }
            var sprites = new Sprite[items.Length];
            for (int i = 0; i < items.Length; i++)
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"{IconFolder}/{items[i]}.png");
            icons.Fill(sprites);
            EditorUtility.SetDirty(icons);
            return icons;
        }
    }
}

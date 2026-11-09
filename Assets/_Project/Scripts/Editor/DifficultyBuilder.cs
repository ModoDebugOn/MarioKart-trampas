using KartRace.Domain.Race;
using KartRace.Infrastructure;
using KartRace.Infrastructure.Profiles;
using UnityEditor;
using UnityEngine;
using static KartRace.Editor.BuildTools;

namespace KartRace.Editor
{
    /// <summary>
    /// Creates the three difficulty levels (Domain's Difficulty.Easy, Normal, Hard) as assets, with
    /// their name and colour on screen, and plugs them into the race of the open scene.
    /// </summary>
    static class DifficultyBuilder
    {
        const string Folder = "Assets/_Project/ScriptableObjects/Difficulty";

        [MenuItem("Tools/Kart Race/Build Difficulty Profiles")]
        static void BuildMenu()
        {
            DifficultyProfile[] profiles = Build();
            var race = Object.FindAnyObjectByType<RaceBootstrap>();
            if (race != null) Assign(race, profiles);
            Debug.Log($"[DifficultyBuilder] 3 profiles in {Folder}" + (race != null ? ", plugged into the race." : "."));
        }

        public static DifficultyProfile[] Build()
        {
            EnsureFolder(Folder);
            var profiles = new[]
            {
                Profile("0_Facil", Difficulty.Easy, "Fácil", new Color(0.35f, 0.85f, 0.4f)),
                Profile("1_Normal", Difficulty.Normal, "Normal", new Color(1f, 0.8f, 0.2f)),
                Profile("2_Dificil", Difficulty.Hard, "Difícil", new Color(1f, 0.3f, 0.25f)),
            };
            AssetDatabase.SaveAssets();
            return profiles;
        }

        public static void Assign(RaceBootstrap race, DifficultyProfile[] profiles)
        {
            var so = new SerializedObject(race);
            SerializedProperty list = so.FindProperty("difficulties");
            list.arraySize = profiles.Length;
            for (int i = 0; i < profiles.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = profiles[i];
            so.FindProperty("difficulty").intValue = 1;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(race);
        }

        /// <summary>Loads the asset (or creates it) and overwrites its values with the Domain ones.</summary>
        static DifficultyProfile Profile(string file, Difficulty difficulty, string displayName, Color color)
        {
            string path = $"{Folder}/{file}.asset";
            var profile = AssetDatabase.LoadAssetAtPath<DifficultyProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<DifficultyProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            profile.Fill(difficulty, displayName, color);
            EditorUtility.SetDirty(profile);
            return profile;
        }
    }
}

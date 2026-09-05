using System.IO;
using Game.Config;
using Game.Environment.Editor;
using UnityEditor;
using UnityEngine;

namespace Game.Ledge.Editor
{
    public static class LedgeSetup
    {
        public const string PrefabPath = "Assets/GameAssets/Ledge/LedgeSet.prefab";
        public const string DataPath = "Assets/GameAssets/Design/Ledge/LedgeData.asset";

        private const string LedgeFolder = "Assets/GameAssets/Ledge";
        private const string DesignFolder = "Assets/GameAssets/Design/Ledge";
        private const string GameConfigPath = "Assets/GameAssets/Design/GameConfig.asset";

        private const string RockSpritePath = LedgeFolder + "/Ledge_Rock.png";
        private const string RockEdgeSpritePath = LedgeFolder + "/Ledge_RockEdge.png";
        private const string HintSpritePath = LedgeFolder + "/Ledge_Hint.png";

        private const float BedBottomY = -3.8f;
        private const float NearBedTopY = -2.2f;
        private const float WallWidth = 1.2f;
        private const float UpperBedLength = 30f;
        private const float HintOffsetX = -2.2f;
        private const float HintOffsetY = 0.6f;
        private const float HintWidth = 1f;
        private const float HintHeight = 1.2f;
        private const float ParkX = 60f;
        private const int LedgeOrder = -5;
        private const int HintOrder = -4;

        [MenuItem("Team1004/Generate Ledge Assets")]
        public static void Generate()
        {
            Generate(false);
        }

        [MenuItem("Team1004/Regenerate Ledge Prefab (Overwrite)")]
        public static void RegeneratePrefab()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog(
                    "Regenerate Ledge Prefab",
                    $"{PrefabPath} will be overwritten. {DataPath} and placeholder textures are kept.",
                    "Overwrite",
                    "Cancel"))
                return;

            Generate(true);
        }

        public static void Generate(bool overwrite)
        {
            EnsureFolder(LedgeFolder);
            EnsureFolder(DesignFolder);

            var rock = EnvironmentTextures.EnsureSprite(RockSpritePath, 128, 64, LedgePatterns.Rock);
            var rockEdge = EnvironmentTextures.EnsureSprite(RockEdgeSpritePath, 128, 64, LedgePatterns.RockEdge);
            var hint = EnvironmentTextures.EnsureSprite(HintSpritePath, 64, 64, LedgePatterns.Hint);
            var data = EnsureData();

            if (rock == null || rockEdge == null || hint == null || data == null)
            {
                Debug.LogError("[LedgeSetup] Placeholder assets are missing. Aborted.");
                return;
            }

            if (File.Exists(PrefabPath) && !overwrite)
            {
                Debug.LogWarning($"[LedgeSetup] Prefab already exists and was kept: {PrefabPath}");
                AssetDatabase.SaveAssets();
                return;
            }

            BuildPrefab(rock, rockEdge, hint, data);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LedgeSetup] Ledge assets are ready");
        }

        private static LedgeData EnsureData()
        {
            var data = AssetDatabase.LoadAssetAtPath<LedgeData>(DataPath);

            if (data != null)
                return data;

            data = ScriptableObject.CreateInstance<LedgeData>();
            AssetDatabase.CreateAsset(data, DataPath);
            EditorUtility.SetDirty(data);
            Debug.Log($"[LedgeSetup] Data asset written: {DataPath}");
            return data;
        }

        private static void BuildPrefab(Sprite rock, Sprite rockEdge, Sprite hint, LedgeData data)
        {
            var stepHeight = data.StepHeight;
            var wallTopY = LoadTopLaneY() + stepHeight;
            var upperBedTopY = NearBedTopY + stepHeight;

            var root = new GameObject("LedgeSet");

            try
            {
                var director = root.AddComponent<LedgeDirector>();

                var first = CreateLedge("Ledge1", root.transform, rock, rockEdge, hint, wallTopY, upperBedTopY, true);
                var second = CreateLedge("Ledge2", root.transform, rock, rockEdge, hint, wallTopY, upperBedTopY, false);

                var serialized = new SerializedObject(director);
                serialized.FindProperty("data").objectReferenceValue = data;
                var array = serialized.FindProperty("ledges");
                array.arraySize = 2;
                array.GetArrayElementAtIndex(0).objectReferenceValue = first;
                array.GetArrayElementAtIndex(1).objectReferenceValue = second;
                serialized.FindProperty("environment").objectReferenceValue = null;
                serialized.FindProperty("player").objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"[LedgeSetup] Prefab written: {PrefabPath}");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static LedgeThing CreateLedge(
            string name,
            Transform parent,
            Sprite rock,
            Sprite rockEdge,
            Sprite hint,
            float wallTopY,
            float upperBedTopY,
            bool withHint)
        {
            var target = new GameObject(name);
            target.transform.SetParent(parent, false);
            target.transform.localPosition = new Vector3(ParkX, 0f, 0f);

            var thing = target.AddComponent<LedgeThing>();

            CreateTiled(
                "UpperBed",
                target.transform,
                rock,
                new Vector3(UpperBedLength * 0.5f, (upperBedTopY + BedBottomY) * 0.5f, 0f),
                new Vector2(UpperBedLength, Mathf.Max(0.2f, upperBedTopY - BedBottomY)),
                LedgeOrder);

            CreateTiled(
                "StepWall",
                target.transform,
                rockEdge,
                new Vector3(WallWidth * 0.5f, (wallTopY + BedBottomY) * 0.5f, 0f),
                new Vector2(WallWidth, Mathf.Max(0.2f, wallTopY - BedBottomY)),
                LedgeOrder);

            var hintObject = CreateStretched(
                "Hint",
                target.transform,
                hint,
                new Vector3(HintOffsetX, HintOffsetY, 0f),
                new Vector2(HintWidth, HintHeight),
                HintOrder);
            hintObject.SetActive(false);

            var serialized = new SerializedObject(thing);
            serialized.FindProperty("hint").objectReferenceValue = withHint ? hintObject.transform : null;
            serialized.FindProperty("frontOffsetX").floatValue = 0f;
            serialized.FindProperty("parkX").floatValue = ParkX;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            target.SetActive(false);
            return thing;
        }

        private static void CreateTiled(
            string name,
            Transform parent,
            Sprite sprite,
            Vector3 localPosition,
            Vector2 size,
            int sortingOrder)
        {
            var target = new GameObject(name);
            target.transform.SetParent(parent, false);
            target.transform.localPosition = localPosition;

            var renderer = target.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = sortingOrder;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = size;
        }

        private static GameObject CreateStretched(
            string name,
            Transform parent,
            Sprite sprite,
            Vector3 localPosition,
            Vector2 worldSize,
            int sortingOrder)
        {
            var target = new GameObject(name);
            target.transform.SetParent(parent, false);
            target.transform.localPosition = localPosition;

            var renderer = target.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = sortingOrder;

            var bounds = sprite != null ? sprite.bounds.size : Vector3.one;
            var scaleX = bounds.x > 0f ? worldSize.x / bounds.x : 1f;
            var scaleY = bounds.y > 0f ? worldSize.y / bounds.y : 1f;
            target.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            return target;
        }

        private static float LoadTopLaneY()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameConfigAsset>(GameConfigPath);
            var values = asset != null ? asset.Values : new GameConfigValues();
            return values.LaneCount > 0 ? values.GetLaneY(0) : 1.1f;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}

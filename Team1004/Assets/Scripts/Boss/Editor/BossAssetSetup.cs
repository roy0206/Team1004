using System.IO;
using Game.Boss.Integration;
using Game.Config;
using Game.Player;
using Game.Water;
using UnityEditor;
using UnityEngine;

namespace Game.Boss.Editor
{
    public static class BossAssetSetup
    {
        private const string PlaceholderFolder = "Assets/GameAssets/Placeholder";
        private const string SquareSpritePath = PlaceholderFolder + "/Square.png";
        private const string CircleSpritePath = PlaceholderFolder + "/Circle.png";
        private const string GameConfigPath = "Assets/GameAssets/Design/GameConfig.asset";
        private const string DesignFolder = "Assets/GameAssets/Design/Boss";
        private const string BossFolder = "Assets/GameAssets/Boss";
        private const string FishingLineDataPath = DesignFolder + "/FishingLineBossData.asset";
        private const string WalrusDataPath = DesignFolder + "/WalrusBossData.asset";
        private const string WaterfallDataPath = DesignFolder + "/WaterfallBossData.asset";
        private const string FishingLinePrefabPath = BossFolder + "/FishingLineBoss.prefab";
        private const string WalrusPrefabPath = BossFolder + "/WalrusBoss.prefab";
        private const string WaterfallPrefabPath = BossFolder + "/WaterfallBoss.prefab";
        private const string BossSetPrefabPath = BossFolder + "/BossSet.prefab";

        private const int SpritePixels = 64;
        private const float PixelsPerUnit = 100f;
        private const float CameraSize = 3.6f;
        private const float ViewWidth = CameraSize * 2f * 16f / 9f;
        private const float LaneHeight = 0.9f;
        private const float TelegraphMinAlpha = 0.3f;
        private const float TelegraphMaxAlpha = 0.4f;
        private const float TelegraphBrightAlpha = 0.45f;

        private static readonly Color TelegraphColor = new(1f, 0.2f, 0.2f, 0.35f);
        private static readonly Color HookColor = new(0.85f, 0.85f, 0.85f, 1f);
        private static readonly Color LineColor = new(0.95f, 0.95f, 0.95f, 0.9f);
        private static readonly Color WalrusColor = new(0.55f, 0.4f, 0.3f, 1f);
        private static readonly Color WaterfallColor = new(0.75f, 0.9f, 1f, 0.9f);
        private static readonly Color RapidColor = new(0.5f, 0.75f, 1f, 0.6f);
        private static readonly Color RockColor = new(0.5f, 0.5f, 0.5f, 1f);

        private sealed class Context
        {
            public Sprite Square;
            public Sprite Circle;
            public GameConfigValues Config;
        }

        [MenuItem("Team1004/Generate Boss Assets")]
        public static void Generate()
        {
            Run(false);
        }

        [MenuItem("Team1004/Regenerate Boss Prefabs (Overwrite)")]
        public static void RegeneratePrefabs()
        {
            if (!EditorUtility.DisplayDialog(
                    "Regenerate Boss Prefabs",
                    "Boss prefabs in Assets/GameAssets/Boss will be overwritten. Boss data assets are kept.",
                    "Overwrite",
                    "Cancel"))
                return;

            Run(true);
        }

        public static void Generate(bool overwritePrefabs)
        {
            Run(overwritePrefabs);
        }

        private static void Run(bool overwritePrefabs)
        {
            EnsureFolder(PlaceholderFolder);
            EnsureFolder(DesignFolder);
            EnsureFolder(BossFolder);

            var context = new Context
            {
                Square = EnsureSprite(SquareSpritePath, false),
                Circle = EnsureSprite(CircleSpritePath, true),
                Config = LoadConfig()
            };

            if (context.Square == null || context.Circle == null)
            {
                Debug.LogError("[BossAssetSetup] Placeholder sprites are missing. Aborted.");
                return;
            }

            var fishingData = EnsureData<FishingLineBossData>(FishingLineDataPath);
            var walrusData = EnsureData<WalrusBossData>(WalrusDataPath);
            var waterfallData = EnsureData<WaterfallBossData>(WaterfallDataPath);

            if (ShouldBuildPrefab(FishingLinePrefabPath, overwritePrefabs))
                BuildFishingLinePrefab(context, fishingData);

            if (ShouldBuildPrefab(WalrusPrefabPath, overwritePrefabs))
                BuildWalrusPrefab(context, walrusData);

            if (ShouldBuildPrefab(WaterfallPrefabPath, overwritePrefabs))
                BuildWaterfallPrefab(context, waterfallData);

            if (ShouldBuildPrefab(BossSetPrefabPath, overwritePrefabs))
                BuildBossSetPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BossAssetSetup] Boss assets are ready");
        }

        private static bool ShouldBuildPrefab(string path, bool overwrite)
        {
            if (!File.Exists(path))
                return true;

            if (overwrite)
                return true;

            Debug.LogWarning($"[BossAssetSetup] Prefab already exists and was kept: {path}");
            return false;
        }

        private static GameConfigValues LoadConfig()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameConfigAsset>(GameConfigPath);
            return asset != null ? asset.Values : new GameConfigValues();
        }

        private static T EnsureData<T>(string path) where T : BossData
        {
            var data = AssetDatabase.LoadAssetAtPath<T>(path);

            if (data == null)
            {
                data = ScriptableObject.CreateInstance<T>();
                data.EnsureDefaultPatterns();
                AssetDatabase.CreateAsset(data, path);
                return data;
            }

            data.EnsureDefaultPatterns();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void BuildFishingLinePrefab(Context context, FishingLineBossData data)
        {
            var config = context.Config;
            var root = new GameObject("FishingLineBoss");

            try
            {
                var boss = root.AddComponent<FishingLineBoss>();
                var telegraph = CreateTelegraph(context, root.transform);
                var laneHazard = CreateLaneHazard(context, root.transform, "FishingHook");

                var parkY = config.WaterSurfaceY + data.HookParkOffset;
                var hook = new GameObject("Hook");
                hook.transform.SetParent(root.transform, false);
                hook.transform.position = new Vector3(data.HookExitX, parkY, 0f);
                AddWaterBody(hook, new Vector2(0.5f, 0.6f));

                var hookSprite = CreateSprite("HookSprite", hook.transform, context.Circle, HookColor,
                    hook.transform.position, new Vector2(0.5f, 0.5f), 6);
                var hookRenderer = hookSprite.GetComponent<SpriteRenderer>();
                hookRenderer.enabled = false;

                var lineSprite = CreateSprite("Line", root.transform, context.Square, LineColor,
                    hook.transform.position, new Vector2(data.LineWidth, 1f), 5);
                var lineRenderer = lineSprite.GetComponent<SpriteRenderer>();
                lineRenderer.enabled = false;

                var serialized = new SerializedObject(boss);
                serialized.FindProperty("data").objectReferenceValue = data;
                serialized.FindProperty("telegraph").objectReferenceValue = telegraph;
                serialized.FindProperty("laneHazard").objectReferenceValue = laneHazard;
                serialized.FindProperty("hook").objectReferenceValue = hook.transform;
                serialized.FindProperty("hookRenderer").objectReferenceValue = hookRenderer;
                serialized.FindProperty("line").objectReferenceValue = lineSprite.transform;
                serialized.FindProperty("lineRenderer").objectReferenceValue = lineRenderer;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                SavePrefab(root, FishingLinePrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void BuildWalrusPrefab(Context context, WalrusBossData data)
        {
            var config = context.Config;
            var root = new GameObject("WalrusBoss");

            try
            {
                var middleLane = config.LaneCount / 2;
                root.transform.position = Vector3.zero;

                var boss = root.AddComponent<WalrusBoss>();
                var telegraph = CreateTelegraph(context, root.transform);
                var laneHazard = CreateLaneHazard(context, root.transform, "Walrus");

                var body = CreateSprite("Body", root.transform, context.Square, WalrusColor,
                    new Vector3(data.RestX, config.GetLaneY(middleLane), 0f),
                    new Vector2(data.BodyWidth, data.SingleBodyHeight), 6);
                AddWaterBody(body, context.Square.bounds.size);

                var serialized = new SerializedObject(boss);
                serialized.FindProperty("data").objectReferenceValue = data;
                serialized.FindProperty("telegraph").objectReferenceValue = telegraph;
                serialized.FindProperty("laneHazard").objectReferenceValue = laneHazard;
                serialized.FindProperty("body").objectReferenceValue = body.transform;
                serialized.FindProperty("bodyRenderer").objectReferenceValue = body.GetComponent<SpriteRenderer>();
                serialized.ApplyModifiedPropertiesWithoutUndo();

                SavePrefab(root, WalrusPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void BuildWaterfallPrefab(Context context, WaterfallBossData data)
        {
            var config = context.Config;
            var root = new GameObject("WaterfallBoss");

            try
            {
                var boss = root.AddComponent<WaterfallBoss>();
                var telegraph = CreateTelegraph(context, root.transform);
                var laneHazard = CreateLaneHazard(context, root.transform, "Rapid");

                var waterfall = CreateSprite("Waterfall", root.transform, context.Square, WaterfallColor,
                    new Vector3(data.WaterfallX, 0f, 0f), new Vector2(1.5f, CameraSize * 2f + 0.8f), 4);

                var laneCount = config.LaneCount;
                var rapids = new Transform[laneCount];
                var rocks = new Transform[laneCount];

                for (var lane = 0; lane < laneCount; lane++)
                {
                    var y = config.GetLaneY(lane);

                    var rapid = CreateSprite($"Rapid{lane}", root.transform, context.Square, RapidColor,
                        new Vector3(data.SpawnX, y, 0f), new Vector2(ViewWidth * 0.25f, LaneHeight), 5);
                    rapids[lane] = rapid.transform;
                    rapid.SetActive(false);

                    var rock = CreateSprite($"Rock{lane}", root.transform, context.Circle, RockColor,
                        new Vector3(data.SpawnX + data.RockTrail, y, 0f), new Vector2(0.7f, 0.7f), 6);
                    rocks[lane] = rock.transform;
                    rock.SetActive(false);
                }

                var serialized = new SerializedObject(boss);
                serialized.FindProperty("data").objectReferenceValue = data;
                serialized.FindProperty("telegraph").objectReferenceValue = telegraph;
                serialized.FindProperty("laneHazard").objectReferenceValue = laneHazard;
                serialized.FindProperty("waterfall").objectReferenceValue = waterfall.transform;
                serialized.FindProperty("waterfallRenderer").objectReferenceValue = waterfall.GetComponent<SpriteRenderer>();
                SetArray(serialized.FindProperty("rapids"), rapids);
                SetArray(serialized.FindProperty("rocks"), rocks);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                SavePrefab(root, WaterfallPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void BuildBossSetPrefab()
        {
            var root = new GameObject("BossSet");

            try
            {
                var director = root.AddComponent<BossDirector>();
                var bosses = new BossThing[]
                {
                    InstantiateBoss(FishingLinePrefabPath, root.transform),
                    InstantiateBoss(WalrusPrefabPath, root.transform),
                    InstantiateBoss(WaterfallPrefabPath, root.transform)
                };

                var serialized = new SerializedObject(director);
                SetArray(serialized.FindProperty("bosses"), bosses);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, BossSetPrefabPath);
                Debug.Log($"[BossAssetSetup] Prefab written: {BossSetPrefabPath}");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static BossThing InstantiateBoss(string prefabPath, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab == null)
            {
                Debug.LogError($"[BossAssetSetup] Boss prefab is missing: {prefabPath}");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(parent, false);
            instance.SetActive(false);
            return instance.GetComponent<BossThing>();
        }

        private static LaneTelegraph CreateTelegraph(Context context, Transform parent)
        {
            var config = context.Config;
            var telegraphObject = new GameObject("Telegraph");
            telegraphObject.transform.SetParent(parent, false);
            telegraphObject.transform.position = Vector3.zero;
            var telegraph = telegraphObject.AddComponent<LaneTelegraph>();

            var renderers = new SpriteRenderer[config.LaneCount];

            for (var lane = 0; lane < config.LaneCount; lane++)
            {
                var laneObject = CreateSprite($"Lane{lane}", telegraphObject.transform, context.Square, TelegraphColor,
                    new Vector3(0f, config.GetLaneY(lane), 0f),
                    new Vector2(config.BossLaneBandWidth, config.BossLaneBandHeight), 3);
                renderers[lane] = laneObject.GetComponent<SpriteRenderer>();
                renderers[lane].enabled = false;
            }

            var serialized = new SerializedObject(telegraph);
            SetArray(serialized.FindProperty("laneRenderers"), renderers);
            serialized.FindProperty("minAlpha").floatValue = TelegraphMinAlpha;
            serialized.FindProperty("maxAlpha").floatValue = TelegraphMaxAlpha;
            serialized.FindProperty("brightAlpha").floatValue = TelegraphBrightAlpha;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return telegraph;
        }

        private static GameObject CreateSprite(
            string name,
            Transform parent,
            Sprite sprite,
            Color color,
            Vector3 position,
            Vector2 worldSize,
            int sortingOrder)
        {
            var spriteObject = new GameObject(name);
            spriteObject.transform.SetParent(parent, false);
            spriteObject.transform.position = position;

            var renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            var bounds = sprite.bounds.size;
            var scaleX = bounds.x > 0f ? worldSize.x / bounds.x : 1f;
            var scaleY = bounds.y > 0f ? worldSize.y / bounds.y : 1f;
            spriteObject.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            return spriteObject;
        }

        private static void AddWaterBody(GameObject target, Vector2 colliderSize)
        {
            var body = target.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.useFullKinematicContacts = true;
            body.gravityScale = 0f;

            var collider = target.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = colliderSize;

            var interactor = target.AddComponent<WaterInteractor>();
            var serialized = new SerializedObject(interactor);
            serialized.FindProperty("shape").objectReferenceValue = collider;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static LaneHazard CreateLaneHazard(Context context, Transform parent, string kind)
        {
            var config = context.Config;
            var root = new GameObject("LaneHazard");
            root.transform.SetParent(parent, false);
            root.transform.position = Vector3.zero;

            var laneHazard = root.AddComponent<LaneHazard>();
            var laneCount = config.LaneCount;
            var hazards = new Hazard[laneCount];
            var boxes = new BoxCollider2D[laneCount];
            var width = config.BossLaneBandWidth;
            var height = config.BossLaneBandHeight;

            for (var lane = 0; lane < laneCount; lane++)
            {
                var laneObject = new GameObject($"Band{lane}");
                laneObject.transform.SetParent(root.transform, false);
                laneObject.transform.position = new Vector3(0f, config.GetLaneY(lane), 0f);

                var body = laneObject.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.useFullKinematicContacts = true;
                body.gravityScale = 0f;

                var box = laneObject.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(width, height);

                var hazard = laneObject.AddComponent<Hazard>();
                var hazardSerialized = new SerializedObject(hazard);
                hazardSerialized.FindProperty("kind").stringValue = kind;
                hazardSerialized.ApplyModifiedPropertiesWithoutUndo();

                hazards[lane] = hazard;
                boxes[lane] = box;
                laneObject.SetActive(false);
            }

            var serialized = new SerializedObject(laneHazard);
            SetArray(serialized.FindProperty("laneHazards"), hazards);
            SetArray(serialized.FindProperty("laneBoxes"), boxes);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return laneHazard;
        }

        private static void SetArray<T>(SerializedProperty property, T[] values) where T : Object
        {
            property.arraySize = values.Length;

            for (var i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static void SavePrefab(GameObject root, string path)
        {
            root.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Debug.Log($"[BossAssetSetup] Prefab written: {path}");
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

        private static Sprite EnsureSprite(string path, bool circle)
        {
            if (!File.Exists(path))
            {
                var texture = new Texture2D(SpritePixels, SpritePixels, TextureFormat.RGBA32, false);
                var pixels = new Color32[SpritePixels * SpritePixels];
                var center = (SpritePixels - 1) * 0.5f;
                var radius = SpritePixels * 0.5f;

                for (var y = 0; y < SpritePixels; y++)
                {
                    for (var x = 0; x < SpritePixels; x++)
                    {
                        var alpha = 1f;

                        if (circle)
                        {
                            var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                            alpha = Mathf.Clamp01(radius - distance + 0.5f);
                        }

                        pixels[y * SpritePixels + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            if (AssetImporter.GetAtPath(path) is TextureImporter importer &&
                (importer.textureType != TextureImporterType.Sprite ||
                 !Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit)))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = PixelsPerUnit;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (sprite == null)
                Debug.LogError($"[BossAssetSetup] Sprite was not imported: {path}");

            return sprite;
        }
    }
}

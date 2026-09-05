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
        private const string HitboxFrameSpritePath = PlaceholderFolder + "/HitboxFrame.png";
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
        private const int HitboxFramePixels = 64;
        private const int HitboxFrameBorder = 8;
        private const byte HitboxFrameFillAlpha = 64;
        private const int HitboxFrameOrder = 9;
        private const float TelegraphMinAlpha = 0.3f;
        private const float TelegraphMaxAlpha = 0.4f;
        private const float TelegraphBrightAlpha = 0.45f;

        private static readonly Color TelegraphColor = new(1f, 0.2f, 0.2f, 0.35f);
        private static readonly Color HitboxFrameColor = new(1f, 0.15f, 0.15f, 0.35f);
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
            public Sprite HitboxFrame;
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
                HitboxFrame = EnsureHitboxFrameSprite(),
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

                var parkY = config.WaterSurfaceY + data.HookParkOffset;
                var hook = new GameObject("Hook");
                hook.transform.SetParent(root.transform, false);
                hook.transform.position = new Vector3(config.PlayerX, parkY, 0f);
                var hookHazard = AddHazard(context, hook, "FishingHook", new Vector2(0.5f, 0.6f), true);

                CreateSprite("HookSprite", hook.transform, context.Circle, HookColor,
                    hook.transform.position, new Vector2(0.5f, 0.5f), 6);
                CreateSprite("Line", hook.transform, context.Square, LineColor,
                    hook.transform.position + new Vector3(0f, 4f, 0f), new Vector2(0.06f, 8f), 5);

                var serialized = new SerializedObject(boss);
                serialized.FindProperty("data").objectReferenceValue = data;
                serialized.FindProperty("telegraph").objectReferenceValue = telegraph;
                serialized.FindProperty("hook").objectReferenceValue = hook.transform;
                serialized.FindProperty("hookHazard").objectReferenceValue = hookHazard;
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
                root.transform.position = new Vector3(data.RestX, config.GetLaneY(middleLane), 0f);

                var boss = root.AddComponent<WalrusBoss>();
                var telegraph = CreateTelegraph(context, root.transform);

                var body = CreateSprite("Body", root.transform, context.Square, WalrusColor,
                    root.transform.position, new Vector2(data.BodyWidth, data.SingleBodyHeight), 6);
                var bodyHazard = AddHazard(context, body, "Walrus", context.Square.bounds.size, true);

                var serialized = new SerializedObject(boss);
                serialized.FindProperty("data").objectReferenceValue = data;
                serialized.FindProperty("telegraph").objectReferenceValue = telegraph;
                serialized.FindProperty("body").objectReferenceValue = body.transform;
                serialized.FindProperty("bodyRenderer").objectReferenceValue = body.GetComponent<SpriteRenderer>();
                serialized.FindProperty("bodyHazard").objectReferenceValue = bodyHazard;
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

                var waterfall = CreateSprite("Waterfall", root.transform, context.Square, WaterfallColor,
                    new Vector3(data.WaterfallX, 0f, 0f), new Vector2(1.5f, CameraSize * 2f + 0.8f), 4);

                var laneCount = config.LaneCount;
                var rapids = new Transform[laneCount];
                var rapidHazards = new Hazard[laneCount];
                var rocks = new Transform[laneCount];
                var rockHazards = new Hazard[laneCount];

                for (var lane = 0; lane < laneCount; lane++)
                {
                    var y = config.GetLaneY(lane);

                    var rapid = CreateSprite($"Rapid{lane}", root.transform, context.Square, RapidColor,
                        new Vector3(data.SpawnX, y, 0f), new Vector2(3f, LaneHeight), 5);
                    rapids[lane] = rapid.transform;
                    rapidHazards[lane] = AddHazard(context, rapid, "Rapid", context.Square.bounds.size, false);

                    var rock = CreateSprite($"Rock{lane}", root.transform, context.Circle, RockColor,
                        new Vector3(data.SpawnX + data.RockTrail, y, 0f), new Vector2(0.7f, 0.7f), 6);
                    rocks[lane] = rock.transform;
                    rockHazards[lane] = AddHazard(context, rock, "Rock", context.Circle.bounds.size, false);
                }

                var serialized = new SerializedObject(boss);
                serialized.FindProperty("data").objectReferenceValue = data;
                serialized.FindProperty("telegraph").objectReferenceValue = telegraph;
                serialized.FindProperty("waterfall").objectReferenceValue = waterfall.transform;
                serialized.FindProperty("waterfallRenderer").objectReferenceValue = waterfall.GetComponent<SpriteRenderer>();
                SetArray(serialized.FindProperty("rapids"), rapids);
                SetArray(serialized.FindProperty("rapidHazards"), rapidHazards);
                SetArray(serialized.FindProperty("rocks"), rocks);
                SetArray(serialized.FindProperty("rockHazards"), rockHazards);
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
                    new Vector3(0f, config.GetLaneY(lane), 0f), new Vector2(ViewWidth, LaneHeight), 3);
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

        private static Hazard AddHazard(Context context, GameObject target, string kind, Vector2 colliderSize, bool touchesWater)
        {
            var body = target.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.useFullKinematicContacts = true;
            body.gravityScale = 0f;

            var collider = target.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = colliderSize;

            var hazard = target.AddComponent<Hazard>();
            var serialized = new SerializedObject(hazard);
            serialized.FindProperty("kind").stringValue = kind;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            hazard.enabled = false;

            AddHitboxView(context, target, collider, colliderSize);

            if (touchesWater)
            {
                var interactor = target.AddComponent<WaterInteractor>();
                var interactorSerialized = new SerializedObject(interactor);
                interactorSerialized.FindProperty("shape").objectReferenceValue = collider;
                interactorSerialized.ApplyModifiedPropertiesWithoutUndo();
            }

            return hazard;
        }

        private static HitboxView AddHitboxView(Context context, GameObject target, BoxCollider2D collider, Vector2 colliderSize)
        {
            var frameObject = new GameObject("HitboxFrame");
            frameObject.transform.SetParent(target.transform, false);
            frameObject.transform.localPosition = new Vector3(collider.offset.x, collider.offset.y, 0f);

            var frame = frameObject.AddComponent<SpriteRenderer>();
            frame.sprite = context.HitboxFrame;
            frame.drawMode = SpriteDrawMode.Sliced;
            frame.size = colliderSize;
            frame.color = HitboxFrameColor;
            frame.sortingOrder = HitboxFrameOrder;
            frame.enabled = false;

            var view = target.AddComponent<HitboxView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("frame").objectReferenceValue = frame;
            serialized.FindProperty("box").objectReferenceValue = collider;
            serialized.FindProperty("color").colorValue = HitboxFrameColor;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static Sprite EnsureHitboxFrameSprite()
        {
            var path = HitboxFrameSpritePath;

            if (!File.Exists(path))
            {
                var texture = new Texture2D(HitboxFramePixels, HitboxFramePixels, TextureFormat.RGBA32, false);
                var pixels = new Color32[HitboxFramePixels * HitboxFramePixels];

                for (var y = 0; y < HitboxFramePixels; y++)
                {
                    for (var x = 0; x < HitboxFramePixels; x++)
                    {
                        var onBorder = x < HitboxFrameBorder || y < HitboxFrameBorder ||
                                       x >= HitboxFramePixels - HitboxFrameBorder ||
                                       y >= HitboxFramePixels - HitboxFrameBorder;
                        pixels[y * HitboxFramePixels + x] = new Color32(255, 255, 255, onBorder ? (byte)255 : HitboxFrameFillAlpha);
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            var border = new Vector4(HitboxFrameBorder, HitboxFrameBorder, HitboxFrameBorder, HitboxFrameBorder);

            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);

                if (settings.textureType != TextureImporterType.Sprite ||
                    settings.spriteBorder != border ||
                    settings.spriteMeshType != SpriteMeshType.FullRect ||
                    !Mathf.Approximately(settings.spritePixelsPerUnit, PixelsPerUnit))
                {
                    settings.textureType = TextureImporterType.Sprite;
                    settings.spriteMode = (int)SpriteImportMode.Single;
                    settings.spritePixelsPerUnit = PixelsPerUnit;
                    settings.spriteBorder = border;
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    settings.mipmapEnabled = false;
                    settings.alphaIsTransparency = true;
                    settings.filterMode = FilterMode.Bilinear;
                    importer.SetTextureSettings(settings);
                    importer.SaveAndReimport();
                }
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (sprite == null)
                Debug.LogError($"[BossAssetSetup] Sprite was not imported: {path}");

            return sprite;
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

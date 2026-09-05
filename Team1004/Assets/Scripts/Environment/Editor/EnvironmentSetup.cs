using System.IO;
using Game.Config;
using Game.Tools.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using WaterSurface = Game.Water.Water;

namespace Game.Environment.Editor
{
    public static class EnvironmentSetup
    {
        public const string PrefabPath = "Assets/GameAssets/Environment/Environment.prefab";

        private const string PlaceholderFolder = "Assets/GameAssets/Placeholder";
        private const string EnvironmentFolder = "Assets/GameAssets/Environment";
        private const string GameConfigPath = "Assets/GameAssets/Design/GameConfig.asset";
        private const string ProfilePath = EnvironmentFolder + "/WaterSurfaceProfile.asset";
        private const string LitMaterialPath =
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";

        private const string SkySpritePath = PlaceholderFolder + "/Env_Sky.png";
        private const string FarSpritePath = PlaceholderFolder + "/Env_Far.png";
        private const string MidSpritePath = PlaceholderFolder + "/Env_Mid.png";
        private const string NearSpritePath = PlaceholderFolder + "/Env_Near.png";
        private const string FlowSpritePath = PlaceholderFolder + "/Env_Flow.png";
        private const string HomelandSpritePath = PlaceholderFolder + "/Env_Homeland.png";
        private const string CalmSpritePath = PlaceholderFolder + "/Env_Calm.png";
        private const string WaterFillTexturePath = PlaceholderFolder + "/Env_WaterFill.png";

        private const float CameraSize = 3.6f;
        private const float ViewWidth = CameraSize * 2f * 16f / 9f;
        private const float Margin = 1f;
        private const float WaterWidth = 16f;
        private const float WaterDepth = 0.8f;

        private const float FarTileWidth = 8f;
        private const float MidTileWidth = 6.4f;
        private const float NearTileWidth = 6.4f;
        private const float FlowTileWidth = 4.8f;
        private const float HomelandTileWidth = 8f;

        private const float FarSpeedScale = 0.3f;
        private const float MidSpeedScale = 0.6f;
        private const float NearSpeedScale = 1f;
        private const float FlowSpeedScale = 0.8f;
        private const float HomelandSpeedScale = 1f;

        private const float BaseFlowSpeed = 0.6f;
        private const float CalmTimeScale = 0.6f;
        private const float SplashRadius = 0.55f;
        private const float SplashMaxDepth = 0.6f;

        private const int SkyOrder = -9;
        private const int FarOrder = -8;
        private const int MidOrder = -7;
        private const int NearOrder = -6;
        private const int HomelandOrder = -4;
        private const int FlowOrder = -3;
        private const int WaterOrder = -2;
        private const int CalmOrder = -1;

        private static readonly Color White = Color.white;

        [MenuItem("Team1004/Generate Environment Assets")]
        public static void Generate()
        {
            if (GeneratorFreeze.Block(nameof(EnvironmentSetup)))
                return;

            Generate(false);
        }

        [MenuItem("Team1004/Regenerate Environment Prefab (Overwrite)")]
        public static void RegeneratePrefab()
        {
            if (GeneratorFreeze.Block(nameof(EnvironmentSetup)))
                return;

            if (!Application.isBatchMode && !EditorUtility.DisplayDialog(
                    "Regenerate Environment Prefab",
                    $"{PrefabPath} will be overwritten. Placeholder textures are kept.",
                    "Overwrite",
                    "Cancel"))
                return;

            Generate(true);
        }

        public static void Generate(bool overwrite)
        {
            if (GeneratorFreeze.Block(nameof(EnvironmentSetup)))
                return;

            EnsureFolder(PlaceholderFolder);
            EnsureFolder(EnvironmentFolder);

            var sky = EnvironmentTextures.EnsureSprite(SkySpritePath, 64, 64, EnvironmentPatterns.Sky);
            var far = EnvironmentTextures.EnsureSprite(FarSpritePath, 128, 128, EnvironmentPatterns.Far);
            var mid = EnvironmentTextures.EnsureSprite(MidSpritePath, 128, 64, EnvironmentPatterns.Mid);
            var near = EnvironmentTextures.EnsureSprite(NearSpritePath, 128, 64, EnvironmentPatterns.Near);
            var flow = EnvironmentTextures.EnsureSprite(FlowSpritePath, 128, 32, EnvironmentPatterns.Flow);
            var homeland = EnvironmentTextures.EnsureSprite(HomelandSpritePath, 128, 64, EnvironmentPatterns.Homeland);
            var calm = EnvironmentTextures.EnsureSprite(CalmSpritePath, 32, 32, EnvironmentPatterns.Calm);
            var fill = EnvironmentTextures.EnsureTexture(WaterFillTexturePath, 32, 32, EnvironmentPatterns.WaterFill);

            var profile = EnsureProfile(fill);

            if (sky == null || far == null || mid == null || near == null ||
                flow == null || homeland == null || calm == null)
            {
                Debug.LogError("[EnvironmentSetup] Placeholder sprites are missing. Aborted.");
                return;
            }

            if (File.Exists(PrefabPath) && !overwrite)
            {
                Debug.LogWarning($"[EnvironmentSetup] Prefab already exists and was kept: {PrefabPath}");
                AssetDatabase.SaveAssets();
                return;
            }

            BuildPrefab(new Context
            {
                Sky = sky,
                Far = far,
                Mid = mid,
                Near = near,
                Flow = flow,
                Homeland = homeland,
                Calm = calm,
                Profile = profile,
                Config = LoadConfig()
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[EnvironmentSetup] Environment assets are ready");
        }

        private sealed class Context
        {
            public Sprite Sky;
            public Sprite Far;
            public Sprite Mid;
            public Sprite Near;
            public Sprite Flow;
            public Sprite Homeland;
            public Sprite Calm;
            public SpriteShape Profile;
            public GameConfigValues Config;
        }

        private static GameConfigValues LoadConfig()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameConfigAsset>(GameConfigPath);
            return asset != null ? asset.Values : new GameConfigValues();
        }

        private static void BuildPrefab(Context context)
        {
            var surfaceY = context.Config.WaterSurfaceY;
            var bottom = -CameraSize - Margin;
            var top = CameraSize + Margin;

            var root = new GameObject("Environment");

            try
            {
                var thing = root.AddComponent<EnvironmentThing>();

                CreateStretchedSprite("Sky", root.transform, context.Sky, White,
                    new Vector3(0f, (surfaceY + top) * 0.5f, 0f),
                    new Vector2(ViewWidth + 2f, Mathf.Max(0.5f, top - surfaceY)), SkyOrder);

                var farLayer = CreateLayer("FarLayer", root.transform, context.Far, White,
                    (surfaceY + bottom) * 0.5f, FarTileWidth, Mathf.Max(1f, surfaceY - bottom), FarOrder);

                var midLayer = CreateLayer("MidLayer", root.transform, context.Mid, White,
                    -0.2f, MidTileWidth, 4.4f, MidOrder);

                var nearLayer = CreateLayer("NearLayer", root.transform, context.Near, White,
                    -3f, NearTileWidth, 1.6f, NearOrder);

                var homelandLayer = CreateLayer("Homeland", root.transform, context.Homeland, White,
                    -2.55f, HomelandTileWidth, 1.5f, HomelandOrder);
                homelandLayer.gameObject.SetActive(false);

                var flowLayer = CreateLayer("FlowLayer", root.transform, context.Flow, White,
                    surfaceY - 0.35f, FlowTileWidth, 0.7f, FlowOrder);

                var surface = CreateWaterSurface(context, root.transform, surfaceY);

                var calmOverlay = CreateStretchedSprite("CalmOverlay", root.transform, context.Calm, White,
                    Vector3.zero, new Vector2(ViewWidth + 2f, CameraSize * 2f + 2f), CalmOrder);
                calmOverlay.SetActive(false);

                var serialized = new SerializedObject(thing);
                WriteLayer(serialized, "far", farLayer, FarTileWidth, FarSpeedScale);
                WriteLayer(serialized, "mid", midLayer, MidTileWidth, MidSpeedScale);
                WriteLayer(serialized, "near", nearLayer, NearTileWidth, NearSpeedScale);
                WriteLayer(serialized, "flow", flowLayer, FlowTileWidth, FlowSpeedScale);
                WriteLayer(serialized, "homeland", homelandLayer, HomelandTileWidth, HomelandSpeedScale);
                serialized.FindProperty("surface").objectReferenceValue = surface;
                serialized.FindProperty("calmOverlay").objectReferenceValue = calmOverlay;
                serialized.FindProperty("baseFlowSpeed").floatValue = BaseFlowSpeed;
                serialized.FindProperty("calmTimeScale").floatValue = CalmTimeScale;
                serialized.FindProperty("splashRadius").floatValue = SplashRadius;
                serialized.FindProperty("splashMaxDepth").floatValue = SplashMaxDepth;
                serialized.FindProperty("scrollingOnAwake").boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"[EnvironmentSetup] Prefab written: {PrefabPath}");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void WriteLayer(SerializedObject serialized, string field, Transform root, float tileWidth, float speedScale)
        {
            serialized.FindProperty($"{field}.root").objectReferenceValue = root;
            serialized.FindProperty($"{field}.tileWidth").floatValue = tileWidth;
            serialized.FindProperty($"{field}.speedScale").floatValue = speedScale;
        }

        private static Transform CreateLayer(
            string name,
            Transform parent,
            Sprite sprite,
            Color tint,
            float y,
            float tileWidth,
            float tileHeight,
            int sortingOrder)
        {
            var layer = new GameObject(name).transform;
            layer.SetParent(parent, false);
            layer.localPosition = new Vector3(0f, y, 0f);

            var count = TileStrip.MinimumTileCount(tileWidth, ViewWidth);

            for (var i = 0; i < count; i++)
            {
                var tile = new GameObject($"Tile{i}");
                tile.transform.SetParent(layer, false);
                tile.transform.localPosition = new Vector3(TileStrip.TileX(tileWidth, count, 0f, i), 0f, 0f);

                var renderer = tile.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = tint;
                renderer.sortingOrder = sortingOrder;
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.tileMode = SpriteTileMode.Continuous;
                renderer.size = new Vector2(tileWidth, tileHeight);
            }

            return layer;
        }

        private static GameObject CreateStretchedSprite(
            string name,
            Transform parent,
            Sprite sprite,
            Color tint,
            Vector3 localPosition,
            Vector2 worldSize,
            int sortingOrder)
        {
            var target = new GameObject(name);
            target.transform.SetParent(parent, false);
            target.transform.localPosition = localPosition;

            var renderer = target.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = tint;
            renderer.sortingOrder = sortingOrder;

            var bounds = sprite != null ? sprite.bounds.size : Vector3.one;
            var scaleX = bounds.x > 0f ? worldSize.x / bounds.x : 1f;
            var scaleY = bounds.y > 0f ? worldSize.y / bounds.y : 1f;
            target.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            return target;
        }

        private static WaterSurface CreateWaterSurface(Context context, Transform parent, float surfaceY)
        {
            var target = new GameObject("WaterSurface");
            target.transform.SetParent(parent, false);
            target.transform.localPosition = new Vector3(0f, surfaceY, 0f);

            var collider = target.AddComponent<PolygonCollider2D>();
            collider.isTrigger = true;

            var controller = target.AddComponent<SpriteShapeController>();
            controller.spriteShape = context.Profile;
            controller.autoUpdateCollider = false;
            controller.worldSpaceUVs = false;
            controller.fillPixelsPerUnit = EnvironmentTextures.PixelsPerUnit;

            var renderer = target.GetComponent<SpriteShapeRenderer>();

            if (renderer != null)
            {
                renderer.sortingOrder = WaterOrder;
                var material = AssetDatabase.LoadAssetAtPath<Material>(LitMaterialPath);

                if (material != null)
                    renderer.sharedMaterials = new[] { material, material };
                else
                    Debug.LogWarning($"[EnvironmentSetup] URP sprite material was not found: {LitMaterialPath}");
            }

            var size = new Vector2(WaterWidth, WaterDepth);
            BuildFlatShape(controller, collider, size);

            var water = target.AddComponent<WaterSurface>();
            var serialized = new SerializedObject(water);
            serialized.FindProperty("size").vector2Value = size;
            serialized.FindProperty("useSurface").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            BuildFlatShape(controller, collider, size);
            return water;
        }

        private static void BuildFlatShape(SpriteShapeController controller, PolygonCollider2D collider, Vector2 size)
        {
            var half = size.x * 0.5f;
            var spline = controller.spline;
            spline.Clear();
            spline.isOpenEnded = false;

            InsertPoint(spline, 0, new Vector3(-half, 0f, 0f), 0.2f);
            InsertPoint(spline, 1, new Vector3(half, 0f, 0f), 0.2f);
            InsertPoint(spline, 2, new Vector3(half, -size.y, 0f), 0f);
            InsertPoint(spline, 3, new Vector3(-half, -size.y, 0f), 0f);

            controller.BakeMesh();

            collider.pathCount = 1;
            collider.SetPath(0, new[]
            {
                new Vector2(-half, 0f),
                new Vector2(half, 0f),
                new Vector2(half, -size.y),
                new Vector2(-half, -size.y)
            });
        }

        private static void InsertPoint(Spline spline, int index, Vector3 position, float height)
        {
            spline.InsertPointAt(index, position);
            spline.SetTangentMode(index, ShapeTangentMode.Broken);
            spline.SetHeight(index, height);
        }

        private static SpriteShape EnsureProfile(Texture2D fill)
        {
            var profile = AssetDatabase.LoadAssetAtPath<SpriteShape>(ProfilePath);

            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<SpriteShape>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            profile.fillTexture = fill;
            profile.fillOffset = 0f;
            profile.useSpriteBorders = false;
            EditorUtility.SetDirty(profile);
            return profile;
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

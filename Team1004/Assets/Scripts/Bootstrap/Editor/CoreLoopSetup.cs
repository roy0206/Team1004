using System;
using System.IO;
using Game.Animation;
using Game.Animation.Editor;
using Game.Config;
using Game.Boss.Editor;
using Game.Boss.Integration;
using Game.Environment;
using Game.Environment.Editor;
using Game.Cutscene;
using Game.Cutscene.Editor;
using Game.Play;
using Game.Player;
using Game.Settings;
using Game.Spawner;
using Game.Spawner.Editor;
using Game.Title;
using Game.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Game.Bootstrap.Editor
{
    public static class CoreLoopSetup
    {
        private const string ScenesFolder = "Assets/Scenes";
        private const string BootstrapFolder = "Assets/Scripts/Bootstrap";
        private const string DesignFolder = "Assets/GameAssets/Design";
        private const string PlaceholderFolder = "Assets/GameAssets/Placeholder";
        private const string UiFolder = "Assets/GameAssets/UI";

        private const string StartScenePath = ScenesFolder + "/Start.unity";
        private const string PlayScenePath = ScenesFolder + "/Play.unity";
        private const string BootstrapScenePath = ScenesFolder + "/Bootstrap.unity";
        private const string StartSceneReferencePath = BootstrapFolder + "/StartScene.asset";
        private const string PlaySceneReferencePath = BootstrapFolder + "/PlayScene.asset";
        private const string SceneSettingsPath = BootstrapFolder + "/SceneSettings.asset";
        private const string InputActionsPath = BootstrapFolder + "/GameInput.inputactions";
        private const string ManifestPath = BootstrapFolder + "/AudioManifest.json";
        private const string GameConfigPath = DesignFolder + "/GameConfig.asset";
        private const string SquareSpritePath = PlaceholderFolder + "/Square.png";
        private const string CircleSpritePath = PlaceholderFolder + "/Circle.png";
        private const string FontFolder = PlaceholderFolder + "/Fonts";
        private const string FontPath = "Assets/GameAssets/Font/KOTRA HOPE.otf";
        private const string FallbackFontPath = FontFolder + "/MonaS12.ttf";
        private const string SettingsPanelPrefabPath = UiFolder + "/SettingsPanel.prefab";
        private const string CutsceneDialoguePrefabPath = UiFolder + "/CutsceneDialogue.prefab";
        private const string SpawnerSettingsPath = DesignFolder + "/Spawner/ObstacleSpawnerSettings.asset";
        private const string BossSetPrefabPath = "Assets/GameAssets/Boss/BossSet.prefab";
        private const string DialogueCsvPath = "Assets/GameAssets/Design/Dialogue/dialogue.csv";
        private const string EnvironmentPrefabPath = EnvironmentSetup.PrefabPath;
        private const string ControlHintText = "↑↓ : 레인 이동   ·   가장 위에서 ↑ : 점프";
        private const string TitleHintText = "↑↓ : 레인 이동 / 가장 위에서 ↑ : 점프";
        private const int PlayerSortingOrder = 10;

        private const string BuiltinFontName = "LegacyRuntime.ttf";
        private const string BuiltinUiSpritePath = "UI/Skin/UISprite.psd";
        private const string MainCameraTag = "MainCamera";
        private const int UiLayer = 5;
        private const int SpritePixels = 64;
        private const float PixelsPerUnit = 100f;
        private const float SpriteUnits = SpritePixels / PixelsPerUnit;
        private const float CameraSize = 3.6f;
        private const float ViewHeight = CameraSize * 2f;
        private const float ViewWidth = ViewHeight * 16f / 9f;
        private const float UiScale = 1.5f;
        private const float CanvasPlaneDistance = 1f;
        private static readonly Vector2 ReferenceResolution = new(1920f, 1080f);

        private static readonly Vector2 Center = new(0.5f, 0.5f);
        private static readonly Vector2 TopLeft = new(0f, 1f);
        private static readonly Vector2 TopRight = new(1f, 1f);
        private static readonly Vector2 TopCenter = new(0.5f, 1f);
        private static readonly Vector2 BottomLeft = new(0f, 0f);

        private static readonly Color CameraBackground = new(0.02f, 0.2f, 0.26f, 1f);
        private static readonly Color WaterColor = new(0.05f, 0.36f, 0.44f, 1f);
        private static readonly Color LaneGuideColor = new(1f, 1f, 1f, 0.08f);
        private static readonly Color RockColor = new(0.45f, 0.45f, 0.47f, 1f);
        private static readonly Color PlayerColor = new(1f, 0.52f, 0.16f, 1f);
        private static readonly Color DimColor = new(0f, 0f, 0f, 0.6f);
        private static readonly Color DeepDimColor = new(0f, 0f, 0f, 0.85f);
        private static readonly Color WindowColor = new(0.08f, 0.14f, 0.18f, 0.96f);
        private static readonly Color ButtonColor = new(0.86f, 0.9f, 0.92f, 1f);
        private static readonly Color ButtonTextColor = new(0.1f, 0.12f, 0.15f, 1f);
        private static readonly Color FieldColor = new(0.95f, 0.96f, 0.97f, 1f);
        private static readonly Color BarBackgroundColor = new(0f, 0f, 0f, 0.5f);
        private static readonly Color BarFillColor = new(0.4f, 0.88f, 0.7f, 1f);
        private static readonly Color SliderTrackColor = new(0.25f, 0.3f, 0.35f, 1f);
        private static readonly Color JumpRingBackgroundColor = new(0f, 0f, 0f, 0.55f);
        private static readonly Color CutsceneBossColor = new(0.25f, 0.2f, 0.35f, 1f);
        private static readonly Color CutsceneLandmarkColor = new(0.75f, 0.7f, 0.6f, 1f);

        private sealed class Context
        {
            public Sprite Square;
            public Sprite Circle;
            public Sprite UiSprite;
            public Font Font;
            public GameConfigAsset Config;
        }

        [MenuItem("Team1004/Generate Core Loop Scenes")]
        public static void Generate()
        {
            Run(false);
        }

        [MenuItem("Team1004/Regenerate Core Loop Scenes (Overwrite)")]
        public static void Regenerate()
        {
            var confirmed = Application.isBatchMode || EditorUtility.DisplayDialog(
                "Regenerate Core Loop Scenes",
                "Start.unity, Play.unity, Bootstrap.unity, SettingsPanel.prefab을 다시 만듭니다.\n" +
                "직접 고친 내용은 사라집니다. 계속할까요?",
                "덮어쓰기",
                "취소");

            if (confirmed)
                Run(true);
        }

        private static void Run(bool overwrite)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[CoreLoopSetup] Exit Play Mode before generating scenes.");
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EnsureFolders();

            var context = new Context
            {
                Square = EnsureSprite(SquareSpritePath, false),
                Circle = EnsureSprite(CircleSpritePath, true),
                UiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(BuiltinUiSpritePath),
                Font = EnsureFont(),
                Config = EnsureGameConfig()
            };

            if (context.UiSprite == null)
                context.UiSprite = context.Square;

            EnsureSpawnerAssets(overwrite);
            EnsureCutsceneDialoguePrefab();
            EnsureBossAssets(overwrite);
            EnsureAnimationAssets();
            EnsureEnvironmentAssets();

            EnsureSceneReference(StartSceneReferencePath);
            EnsureSceneReference(PlaySceneReferencePath);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EnsureSettingsPanelPrefab(context, overwrite);

            if (overwrite || !File.Exists(StartScenePath))
                CreateStartScene(context);
            else
                WarnExisting(StartScenePath);

            if (overwrite || !File.Exists(PlayScenePath))
                CreatePlayScene(context);
            else
                WarnExisting(PlayScenePath);

            AssetDatabase.ImportAsset(StartScenePath);
            AssetDatabase.ImportAsset(PlayScenePath);
            FillScenePath(StartSceneReferencePath, "scene", StartScenePath);
            FillScenePath(PlaySceneReferencePath, "scene", PlayScenePath);

            EnsureSceneSettings();

            if (overwrite || !File.Exists(BootstrapScenePath))
                CreateBootstrapScene(context);
            else
                WarnExisting(BootstrapScenePath);

            AssetDatabase.ImportAsset(BootstrapScenePath);
            FillScenePath(SceneSettingsPath, "bootstrapScene", BootstrapScenePath);
            RegisterBuildScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(StartScenePath, OpenSceneMode.Single);
            Debug.Log("[CoreLoopSetup] Core loop scenes are ready. Press Play (Play From Bootstrap starts from Bootstrap.unity).");
        }

        private static void EnsureCutsceneDialoguePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CutsceneDialoguePrefabPath) != null)
                return;

            Debug.Log("[CoreLoopSetup] CutsceneDialogue.prefab is missing. " +
                      "Running 'Team1004/Generate Cutscene Dialogue Prefab' first.");
            CutsceneSetup.Generate();
        }

        private static void EnsureSpawnerAssets(bool overwrite)
        {
            if (overwrite)
            {
                Debug.Log("[CoreLoopSetup] Regenerating spawner assets (overwrite) so obstacle defaults and stale prefabs are refreshed.");
                SpawnerAssetGenerator.Generate(true);
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<ObstacleSpawnerSettings>(SpawnerSettingsPath) != null)
                return;

            Debug.Log("[CoreLoopSetup] Spawner assets are missing. Running 'Team1004/Generate Spawner Assets' first.");
            SpawnerAssetGenerator.GenerateMissing();
        }

        private static void EnsureBossAssets(bool overwrite)
        {
            if (overwrite)
            {
                Debug.Log("[CoreLoopSetup] Regenerating boss prefabs (overwrite) so hitbox views and telegraph values are refreshed.");
                BossAssetSetup.Generate(true);
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(BossSetPrefabPath) != null)
                return;

            Debug.Log("[CoreLoopSetup] Boss assets are missing. Running 'Team1004/Generate Boss Assets' first.");
            BossAssetSetup.Generate();
        }

        private static void EnsureAnimationAssets()
        {
            AnimationAssetGenerator.GenerateMissing();
        }

        private static void EnsureEnvironmentAssets()
        {
            EnvironmentSetup.Generate(false);
        }

        private static void WarnExisting(string path)
        {
            Debug.LogWarning(
                $"[CoreLoopSetup] '{path}' already exists and was left untouched. " +
                "Scripts or fields added after it was generated are missing there; " +
                "run 'Team1004/Regenerate Core Loop Scenes (Overwrite)' to rebuild it.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/Scripts");
            EnsureFolder(BootstrapFolder);
            EnsureFolder("Assets/GameAssets");
            EnsureFolder(DesignFolder);
            EnsureFolder(PlaceholderFolder);
            EnsureFolder(UiFolder);
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
                Debug.LogError($"[CoreLoopSetup] Sprite was not imported: {path}");

            return sprite;
        }

        private static Font EnsureFont()
        {
            var font = LoadFont(FontPath) ?? LoadFont(FallbackFontPath);
            if (font != null)
                return font;

            Debug.LogError($"[CoreLoopSetup] Font file is missing: {FontPath} / {FallbackFontPath}. See Assets/GameAssets/Placeholder/Fonts/README.md");

            Debug.LogWarning(
                $"[CoreLoopSetup] Falling back to the builtin font '{BuiltinFontName}'. Korean text will not render.");
            return Resources.GetBuiltinResource<Font>(BuiltinFontName);
        }

        private static Font LoadFont(string path)
        {
            if (!File.Exists(path))
                return null;

            AssetDatabase.ImportAsset(path);
            var font = AssetDatabase.LoadAssetAtPath<Font>(path);
            if (font == null)
                Debug.LogWarning($"[CoreLoopSetup] Font asset could not be loaded: {path}");

            return font;
        }

        private static GameConfigAsset EnsureGameConfig()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameConfigAsset>(GameConfigPath);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<GameConfigAsset>();
            AssetDatabase.CreateAsset(asset, GameConfigPath);
            return asset;
        }

        private static SceneReference EnsureSceneReference(string path)
        {
            var reference = AssetDatabase.LoadAssetAtPath<SceneReference>(path);
            if (reference != null)
                return reference;

            reference = ScriptableObject.CreateInstance<SceneReference>();
            AssetDatabase.CreateAsset(reference, path);
            return reference;
        }

        private static SceneSettings EnsureSceneSettings()
        {
            var startScene = LoadRequired<SceneReference>(StartSceneReferencePath);
            var playScene = LoadRequired<SceneReference>(PlaySceneReferencePath);
            var settings = AssetDatabase.LoadAssetAtPath<SceneSettings>(SceneSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<SceneSettings>();
                AssetDatabase.CreateAsset(settings, SceneSettingsPath);
            }

            var serialized = new SerializedObject(settings);
            var scenes = serialized.FindProperty("scenes");
            scenes.ClearArray();
            scenes.InsertArrayElementAtIndex(0);
            scenes.GetArrayElementAtIndex(0).objectReferenceValue = startScene;
            scenes.InsertArrayElementAtIndex(1);
            scenes.GetArrayElementAtIndex(1).objectReferenceValue = playScene;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            return settings;
        }

        private static T LoadRequired<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new InvalidOperationException($"[CoreLoopSetup] Asset is missing: {path}");

            return asset;
        }

        private static void RefreshContext(Context context)
        {
            context.Square = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
            context.Circle = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
            context.UiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(BuiltinUiSpritePath);
            if (context.UiSprite == null)
                context.UiSprite = context.Square;

            context.Font = EnsureFont();
            context.Config = EnsureGameConfig();
        }

        private static void FillScenePath(string assetPath, string property, string scenePath)
        {
            var target = LoadRequired<Object>(assetPath);
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property + ".path").stringValue = scenePath;
            serialized.FindProperty(property + ".guid").stringValue = AssetDatabase.AssetPathToGUID(scenePath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void RegisterBuildScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootstrapScenePath, true),
                new EditorBuildSettingsScene(StartScenePath, true),
                new EditorBuildSettingsScene(PlayScenePath, true)
            };
        }

        private static GameObject EnsureSettingsPanelPrefab(Context context, bool overwrite)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(SettingsPanelPrefabPath);
            if (existing != null && !overwrite)
                return existing;

            var root = CreateRect("SettingsPanel", null);
            var dim = root.gameObject.AddComponent<Image>();
            dim.sprite = context.Square;
            dim.color = DimColor;
            dim.raycastTarget = true;
            Stretch(root, Vector2.zero, Vector2.zero);
            var panel = root.gameObject.AddComponent<SettingsPanel>();

            var window = CreateImage(context, root, "Window", context.UiSprite, WindowColor);
            Place(window.rectTransform, Center, Vector2.zero, new Vector2(620f, 560f));

            CreateText(context, window.transform, "Title", "설정", 40, Color.white, TextAnchor.MiddleCenter,
                Center, new Vector2(0f, 240f), new Vector2(400f, 60f));

            var masterSlider = CreateLabeledSlider(context, window.transform, "Master", "전체 볼륨", 150f);
            var bgmSlider = CreateLabeledSlider(context, window.transform, "Bgm", "배경음", 90f);
            var sfxSlider = CreateLabeledSlider(context, window.transform, "Sfx", "효과음", 30f);

            var developerButton = CreateButton(context, window.transform, "DeveloperButton", "개발자 설정",
                Center, new Vector2(0f, -60f), new Vector2(280f, 52f));
            var titleButton = CreateButton(context, window.transform, "TitleButton", "타이틀로",
                Center, new Vector2(0f, -120f), new Vector2(280f, 52f));
            var quitButton = CreateButton(context, window.transform, "QuitButton", "게임 종료",
                Center, new Vector2(0f, -180f), new Vector2(280f, 52f));
            var closeButton = CreateButton(context, window.transform, "CloseButton", "닫기",
                Center, new Vector2(0f, -240f), new Vector2(280f, 52f));

            var configPanel = CreateConfigPanel(context, root);

            var serialized = new SerializedObject(panel);
            serialized.FindProperty("masterSlider").objectReferenceValue = masterSlider;
            serialized.FindProperty("bgmSlider").objectReferenceValue = bgmSlider;
            serialized.FindProperty("sfxSlider").objectReferenceValue = sfxSlider;
            serialized.FindProperty("closeButton").objectReferenceValue = closeButton;
            serialized.FindProperty("titleButton").objectReferenceValue = titleButton;
            serialized.FindProperty("quitButton").objectReferenceValue = quitButton;
            serialized.FindProperty("developerButton").objectReferenceValue = developerButton;
            serialized.FindProperty("configPanel").objectReferenceValue = configPanel;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, SettingsPanelPrefabPath);
            Object.DestroyImmediate(root.gameObject);
            return prefab;
        }

        private static ConfigPanel CreateConfigPanel(Context context, Transform parent)
        {
            var root = CreateRect("ConfigPanel", parent);
            var dim = root.gameObject.AddComponent<Image>();
            dim.sprite = context.Square;
            dim.color = DeepDimColor;
            dim.raycastTarget = true;
            Stretch(root, Vector2.zero, Vector2.zero);
            var panel = root.gameObject.AddComponent<ConfigPanel>();

            var window = CreateImage(context, root, "Window", context.UiSprite, WindowColor);
            Place(window.rectTransform, Center, Vector2.zero, new Vector2(960f, 640f));

            CreateText(context, window.transform, "Title", "GameConfig (JSON)", 32, Color.white,
                TextAnchor.MiddleCenter, Center, new Vector2(0f, 280f), new Vector2(600f, 50f));

            var field = CreateInputField(context, window.transform, "JsonField",
                Center, new Vector2(0f, 30f), new Vector2(900f, 420f));

            var status = CreateText(context, window.transform, "Status", string.Empty, 18, Color.white,
                TextAnchor.MiddleLeft, Center, new Vector2(0f, -215f), new Vector2(900f, 32f));

            var applyButton = CreateButton(context, window.transform, "ApplyButton", "적용",
                Center, new Vector2(-220f, -270f), new Vector2(200f, 52f));
            var resetButton = CreateButton(context, window.transform, "ResetButton", "초기화",
                Center, new Vector2(0f, -270f), new Vector2(200f, 52f));
            var closeButton = CreateButton(context, window.transform, "CloseButton", "닫기",
                Center, new Vector2(220f, -270f), new Vector2(200f, 52f));

            var serialized = new SerializedObject(panel);
            serialized.FindProperty("jsonField").objectReferenceValue = field;
            serialized.FindProperty("applyButton").objectReferenceValue = applyButton;
            serialized.FindProperty("resetButton").objectReferenceValue = resetButton;
            serialized.FindProperty("closeButton").objectReferenceValue = closeButton;
            serialized.FindProperty("statusText").objectReferenceValue = status;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            root.gameObject.SetActive(false);
            return panel;
        }

        private static void CreateStartScene(Context context)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RefreshContext(context);
            var panelPrefab = LoadRequired<GameObject>(SettingsPanelPrefabPath);
            var playScene = LoadRequired<SceneReference>(PlaySceneReferencePath);

            var camera = CreateCamera();
            var canvas = CreateCanvas("Canvas", camera);
            var canvasTransform = canvas.transform;

            CreateText(context, canvasTransform, "Title", "초지일관 : 귀향 (임시)", 56, Color.white,
                TextAnchor.MiddleCenter, Center, new Vector2(0f, 170f), new Vector2(1000f, 100f));

            var startButton = CreateButton(context, canvasTransform, "StartButton", "시작",
                Center, new Vector2(0f, 30f), new Vector2(300f, 64f));
            var settingsButton = CreateButton(context, canvasTransform, "SettingsButton", "설정",
                Center, new Vector2(0f, -50f), new Vector2(300f, 64f));
            var quitButton = CreateButton(context, canvasTransform, "QuitButton", "종료",
                Center, new Vector2(0f, -130f), new Vector2(300f, 64f));

            CreateText(context, canvasTransform, "Hint", TitleHintText, 22,
                Color.white, TextAnchor.MiddleCenter, Center, new Vector2(0f, -230f), new Vector2(1000f, 40f));

            var record = CreateText(context, canvasTransform, "Record", "기록 없음", 20, Color.white,
                TextAnchor.LowerLeft, BottomLeft, new Vector2(20f, 16f), new Vector2(700f, 96f));

            var settingsPanel = InstantiateSettingsPanel(panelPrefab, canvasTransform, null);

            var titleObject = new GameObject("TitleScreen");
            var title = titleObject.AddComponent<TitleScreen>();
            var serialized = new SerializedObject(title);
            serialized.FindProperty("startButton").objectReferenceValue = startButton;
            serialized.FindProperty("settingsButton").objectReferenceValue = settingsButton;
            serialized.FindProperty("quitButton").objectReferenceValue = quitButton;
            serialized.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            serialized.FindProperty("playScene").objectReferenceValue = playScene;
            serialized.FindProperty("recordText").objectReferenceValue = record;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, StartScenePath);
        }

        private static void CreatePlayScene(Context context)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RefreshContext(context);
            var panelPrefab = LoadRequired<GameObject>(SettingsPanelPrefabPath);
            var playScene = LoadRequired<SceneReference>(PlaySceneReferencePath);
            var startScene = LoadRequired<SceneReference>(StartSceneReferencePath);
            var config = context.Config.Values;

            var camera = CreateCamera();
            CreateGlobalLight();

            var environment = InstantiateEnvironment();

            var guides = new GameObject("LaneGuides").transform;
            for (var i = 0; i < config.LaneCount; i++)
            {
                CreateSpriteObject($"LaneGuide{i}", guides, context.Square, LaneGuideColor,
                    new Vector3(0f, config.GetLaneY(i), 0f), new Vector2(ViewWidth + 2f, 0.06f), -5);
            }

            var scrollRoot = new GameObject("ScrollRoot");
            var scroller = scrollRoot.AddComponent<StageScroller>();

            var spawnerObject = new GameObject("ObstacleSpawner");
            var spawner = spawnerObject.AddComponent<ObstacleSpawner>();
            var spawnerSerialized = new SerializedObject(spawner);
            spawnerSerialized.FindProperty("settings").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<ObstacleSpawnerSettings>(SpawnerSettingsPath);
            spawnerSerialized.FindProperty("scrollRoot").objectReferenceValue = scrollRoot.transform;
            spawnerSerialized.ApplyModifiedPropertiesWithoutUndo();

            var playerRenderer = CreatePlayerRenderer(context, config, out var playerColliderSize, out var playerColliderOffset);
            var playerObject = playerRenderer.gameObject;
            var body = playerObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.useFullKinematicContacts = true;
            body.gravityScale = 0f;
            var playerCollider = playerObject.AddComponent<BoxCollider2D>();
            playerCollider.isTrigger = true;
            playerCollider.size = playerColliderSize;
            playerCollider.offset = playerColliderOffset;
            var player = playerObject.AddComponent<LanePlayer>();

            var swimClip = AssetDatabase.LoadAssetAtPath<CustomAnimation>(AnimationAssetGenerator.SwimClipPath);
            var laneUpClip = AssetDatabase.LoadAssetAtPath<CustomAnimation>(AnimationAssetGenerator.LaneUpClipPath);
            var laneDownClip = AssetDatabase.LoadAssetAtPath<CustomAnimation>(AnimationAssetGenerator.LaneDownClipPath);
            var jumpClip = AssetDatabase.LoadAssetAtPath<CustomAnimation>(AnimationAssetGenerator.JumpClipPath);
            if (swimClip == null || laneUpClip == null || laneDownClip == null || jumpClip == null)
                Debug.LogWarning("[CoreLoopSetup] Player animation clips are missing. Run 'Team1004/Generate Animation Assets'.");

            var playerSerialized = new SerializedObject(player);
            playerSerialized.FindProperty("swimClip").objectReferenceValue = swimClip;
            playerSerialized.FindProperty("laneUpClip").objectReferenceValue = laneUpClip;
            playerSerialized.FindProperty("laneDownClip").objectReferenceValue = laneDownClip;
            playerSerialized.FindProperty("jumpClip").objectReferenceValue = jumpClip;
            playerSerialized.ApplyModifiedPropertiesWithoutUndo();

            var playerActor = playerObject.AddComponent<CutsceneActor>();
            SetupActor(playerActor, CutsceneActorIds.Player, playerRenderer);

            var bossRenderer = CreateSpriteObject("CutsceneBoss", null, context.Square, CutsceneBossColor,
                new Vector3(9f, 0.3f, 0f), new Vector2(2.4f, 1.4f), 5);
            bossRenderer.enabled = false;
            var bossActor = bossRenderer.gameObject.AddComponent<CutsceneActor>();
            SetupActor(bossActor, CutsceneActorIds.Boss, bossRenderer);

            var landmarkRenderer = CreateSpriteObject("CutsceneLandmark", null, context.Circle, CutsceneLandmarkColor,
                new Vector3(2f, -1.6f, 0f), new Vector2(1.2f, 0.6f), 5);
            landmarkRenderer.enabled = false;
            var landmarkActor = landmarkRenderer.gameObject.AddComponent<CutsceneActor>();
            SetupActor(landmarkActor, CutsceneActorIds.Landmark, landmarkRenderer);

            var cutscenePlayer = CreateCutscenePlayer(camera, playerActor, bossActor, landmarkActor);
            WireCutsceneClips(cutscenePlayer, jumpClip, swimClip, laneUpClip, laneDownClip);
            InstantiateBossSet(camera);

            var hudCanvas = CreateCanvas("HudCanvas", camera);
            var hudTransform = hudCanvas.transform;

            var barBackground = CreateImage(context, hudTransform, "ProgressBar", context.UiSprite, BarBackgroundColor);
            Place(barBackground.rectTransform, TopCenter, new Vector2(0f, -28f), new Vector2(600f, 20f));
            var barFill = CreateImage(context, barBackground.transform, "Fill", context.Square, BarFillColor);
            Stretch(barFill.rectTransform, new Vector2(2f, 2f), new Vector2(-2f, -2f));
            barFill.type = Image.Type.Filled;
            barFill.fillMethod = Image.FillMethod.Horizontal;
            barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            barFill.fillAmount = 0f;

            var distanceText = CreateText(context, hudTransform, "Distance", "거리 0 m", 22, Color.white,
                TextAnchor.UpperLeft, TopLeft, new Vector2(20f, -16f), new Vector2(300f, 32f));
            var pauseButton = CreateButton(context, hudTransform, "PauseButton", "일시정지",
                TopRight, new Vector2(-20f, -16f), new Vector2(150f, 48f));

            var controlHint = CreateText(context, hudTransform, "ControlHint", ControlHintText, 26, Color.white,
                TextAnchor.MiddleCenter, Center, new Vector2(0f, -150f), new Vector2(900f, 48f));
            controlHint.gameObject.SetActive(false);

            var hud = hudCanvas.gameObject.AddComponent<PlayHud>();
            var hudSerialized = new SerializedObject(hud);
            hudSerialized.FindProperty("progressFill").objectReferenceValue = barFill;
            hudSerialized.FindProperty("distanceText").objectReferenceValue = distanceText;
            hudSerialized.FindProperty("pauseButton").objectReferenceValue = pauseButton;
            hudSerialized.FindProperty("controlHint").objectReferenceValue = controlHint;
            hudSerialized.ApplyModifiedPropertiesWithoutUndo();

            CreateJumpCooldownView(context, hudTransform);
            var bossTimer = CreateBossTimerView(context, hudTransform);

            var pausePanel = CreatePausePanel(context, hudTransform);
            var resultPanel = CreateResultPanel(context, hudTransform);
            var settingsPanel = InstantiateSettingsPanel(panelPrefab, hudTransform, startScene);

            var pauseSerialized = new SerializedObject(pausePanel);
            pauseSerialized.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            pauseSerialized.ApplyModifiedPropertiesWithoutUndo();

            var flowObject = new GameObject("PlayFlow");
            var flow = flowObject.AddComponent<PlayFlow>();
            var flowSerialized = new SerializedObject(flow);
            flowSerialized.FindProperty("player").objectReferenceValue = player;
            flowSerialized.FindProperty("scroller").objectReferenceValue = scroller;
            flowSerialized.FindProperty("spawner").objectReferenceValue = spawner;
            flowSerialized.FindProperty("bossTimer").objectReferenceValue = bossTimer;
            flowSerialized.FindProperty("hud").objectReferenceValue = hud;
            flowSerialized.FindProperty("pausePanel").objectReferenceValue = pausePanel;
            flowSerialized.FindProperty("resultPanel").objectReferenceValue = resultPanel;
            flowSerialized.FindProperty("cutscene").objectReferenceValue = cutscenePlayer;
            flowSerialized.FindProperty("environment").objectReferenceValue = environment;
            flowSerialized.FindProperty("introCutsceneId").stringValue = CutsceneCatalog.Intro;
            var sectionCutsceneIds = flowSerialized.FindProperty("sectionCutsceneIds");
            var sectionIds = CutsceneCatalog.SectionIds;
            sectionCutsceneIds.ClearArray();
            for (var i = 0; i < sectionIds.Count; i++)
            {
                sectionCutsceneIds.InsertArrayElementAtIndex(i);
                sectionCutsceneIds.GetArrayElementAtIndex(i).stringValue = sectionIds[i];
            }

            flowSerialized.FindProperty("endingCutsceneId").stringValue = CutsceneCatalog.Ending;
            flowSerialized.FindProperty("playScene").objectReferenceValue = playScene;
            flowSerialized.FindProperty("startScene").objectReferenceValue = startScene;
            flowSerialized.ApplyModifiedPropertiesWithoutUndo();

            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, PlayScenePath);
        }

        private static void CreateBootstrapScene(Context context)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RefreshContext(context);
            var settings = LoadRequired<SceneSettings>(SceneSettingsPath);
            var firstScene = LoadRequired<SceneReference>(StartSceneReferencePath);

            var controllerObject = new GameObject("SceneController");
            var controller = controllerObject.AddComponent<SceneController>();

            var transitionObject = new GameObject("Transition");
            transitionObject.transform.SetParent(controllerObject.transform, false);
            var transition = transitionObject.AddComponent<CanvasGroupFadeTransition>();

            var controllerSerialized = new SerializedObject(controller);
            controllerSerialized.FindProperty("settings").objectReferenceValue = settings;
            controllerSerialized.FindProperty("transitionSource").objectReferenceValue = transition;
            controllerSerialized.ApplyModifiedPropertiesWithoutUndo();

            new GameObject("ResourceManager").AddComponent<ResourceManager>();
            new GameObject("PoolManager").AddComponent<PoolManager>();
            var audioObject = new GameObject("AudioManager");
            audioObject.AddComponent<AudioManager>();
            audioObject.AddComponent<AudioListener>();
            new GameObject("InputManager").AddComponent<InputManager>();

            var bootstrap = new GameObject("Bootstrap").AddComponent<GameBootstrap>();
            var bootstrapSerialized = new SerializedObject(bootstrap);
            bootstrapSerialized.FindProperty("inputActions").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            bootstrapSerialized.FindProperty("audioManifest").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TextAsset>(ManifestPath);
            bootstrapSerialized.FindProperty("configAsset").objectReferenceValue = context.Config;
            var dialogueCsv = AssetDatabase.LoadAssetAtPath<TextAsset>(DialogueCsvPath);
            if (dialogueCsv == null)
                Debug.LogWarning($"[CoreLoopSetup] '{DialogueCsvPath}' is missing. Run 'Team1004/Import Dialogue CSV'.");
            bootstrapSerialized.FindProperty("dialogueCsv").objectReferenceValue = dialogueCsv;
            bootstrapSerialized.FindProperty("firstScene").objectReferenceValue = firstScene;
            bootstrapSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
        }

        private static JumpCooldownView CreateJumpCooldownView(Context context, Transform parent)
        {
            var background = CreateImage(context, parent, "JumpCooldown", context.Circle, JumpRingBackgroundColor);
            Place(background.rectTransform, BottomLeft, new Vector2(24f, 24f), new Vector2(96f, 96f));
            background.raycastTarget = false;

            var gauge = CreateImage(context, background.transform, "Gauge", context.Circle, BarFillColor);
            Stretch(gauge.rectTransform, new Vector2(4f, 4f), new Vector2(-4f, -4f));
            gauge.raycastTarget = false;
            gauge.type = Image.Type.Filled;
            gauge.fillMethod = Image.FillMethod.Radial360;
            gauge.fillOrigin = (int)Image.Origin360.Top;
            gauge.fillClockwise = true;
            gauge.fillAmount = 1f;

            var icon = CreateImage(context, background.transform, "Icon", context.Circle, Color.white);
            Place(icon.rectTransform, Center, Vector2.zero, new Vector2(36f, 36f));
            icon.raycastTarget = false;

            var view = background.gameObject.AddComponent<JumpCooldownView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("gauge").objectReferenceValue = gauge;
            serialized.FindProperty("icon").objectReferenceValue = icon;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static BossTimerView CreateBossTimerView(Context context, Transform parent)
        {
            var root = CreateRect("BossTimer", parent);
            Place(root, TopCenter, new Vector2(0f, -56f), new Vector2(360f, 110f));

            var name = CreateText(context, root, "Name", "낚싯줄", 24, Color.white, TextAnchor.MiddleCenter,
                TopCenter, Vector2.zero, new Vector2(360f, 32f));
            var text = CreateText(context, root, "Timer", "30", 56, Color.white, TextAnchor.MiddleCenter,
                TopCenter, new Vector2(0f, -34f), new Vector2(360f, 70f));

            var view = root.gameObject.AddComponent<BossTimerView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("nameText").objectReferenceValue = name;
            serialized.FindProperty("timerText").objectReferenceValue = text;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            root.gameObject.SetActive(false);
            return view;
        }

        private static PausePanel CreatePausePanel(Context context, Transform parent)
        {
            var root = CreateRect("PausePanel", parent);
            var dim = root.gameObject.AddComponent<Image>();
            dim.sprite = context.Square;
            dim.color = DimColor;
            dim.raycastTarget = true;
            Stretch(root, Vector2.zero, Vector2.zero);
            var panel = root.gameObject.AddComponent<PausePanel>();

            var window = CreateImage(context, root, "Window", context.UiSprite, WindowColor);
            Place(window.rectTransform, Center, Vector2.zero, new Vector2(420f, 360f));

            CreateText(context, window.transform, "Title", "일시정지", 40, Color.white, TextAnchor.MiddleCenter,
                Center, new Vector2(0f, 120f), new Vector2(360f, 60f));

            var resumeButton = CreateButton(context, window.transform, "ResumeButton", "계속",
                Center, new Vector2(0f, 30f), new Vector2(280f, 56f));
            var settingsButton = CreateButton(context, window.transform, "SettingsButton", "설정",
                Center, new Vector2(0f, -40f), new Vector2(280f, 56f));
            var titleButton = CreateButton(context, window.transform, "TitleButton", "타이틀로",
                Center, new Vector2(0f, -110f), new Vector2(280f, 56f));

            var serialized = new SerializedObject(panel);
            serialized.FindProperty("resumeButton").objectReferenceValue = resumeButton;
            serialized.FindProperty("settingsButton").objectReferenceValue = settingsButton;
            serialized.FindProperty("titleButton").objectReferenceValue = titleButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            root.gameObject.SetActive(false);
            return panel;
        }

        private static ResultPanel CreateResultPanel(Context context, Transform parent)
        {
            var root = CreateRect("ResultPanel", parent);
            var dim = root.gameObject.AddComponent<Image>();
            dim.sprite = context.Square;
            dim.color = DimColor;
            dim.raycastTarget = true;
            Stretch(root, Vector2.zero, Vector2.zero);
            var panel = root.gameObject.AddComponent<ResultPanel>();

            var window = CreateImage(context, root, "Window", context.UiSprite, WindowColor);
            Place(window.rectTransform, Center, Vector2.zero, new Vector2(460f, 320f));

            var title = CreateText(context, window.transform, "Title", "결과", 40, Color.white,
                TextAnchor.MiddleCenter, Center, new Vector2(0f, 90f), new Vector2(420f, 60f));

            var retryButton = CreateButton(context, window.transform, "RetryButton", "다시하기",
                Center, new Vector2(0f, 0f), new Vector2(280f, 56f));
            var titleButton = CreateButton(context, window.transform, "TitleButton", "타이틀로",
                Center, new Vector2(0f, -70f), new Vector2(280f, 56f));

            var serialized = new SerializedObject(panel);
            serialized.FindProperty("titleText").objectReferenceValue = title;
            serialized.FindProperty("retryButton").objectReferenceValue = retryButton;
            serialized.FindProperty("titleButton").objectReferenceValue = titleButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            root.gameObject.SetActive(false);
            return panel;
        }

        private static SettingsPanel InstantiateSettingsPanel(
            GameObject prefab,
            Transform parent,
            SceneReference titleScene)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(parent, false);
            instance.SetActive(false);

            var panel = instance.GetComponent<SettingsPanel>();
            if (titleScene != null)
            {
                var serialized = new SerializedObject(panel);
                serialized.FindProperty("titleScene").objectReferenceValue = titleScene;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            return panel;
        }

        private static EnvironmentThing InstantiateEnvironment()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnvironmentPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[CoreLoopSetup] '{EnvironmentPrefabPath}' is missing. The play scene has no background until it exists.");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(null, false);
            instance.transform.localPosition = Vector3.zero;

            var environment = instance.GetComponent<EnvironmentThing>();
            if (environment == null)
                Debug.LogWarning("[CoreLoopSetup] Environment.prefab has no EnvironmentThing on its root.");

            return environment;
        }

        private static SpriteRenderer CreatePlayerRenderer(
            Context context,
            GameConfigValues config,
            out Vector2 colliderSize,
            out Vector2 colliderOffset)
        {
            var position = new Vector3(config.PlayerX, config.GetLaneY(1), 0f);
            var artPath = AnimationAssetGenerator.DefaultPlayerSpritePath;
            var artSprite = AssetDatabase.LoadAssetAtPath<Sprite>(artPath);

            if (artSprite != null && AnimationAssetGenerator.TryGetSalmonBounds(out var pixelBounds, out _))
            {
                var playerObject = new GameObject("Player");
                playerObject.transform.localPosition = position;
                playerObject.transform.localScale = new Vector3(config.PlayerScale, config.PlayerScale, 1f);

                var renderer = playerObject.AddComponent<SpriteRenderer>();
                renderer.sprite = artSprite;
                renderer.color = Color.white;
                renderer.sortingOrder = PlayerSortingOrder;

                var ppu = artSprite.pixelsPerUnit > 0f ? artSprite.pixelsPerUnit : PixelsPerUnit;
                colliderSize = new Vector2(pixelBounds.width, pixelBounds.height) / ppu * config.PlayerHitboxScale;
                colliderOffset = (pixelBounds.center - artSprite.pivot) / ppu;
                return renderer;
            }

            Debug.LogWarning($"[CoreLoopSetup] '{artPath}' is missing. The player uses the placeholder square.");
            colliderSize = new Vector2(SpriteUnits, SpriteUnits);
            colliderOffset = Vector2.zero;
            return CreateSpriteObject("Player", null, context.Square, PlayerColor, position, new Vector2(0.9f, 0.5f), PlayerSortingOrder);
        }

        private static void WireCutsceneClips(
            CutscenePlayer cutscenePlayer,
            CustomAnimation jumpClip,
            CustomAnimation swimClip,
            CustomAnimation laneUpClip,
            CustomAnimation laneDownClip)
        {
            var serialized = new SerializedObject(cutscenePlayer);
            var clips = serialized.FindProperty("clips");
            clips.ClearArray();

            AddClipEntry(clips, CutsceneClipIds.PlayerJump, jumpClip);
            AddClipEntry(clips, CutsceneClipIds.PlayerSwim, swimClip);
            AddClipEntry(clips, CutsceneClipIds.PlayerLaneUp, laneUpClip);
            AddClipEntry(clips, CutsceneClipIds.PlayerLaneDown, laneDownClip);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddClipEntry(SerializedProperty clips, string id, CustomAnimation clip)
        {
            var index = clips.arraySize;
            clips.InsertArrayElementAtIndex(index);
            var entry = clips.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("id").stringValue = id;
            entry.FindPropertyRelative("clip").objectReferenceValue = clip;
        }

        private static void InstantiateBossSet(Camera camera)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossSetPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[CoreLoopSetup] '{BossSetPrefabPath}' is missing. Boss steps will be skipped until it exists.");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(null, false);
            instance.transform.localPosition = Vector3.zero;

            var director = instance.GetComponent<BossDirector>();
            if (director == null)
                return;

            var serialized = new SerializedObject(director);
            serialized.FindProperty("stageCamera").objectReferenceValue = camera;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetupActor(CutsceneActor actor, string id, SpriteRenderer renderer)
        {
            var serialized = new SerializedObject(actor);
            serialized.FindProperty("id").stringValue = id;
            serialized.FindProperty("spriteRenderer").objectReferenceValue = renderer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static CutscenePlayer CreateCutscenePlayer(
            Camera camera,
            CutsceneActor playerActor,
            CutsceneActor bossActor,
            CutsceneActor landmarkActor)
        {
            CutsceneDialogueView dialogueView = null;
            var dialoguePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CutsceneDialoguePrefabPath);
            if (dialoguePrefab != null)
            {
                var dialogueInstance = (GameObject)PrefabUtility.InstantiatePrefab(dialoguePrefab);
                dialogueView = dialogueInstance.GetComponent<CutsceneDialogueView>();

                var dialogueCanvas = dialogueInstance.GetComponent<Canvas>();
                if (dialogueCanvas != null)
                    dialogueCanvas.worldCamera = camera;
            }
            else
            {
                Debug.LogError(
                    $"[CoreLoopSetup] '{CutsceneDialoguePrefabPath}' is missing. Run 'Team1004/Generate Cutscene Assets' " +
                    "and then 'Regenerate Core Loop Scenes (Overwrite)'.");
            }

            var playerObject = new GameObject("CutscenePlayer");
            var cutscenePlayer = playerObject.AddComponent<CutscenePlayer>();

            var serialized = new SerializedObject(cutscenePlayer);
            serialized.FindProperty("stageCamera").objectReferenceValue = camera;
            serialized.FindProperty("dialogueView").objectReferenceValue = dialogueView;
            var actors = serialized.FindProperty("actors");
            actors.ClearArray();
            actors.InsertArrayElementAtIndex(0);
            actors.GetArrayElementAtIndex(0).objectReferenceValue = playerActor;
            actors.InsertArrayElementAtIndex(1);
            actors.GetArrayElementAtIndex(1).objectReferenceValue = bossActor;
            actors.InsertArrayElementAtIndex(2);
            actors.GetArrayElementAtIndex(2).objectReferenceValue = landmarkActor;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return cutscenePlayer;
        }

        private static Camera CreateCamera()
        {
            var letterboxObject = new GameObject("Letterbox Camera");
            letterboxObject.transform.position = new Vector3(0f, 0f, -10f);
            var letterbox = letterboxObject.AddComponent<Camera>();
            letterbox.orthographic = true;
            letterbox.orthographicSize = CameraSize;
            letterbox.clearFlags = CameraClearFlags.SolidColor;
            letterbox.backgroundColor = Color.black;
            letterbox.cullingMask = 0;
            letterbox.depth = -100f;
            letterbox.nearClipPlane = 0.3f;
            letterbox.farClipPlane = 100f;
            letterbox.GetUniversalAdditionalCameraData();

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = MainCameraTag;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = CameraSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = CameraBackground;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 100f;
            camera.depth = 0f;
            camera.GetUniversalAdditionalCameraData();
            cameraObject.AddComponent<GameCamera>();
            return camera;
        }

        private static void CreateGlobalLight()
        {
            var lightObject = new GameObject("Global Light 2D");
            var light = lightObject.AddComponent<Light2D>();

            var serialized = new SerializedObject(light);
            serialized.FindProperty("m_LightType").intValue = (int)Light2D.LightType.Global;
            serialized.FindProperty("m_Intensity").floatValue = 1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var layers = SortingLayer.layers;
            var ids = new int[layers.Length];
            for (var i = 0; i < layers.Length; i++)
                ids[i] = layers[i].id;

            light.targetSortingLayers = ids;
        }

        private static void CreateEventSystem()
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        private static Canvas CreateCanvas(string name, Camera camera, int sortingOrder = 0)
        {
            var canvasObject = new GameObject(name, typeof(RectTransform));
            canvasObject.layer = UiLayer;

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = CanvasPlaneDistance;
            canvas.sortingOrder = sortingOrder;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static SpriteRenderer CreateSpriteObject(
            string name,
            Transform parent,
            Sprite sprite,
            Color color,
            Vector3 position,
            Vector2 size,
            int sortingOrder)
        {
            var spriteObject = new GameObject(name);
            spriteObject.transform.SetParent(parent, false);
            spriteObject.transform.localPosition = position;
            spriteObject.transform.localScale = new Vector3(size.x / SpriteUnits, size.y / SpriteUnits, 1f);

            var renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var rectObject = new GameObject(name, typeof(RectTransform));
            rectObject.layer = UiLayer;

            var rect = (RectTransform)rectObject.transform;
            if (parent != null)
                rect.SetParent(parent, false);

            return rect;
        }

        private static Image CreateImage(Context context, Transform parent, string name, Sprite sprite, Color color)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = sprite == context.UiSprite && sprite != context.Square ? Image.Type.Sliced : Image.Type.Simple;
            return image;
        }

        private static Text CreateText(
            Context context,
            Transform parent,
            string name,
            string content,
            int fontSize,
            Color color,
            TextAnchor alignment,
            Vector2 anchor,
            Vector2 position,
            Vector2 size)
        {
            var rect = CreateRect(name, parent);
            Place(rect, anchor, position, size);

            var text = rect.gameObject.AddComponent<Text>();
            text.font = context.Font;
            text.fontSize = Mathf.RoundToInt(fontSize * UiScale);
            text.text = content;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(
            Context context,
            Transform parent,
            string name,
            string label,
            Vector2 anchor,
            Vector2 position,
            Vector2 size)
        {
            var image = CreateImage(context, parent, name, context.UiSprite, ButtonColor);
            Place(image.rectTransform, anchor, position, size);

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var text = CreateText(context, image.transform, "Label", label, 24, ButtonTextColor,
                TextAnchor.MiddleCenter, Center, Vector2.zero, size);
            Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
            return button;
        }

        private static Slider CreateLabeledSlider(Context context, Transform parent, string name, string label, float y)
        {
            CreateText(context, parent, name + "Label", label, 24, Color.white, TextAnchor.MiddleLeft,
                Center, new Vector2(-180f, y), new Vector2(200f, 36f));

            return CreateSlider(context, parent, name + "Slider", new Vector2(100f, y), new Vector2(320f, 24f));
        }

        private static Slider CreateSlider(Context context, Transform parent, string name, Vector2 position, Vector2 size)
        {
            var root = CreateRect(name, parent);
            Place(root, Center, position, size);
            var slider = root.gameObject.AddComponent<Slider>();

            var background = CreateImage(context, root, "Background", context.UiSprite, SliderTrackColor);
            Stretch(background.rectTransform, Vector2.zero, Vector2.zero);

            var fillArea = CreateRect("Fill Area", root);
            Stretch(fillArea, new Vector2(5f, 0f), new Vector2(-5f, 0f));
            var fill = CreateImage(context, fillArea, "Fill", context.UiSprite, BarFillColor);
            Stretch(fill.rectTransform, Vector2.zero, Vector2.zero);

            var handleArea = CreateRect("Handle Slide Area", root);
            Stretch(handleArea, new Vector2(10f, 0f), new Vector2(-10f, 0f));
            var handle = CreateImage(context, handleArea, "Handle", context.UiSprite, Color.white);
            handle.rectTransform.anchorMin = new Vector2(0f, 0f);
            handle.rectTransform.anchorMax = new Vector2(0f, 1f);
            handle.rectTransform.pivot = Center;
            handle.rectTransform.anchoredPosition = Vector2.zero;
            handle.rectTransform.sizeDelta = new Vector2(20f * UiScale, 0f);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.value = 1f;
            return slider;
        }

        private static InputField CreateInputField(
            Context context,
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 position,
            Vector2 size)
        {
            var background = CreateImage(context, parent, name, context.UiSprite, FieldColor);
            Place(background.rectTransform, anchor, position, size);

            var placeholder = CreateText(context, background.transform, "Placeholder", "{ }", 18,
                new Color(0.5f, 0.5f, 0.5f, 1f), TextAnchor.UpperLeft, Center, Vector2.zero, size);
            placeholder.fontStyle = FontStyle.Italic;
            Stretch(placeholder.rectTransform, new Vector2(10f, 8f), new Vector2(-10f, -8f));

            var text = CreateText(context, background.transform, "Text", string.Empty, 18, Color.black,
                TextAnchor.UpperLeft, Center, Vector2.zero, size);
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            Stretch(text.rectTransform, new Vector2(10f, 8f), new Vector2(-10f, -8f));

            var field = background.gameObject.AddComponent<InputField>();
            field.targetGraphic = background;
            field.textComponent = text;
            field.placeholder = placeholder;
            field.lineType = InputField.LineType.MultiLineNewline;
            field.characterLimit = 0;
            return field;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position * UiScale;
            rect.sizeDelta = size * UiScale;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = Center;
            rect.offsetMin = offsetMin * UiScale;
            rect.offsetMax = offsetMax * UiScale;
        }
    }
}

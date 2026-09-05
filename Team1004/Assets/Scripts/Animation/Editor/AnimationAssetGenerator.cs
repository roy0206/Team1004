using System;
using System.Collections.Generic;
using System.IO;
using Game.Config;
using UnityEditor;
using UnityEngine;

namespace Game.Animation.Editor
{
    public static class AnimationAssetGenerator
    {
        public const string DesignFolder = "Assets/GameAssets/Design/Animations";
        public const string ArtFolder = "Assets/GameAssets/Art/물고기 애니메이팅";
        public const string GameConfigPath = "Assets/GameAssets/Design/GameConfig.asset";
        public const string LegacyIdleClipPath = DesignFolder + "/Player_Idle.asset";
        public const float PixelsPerUnit = 100f;

        private const int MaxTextureSize = 2048;
        private const byte AlphaThreshold = 8;
        private const double PivotCacheSeconds = 30d;

        private static readonly Vector2 FallbackPivot = new(0.5309896f, 0.37592593f);

        private static Vector2 cachedPivot = FallbackPivot;
        private static double cachedPivotTime = double.NegativeInfinity;

        public static string SwimClipPath => SalmonClipTable.Swim.AssetPath;
        public static string LaneUpClipPath => SalmonClipTable.LaneUp.AssetPath;
        public static string LaneDownClipPath => SalmonClipTable.LaneDown.AssetPath;
        public static string JumpClipPath => SalmonClipTable.Jump.AssetPath;
        public static string DefaultPlayerSpritePath => GetFramePath(SalmonClipTable.Swim, 0);

        [MenuItem("Team1004/Generate Animation Assets")]
        public static void GenerateMissing()
        {
            Generate(false);
        }

        [MenuItem("Team1004/Regenerate Animation Assets (Overwrite)")]
        public static void RegenerateAll()
        {
            if (!EditorUtility.DisplayDialog(
                    "Regenerate Animation Assets",
                    "Design/Animations의 CustomAnimation 에셋을 기본값으로 다시 씁니다. 손으로 고친 값이 사라집니다. 계속할까요?",
                    "덮어쓰기",
                    "취소"))
                return;

            Generate(true);
        }

        public static void RegenerateAllBatch()
        {
            Generate(true);
        }

        public static void Generate()
        {
            Generate(false);
        }

        public static void Generate(bool overwrite)
        {
            EnsureFolder(DesignFolder);

            var pivot = ResolveSharedPivot();
            cachedPivot = pivot;
            cachedPivotTime = EditorApplication.timeSinceStartup;

            var framePaths = EnumerateFramePaths();

            for (var i = 0; i < framePaths.Count; i++)
                ConfigureSpriteImporter(framePaths[i], pivot);

            AssetDatabase.Refresh();
            DeleteLegacyAssets();

            var config = LoadConfigValues();
            var definitions = SalmonClipTable.All;

            for (var i = 0; i < definitions.Count; i++)
                EnsureClip(definitions[i], config, overwrite);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[AnimationAssetGenerator] Animation assets are ready. pivot=" + pivot +
                      ", frames=" + framePaths.Count + ", folder=" + DesignFolder);
        }

        public static string GetFramePath(SalmonClipDefinition definition, int index)
        {
            if (definition == null || index < 0 || index >= definition.FrameCount)
                return null;

            var folder = definition.FolderPath;
            var names = ListPngFileNames(folder);
            var match = SalmonFrameMatcher.FindFrame(names, definition.FramePrefix, index + 1);

            if (match != null)
                return folder + "/" + match;

            return folder + "/" + definition.FramePrefix + "-" + (index + 1) + SalmonFrameMatcher.Extension;
        }

        public static IReadOnlyList<string> GetFramePaths(SalmonClipDefinition definition)
        {
            if (definition == null)
                return Array.Empty<string>();

            var paths = new string[definition.FrameCount];

            for (var i = 0; i < definition.FrameCount; i++)
                paths[i] = GetFramePath(definition, i);

            return paths;
        }

        public static IReadOnlyList<string> EnumerateFramePaths()
        {
            var paths = new List<string>(SalmonClipTable.TotalFrameCount);
            var definitions = SalmonClipTable.All;

            for (var i = 0; i < definitions.Count; i++)
            {
                var frames = GetFramePaths(definitions[i]);

                for (var frame = 0; frame < frames.Count; frame++)
                {
                    if (!paths.Contains(frames[frame]))
                        paths.Add(frames[frame]);
                }
            }

            return paths;
        }

        public static bool IsSalmonFramePath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;

            var normalized = assetPath.Replace('\\', '/');
            return normalized.StartsWith(ArtFolder + "/", StringComparison.Ordinal) &&
                   normalized.EndsWith(SalmonFrameMatcher.Extension, StringComparison.OrdinalIgnoreCase);
        }

        public static Vector2 ResolveSharedPivot()
        {
            if (!TryGetSalmonBounds(out var bounds, out var textureSize))
            {
                Debug.LogWarning("[AnimationAssetGenerator] 알파 바운딩 박스를 구하지 못해 pivot을 기본값 " + FallbackPivot + "로 둔다.");
                return FallbackPivot;
            }

            return new Vector2(bounds.center.x / textureSize.x, bounds.center.y / textureSize.y);
        }

        public static Vector2 ResolveSharedPivotCached()
        {
            var now = EditorApplication.timeSinceStartup;

            if (now - cachedPivotTime <= PivotCacheSeconds)
                return cachedPivot;

            cachedPivot = ResolveSharedPivot();
            cachedPivotTime = now;
            return cachedPivot;
        }

        public static bool TryGetSalmonBounds(out Rect pixelBounds, out Vector2Int textureSize)
        {
            var framePaths = EnumerateFramePaths();
            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var maxX = int.MinValue;
            var maxY = int.MinValue;
            var width = 0;
            var height = 0;

            for (var i = 0; i < framePaths.Count; i++)
            {
                if (!TryReadPixels(framePaths[i], out var pixels, out var w, out var h))
                    continue;

                width = w;
                height = h;

                for (var y = 0; y < h; y++)
                {
                    var row = y * w;

                    for (var x = 0; x < w; x++)
                    {
                        if (pixels[row + x].a < AlphaThreshold)
                            continue;

                        if (x < minX)
                            minX = x;

                        if (x > maxX)
                            maxX = x;

                        if (y < minY)
                            minY = y;

                        if (y > maxY)
                            maxY = y;
                    }
                }
            }

            if (width <= 0 || height <= 0 || maxX < minX || maxY < minY)
            {
                pixelBounds = default;
                textureSize = default;
                return false;
            }

            pixelBounds = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
            textureSize = new Vector2Int(width, height);
            return true;
        }

        public static void ApplyImportSettings(TextureImporter importer, Vector2 pivot)
        {
            if (importer == null)
                return;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);

            settings.textureType = TextureImporterType.Sprite;
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spritePixelsPerUnit = PixelsPerUnit;
            settings.spriteMeshType = SpriteMeshType.Tight;
            settings.spriteGenerateFallbackPhysicsShape = false;
            settings.alphaIsTransparency = true;
            settings.mipmapEnabled = false;
            settings.readable = false;
            settings.filterMode = FilterMode.Bilinear;
            settings.wrapMode = TextureWrapMode.Clamp;

            importer.SetTextureSettings(settings);
            importer.maxTextureSize = MaxTextureSize;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
        }

        public static float ResolveDuration(SalmonClipDefinition definition, GameConfigValues config)
        {
            if (definition == null)
                return 0f;

            var values = config ?? new GameConfigValues();

            return definition.DurationSource switch
            {
                SalmonClipDuration.LaneMove => Mathf.Max(0f, values.LaneMoveDuration),
                SalmonClipDuration.Jump => Mathf.Max(0f, values.JumpDuration),
                _ => 0f
            };
        }

        public static GameConfigValues LoadConfigValues()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameConfigAsset>(GameConfigPath);
            return asset != null && asset.Values != null ? asset.Values : new GameConfigValues();
        }

        private static IReadOnlyList<string> ListPngFileNames(string assetFolder)
        {
            var fullPath = ToFullPath(assetFolder);

            if (!Directory.Exists(fullPath))
                return Array.Empty<string>();

            var files = Directory.GetFiles(fullPath);
            var names = new List<string>(files.Length);

            for (var i = 0; i < files.Length; i++)
            {
                var name = Path.GetFileName(files[i]);

                if (name.EndsWith(SalmonFrameMatcher.Extension, StringComparison.OrdinalIgnoreCase))
                    names.Add(name);
            }

            names.Sort(StringComparer.Ordinal);
            return names;
        }

        private static string ToFullPath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        }

        private static bool TryReadPixels(string assetPath, out Color32[] pixels, out int width, out int height)
        {
            pixels = null;
            width = 0;
            height = 0;

            var fullPath = ToFullPath(assetPath);

            if (!File.Exists(fullPath))
            {
                Debug.LogWarning("[AnimationAssetGenerator] 파일이 없다: " + assetPath);
                return false;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(fullPath), false))
                {
                    Debug.LogWarning("[AnimationAssetGenerator] PNG를 읽지 못했다: " + assetPath);
                    return false;
                }

                pixels = texture.GetPixels32();
                width = texture.width;
                height = texture.height;
                return true;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void ConfigureSpriteImporter(string assetPath, Vector2 pivot)
        {
            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
            {
                Debug.LogWarning("[AnimationAssetGenerator] TextureImporter가 없다: " + assetPath);
                return;
            }

            ApplyImportSettings(importer, pivot);
            importer.SaveAndReimport();
        }

        private static Sprite[] LoadFrames(SalmonClipDefinition definition)
        {
            var paths = GetFramePaths(definition);
            var frames = new Sprite[paths.Count];
            var missing = 0;

            for (var i = 0; i < paths.Count; i++)
            {
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(paths[i]);

                if (frames[i] == null)
                    missing++;
            }

            if (missing > 0)
                Debug.LogWarning("[AnimationAssetGenerator] " + definition.AssetName + " 스프라이트 " + missing +
                                 "장을 찾지 못했다. 경로: " + definition.FolderPath);

            return frames;
        }

        private static void EnsureClip(SalmonClipDefinition definition, GameConfigValues config, bool overwrite)
        {
            var path = definition.AssetPath;
            var asset = AssetDatabase.LoadAssetAtPath<CustomAnimation>(path);
            var create = asset == null;

            if (!create && !overwrite && !NeedsRepair(asset, definition))
                return;

            var frames = LoadFrames(definition);
            var duration = ResolveDuration(definition, config);

            if (create)
                asset = ScriptableObject.CreateInstance<CustomAnimation>();

            asset.EditorInitialize(
                frames,
                definition.FramesPerSecond,
                definition.Loop,
                duration,
                Array.Empty<CustomAnimationEvent>());

            if (create)
                AssetDatabase.CreateAsset(asset, path);
            else
                EditorUtility.SetDirty(asset);

            Report(path, asset);
        }

        private static bool NeedsRepair(CustomAnimation asset, SalmonClipDefinition definition)
        {
            if (asset.FrameCount != definition.FrameCount)
                return true;

            for (var i = 0; i < definition.FrameCount; i++)
            {
                if (asset.GetSprite(i) == null)
                    return true;
            }

            return false;
        }

        private static void DeleteLegacyAssets()
        {
            if (AssetDatabase.LoadAssetAtPath<CustomAnimation>(LegacyIdleClipPath) == null)
                return;

            AssetDatabase.DeleteAsset(LegacyIdleClipPath);
            Debug.Log("[AnimationAssetGenerator] " + LegacyIdleClipPath + "을(를) 지웠다. Swim 0프레임이 정지 포즈를 대신한다.");
        }

        private static void Report(string path, CustomAnimation asset)
        {
            if (asset.Validate(out var error))
                return;

            Debug.LogWarning("[AnimationAssetGenerator] " + path + ": " + error);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parts = path.Split('/');
            var current = parts[0];

            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];

                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);

                current = next;
            }
        }
    }
}

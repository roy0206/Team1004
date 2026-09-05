using System;
using System.Collections.Generic;
using System.IO;
using Game.Animation;
using Game.Animation.Editor;
using Game.Tools.Editor;
using UnityEditor;
using UnityEngine;

namespace Game.Spawner.Editor
{
    public static class SpawnerAssetGenerator
    {
        private static readonly string[] RockArtPaths =
        {
            ObjectArtPostprocessor.ArtFolder + "/돌1.png",
            ObjectArtPostprocessor.ArtFolder + "/돌 2.png"
        };

        private static readonly string[] LogArtPaths =
        {
            ObjectArtPostprocessor.ArtFolder + "/통나무.png"
        };

        private const string PlaceholderFolder = "Assets/GameAssets/Placeholder";
        private const string ObstacleFolder = "Assets/GameAssets/Obstacles";
        private const string DesignFolder = "Assets/GameAssets/Design/Spawner";
        private const string ObstacleDefinitionFolder = DesignFolder + "/Obstacles";
        private const string PatternFolder = DesignFolder + "/Patterns";
        private const string LibraryPath = DesignFolder + "/PatternLibrary.asset";
        private const string CurvePath = DesignFolder + "/DifficultyCurve.asset";
        private const string SettingsPath = DesignFolder + "/ObstacleSpawnerSettings.asset";
        private const float LaneWidth = 1.1f;
        private const float VisualHeightInset = 0.1f;
        private const float ColliderHeightInset = 0.3f;

        [MenuItem("Team1004/Generate Spawner Assets")]
        public static void GenerateMissing()
        {
            if (GeneratorFreeze.Block(nameof(SpawnerAssetGenerator)))
                return;

            Generate(false);
        }

        [MenuItem("Team1004/Regenerate Spawner Assets (Overwrite)")]
        public static void RegenerateAll()
        {
            if (GeneratorFreeze.Block(nameof(SpawnerAssetGenerator)))
                return;

            if (!EditorUtility.DisplayDialog(
                    "Regenerate Spawner Assets",
                    "장애물 프리팹과 Design/Spawner 에셋을 기본값으로 다시 씁니다. 손으로 고친 값이 사라집니다. 계속할까요?",
                    "덮어쓰기",
                    "취소"))
                return;

            Generate(true);
        }

        public static void RegenerateAllBatch()
        {
            if (GeneratorFreeze.Block(nameof(SpawnerAssetGenerator)))
                return;

            Generate(true);
        }

        public static void Generate(bool overwrite)
        {
            if (GeneratorFreeze.Block(nameof(SpawnerAssetGenerator)))
                return;

            EnsureFolder(PlaceholderFolder);
            EnsureFolder(ObstacleFolder);
            EnsureFolder(DesignFolder);
            EnsureFolder(ObstacleDefinitionFolder);
            EnsureFolder(PatternFolder);

            EnsureObjectArt();
            EnsureObstacleClips();

            var square = EnsureSprite("Square");
            var obstacleSpecs = SpawnerDefaults.CreateObstacles();
            var definitions = new Dictionary<string, ObstacleDefinition>(StringComparer.Ordinal);
            var patternSpecs = SpawnerDefaults.CreatePatterns(obstacleSpecs);

            if (overwrite)
                DeleteStaleAssets(obstacleSpecs, patternSpecs);

            for (var i = 0; i < obstacleSpecs.Count; i++)
            {
                var spec = obstacleSpecs[i];
                var prefab = EnsurePrefab(spec, square, overwrite);
                var definition = EnsureAsset<ObstacleDefinition>(
                    ObstacleDefinitionFolder + "/" + spec.Id + ".asset",
                    overwrite,
                    asset => asset.EditorInitialize(spec, prefab));

                definitions[spec.Id] = definition;
            }

            var patterns = new List<SpawnPattern>(patternSpecs.Count);

            for (var i = 0; i < patternSpecs.Count; i++)
            {
                var spec = patternSpecs[i];
                var entries = new List<SpawnPatternEntry>(spec.Entries.Count);

                for (var j = 0; j < spec.Entries.Count; j++)
                {
                    var entry = spec.Entries[j];
                    entries.Add(new SpawnPatternEntry(definitions[entry.Obstacle.Id], (Lane)entry.StartLane, entry.ArrivalOffset));
                }

                patterns.Add(EnsureAsset<SpawnPattern>(
                    PatternFolder + "/" + spec.Id + ".asset",
                    overwrite,
                    asset => asset.EditorInitialize(spec.Id, entries, spec.ManualJumpRequired)));
            }

            var library = EnsureAsset<PatternLibrary>(LibraryPath, overwrite, asset => asset.EditorInitialize(new List<SpawnPattern>(patterns)));

            var curve = EnsureAsset<DifficultyCurve>(CurvePath, overwrite, asset =>
            {
                var sections = new SectionDifficulty[SpawnerDefaults.SectionCount];
                for (var i = 0; i < sections.Length; i++)
                {
                    SpawnerDefaults.GetSectionEndpoints(i, out var start, out var end, out var stop);
                    sections[i] = new SectionDifficulty(start, end, stop);
                }

                asset.EditorInitialize(sections);
            });

            var settings = EnsureAsset<ObstacleSpawnerSettings>(SettingsPath, overwrite, asset => asset.EditorInitialize(library, curve));

            if (settings.Library == null || settings.Difficulty == null)
            {
                settings.EditorInitialize(settings.Library != null ? settings.Library : library, settings.Difficulty != null ? settings.Difficulty : curve);
                EditorUtility.SetDirty(settings);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SpawnerAssetGenerator] Spawner assets are ready. settings=" + SettingsPath + ", patterns=" + patterns.Count);
        }

        private static void DeleteStaleAssets(List<ObstacleSpec> obstacleSpecs, List<PatternSpec> patternSpecs)
        {
            var obstacleIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < obstacleSpecs.Count; i++)
                obstacleIds.Add(obstacleSpecs[i].Id);

            var patternIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < patternSpecs.Count; i++)
                patternIds.Add(patternSpecs[i].Id);

            DeleteAssetsNotIn(PatternFolder, "t:" + nameof(SpawnPattern), patternIds);

            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(ObstacleDefinition), new[] { ObstacleDefinitionFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = Path.GetFileNameWithoutExtension(path);
                if (obstacleIds.Contains(name))
                    continue;

                AssetDatabase.DeleteAsset(path);
                var prefabPath = ObstacleFolder + "/" + name + ".prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                    AssetDatabase.DeleteAsset(prefabPath);

                Debug.Log("[SpawnerAssetGenerator] Removed obstacle no longer in defaults: " + name);
            }
        }

        private static void DeleteAssetsNotIn(string folder, string filter, HashSet<string> keep)
        {
            foreach (var guid in AssetDatabase.FindAssets(filter, new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = Path.GetFileNameWithoutExtension(path);
                if (keep.Contains(name))
                    continue;

                AssetDatabase.DeleteAsset(path);
                Debug.Log("[SpawnerAssetGenerator] Removed pattern no longer in defaults: " + name);
            }
        }

        private static T EnsureAsset<T>(string path, bool overwrite, Action<T> initialize) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                initialize(asset);
                AssetDatabase.CreateAsset(asset, path);
                return asset;
            }

            if (overwrite)
            {
                initialize(asset);
                EditorUtility.SetDirty(asset);
            }

            return asset;
        }

        private static GameObject EnsurePrefab(ObstacleSpec spec, Sprite square, bool overwrite)
        {
            var path = ObstacleFolder + "/" + spec.Id + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (existing != null && !overwrite)
                return existing;

            var root = new GameObject(spec.Id);

            try
            {
                var height = spec.LaneSpan * LaneWidth;
                var visualHeight = Mathf.Max(0.2f, height - VisualHeightInset);
                var colliderHeight = Mathf.Max(0.1f, height - ColliderHeightInset);

                var renderer = root.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = 5;

                var collider = root.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
                collider.offset = Vector2.zero;

                var artPaths = ArtPathsFor(spec.Id);
                var artSprites = LoadSprites(artPaths);
                var variants = LoadSprites(VariantPathsFor(spec.Id));

                if (artSprites.Length > 0 && TryArtSizePixels(spec.Id, out var alphaSize))
                {
                    var scale = ObstacleVisual.UniformScale(spec, alphaSize.x, alphaSize.y);
                    var artSize = ObstacleVisual.VisualWorldSize(spec, alphaSize.x, alphaSize.y);
                    var artHeight = artSize.y;
                    var heightBudget = spec.FitAxis == ObstacleFitAxis.Height ? height : visualHeight;

                    renderer.sprite = artSprites[0];
                    renderer.color = Color.white;
                    root.transform.localScale = new Vector3(scale, scale, 1f);
                    var artCollisionHeight = artHeight * ObstacleVisual.HitboxScale;
                    collider.size = ObstacleVisual.LocalSize(scale, spec.CollisionLength, spec.CollisionHeight);

                    if (Mathf.Abs(artCollisionHeight - spec.CollisionHeight) > 0.02f)
                        Debug.LogWarning("[SpawnerAssetGenerator] " + spec.Id + " 충돌 높이 " + spec.CollisionHeight.ToString("0.000") +
                                         "가 아트 높이 × " + ObstacleVisual.HitboxScale.ToString("0.00") + " = " +
                                         artCollisionHeight.ToString("0.000") + "와 다르다.");

                    if (Mathf.Abs(artSize.x - spec.BodyLength) > 0.02f)
                        Debug.LogWarning("[SpawnerAssetGenerator] " + spec.Id + " 몸길이 " + spec.BodyLength.ToString("0.000") +
                                         "가 아트에서 나온 가로 " + artSize.x.ToString("0.000") + "와 다르다.");

                    if (artHeight > heightBudget + 0.001f)
                        Debug.LogWarning("[SpawnerAssetGenerator] " + spec.Id + " 아트 높이 " + artHeight.ToString("0.000") +
                                         "가 레인 표시 높이 " + heightBudget.ToString("0.000") + "를 넘는다. 아트나 몸길이를 조정해야 한다.");
                }
                else
                {
                    var spriteSize = square != null ? (Vector2)square.bounds.size : Vector2.one;

                    if (spriteSize.x <= 0f)
                        spriteSize.x = 1f;

                    if (spriteSize.y <= 0f)
                        spriteSize.y = 1f;

                    renderer.sprite = square;
                    renderer.color = ColorFor(spec.Id);
                    root.transform.localScale = new Vector3(spec.BodyLength / spriteSize.x, visualHeight / spriteSize.y, 1f);
                    collider.size = new Vector2(
                        spec.CollisionLength / spec.BodyLength * spriteSize.x,
                        colliderHeight / visualHeight * spriteSize.y);
                }

                var thing = root.AddComponent<ObstacleThing>();
                var serialized = new SerializedObject(thing);
                var kind = serialized.FindProperty("kind");
                if (kind != null)
                    kind.stringValue = spec.Id;

                var visual = serialized.FindProperty("visual");
                if (visual != null)
                    visual.objectReferenceValue = renderer;

                var variantsProperty = serialized.FindProperty("variants");
                if (variantsProperty != null)
                {
                    variantsProperty.arraySize = variants.Length;

                    for (var i = 0; i < variants.Length; i++)
                        variantsProperty.GetArrayElementAtIndex(i).objectReferenceValue = variants[i];
                }

                var swimClip = serialized.FindProperty("swimClip");
                if (swimClip != null)
                    swimClip.objectReferenceValue = ClipFor(spec.Id);

                serialized.ApplyModifiedPropertiesWithoutUndo();

                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void EnsureObjectArt()
        {
            var groups = new[] { RockArtPaths, LogArtPaths, FishArtPaths() };

            for (var group = 0; group < groups.Length; group++)
            {
                var paths = groups[group];

                for (var i = 0; i < paths.Length; i++)
                {
                    if (AssetDatabase.LoadAssetAtPath<Texture2D>(paths[i]) == null)
                    {
                        Debug.LogWarning("[SpawnerAssetGenerator] 오브젝트 아트가 없다: " + paths[i] +
                                         ". 해당 장애물은 Placeholder 사각형으로 만든다.");
                        continue;
                    }

                    if (AssetDatabase.LoadAssetAtPath<Sprite>(paths[i]) != null)
                        continue;

                    ObjectArtPostprocessor.Reimport(paths[i]);
                }
            }
        }

        private static void EnsureObstacleClips()
        {
            var definitions = ObstacleClipTable.All;

            for (var i = 0; i < definitions.Count; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<CustomAnimation>(definitions[i].AssetPath) != null)
                    continue;

                AnimationAssetGenerator.Generate();
                return;
            }
        }

        private static string[] FishArtPaths()
        {
            var frames = AnimationAssetGenerator.GetFramePaths(ObstacleClipTable.Fish);
            var paths = new string[frames.Count];

            for (var i = 0; i < frames.Count; i++)
                paths[i] = frames[i];

            return paths;
        }

        private static string[] ArtPathsFor(string obstacleId)
        {
            switch (obstacleId)
            {
                case SpawnerDefaults.RockId: return RockArtPaths;
                case SpawnerDefaults.LogId: return LogArtPaths;
                case SpawnerDefaults.FishId: return FishArtPaths();
                default: return Array.Empty<string>();
            }
        }

        private static string[] VariantPathsFor(string obstacleId)
        {
            switch (obstacleId)
            {
                case SpawnerDefaults.RockId: return RockArtPaths;
                case SpawnerDefaults.LogId: return LogArtPaths;
                default: return Array.Empty<string>();
            }
        }

        private static CustomAnimation ClipFor(string obstacleId)
        {
            if (obstacleId != SpawnerDefaults.FishId)
                return null;

            var path = ObstacleClipTable.Fish.AssetPath;
            var clip = AssetDatabase.LoadAssetAtPath<CustomAnimation>(path);

            if (clip == null)
                Debug.LogWarning("[SpawnerAssetGenerator] 장애물 클립이 없다: " + path +
                                 ". 'Team1004/Generate Animation Assets'를 먼저 돌린다.");

            return clip;
        }

        private static Sprite[] LoadSprites(string[] paths)
        {
            var sprites = new List<Sprite>(paths.Length);

            for (var i = 0; i < paths.Length; i++)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(paths[i]);

                if (sprite != null)
                    sprites.Add(sprite);
                else
                    Debug.LogWarning("[SpawnerAssetGenerator] 오브젝트 아트 스프라이트를 찾지 못했다: " + paths[i]);
            }

            return sprites.ToArray();
        }

        private static bool TryArtSizePixels(string obstacleId, out Vector2 sizePixels)
        {
            var paths = ArtPathsFor(obstacleId);

            for (var i = 0; i < paths.Length; i++)
                if (ObjectArtPostprocessor.TryGetAlphaSizePixels(paths[i], out sizePixels))
                    return true;

            sizePixels = Vector2.zero;
            return false;
        }

        private static Sprite EnsureSprite(string name)
        {
            var path = PlaceholderFolder + "/" + name + ".png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (sprite != null)
                return sprite;

            if (!File.Exists(path))
            {
                var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                var pixels = new Color32[64 * 64];
                for (var i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color32(255, 255, 255, 255);

                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }

            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.filterMode = FilterMode.Bilinear;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Color ColorFor(string obstacleId)
        {
            switch (obstacleId)
            {
                case SpawnerDefaults.RockId: return new Color(0.55f, 0.55f, 0.55f, 1f);
                case SpawnerDefaults.FishId: return new Color(0.62f, 0.72f, 0.82f, 1f);
                case SpawnerDefaults.LogId: return new Color(0.45f, 0.30f, 0.15f, 1f);
                default: return Color.white;
            }
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

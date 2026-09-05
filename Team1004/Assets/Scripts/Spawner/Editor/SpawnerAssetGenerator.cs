using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Spawner.Editor
{
    public static class SpawnerAssetGenerator
    {
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
            Generate(false);
        }

        [MenuItem("Team1004/Regenerate Spawner Assets (Overwrite)")]
        public static void RegenerateAll()
        {
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
            Generate(true);
        }

        public static void Generate(bool overwrite)
        {
            EnsureFolder(PlaceholderFolder);
            EnsureFolder(ObstacleFolder);
            EnsureFolder(DesignFolder);
            EnsureFolder(ObstacleDefinitionFolder);
            EnsureFolder(PatternFolder);

            var square = EnsureSprite("Square");
            var obstacleSpecs = SpawnerDefaults.CreateObstacles();
            var definitions = new Dictionary<string, ObstacleDefinition>(StringComparer.Ordinal);
            var patternSpecs = SpawnerDefaults.CreatePatterns(obstacleSpecs);

            if (overwrite)
                DeleteStaleAssets(obstacleSpecs, patternSpecs);

            for (var i = 0; i < obstacleSpecs.Count; i++)
            {
                var spec = obstacleSpecs[i];
                var prefab = EnsurePrefab(spec, square, ColorFor(spec.Id), overwrite);
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

        private static GameObject EnsurePrefab(ObstacleSpec spec, Sprite sprite, Color color, bool overwrite)
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
                var spriteSize = sprite != null ? (Vector2)sprite.bounds.size : Vector2.one;

                if (spriteSize.x <= 0f)
                    spriteSize.x = 1f;

                if (spriteSize.y <= 0f)
                    spriteSize.y = 1f;

                var renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = color;
                renderer.sortingOrder = 5;

                root.transform.localScale = new Vector3(spec.BodyLength / spriteSize.x, visualHeight / spriteSize.y, 1f);

                var collider = root.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
                collider.size = new Vector2(
                    spec.CollisionLength / spec.BodyLength * spriteSize.x,
                    colliderHeight / visualHeight * spriteSize.y);

                var thing = root.AddComponent<ObstacleThing>();
                var serialized = new SerializedObject(thing);
                var kind = serialized.FindProperty("kind");
                if (kind != null)
                    kind.stringValue = spec.Id;

                serialized.ApplyModifiedPropertiesWithoutUndo();

                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
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

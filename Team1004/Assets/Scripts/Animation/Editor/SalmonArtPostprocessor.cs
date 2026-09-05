using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Animation.Editor
{
    public sealed class SalmonArtPostprocessor : AssetPostprocessor
    {
        public const float PivotEpsilon = 1e-5f;

        private static readonly HashSet<string> PendingSiblings = new();
        private static bool flushScheduled;

        private void OnPreprocessTexture()
        {
            if (!AnimationAssetGenerator.IsSalmonFramePath(assetPath))
                return;

            if (assetImporter is not TextureImporter importer)
                return;

            var pivot = AnimationAssetGenerator.ResolveSharedPivotCached();

            AnimationAssetGenerator.ApplyImportSettings(importer, pivot);
            QueueStaleSiblings(assetPath, pivot);
        }

        public static bool IsPivotStale(string assetPath, Vector2 pivot)
        {
            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
                return false;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);

            if (settings.spriteAlignment != (int)SpriteAlignment.Custom)
                return true;

            return Mathf.Abs(settings.spritePivot.x - pivot.x) > PivotEpsilon ||
                   Mathf.Abs(settings.spritePivot.y - pivot.y) > PivotEpsilon;
        }

        public static IReadOnlyList<string> FindStaleSiblings(string assetPath, Vector2 pivot)
        {
            var stale = new List<string>();
            var frames = AnimationAssetGenerator.EnumerateFramePaths();

            for (var i = 0; i < frames.Count; i++)
            {
                if (frames[i] == assetPath)
                    continue;

                if (IsPivotStale(frames[i], pivot))
                    stale.Add(frames[i]);
            }

            return stale;
        }

        private static void QueueStaleSiblings(string assetPath, Vector2 pivot)
        {
            var stale = FindStaleSiblings(assetPath, pivot);

            if (stale.Count == 0)
                return;

            for (var i = 0; i < stale.Count; i++)
                PendingSiblings.Add(stale[i]);

            if (flushScheduled)
                return;

            flushScheduled = true;
            EditorApplication.delayCall += Flush;
        }

        private static void Flush()
        {
            flushScheduled = false;

            if (PendingSiblings.Count == 0)
                return;

            var paths = new string[PendingSiblings.Count];
            PendingSiblings.CopyTo(paths);
            PendingSiblings.Clear();

            AssetDatabase.StartAssetEditing();

            try
            {
                for (var i = 0; i < paths.Length; i++)
                    AssetDatabase.ImportAsset(paths[i], ImportAssetOptions.ForceUpdate);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            Debug.Log("[SalmonArtPostprocessor] 합집합 pivot이 바뀌어 " + paths.Length + "장을 다시 임포트했다.");
        }
    }
}

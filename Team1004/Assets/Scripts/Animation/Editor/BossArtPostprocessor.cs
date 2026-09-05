using System;
using UnityEditor;
using UnityEngine;

namespace Game.Animation.Editor
{
    public sealed class BossArtPostprocessor : AssetPostprocessor
    {
        public const string ArtFolder = "Assets/GameAssets/Art/보스";
        public const string WaveFolder = ArtFolder + "/물결";
        public const string HookPath = ArtFolder + "/낚시바늘.png";

        public static readonly Vector2 HookPivot = new(0.5390625f, 0.19305556f);
        public static readonly Vector2 WaveFallbackPivot = new(0.5085937f, 0.36712963f);

        private const string Extension = ".png";

        private void OnPreprocessTexture()
        {
            if (!IsBossArtPath(assetPath))
                return;

            if (assetImporter is not TextureImporter importer)
                return;

            AnimationAssetGenerator.ApplyImportSettings(importer, ResolvePivotCached(assetPath));
        }

        public static bool IsBossArtPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;

            var normalized = assetPath.Replace('\\', '/');

            if (!normalized.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
                return false;

            if (string.Equals(normalized, HookPath, StringComparison.Ordinal))
                return true;

            return normalized.StartsWith(WaveFolder + "/", StringComparison.Ordinal) &&
                   normalized.IndexOf('/', WaveFolder.Length + 1) < 0;
        }

        public static bool IsHookPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;

            return string.Equals(assetPath.Replace('\\', '/'), HookPath, StringComparison.Ordinal);
        }

        public static Vector2 ResolvePivot(string assetPath)
        {
            if (IsHookPath(assetPath))
                return HookPivot;

            var shared = SharedPivotFolders.Find(assetPath);

            if (shared != null)
                return shared.ResolvePivot();

            return ResolveOwnPivot(assetPath);
        }

        public static Vector2 ResolvePivotCached(string assetPath)
        {
            if (IsHookPath(assetPath))
                return HookPivot;

            var shared = SharedPivotFolders.Find(assetPath);

            if (shared != null)
                return shared.ResolvePivotCached();

            return ResolveOwnPivot(assetPath);
        }

        public static bool TryGetAlphaSizePixels(string assetPath, out Vector2 sizePixels)
        {
            var shared = SharedPivotFolders.Find(assetPath);
            Rect bounds;

            var found = shared != null
                ? shared.TryGetBounds(out bounds, out _)
                : AnimationAssetGenerator.TryGetAlphaBounds(assetPath, out bounds, out _);

            if (!found)
            {
                sizePixels = Vector2.zero;
                return false;
            }

            sizePixels = bounds.size;
            return sizePixels.x > 0f && sizePixels.y > 0f;
        }

        public static void Reimport(string assetPath)
        {
            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
            {
                Debug.LogWarning("[BossArtPostprocessor] TextureImporter가 없다: " + assetPath);
                return;
            }

            AnimationAssetGenerator.ApplyImportSettings(importer, ResolvePivotCached(assetPath));
            importer.SaveAndReimport();
        }

        private static Vector2 ResolveOwnPivot(string assetPath)
        {
            if (!AnimationAssetGenerator.TryGetAlphaBounds(assetPath, out var bounds, out var textureSize))
            {
                Debug.LogWarning("[BossArtPostprocessor] 알파 바운딩 박스를 구하지 못해 pivot을 가운데로 둔다: " + assetPath);
                return new Vector2(0.5f, 0.5f);
            }

            return AnimationAssetGenerator.AlphaBoundsPivot(bounds, textureSize);
        }
    }
}

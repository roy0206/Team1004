using System;
using UnityEditor;
using UnityEngine;

namespace Game.Animation.Editor
{
    public sealed class ObjectArtPostprocessor : AssetPostprocessor
    {
        public const string ArtFolder = "Assets/GameAssets/Art/오브젝트";
        public const string ObstacleFishFolder = ArtFolder + "/이빨물고기";
        public const string BossArtFolder = "Assets/GameAssets/Art/보스";

        private const string Extension = ".png";

        private void OnPreprocessTexture()
        {
            if (!IsObjectArtPath(assetPath) && !IsBossSubfolderArtPath(assetPath))
                return;

            if (assetImporter is not TextureImporter importer)
                return;

            AnimationAssetGenerator.ApplyImportSettings(importer, ResolvePivotCached(assetPath));
        }

        public static bool IsBossSubfolderArtPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;

            var normalized = assetPath.Replace('\\', '/');

            if (!normalized.StartsWith(BossArtFolder + "/", StringComparison.Ordinal))
                return false;

            if (!normalized.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
                return false;

            return !BossArtPostprocessor.IsBossArtPath(normalized);
        }

        public static bool IsObjectArtPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;

            var normalized = assetPath.Replace('\\', '/');
            return normalized.StartsWith(ArtFolder + "/", StringComparison.Ordinal) &&
                   normalized.EndsWith(Extension, StringComparison.OrdinalIgnoreCase);
        }

        public static Vector2 ResolvePivot(string assetPath)
        {
            var shared = SharedPivotFolders.Find(assetPath);

            if (shared != null)
                return shared.ResolvePivot();

            return ResolveOwnPivot(assetPath);
        }

        public static Vector2 ResolvePivotCached(string assetPath)
        {
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
                Debug.LogWarning("[ObjectArtPostprocessor] TextureImporter가 없다: " + assetPath);
                return;
            }

            AnimationAssetGenerator.ApplyImportSettings(importer, ResolvePivotCached(assetPath));
            importer.SaveAndReimport();
        }

        private static Vector2 ResolveOwnPivot(string assetPath)
        {
            if (!AnimationAssetGenerator.TryGetAlphaBounds(assetPath, out var bounds, out var textureSize))
            {
                Debug.LogWarning("[ObjectArtPostprocessor] 알파 바운딩 박스를 구하지 못해 pivot을 가운데로 둔다: " + assetPath);
                return new Vector2(0.5f, 0.5f);
            }

            return AnimationAssetGenerator.AlphaBoundsPivot(bounds, textureSize);
        }
    }
}

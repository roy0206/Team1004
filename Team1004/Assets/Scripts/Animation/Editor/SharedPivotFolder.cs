using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Animation.Editor
{
    public sealed class SharedPivotFolder
    {
        private const double CacheSeconds = 30d;

        private readonly Func<IReadOnlyList<string>> frameSource;

        private Vector2 cachedPivot;
        private double cachedPivotTime = double.NegativeInfinity;

        public SharedPivotFolder(string folderPath, bool recursive, Vector2 fallbackPivot)
            : this(folderPath, recursive, fallbackPivot, null)
        {
        }

        public SharedPivotFolder(
            string folderPath,
            bool recursive,
            Vector2 fallbackPivot,
            Func<IReadOnlyList<string>> frameSource)
        {
            FolderPath = folderPath;
            Recursive = recursive;
            FallbackPivot = fallbackPivot;
            this.frameSource = frameSource;
            cachedPivot = fallbackPivot;
        }

        public string FolderPath { get; }
        public bool Recursive { get; }
        public Vector2 FallbackPivot { get; }

        public bool Contains(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(FolderPath))
                return false;

            var normalized = assetPath.Replace('\\', '/');

            if (!normalized.StartsWith(FolderPath + "/", StringComparison.Ordinal))
                return false;

            if (!normalized.EndsWith(SalmonFrameMatcher.Extension, StringComparison.OrdinalIgnoreCase))
                return false;

            return Recursive || normalized.IndexOf('/', FolderPath.Length + 1) < 0;
        }

        public IReadOnlyList<string> EnumerateFrames()
        {
            if (frameSource != null)
                return frameSource() ?? Array.Empty<string>();

            return ListPngPaths(FolderPath, Recursive);
        }

        public bool TryGetBounds(out Rect pixelBounds, out Vector2Int textureSize)
        {
            return AnimationAssetGenerator.TryGetAlphaBounds(EnumerateFrames(), out pixelBounds, out textureSize);
        }

        public Vector2 ResolvePivot()
        {
            if (!TryGetBounds(out var bounds, out var textureSize))
            {
                Debug.LogWarning("[SharedPivotFolder] " + FolderPath +
                                 "의 알파 바운딩 박스를 구하지 못해 pivot을 기본값 " + FallbackPivot + "로 둔다.");
                return FallbackPivot;
            }

            return AnimationAssetGenerator.AlphaBoundsPivot(bounds, textureSize);
        }

        public Vector2 ResolvePivotCached()
        {
            if (EditorApplication.timeSinceStartup - cachedPivotTime <= CacheSeconds)
                return cachedPivot;

            return Refresh();
        }

        public Vector2 Refresh()
        {
            cachedPivot = ResolvePivot();
            cachedPivotTime = EditorApplication.timeSinceStartup;
            return cachedPivot;
        }

        public void Invalidate()
        {
            cachedPivotTime = double.NegativeInfinity;
        }

        public static IReadOnlyList<string> ListPngPaths(string folderPath, bool recursive)
        {
            if (string.IsNullOrEmpty(folderPath))
                return Array.Empty<string>();

            var root = AnimationAssetGenerator.ToFullPath(folderPath).Replace('\\', '/').TrimEnd('/');

            if (!Directory.Exists(root))
                return Array.Empty<string>();

            var files = Directory.GetFiles(
                root,
                "*" + SalmonFrameMatcher.Extension,
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            var paths = new List<string>(files.Length);

            for (var i = 0; i < files.Length; i++)
            {
                var normalized = files[i].Replace('\\', '/');

                if (!normalized.StartsWith(root + "/", StringComparison.Ordinal))
                    continue;

                if (!normalized.EndsWith(SalmonFrameMatcher.Extension, StringComparison.OrdinalIgnoreCase))
                    continue;

                paths.Add(folderPath + "/" + normalized.Substring(root.Length + 1));
            }

            paths.Sort(StringComparer.Ordinal);
            return paths;
        }
    }

    public static class SharedPivotFolders
    {
        private static readonly SharedPivotFolder SalmonFolder = new(
            AnimationAssetGenerator.ArtFolder,
            true,
            AnimationAssetGenerator.SalmonFallbackPivot,
            AnimationAssetGenerator.EnumerateFramePaths);

        private static readonly SharedPivotFolder ObstacleFishFolder = new(
            ObjectArtPostprocessor.ObstacleFishFolder,
            false,
            new Vector2(0.5270833f, 0.4912037f));

        private static readonly SharedPivotFolder[] Folders =
        {
            SalmonFolder,
            ObstacleFishFolder
        };

        public static SharedPivotFolder Salmon => SalmonFolder;
        public static SharedPivotFolder ObstacleFish => ObstacleFishFolder;
        public static IReadOnlyList<SharedPivotFolder> All => Folders;

        public static SharedPivotFolder Find(string assetPath)
        {
            for (var i = 0; i < Folders.Length; i++)
            {
                if (Folders[i].Contains(assetPath))
                    return Folders[i];
            }

            return null;
        }

        public static void InvalidateAll()
        {
            for (var i = 0; i < Folders.Length; i++)
                Folders[i].Invalidate();
        }
    }
}

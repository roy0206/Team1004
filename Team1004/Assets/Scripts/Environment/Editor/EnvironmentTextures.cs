using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Environment.Editor
{
    public static class EnvironmentTextures
    {
        public const float PixelsPerUnit = 100f;

        public static Sprite EnsureSprite(string path, int width, int height, Func<float, float, Color> shade)
        {
            EnsurePng(path, width, height, shade);
            ApplySpriteImporter(path);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (sprite == null)
                Debug.LogError($"[EnvironmentSetup] Sprite was not imported: {path}");

            return sprite;
        }

        public static Texture2D EnsureTexture(string path, int width, int height, Func<float, float, Color> shade)
        {
            EnsurePng(path, width, height, shade);
            ApplyTextureImporter(path);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            if (texture == null)
                Debug.LogError($"[EnvironmentSetup] Texture was not imported: {path}");

            return texture;
        }

        private static void EnsurePng(string path, int width, int height, Func<float, float, Color> shade)
        {
            if (File.Exists(path))
                return;

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];

            for (var y = 0; y < height; y++)
            {
                var v = height > 1 ? y / (float)(height - 1) : 0f;

                for (var x = 0; x < width; x++)
                {
                    var u = x / (float)width;
                    var color = shade(u, v);
                    pixels[y * width + x] = new Color32(
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(color.r) * 255f),
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(color.g) * 255f),
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(color.b) * 255f),
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(color.a) * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        private static void ApplySpriteImporter(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                return;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);

            var needsUpdate =
                importer.textureType != TextureImporterType.Sprite ||
                importer.wrapMode != TextureWrapMode.Repeat ||
                settings.spriteMeshType != SpriteMeshType.FullRect ||
                !Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit);

            if (!needsUpdate)
                return;

            settings.textureType = TextureImporterType.Sprite;
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spritePixelsPerUnit = PixelsPerUnit;
            settings.spriteExtrude = 0;
            settings.wrapMode = TextureWrapMode.Repeat;
            settings.wrapModeU = TextureWrapMode.Repeat;
            settings.wrapModeV = TextureWrapMode.Repeat;
            settings.filterMode = FilterMode.Bilinear;
            settings.mipmapEnabled = false;
            settings.alphaIsTransparency = true;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static void ApplyTextureImporter(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                return;

            if (importer.textureType == TextureImporterType.Default && importer.wrapMode == TextureWrapMode.Repeat)
                return;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.textureType = TextureImporterType.Default;
            settings.wrapMode = TextureWrapMode.Repeat;
            settings.wrapModeU = TextureWrapMode.Repeat;
            settings.wrapModeV = TextureWrapMode.Repeat;
            settings.filterMode = FilterMode.Bilinear;
            settings.mipmapEnabled = false;
            settings.alphaIsTransparency = true;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}

using UnityEditor;

namespace Game.Animation.Editor
{
    public sealed class SalmonArtPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!AnimationAssetGenerator.IsSalmonFramePath(assetPath))
                return;

            if (assetImporter is not TextureImporter importer)
                return;

            AnimationAssetGenerator.ApplyImportSettings(importer, AnimationAssetGenerator.ResolveSharedPivotCached());
        }
    }
}

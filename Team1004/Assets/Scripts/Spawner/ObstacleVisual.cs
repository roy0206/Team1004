using UnityEngine;

namespace Game.Spawner
{
    public static class ObstacleVisual
    {
        public const float PixelsPerUnit = 100f;
        public const float HitboxScale = 0.87f;

        public static float PixelsToUnits(float pixels)
        {
            return pixels / PixelsPerUnit;
        }

        public static float UniformScale(float alphaWidthPixels, float bodyLength)
        {
            var widthUnits = PixelsToUnits(alphaWidthPixels);

            if (widthUnits <= 0f || bodyLength <= 0f)
                return 1f;

            return bodyLength / widthUnits;
        }

        public static float UniformScaleForHeight(float alphaHeightPixels, float visualHeight)
        {
            var heightUnits = PixelsToUnits(alphaHeightPixels);

            if (heightUnits <= 0f || visualHeight <= 0f)
                return 1f;

            return visualHeight / heightUnits;
        }

        public static float UniformScale(ObstacleSpec spec, float alphaWidthPixels, float alphaHeightPixels)
        {
            if (spec == null)
                return 1f;

            return spec.FitAxis == ObstacleFitAxis.Height
                ? UniformScaleForHeight(alphaHeightPixels, spec.VisualHeight)
                : UniformScale(alphaWidthPixels, spec.BodyLength);
        }

        public static float BodyLengthForHeight(float alphaWidthPixels, float alphaHeightPixels, float visualHeight)
        {
            return PixelsToUnits(alphaWidthPixels) * UniformScaleForHeight(alphaHeightPixels, visualHeight);
        }

        public static Vector2 VisualWorldSize(ObstacleSpec spec, float alphaWidthPixels, float alphaHeightPixels)
        {
            var scale = UniformScale(spec, alphaWidthPixels, alphaHeightPixels);
            return new Vector2(PixelsToUnits(alphaWidthPixels) * scale, PixelsToUnits(alphaHeightPixels) * scale);
        }

        public static float TargetVisualHeight(ObstacleSpec spec, float alphaWidthPixels, float alphaHeightPixels)
        {
            if (spec == null)
                return 0f;

            return spec.FitAxis == ObstacleFitAxis.Height
                ? spec.VisualHeight
                : VisualHeight(alphaWidthPixels, alphaHeightPixels, spec.BodyLength);
        }

        public static float VisualHeight(float alphaWidthPixels, float alphaHeightPixels, float bodyLength)
        {
            return PixelsToUnits(alphaHeightPixels) * UniformScale(alphaWidthPixels, bodyLength);
        }

        public static float CollisionHeight(float alphaWidthPixels, float alphaHeightPixels, float bodyLength, float hitboxScale)
        {
            var height = VisualHeight(alphaWidthPixels, alphaHeightPixels, bodyLength);

            if (height <= 0f)
                return 0f;

            return height * Mathf.Clamp(hitboxScale, 0.01f, 1f);
        }

        public static Vector2 HitboxWorldSize(
            float alphaWidthPixels,
            float alphaHeightPixels,
            float objectScale,
            float widthScale,
            float heightScale)
        {
            var width = PixelsToUnits(alphaWidthPixels) * objectScale * Mathf.Max(0f, widthScale);
            var height = PixelsToUnits(alphaHeightPixels) * objectScale * Mathf.Max(0f, heightScale);
            return new Vector2(width, height);
        }

        public static Vector2 LocalSize(float uniformScale, float worldWidth, float worldHeight)
        {
            if (uniformScale <= 0f)
                return new Vector2(worldWidth, worldHeight);

            return new Vector2(worldWidth / uniformScale, worldHeight / uniformScale);
        }

        public static int PickVariant(uint seed, int variantCount)
        {
            if (variantCount <= 1)
                return 0;

            return (int)(seed % (uint)variantCount);
        }
    }
}

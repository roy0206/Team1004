using UnityEngine;

namespace Game.View
{
    public static class CameraComposition
    {
        public const float MinSize = 0.01f;

        public static Vector3 Compose(Vector3 basePosition, Vector2 pan, Vector2 shake)
        {
            return new Vector3(
                basePosition.x + pan.x + shake.x,
                basePosition.y + pan.y + shake.y,
                basePosition.z);
        }

        public static float ComposeSize(float baseSize, float zoomFactor, float minFactor, float maxFactor)
        {
            var clamped = Mathf.Clamp(zoomFactor, minFactor, maxFactor);
            var size = baseSize * clamped;
            return size < MinSize ? MinSize : size;
        }

        public static float ComposeRoll(float baseRoll, float shakeRoll)
        {
            return baseRoll + shakeRoll;
        }

        public static float VisibleHalfHeight(float baseSize, float zoomFactor)
        {
            return baseSize * zoomFactor;
        }

        public static float VisibleHalfWidth(float baseSize, float zoomFactor, float aspect)
        {
            return baseSize * zoomFactor * aspect;
        }
    }
}

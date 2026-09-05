using UnityEngine;

namespace Game.Water
{
    public static class WaterWake
    {
        public const float BowFront = 0.25f;
        public const float BowLift = 0.35f;

        public static float CapInjection(float previous, float injected, float cap)
        {
            if (cap <= 0f || float.IsNaN(cap))
                return injected;

            var limit = Mathf.Max(cap, Mathf.Abs(previous));
            return Mathf.Clamp(injected, -limit, limit);
        }

        public static float Proximity(float nodeY, float minY, float maxY, float influence)
        {
            if (nodeY < minY)
                return 0f;

            if (nodeY <= maxY)
                return 1f;

            if (influence <= 0f)
                return 0f;

            return Mathf.Clamp01(1f - (nodeY - maxY) / influence);
        }

        public static float Shape(float nodeX, float minX, float maxX, float horizontalVelocity)
        {
            var width = maxX - minX;
            var t = width > 0f ? Mathf.Clamp01((nodeX - minX) / width) : 1f;
            var s = horizontalVelocity < 0f ? t : 1f - t;

            return s < BowFront
                ? -BowLift * (1f - s / BowFront)
                : (s - BowFront) / (1f - BowFront);
        }

        public static float Contribution(
            float nodeX, float nodeY,
            float minX, float maxX, float minY, float maxY,
            float horizontalVelocity, float surfaceInfluence, float transfer)
        {
            var speed = Mathf.Abs(horizontalVelocity);

            if (speed <= 0f || transfer <= 0f)
                return 0f;

            if (nodeX < minX || nodeX > maxX)
                return 0f;

            var proximity = Proximity(nodeY, minY, maxY, surfaceInfluence);

            if (proximity <= 0f)
                return 0f;

            return speed * transfer * proximity * Shape(nodeX, minX, maxX, horizontalVelocity);
        }

        public static float NodeVelocity(
            float velocity, float nodeX, float nodeY,
            float minX, float maxX, float minY, float maxY,
            float horizontalVelocity, float surfaceInfluence, float transfer)
        {
            var contribution = Contribution(
                nodeX, nodeY, minX, maxX, minY, maxY,
                horizontalVelocity, surfaceInfluence, transfer);

            if (contribution == 0f)
                return velocity;

            var limit = Mathf.Max(Mathf.Abs(velocity), Mathf.Abs(horizontalVelocity) * transfer);
            return Mathf.Clamp(velocity - contribution, -limit, limit);
        }

        public static float NodeVelocity(
            float velocity, float nodeX, float nodeY, Bounds bounds,
            float horizontalVelocity, float surfaceInfluence, float transfer)
        {
            var min = bounds.min;
            var max = bounds.max;
            return NodeVelocity(
                velocity, nodeX, nodeY, min.x, max.x, min.y, max.y,
                horizontalVelocity, surfaceInfluence, transfer);
        }

        public static int Apply(
            float[] velocities, float[] nodeX, float[] nodeY,
            float minX, float maxX, float minY, float maxY,
            float horizontalVelocity, float surfaceInfluence, float transfer)
        {
            if (velocities == null || nodeX == null || nodeY == null)
                return 0;

            var count = Mathf.Min(velocities.Length, Mathf.Min(nodeX.Length, nodeY.Length));
            var changed = 0;

            for (var i = 0; i < count; i++)
            {
                var next = NodeVelocity(
                    velocities[i], nodeX[i], nodeY[i], minX, maxX, minY, maxY,
                    horizontalVelocity, surfaceInfluence, transfer);

                if (next == velocities[i])
                    continue;

                velocities[i] = next;
                changed++;
            }

            return changed;
        }
    }
}

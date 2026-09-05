using System.Collections.Generic;
using Game.Player;
using UnityEngine;

namespace Game.Boss
{
    public sealed class LaneHazard : MonoThing
    {
        [SerializeField] private Hazard[] laneHazards;
        [SerializeField] private BoxCollider2D[] laneBoxes;

        public int LaneCount => laneHazards != null ? laneHazards.Length : 0;
        public int Mask { get; private set; }
        public bool IsArmed { get; private set; }

        public Hazard GetHazard(int lane)
        {
            return laneHazards != null && lane >= 0 && lane < laneHazards.Length ? laneHazards[lane] : null;
        }

        public BoxCollider2D GetBox(int lane)
        {
            return laneBoxes != null && lane >= 0 && lane < laneBoxes.Length ? laneBoxes[lane] : null;
        }

        public bool IsLaneArmed(int lane)
        {
            var hazard = GetHazard(lane);
            return hazard != null && hazard.isActiveAndEnabled;
        }

        public bool TryGetLaneBand(int lane, out Rect band)
        {
            var box = GetBox(lane);

            if (box == null)
            {
                band = default;
                return false;
            }

            var center = (Vector2)box.transform.TransformPoint(box.offset);
            var size = Vector2.Scale(box.size, box.transform.lossyScale);
            band = new Rect(center - size * 0.5f, size);
            return true;
        }

        public void ApplyLayout(IReadOnlyList<float> laneY, float width, float height)
        {
            if (laneY == null || width <= 0f || height <= 0f)
                return;

            for (var lane = 0; lane < LaneCount; lane++)
            {
                var hazard = GetHazard(lane);

                if (hazard == null || lane >= laneY.Count)
                    continue;

                var target = hazard.transform;
                target.localScale = Vector3.one;
                target.position = new Vector3(0f, laneY[lane], target.position.z);

                var box = GetBox(lane);

                if (box == null)
                    continue;

                box.offset = Vector2.zero;
                box.size = new Vector2(width, height);
            }
        }

        public void Prepare(int laneMask)
        {
            Disarm();
            Mask = laneMask;
        }

        public void Arm()
        {
            IsArmed = true;
            ApplyActiveLanes();
        }

        public void Disarm()
        {
            IsArmed = false;
            ApplyActiveLanes();
        }

        public void Clear()
        {
            Mask = 0;
            Disarm();
        }

        protected override void OnThingDestroy()
        {
            Mask = 0;
            IsArmed = false;
        }

        private void ApplyActiveLanes()
        {
            for (var lane = 0; lane < LaneCount; lane++)
            {
                var hazard = GetHazard(lane);

                if (hazard == null)
                    continue;

                var active = IsArmed && BossLanes.Contains(Mask, lane);

                if (hazard.gameObject.activeSelf != active)
                    hazard.gameObject.SetActive(active);
            }
        }
    }
}

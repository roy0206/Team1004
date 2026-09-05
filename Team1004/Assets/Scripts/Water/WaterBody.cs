using System;
using UnityEngine;

namespace Game.Water
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class WaterBody : MonoBehaviour, IWaterCollider
    {
        [HideInInspector]
        public Rigidbody2D rigidbody;
        [HideInInspector]
        public Collider2D collider;

        public Bounds bounds => collider.bounds;
        public Bounds Bounds => bounds;
        public float VerticalVelocity => rigidbody != null ? rigidbody.linearVelocityY : 0f;
        public bool OverlapPoint(Vector2 point) => collider != null && collider.OverlapPoint(point);
        void Awake()
        {
            rigidbody = GetComponent<Rigidbody2D>();
            collider = GetComponent<Collider2D>();
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (other.TryGetComponent(out Water water))
            {
                WaterSystem.Collide(water, this);
            }
        }
    }
}

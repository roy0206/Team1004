using System;
using UnityEngine;

namespace Game.Animation
{
    [Serializable]
    public struct CustomAnimationEvent
    {
        [SerializeField] private int frame;
        [SerializeField] private string id;

        public CustomAnimationEvent(int frame, string id)
        {
            this.frame = frame;
            this.id = id;
        }

        public int Frame => frame;
        public string Id => id;
        public bool HasId => !string.IsNullOrEmpty(id);
    }
}

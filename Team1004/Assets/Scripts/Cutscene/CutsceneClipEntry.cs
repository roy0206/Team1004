using System;
using Game.Animation;
using UnityEngine;

namespace Game.Cutscene
{
    [Serializable]
    public sealed class CutsceneClipEntry
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private CustomAnimation clip;

        public string Id => id;
        public CustomAnimation Clip => clip;
    }
}

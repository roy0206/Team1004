using UnityEngine;
using UnityEngine.UI;

namespace Game.StageProgress
{
    [AddComponentMenu("UI/Effects/Stage Progress Gradient")]
    [DisallowMultipleComponent]
    public sealed class UIGradient : BaseMeshEffect
    {
        [SerializeField] private Color leftColor = new Color(0.20f, 0.78f, 0.86f, 1f);
        [SerializeField] private Color rightColor = new Color(0.36f, 0.94f, 0.70f, 1f);

        public void SetColors(Color left, Color right)
        {
            leftColor = left;
            rightColor = right;
            MarkDirty();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh == null)
                return;

            var count = vh.currentVertCount;

            if (count == 0)
                return;

            var vertex = new UIVertex();
            var minX = float.MaxValue;
            var maxX = float.MinValue;

            for (var i = 0; i < count; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                minX = Mathf.Min(minX, vertex.position.x);
                maxX = Mathf.Max(maxX, vertex.position.x);
            }

            var width = maxX - minX;

            if (width <= Mathf.Epsilon)
                return;

            for (var i = 0; i < count; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);

                var t = Mathf.Clamp01((vertex.position.x - minX) / width);
                vertex.color = Multiply(vertex.color, Color.Lerp(leftColor, rightColor, t));

                vh.SetUIVertex(vertex, i);
            }
        }

        private void MarkDirty()
        {
            if (graphic != null)
                graphic.SetVerticesDirty();
        }

        private static Color32 Multiply(Color32 source, Color tint)
        {
            return new Color32(
                (byte)Mathf.Clamp(source.r * tint.r, 0f, 255f),
                (byte)Mathf.Clamp(source.g * tint.g, 0f, 255f),
                (byte)Mathf.Clamp(source.b * tint.b, 0f, 255f),
                (byte)Mathf.Clamp(source.a * tint.a, 0f, 255f));
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            MarkDirty();
        }
#endif
    }
}

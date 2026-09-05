using System.Collections.Generic;
using Game.Config;
using UnityEngine;
using WaterSurface = Game.Water.Water;

namespace Game.Environment
{
    public sealed class EnvironmentThing : MonoThing
    {
        [SerializeField] private EnvironmentLayer far = new();
        [SerializeField] private EnvironmentLayer mid = new();
        [SerializeField] private EnvironmentLayer near = new();
        [SerializeField] private EnvironmentLayer flow = new();
        [SerializeField] private EnvironmentLayer homeland = new();
        [SerializeField] private WaterSurface surface;
        [SerializeField] private GameObject calmOverlay;
        [SerializeField] private float baseFlowSpeed = 0.6f;
        [SerializeField] private float calmTimeScale = 0.6f;
        [SerializeField] private float splashRadius = 0.55f;
        [SerializeField] private float splashMaxDepth = 0.6f;
        [SerializeField] private bool scrollingOnAwake;

        private ParallaxLayerModule farLayer;
        private ParallaxLayerModule midLayer;
        private ParallaxLayerModule nearLayer;
        private WaterFlowModule flowLayer;
        private HomelandModule homelandLayer;
        private WaterSplashModule splash;

        private float speed;
        private bool isScrolling;
        private bool isCalm;

        public float Speed
        {
            get => speed;
            set
            {
                speed = Mathf.Max(0f, value);
                ApplySpeed();
            }
        }

        public bool IsScrolling => isScrolling;
        public bool IsCalm => isCalm;
        public bool IsHomelandVisible => homelandLayer != null && homelandLayer.IsVisible;
        public WaterSurface Surface => surface;
        public float SurfaceY => surface != null ? surface.transform.position.y : GameConfig.Current.WaterSurfaceY;

        private void Awake()
        {
            speed = GameConfig.Current.ScrollSpeed;

            var buffer = new List<Transform>();

            farLayer = AddModule(new ParallaxLayerModule(far.CreateRunner(buffer), far.SpeedScale, speed));
            midLayer = AddModule(new ParallaxLayerModule(mid.CreateRunner(buffer), mid.SpeedScale, speed));
            nearLayer = AddModule(new ParallaxLayerModule(near.CreateRunner(buffer), near.SpeedScale, speed));
            flowLayer = AddModule(new WaterFlowModule(flow.CreateRunner(buffer), flow.SpeedScale, baseFlowSpeed, speed));

            var homelandRoot = homeland.Root != null ? homeland.Root.gameObject : null;
            homelandLayer = AddModule(new HomelandModule(homelandRoot, homeland.CreateRunner(buffer), homeland.SpeedScale, speed));

            splash = AddModule(new WaterSplashModule(surface, splashRadius, splashMaxDepth));

            SetCalm(false);
            SetScrolling(scrollingOnAwake);

            GameConfig.Changed += OnConfigChanged;
        }

        protected override void OnThingDestroy()
        {
            GameConfig.Changed -= OnConfigChanged;
        }

        public void SetScrolling(bool scrolling)
        {
            isScrolling = scrolling;

            if (farLayer != null)
                farLayer.IsScrolling = scrolling;

            if (midLayer != null)
                midLayer.IsScrolling = scrolling;

            if (nearLayer != null)
                nearLayer.IsScrolling = scrolling;

            if (flowLayer != null)
                flowLayer.IsScrolling = scrolling;

            if (homelandLayer != null)
                homelandLayer.IsScrolling = scrolling;
        }

        public void SetHomeland(bool visible)
        {
            homelandLayer?.SetVisible(visible);
        }

        public void SetCalm(bool calm)
        {
            isCalm = calm;

            if (calmOverlay != null && calmOverlay.activeSelf != calm)
                calmOverlay.SetActive(calm);

            if (flowLayer != null)
                flowLayer.TimeScale = calm ? Mathf.Max(0f, calmTimeScale) : 1f;
        }

        public bool Splash(float x, float strength)
        {
            return splash != null && splash.Splash(x, strength);
        }

        private void ApplySpeed()
        {
            if (farLayer != null)
                farLayer.Speed = speed;

            if (midLayer != null)
                midLayer.Speed = speed;

            if (nearLayer != null)
                nearLayer.Speed = speed;

            if (flowLayer != null)
                flowLayer.Speed = speed;

            if (homelandLayer != null)
                homelandLayer.Speed = speed;
        }

        private void OnConfigChanged()
        {
            Speed = GameConfig.Current.ScrollSpeed;
        }
    }
}

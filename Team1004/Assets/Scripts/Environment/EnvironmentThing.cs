using System;
using System.Collections.Generic;
using Game.Config;
using UnityEngine;
using UnityEngine.U2D;
using WaterSurface = Game.Water.Water;

namespace Game.Environment
{
    public sealed class EnvironmentThing : MonoThing
    {
        [SerializeField] private EnvironmentLayer clouds = new();
        [SerializeField] private EnvironmentLayer mountains = new();
        [SerializeField] private EnvironmentLayer hills = new();
        [SerializeField] private EnvironmentLayer ground = new();
        [SerializeField] private EnvironmentLayer riverbed = new();
        [SerializeField] private EnvironmentLayer homeland = new();
        [SerializeField] private WaterSurface surface;
        [SerializeField] private SpriteShapeController overlayShape;
        [SerializeField] private SpriteShapeRenderer overlayRenderer;
        [SerializeField] private float overlayAlpha = 0.15f;
        [SerializeField] private float scrollScale = 0.5f;
        [SerializeField] private float splashRadius = 0.55f;
        [SerializeField] private float splashMaxDepth = 0.6f;
        [SerializeField] private bool scrollingOnAwake;

        private ParallaxLayerModule cloudLayer;
        private ParallaxLayerModule mountainLayer;
        private ParallaxLayerModule hillLayer;
        private ParallaxLayerModule groundLayer;
        private ParallaxLayerModule riverbedLayer;
        private HomelandModule homelandLayer;
        private WaterSplashModule splash;
        private WaterOverlayModule waterOverlay;

        private bool isInitialized;
        private float speed;
        private bool isScrolling;
        private Transform[] offsetRoots;
        private float[] offsetBaseY;
        private float verticalOffset;

        public float Speed
        {
            get => speed;
            set
            {
                speed = Mathf.Max(0f, value);
                ApplySpeed();
            }
        }

        public float ScrollScale
        {
            get => scrollScale;
            set
            {
                scrollScale = Mathf.Max(0f, value);
                ApplySpeed();
            }
        }

        private float LayerSpeed => speed * Mathf.Max(0f, scrollScale);

        public bool IsInitialized => isInitialized;
        public bool IsScrolling => isScrolling;
        public bool IsHomelandVisible => homelandLayer != null && homelandLayer.IsVisible;
        public WaterSurface Surface => surface;
        public float SurfaceY => surface != null ? surface.transform.position.y : GameConfig.Current.WaterSurfaceY;
        public float VerticalOffset => verticalOffset;
        public float OverlayAlpha => overlayAlpha;
        public bool IsOverlayLinked => waterOverlay != null && waterOverlay.IsUsable;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (isInitialized)
                return;

            isInitialized = true;
            speed = GameConfig.Current.ScrollSpeed;

            var buffer = new List<Transform>();

            var layerSpeed = LayerSpeed;

            cloudLayer = AddModule(new ParallaxLayerModule(clouds.CreateRunner(buffer), clouds.SpeedScale, layerSpeed));
            mountainLayer = AddModule(new ParallaxLayerModule(mountains.CreateRunner(buffer), mountains.SpeedScale, layerSpeed));
            hillLayer = AddModule(new ParallaxLayerModule(hills.CreateRunner(buffer), hills.SpeedScale, layerSpeed));
            groundLayer = AddModule(new ParallaxLayerModule(ground.CreateRunner(buffer), ground.SpeedScale, layerSpeed));
            riverbedLayer = AddModule(new ParallaxLayerModule(riverbed.CreateRunner(buffer), riverbed.SpeedScale, layerSpeed));

            var homelandRoot = homeland.Root != null ? homeland.Root.gameObject : null;
            homelandLayer = AddModule(new HomelandModule(homelandRoot, homeland.CreateRunner(buffer), homeland.SpeedScale, layerSpeed));

            splash = AddModule(new WaterSplashModule(surface, splashRadius, splashMaxDepth));

            var sourceShape = surface != null ? surface.GetComponent<SpriteShapeController>() : null;
            waterOverlay = AddModule(new WaterOverlayModule(sourceShape, overlayShape));

            ApplyOverlayAlpha();
            CacheOffsetRoots();
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

            if (cloudLayer != null)
                cloudLayer.IsScrolling = scrolling;

            if (mountainLayer != null)
                mountainLayer.IsScrolling = scrolling;

            if (hillLayer != null)
                hillLayer.IsScrolling = scrolling;

            if (groundLayer != null)
                groundLayer.IsScrolling = scrolling;

            if (riverbedLayer != null)
                riverbedLayer.IsScrolling = scrolling;

            if (homelandLayer != null)
                homelandLayer.IsScrolling = scrolling;
        }

        public void SetHomeland(bool visible)
        {
            homelandLayer?.SetVisible(visible);
        }

        public void SetOverlayAlpha(float alpha)
        {
            overlayAlpha = Mathf.Clamp01(alpha);
            ApplyOverlayAlpha();
        }

        [Obsolete("잔잔한 물 오버레이는 물 폴리곤으로 대체됐다. 아무 것도 하지 않는다.")]
        public bool IsCalm => false;

        [Obsolete("잔잔한 물 오버레이는 물 폴리곤으로 대체됐다. 아무 것도 하지 않는다.")]
        public void SetCalm(bool calm)
        {
        }

        public bool Splash(float x, float strength)
        {
            return splash != null && splash.Splash(x, strength);
        }

        public void SetVerticalOffset(float offset)
        {
            verticalOffset = offset;

            if (offsetRoots == null)
                CacheOffsetRoots();

            if (offsetRoots == null)
                return;

            for (var i = 0; i < offsetRoots.Length; i++)
            {
                var root = offsetRoots[i];

                if (root == null)
                    continue;

                var position = root.localPosition;
                position.y = offsetBaseY[i] + offset;
                root.localPosition = position;
            }
        }

        public async Awaitable RaiseWorldAsync(float amount, float duration)
        {
            var from = verticalOffset;
            var to = from - amount;

            if (duration <= 0f)
            {
                SetVerticalOffset(to);
                return;
            }

            var elapsed = 0f;

            while (elapsed < duration)
            {
                await Awaitable.NextFrameAsync();

                if (this == null)
                    return;

                elapsed += Time.deltaTime;
                SetVerticalOffset(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration))));
            }

            SetVerticalOffset(to);
        }

        private void ApplyOverlayAlpha()
        {
            if (overlayRenderer == null)
                return;

            var color = overlayRenderer.color;
            color.a = Mathf.Clamp01(overlayAlpha);
            overlayRenderer.color = color;
        }

        private void CacheOffsetRoots()
        {
            var roots = new[]
            {
                clouds.Root,
                mountains.Root,
                hills.Root,
                ground.Root,
                riverbed.Root,
                homeland.Root,
                surface != null ? surface.transform : null,
                overlayShape != null ? overlayShape.transform : null
            };

            offsetRoots = roots;
            offsetBaseY = new float[roots.Length];

            for (var i = 0; i < roots.Length; i++)
                offsetBaseY[i] = roots[i] != null ? roots[i].localPosition.y : 0f;
        }

        private void ApplySpeed()
        {
            var layerSpeed = LayerSpeed;

            if (cloudLayer != null)
                cloudLayer.Speed = layerSpeed;

            if (mountainLayer != null)
                mountainLayer.Speed = layerSpeed;

            if (hillLayer != null)
                hillLayer.Speed = layerSpeed;

            if (groundLayer != null)
                groundLayer.Speed = layerSpeed;

            if (riverbedLayer != null)
                riverbedLayer.Speed = layerSpeed;

            if (homelandLayer != null)
                homelandLayer.Speed = layerSpeed;
        }

        private void OnConfigChanged()
        {
            Speed = GameConfig.Current.ScrollSpeed;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

public sealed class CanvasGroupFadeTransition : MonoBehaviour, ISceneTransition
{
    [SerializeField] private Color color = Color.black;
    [SerializeField] private int sortingOrder = 5000;
    [SerializeField] private RenderMode renderMode = RenderMode.ScreenSpaceOverlay;
    [SerializeField, Min(0.01f)] private float cameraPlaneOffset = 0.05f;

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Image image;
    private int operationVersion;

    private void LateUpdate()
    {
        if (renderMode == RenderMode.ScreenSpaceCamera &&
            canvas != null &&
            canvas.worldCamera == null)
            BindCamera();
    }

    private void OnDestroy()
    {
        operationVersion++;
    }

    public async Awaitable CoverAsync(float duration)
    {
        await FadeToAsync(1f, duration);
    }

    public async Awaitable RevealAsync(float duration)
    {
        await FadeToAsync(0f, duration);
    }

    public void SetInstant(float alpha)
    {
        EnsureOverlay();
        operationVersion++;
        SetAlpha(alpha);
    }

    public void SetColor(Color value)
    {
        EnsureOverlay();
        color = value;
        image.color = new Color(value.r, value.g, value.b, 1f);
    }

    private async Awaitable FadeToAsync(float targetAlpha, float duration)
    {
        EnsureOverlay();

        var version = ++operationVersion;
        var startAlpha = canvasGroup.alpha;
        targetAlpha = Mathf.Clamp01(targetAlpha);
        duration = Mathf.Max(0f, duration);

        if (duration <= 0f)
        {
            SetAlpha(targetAlpha);
            return;
        }

        canvasGroup.blocksRaycasts = true;
        var elapsed = 0f;

        while (elapsed < duration && version == operationVersion)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration));
            await Awaitable.NextFrameAsync();

            if (this == null)
                return;
        }

        if (version == operationVersion)
            SetAlpha(targetAlpha);
    }

    private void EnsureOverlay()
    {
        if (canvasGroup != null)
        {
            BindCamera();
            return;
        }

        var root = new GameObject(
            "SceneTransitionCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasGroup),
            typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);

        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = renderMode;
        canvas.sortingOrder = sortingOrder;

        canvasGroup = root.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        var overlay = new GameObject(
            "Overlay",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        overlay.transform.SetParent(root.transform, false);

        image = overlay.GetComponent<Image>();
        image.raycastTarget = true;
        image.color = new Color(color.r, color.g, color.b, 1f);

        var rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        BindCamera();
    }

    private void BindCamera()
    {
        if (canvas == null || renderMode != RenderMode.ScreenSpaceCamera)
            return;

        var targetCamera = Camera.main;
        if (targetCamera == null)
            return;

        canvas.worldCamera = targetCamera;
        canvas.planeDistance = Mathf.Max(
            targetCamera.nearClipPlane + cameraPlaneOffset,
            0.1f);
    }

    private void SetAlpha(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);
        canvasGroup.alpha = alpha;
        canvasGroup.blocksRaycasts = alpha > 0.001f;
    }
}

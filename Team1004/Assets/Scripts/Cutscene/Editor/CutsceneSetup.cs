using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Game.Cutscene.Editor
{
    public static class CutsceneSetup
    {
        private const string UiFolder = "Assets/GameAssets/UI";
        private const string PlaceholderFolder = "Assets/GameAssets/Placeholder";
        private const string FontPath = PlaceholderFolder + "/Fonts/MonaS12.ttf";
        private const string BuiltinFontName = "LegacyRuntime.ttf";
        private const string DialoguePrefabPath = UiFolder + "/CutsceneDialogue.prefab";
        private const string HintLabel = "Enter / 클릭 ▶";

        private const int UiLayer = 5;
        private const int CanvasSortingOrder = 100;
        private const float CanvasPlaneDistance = 10f;
        private static readonly Vector2 ReferenceResolution = new(1920f, 1080f);

        private static readonly Vector2 Center = new(0.5f, 0.5f);
        private static readonly Vector2 TopLeft = new(0f, 1f);
        private static readonly Vector2 BottomRight = new(1f, 0f);
        private static readonly Color PanelColor = new(0f, 0f, 0f, 0.72f);
        private static readonly Color SpeakerColor = new(1f, 0.85f, 0.5f, 1f);
        private static readonly Color HintColor = new(0.8f, 0.8f, 0.8f, 1f);
        private static readonly Color FadeColor = new(0f, 0f, 0f, 0f);

        [MenuItem("Team1004/Generate Cutscene Dialogue Prefab")]
        public static void Generate()
        {
            Run(false);
        }

        [MenuItem("Team1004/Regenerate Cutscene Dialogue Prefab (Overwrite)")]
        public static void Regenerate()
        {
            var confirmed = Application.isBatchMode || EditorUtility.DisplayDialog(
                "Regenerate Cutscene Dialogue Prefab",
                "CutsceneDialogue.prefab을 다시 만듭니다.\n" +
                "직접 고친 내용은 사라집니다. 계속할까요?",
                "덮어쓰기",
                "취소");

            if (confirmed)
                Run(true);
        }

        public static void Generate(bool overwrite)
        {
            Run(overwrite);
        }

        private static void Run(bool overwrite)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[CutsceneSetup] Exit Play Mode before generating cutscene assets.");
                return;
            }

            EnsureFolder(UiFolder);

            var font = LoadFont();
            EnsureDialoguePrefab(font, overwrite);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CutsceneSetup] CutsceneDialogue.prefab is ready. Follow Docs/Cutscene.md to place it in the Play scene.");
        }

        private static void EnsureDialoguePrefab(Font font, bool overwrite)
        {
            if (!overwrite && AssetDatabase.LoadAssetAtPath<GameObject>(DialoguePrefabPath) != null)
                return;

            if (font == null)
            {
                Debug.LogError("[CutsceneSetup] No font is available. CutsceneDialogue.prefab was not created.");
                return;
            }

            var root = CreateRect("CutsceneDialogue", null);
            var canvas = root.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.planeDistance = CanvasPlaneDistance;
            canvas.sortingOrder = CanvasSortingOrder;

            var scaler = root.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var view = root.gameObject.AddComponent<CutsceneDialogueView>();

            var fade = CreateImage(root, "ScreenFade", FadeColor);
            Stretch(fade.rectTransform, Vector2.zero, Vector2.zero);

            var panel = CreateRect("Panel", root);
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = new Vector2(1f, 0f);
            panel.pivot = new Vector2(0.5f, 0f);
            panel.anchoredPosition = new Vector2(0f, 36f);
            panel.sizeDelta = new Vector2(-120f, 250f);
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = PanelColor;
            panelImage.raycastTarget = false;
            var group = panel.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            var speaker = CreateText(panel, "Speaker", font, 32, SpeakerColor, TextAnchor.MiddleLeft);
            speaker.text = "화자";
            Place(speaker.rectTransform, TopLeft, new Vector2(36f, -20f), new Vector2(900f, 44f));

            var body = CreateText(panel, "Body", font, 32, Color.white, TextAnchor.UpperLeft);
            body.text = "대사";
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            Stretch(body.rectTransform, new Vector2(36f, 28f), new Vector2(-36f, -76f));

            var hint = CreateText(panel, "Hint", font, 24, HintColor, TextAnchor.MiddleRight);
            hint.text = HintLabel;
            Place(hint.rectTransform, BottomRight, new Vector2(-28f, 16f), new Vector2(220f, 34f));

            Canvas.ForceUpdateCanvases();
            ValidateText(speaker);
            ValidateText(body);
            ValidateText(hint);
            hint.gameObject.SetActive(false);

            var serialized = new SerializedObject(view);
            serialized.FindProperty("canvas").objectReferenceValue = canvas;
            serialized.FindProperty("panel").objectReferenceValue = group;
            serialized.FindProperty("speakerText").objectReferenceValue = speaker;
            serialized.FindProperty("bodyText").objectReferenceValue = body;
            serialized.FindProperty("hint").objectReferenceValue = hint.gameObject;
            serialized.FindProperty("screenFade").objectReferenceValue = fade;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root.gameObject, DialoguePrefabPath);
            Object.DestroyImmediate(root.gameObject);
        }

        private static void ValidateText(Text text)
        {
            if (text.font == null)
                Debug.LogError($"[CutsceneSetup] Text '{text.name}' has no font.", text);

            if (text.color.a < 1f)
                Debug.LogWarning($"[CutsceneSetup] Text '{text.name}' is not fully opaque.", text);

            var rect = text.rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f)
                Debug.LogError($"[CutsceneSetup] Text '{text.name}' has a zero-sized RectTransform ({rect.width}x{rect.height}).", text);
        }

        private static Font LoadFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font != null)
                return font;

            Debug.LogWarning(
                $"[CutsceneSetup] '{FontPath}' is missing. See Assets/GameAssets/Placeholder/Fonts/README.md. " +
                $"Falling back to the builtin font '{BuiltinFontName}'. Korean text may not render.");
            return Resources.GetBuiltinResource<Font>(BuiltinFontName);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var rectObject = new GameObject(name, typeof(RectTransform));
            rectObject.layer = UiLayer;

            var rect = (RectTransform)rectObject.transform;
            if (parent != null)
                rect.SetParent(parent, false);

            return rect;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(Transform parent, string name, Font font, int fontSize, Color color, TextAnchor alignment)
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = Center;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}

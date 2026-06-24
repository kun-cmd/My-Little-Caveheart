using UnityEngine;
using UnityEngine.UI;

namespace MyLittleCaveheart
{
    public enum ExamCursorMode
    {
        Default,
        Hand,
        Grab
    }

    [DefaultExecutionOrder(10000)]
    public sealed class ExamCursorVisuals : MonoBehaviour
    {
        private const string CursorResourceRoot = "Sprites/Caveheart/CursorIcons/Size64/";

        [Header("Cursor Images")]
        [SerializeField] private Texture2D defaultCursor;
        [SerializeField] private Texture2D handCursor;
        [SerializeField] private Texture2D grabCursor;
        [SerializeField, Min(8f)] private float cursorImageSize = 37f;
        [SerializeField] private Vector2 cursorImageOffset = Vector2.zero;

        [Header("Observe Hold Ring")]
        [SerializeField, Min(0.1f)] private float observeHoldRingRadius = 48f;
        [SerializeField, Min(1f)] private float observeHoldRingWidth = 8f;
        [SerializeField] private Vector2 observeHoldRingOffset = Vector2.zero;
        [SerializeField] private Color observeHoldIdleRingColor = new Color(0f, 0f, 0f, 0.34f);

        private RectTransform cursorImageRect;
        private RawImage cursorImage;
        private RectTransform observeHoldRingRect;
        private RawImage observeHoldIdleRingImage;
        private RawImage observeHoldProgressRingImage;
        private Texture2D observeHoldIdleTexture;
        private Texture2D observeHoldProgressTexture;
        private int observeHoldTextureSize;
        private ExamCursorMode currentMode = (ExamCursorMode)(-1);

        // Reclaims the native cursor when this component is re-enabled after scene or inspector changes.
        private void OnEnable()
        {
            CaveheartCursorVisibilityGuard.SetOverrideActive(this, true);
        }

        // Builds the cursor UI container before the interaction tracker starts using it.
        private void Awake()
        {
            var uiRoot = EnsureUiRoot().transform;
            EnsureCursorImage(uiRoot);
            EnsureObserveHoldIndicator(uiRoot);
            CaveheartCursorVisibilityGuard.SetOverrideActive(this, true);
            SetCursorMode(ExamCursorMode.Default);
            SetObserveHold(false, 0f);
        }

        // Keeps the UI cursor aligned while hiding it when the pointer leaves the game window.
        private void LateUpdate()
        {
            UpdateCursorImageVisibility();
            PositionCursorImage();
        }

        // Restores the OS cursor when this level controller is disabled.
        private void OnDisable()
        {
            HideCursorImage();
            CaveheartCursorVisibilityGuard.SetOverrideActive(this, false);
            SetObserveHold(false, 0f);
        }

        // Applies one of the level's cursor modes: default, hover hand, or active grab.
        public void SetCursorMode(ExamCursorMode mode)
        {
            if (currentMode == mode)
            {
                return;
            }

            currentMode = mode;
            ApplyCursorImage(CursorTexture(currentMode));
        }

        // Shows or hides the observe hold ring and redraws its current progress.
        public void SetObserveHold(bool visible, float progress01)
        {
            if (observeHoldIdleRingImage == null || observeHoldProgressRingImage == null || observeHoldRingRect == null)
            {
                return;
            }

            var outerRadius = observeHoldRingRadius + observeHoldRingWidth * 0.5f;
            var size = Mathf.CeilToInt((outerRadius + 3f) * 2f);
            observeHoldRingRect.SetAsLastSibling();
            observeHoldRingRect.sizeDelta = Vector2.one * size;
            observeHoldIdleRingImage.rectTransform.sizeDelta = Vector2.one * size;
            observeHoldProgressRingImage.rectTransform.sizeDelta = Vector2.one * size;
            observeHoldIdleRingImage.enabled = true;
            observeHoldProgressRingImage.enabled = true;
            observeHoldIdleRingImage.transform.SetAsFirstSibling();
            observeHoldProgressRingImage.transform.SetAsLastSibling();

            EnsureObserveHoldTextures(size);
            DrawRingTexture(observeHoldIdleTexture, 1f, observeHoldIdleRingColor);
            DrawRingTexture(observeHoldProgressTexture, progress01, Color.white);

            if (visible)
            {
                PositionObserveHoldIndicator();
            }

            observeHoldRingRect.gameObject.SetActive(visible);
            cursorImageRect?.SetAsLastSibling();
        }

        // Creates or reuses a screen-space canvas for cursor-only UI elements.
        private static GameObject EnsureUiRoot()
        {
            var uiRoot = GameObject.Find("Exam Cursor UI");
            if (uiRoot == null)
            {
                uiRoot = new GameObject("Exam Cursor UI");
            }

            var canvas = uiRoot.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = uiRoot.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            var scaler = uiRoot.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = uiRoot.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            if (uiRoot.GetComponent<GraphicRaycaster>() == null)
            {
                uiRoot.AddComponent<GraphicRaycaster>();
            }

            return uiRoot;
        }

        // Creates the RawImage that visually replaces Unity's native cursor texture.
        private void EnsureCursorImage(Transform parent)
        {
            var cursorTransform = parent.Find("Exam Cursor Image");
            var cursorObject = cursorTransform == null ? new GameObject("Exam Cursor Image") : cursorTransform.gameObject;
            cursorObject.transform.SetParent(parent, false);
            cursorObject.transform.SetAsLastSibling();

            cursorImageRect = cursorObject.GetComponent<RectTransform>();
            if (cursorImageRect == null)
            {
                cursorImageRect = cursorObject.AddComponent<RectTransform>();
            }

            cursorImageRect.anchorMin = new Vector2(0.5f, 0.5f);
            cursorImageRect.anchorMax = new Vector2(0.5f, 0.5f);
            cursorImageRect.pivot = new Vector2(0.5f, 0.5f);
            cursorImageRect.sizeDelta = Vector2.one * cursorImageSize;

            cursorImage = cursorObject.GetComponent<RawImage>();
            if (cursorImage == null)
            {
                cursorImage = cursorObject.AddComponent<RawImage>();
            }

            cursorImage.color = Color.white;
            cursorImage.raycastTarget = false;
        }

        // Creates the two RawImage layers used for the idle ring and progress arc.
        private void EnsureObserveHoldIndicator(Transform parent)
        {
            var ringTransform = parent.Find("Exam Observe Cursor Ring");
            var ringObject = ringTransform == null ? new GameObject("Exam Observe Cursor Ring") : ringTransform.gameObject;
            ringObject.transform.SetParent(parent, false);
            ringObject.transform.SetAsLastSibling();

            observeHoldRingRect = ringObject.GetComponent<RectTransform>();
            if (observeHoldRingRect == null)
            {
                observeHoldRingRect = ringObject.AddComponent<RectTransform>();
            }

            observeHoldRingRect.anchorMin = new Vector2(0.5f, 0.5f);
            observeHoldRingRect.anchorMax = new Vector2(0.5f, 0.5f);
            observeHoldRingRect.pivot = new Vector2(0.5f, 0.5f);

            observeHoldIdleRingImage = FindOrCreateRingImage(ringObject.transform, "Idle Ring");
            observeHoldProgressRingImage = FindOrCreateRingImage(ringObject.transform, "Progress Ring");
        }

        // Finds or creates a RawImage child for one ring layer.
        private static RawImage FindOrCreateRingImage(Transform parent, string objectName)
        {
            var imageTransform = parent.Find(objectName);
            var imageObject = imageTransform == null ? new GameObject(objectName) : imageTransform.gameObject;
            imageObject.transform.SetParent(parent, false);

            var rect = imageObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = imageObject.AddComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            var image = imageObject.GetComponent<RawImage>();
            if (image == null)
            {
                image = imageObject.AddComponent<RawImage>();
            }

            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        // Applies the current cursor artwork through UI, including the default cursor shown at startup.
        private void ApplyCursorImage(Texture2D texture)
        {
            if (cursorImage == null || cursorImageRect == null)
            {
                CaveheartCursorVisibilityGuard.SetOverrideActive(this, true);
                return;
            }

            cursorImage.texture = texture;
            cursorImageRect.sizeDelta = Vector2.one * cursorImageSize;
            if (texture != null)
            {
                cursorImageRect.SetAsLastSibling();
            }

            UpdateCursorImageVisibility();
        }

        // Resolves the artwork for default, hover hand, and active grab states.
        private Texture2D CursorTexture(ExamCursorMode mode)
        {
            switch (mode)
            {
                case ExamCursorMode.Hand:
                    return handCursor = ResolveCursorTexture(handCursor, "cursor_touch_64");
                case ExamCursorMode.Grab:
                    return grabCursor = ResolveCursorTexture(grabCursor, "cursor_tickled_64");
                default:
                    return defaultCursor = ResolveCursorTexture(defaultCursor, "cursor_default_flint_64");
            }
        }

        // Loads cursor art as a regular UI texture, avoiding Cursor.SetCursor texture restrictions.
        private static Texture2D ResolveCursorTexture(Texture2D assignedTexture, string assetName)
        {
            if (assignedTexture != null)
            {
                return assignedTexture;
            }

            var texture = Resources.Load<Texture2D>(CursorResourceRoot + assetName);
            if (texture != null)
            {
                return texture;
            }

            var sprite = Resources.Load<Sprite>(CursorResourceRoot + assetName);
            return sprite == null ? null : sprite.texture;
        }

        // Allocates ring textures only when the required size changes.
        private void EnsureObserveHoldTextures(int size)
        {
            size = Mathf.Clamp(size, 16, 512);
            if (observeHoldTextureSize == size && observeHoldIdleTexture != null && observeHoldProgressTexture != null)
            {
                return;
            }

            observeHoldTextureSize = size;
            observeHoldIdleTexture = CreateRingTexture(size, "Exam Observe Hold Idle Ring");
            observeHoldProgressTexture = CreateRingTexture(size, "Exam Observe Hold Progress Ring");
            observeHoldIdleRingImage.texture = observeHoldIdleTexture;
            observeHoldProgressRingImage.texture = observeHoldProgressTexture;
        }

        // Creates an editable runtime texture for one ring layer.
        private static Texture2D CreateRingTexture(int size, string textureName)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = textureName;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }

        // Redraws a plain progress arc without tinting the cursor icon artwork.
        private void DrawRingTexture(Texture2D texture, float amount, Color ringColor)
        {
            if (texture == null)
            {
                return;
            }

            var size = texture.width;
            var pixels = new Color32[size * size];
            var center = (size - 1) * 0.5f;
            var outerRadius = observeHoldRingRadius + observeHoldRingWidth * 0.5f;
            var innerRadius = Mathf.Max(0f, observeHoldRingRadius - observeHoldRingWidth * 0.5f);
            var progressDegrees = 360f * Mathf.Clamp01(amount);
            var feather = 1.25f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var ringAlpha = Mathf.Clamp01((distance - innerRadius) / feather)
                        * Mathf.Clamp01((outerRadius - distance) / feather);
                    if (ringAlpha <= 0f || progressDegrees <= 0f)
                    {
                        pixels[y * size + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    var angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    var clockwiseFromTop = Mathf.Repeat(90f - angle, 360f);
                    if (clockwiseFromTop > progressDegrees)
                    {
                        pixels[y * size + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    var color = ringColor;
                    color.a *= ringAlpha;
                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        // Keeps the ring centered around the cursor and inside the screen-space canvas.
        private void PositionObserveHoldIndicator()
        {
            var canvasRect = observeHoldRingRect.parent as RectTransform;
            if (canvasRect == null
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    Input.mousePosition,
                    null,
                    out var localPoint))
            {
                return;
            }

            var halfSize = observeHoldRingRect.sizeDelta * 0.5f;
            var canvasBounds = canvasRect.rect;
            localPoint += observeHoldRingOffset;
            localPoint.x = Mathf.Clamp(localPoint.x, canvasBounds.xMin + halfSize.x, canvasBounds.xMax - halfSize.x);
            localPoint.y = Mathf.Clamp(localPoint.y, canvasBounds.yMin + halfSize.y, canvasBounds.yMax - halfSize.y);
            observeHoldRingRect.anchoredPosition = localPoint;
        }

        // Keeps the UI cursor within the overlay canvas while tracking the real pointer.
        private void PositionCursorImage()
        {
            if (cursorImageRect == null
                || cursorImage == null
                || !cursorImage.enabled
                || !cursorImageRect.gameObject.activeInHierarchy)
            {
                return;
            }

            var canvasRect = cursorImageRect.parent as RectTransform;
            if (canvasRect == null
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    Input.mousePosition,
                    null,
                    out var localPoint))
            {
                return;
            }

            var halfSize = cursorImageRect.sizeDelta * 0.5f;
            var canvasBounds = canvasRect.rect;
            localPoint += cursorImageOffset;
            localPoint.x = Mathf.Clamp(localPoint.x, canvasBounds.xMin + halfSize.x, canvasBounds.xMax - halfSize.x);
            localPoint.y = Mathf.Clamp(localPoint.y, canvasBounds.yMin + halfSize.y, canvasBounds.yMax - halfSize.y);
            cursorImageRect.anchoredPosition = localPoint;
            cursorImageRect.SetAsLastSibling();
        }

        // Shows the UI cursor only while the pointer is inside the game window.
        private void UpdateCursorImageVisibility()
        {
            if (cursorImage == null)
            {
                return;
            }

            var visible = cursorImage.texture != null && CaveheartCursorVisibilityGuard.ShouldShowUiCursor(this);
            cursorImage.enabled = visible;
            cursorImage.gameObject.SetActive(visible);
        }

        // Hides the UI cursor locally without changing native cursor ownership.
        private void HideCursorImage()
        {
            if (cursorImage == null)
            {
                return;
            }

            cursorImage.enabled = false;
            cursorImage.gameObject.SetActive(false);
        }
    }
}

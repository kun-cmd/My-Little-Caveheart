using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MyLittleCaveheart
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class CaveheartActionButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        private static Sprite triangleSprite;

        [SerializeField] private Image background;
        [SerializeField] private Text label;
        [SerializeField] private CaveheartInteractionType interactionType;
        [SerializeField] private Color normalTextColor = new Color32(235, 239, 229, 230);
        [SerializeField] private Color selectedTextColor = Color.white;
        [SerializeField] private Color normalBackgroundColor = new Color(0.12f, 0.15f, 0.18f, 0.82f);
        [SerializeField] private Color selectedBackgroundColor = new Color(0.18f, 0.24f, 0.23f, 0.9f);
        [SerializeField] private Color markerColor = new Color32(255, 197, 100, 230);
        [SerializeField] private Vector2 markerSize = new Vector2(28f, 28f);

        private RectTransform rectTransform;
        private RectTransform labelRect;
        private RectTransform markerLeft;
        private RectTransform markerRight;
        private Vector2 baseButtonPosition;
        private Vector3 baseLabelPosition;
        private Vector2 smoothedMouseOffset;
        private Vector2 mouseOffsetVelocity;
        private bool hasBasePositions;
        private bool selected;
        private bool pressed;

        public void Configure(Image targetBackground, Text targetLabel, CaveheartInteractionType targetInteractionType)
        {
            background = targetBackground;
            label = targetLabel;
            interactionType = targetInteractionType;
            CacheBasePositions();
            EnsureMarkers();
            ApplyImmediate();
        }

        private void Awake()
        {
            CacheBasePositions();
            EnsureMarkers();
        }

        private void OnEnable()
        {
            CacheBasePositions();
            EnsureMarkers();
            ApplyImmediate();
        }

        private void OnDisable()
        {
            pressed = false;
            selected = false;
            smoothedMouseOffset = Vector2.zero;
            mouseOffsetVelocity = Vector2.zero;

            if (rectTransform != null && hasBasePositions)
            {
                rectTransform.anchoredPosition = baseButtonPosition;
            }

            if (labelRect != null && hasBasePositions)
            {
                labelRect.anchoredPosition3D = baseLabelPosition;
                labelRect.localRotation = Quaternion.identity;
            }
        }

        private void Update()
        {
            ApplyAnimated();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            selected = true;
            EventSystem.current?.SetSelectedGameObject(gameObject);
            FlashLinkedSceneObject();
            ApplyImmediate();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
            ApplyImmediate();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            selected = true;
            pressed = true;
            ApplyImmediate();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressed = false;
            ApplyImmediate();
        }

        public void OnSelect(BaseEventData eventData)
        {
            selected = true;
            ApplyImmediate();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            selected = false;
            pressed = false;
            ApplyImmediate();
        }

        private void CacheBasePositions()
        {
            rectTransform = transform as RectTransform;

            if (label == null)
            {
                label = GetComponentInChildren<Text>(true);
            }

            if (label != null)
            {
                labelRect = label.rectTransform;
            }

            if (background == null)
            {
                background = GetComponent<Image>();
            }

            if (!hasBasePositions && rectTransform != null)
            {
                baseButtonPosition = rectTransform.anchoredPosition;
                baseLabelPosition = labelRect != null ? labelRect.anchoredPosition3D : Vector3.zero;
                hasBasePositions = true;
            }
        }

        private void ApplyImmediate()
        {
            if (background != null)
            {
                background.color = selected ? selectedBackgroundColor : normalBackgroundColor;
            }

            if (label != null)
            {
                label.color = selected ? selectedTextColor : normalTextColor;
            }

            if (markerLeft != null)
            {
                markerLeft.gameObject.SetActive(selected);
            }

            if (markerRight != null)
            {
                markerRight.gameObject.SetActive(selected);
            }
        }

        private void ApplyAnimated()
        {
            CacheBasePositions();

            if (rectTransform == null || !hasBasePositions)
            {
                return;
            }

            var pressOffset = pressed ? new Vector2(0f, -2f) : Vector2.zero;
            rectTransform.anchoredPosition = new Vector2(
                Mathf.Round(baseButtonPosition.x + pressOffset.x),
                Mathf.Round(baseButtonPosition.y + pressOffset.y));

            if (labelRect == null)
            {
                return;
            }

            var deltaTime = Time.unscaledDeltaTime > 0f ? Time.unscaledDeltaTime : 0.016f;
            var targetMouseOffset = selected ? GetMouseTextOffset() : Vector2.zero;
            smoothedMouseOffset = Vector2.SmoothDamp(smoothedMouseOffset, targetMouseOffset, ref mouseOffsetVelocity, 0.16f, 140f, deltaTime);

            var idleStrength = selected ? 1f : 0.35f;
            var idleOffset = new Vector2(
                Mathf.Sin(Time.unscaledTime * 1.4f + baseButtonPosition.x * 0.013f) * 3.2f * idleStrength,
                Mathf.Cos(Time.unscaledTime * 1.1f + baseButtonPosition.y * 0.017f) * 2.2f * idleStrength);
            var totalOffset = idleOffset + smoothedMouseOffset;

            var targetPosition = baseLabelPosition + new Vector3(totalOffset.x, totalOffset.y, selected ? -2f : 0f);
            labelRect.anchoredPosition3D = Vector3.Lerp(labelRect.anchoredPosition3D, targetPosition, 0.28f);

            var tiltX = Mathf.Clamp(-totalOffset.y * 0.7f, -9f, 9f);
            var tiltY = Mathf.Clamp(totalOffset.x * 0.72f, -12f, 12f);
            labelRect.localRotation = Quaternion.Lerp(labelRect.localRotation, Quaternion.Euler(tiltX, tiltY, 0f), 0.32f);
        }

        private Vector2 GetMouseTextOffset()
        {
            var canvas = rectTransform.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var buttonCenter = RectTransformUtility.WorldToScreenPoint(camera, rectTransform.TransformPoint(rectTransform.rect.center));
            var delta = (Vector2)Input.mousePosition - buttonCenter;
            var distance = delta.magnitude;
            const float radius = 260f;
            if (distance <= 0.01f || distance > radius)
            {
                return Vector2.zero;
            }

            var pull = Mathf.Pow(1f - distance / radius, 1.2f);
            return delta.normalized * (pull * 15f);
        }

        private void EnsureMarkers()
        {
            EnsureTriangleSprite();
            markerLeft = EnsureMarker("__ActionMarkerL", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-8f, 0f), 0f);
            markerRight = EnsureMarker("__ActionMarkerR", new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), 180f);
        }

        private void FlashLinkedSceneObject()
        {
            if (interactionType == CaveheartInteractionType.Wait)
            {
                return;
            }

            var clickables = FindObjectsOfType<CaveheartClickable>(true);
            for (var i = 0; i < clickables.Length; i++)
            {
                var clickable = clickables[i];
                if (clickable == null || clickable.InteractionType != interactionType)
                {
                    continue;
                }

                var renderer = clickable.TargetRenderer;
                if (renderer == null || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                CaveheartClickFeedback.GetOrCreateActive().Play(renderer);
                return;
            }
        }

        private RectTransform EnsureMarker(string markerName, Vector2 anchor, Vector2 pivot, Vector2 position, float rotationZ)
        {
            var marker = transform.Find(markerName) as RectTransform;
            if (marker == null)
            {
                var markerObject = new GameObject(markerName, typeof(RectTransform), typeof(Image));
                markerObject.transform.SetParent(transform, false);
                marker = markerObject.GetComponent<RectTransform>();
            }

            marker.anchorMin = anchor;
            marker.anchorMax = anchor;
            marker.pivot = pivot;
            marker.sizeDelta = markerSize;
            marker.anchoredPosition = position;
            marker.localScale = Vector3.one;
            marker.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            marker.SetAsFirstSibling();

            var markerImage = marker.GetComponent<Image>();
            markerImage.sprite = triangleSprite;
            markerImage.color = markerColor;
            markerImage.raycastTarget = false;
            markerImage.preserveAspect = true;
            marker.gameObject.SetActive(false);
            return marker;
        }

        private static void EnsureTriangleSprite()
        {
            if (triangleSprite != null)
            {
                return;
            }

            const int size = 32;
            const int samples = 4;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var a = new Vector2(8f, 6f);
            var b = new Vector2(8f, 26f);
            var c = new Vector2(25f, 16f);
            var pixels = new Color32[size * size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var covered = 0;
                    for (var sy = 0; sy < samples; sy++)
                    {
                        for (var sx = 0; sx < samples; sx++)
                        {
                            var p = new Vector2(x + (sx + 0.5f) / samples, y + (sy + 0.5f) / samples);
                            if (IsInsideTriangle(p, a, b, c))
                            {
                                covered++;
                            }
                        }
                    }

                    var alpha = (byte)Mathf.RoundToInt(255f * covered / (samples * samples));
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            triangleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, 32f);
            triangleSprite.hideFlags = HideFlags.HideAndDontSave;
        }

        private static bool IsInsideTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            var d1 = Sign(p, a, b);
            var d2 = Sign(p, b, c);
            var d3 = Sign(p, c, a);
            var hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
            var hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNeg && hasPos);
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace MyLittleCaveheart
{
    [ExecuteAlways]
    public sealed class CaveheartSceneBootstrapper : MonoBehaviour
    {
        [SerializeField] private bool buildOnEnable = true;
        [SerializeField] private bool rebuildEveryPlay = true;

        private Sprite squareSprite;

        private void OnEnable()
        {
            if (!buildOnEnable)
            {
                return;
            }

            if (!Application.isPlaying && transform.childCount > 0)
            {
                return;
            }

            if (Application.isPlaying && rebuildEveryPlay)
            {
                Build();
                return;
            }

            if (transform.childCount == 0)
            {
                Build();
            }
        }

        [ContextMenu("Rebuild Whitebox Runtime Objects")]
        public void Build()
        {
            ClearChildren();
            squareSprite = CreateSquareSprite();
            var camera = EnsureMainCamera();

            var controller = CreateChild("GameController").AddComponent<CaveheartGameController>();
            var audioFeedback = controller.gameObject.AddComponent<CaveheartAudioFeedback>();
            audioFeedback.GetComponent<AudioSource>().playOnAwake = false;
            controller.gameObject.AddComponent<CaveheartEnvironmentFeedback>();
            CreateChild("ClickFeedback").AddComponent<CaveheartClickFeedback>();

            CreateEnvironment();
            var characterRoot = CreateCharacter(out var body, out var blanket, out var face, out var leftArm, out var rightArm, out var tear);
            var coldOverlay = CreateSprite("Cold Stress Overlay", new Vector3(0f, 0f, 5f), new Vector3(24f, 14f, 1f), new Color(0.22f, 0.35f, 0.56f, 0.12f), 20);
            var warmLight = CreateSprite("Warm Morning Light", new Vector3(4.6f, 2.2f, 4.5f), new Vector3(5.5f, 8f, 1f), new Color(1f, 0.8f, 0.42f, 0.18f), 19);
            warmLight.transform.rotation = Quaternion.Euler(0f, 0f, -22f);
            var stressPulse = CreateSprite("Stress Pulse Overlay", new Vector3(0f, 0f, 4.8f), new Vector3(24f, 14f, 1f), new Color(1f, 0.25f, 0.18f, 0f), 21);

            CreateInteractable(controller, CaveheartInteractionType.Alarm, "Alarm", new Vector3(-6.1f, -0.9f, 0f), new Vector2(1.15f, 1.15f), new Color(0.95f, 0.23f, 0.23f));
            CreateInteractable(controller, CaveheartInteractionType.GentleTouch, "Gentle Touch", new Vector3(0.1f, 0.55f, 0f), new Vector2(2.0f, 0.75f), new Color(0.9f, 0.62f, 0.52f));
            CreateInteractable(controller, CaveheartInteractionType.OfferWater, "Water", new Vector3(5.55f, -1.1f, 0f), new Vector2(1.0f, 1.25f), new Color(0.34f, 0.72f, 0.9f));
            CreateInteractable(controller, CaveheartInteractionType.OpenCurtain, "Curtain", new Vector3(5.9f, 1.8f, 0f), new Vector2(1.4f, 3.4f), new Color(0.95f, 0.79f, 0.28f));

            CreateUi(camera, out var stateText, out var reactionText, out var timeText, out var endingText, out var debugText, out var whiteboxValuesText);
            var presenter = controller.gameObject.AddComponent<CaveheartWhiteboxPresenter>();
            presenter.Configure(controller, body, blanket, face, leftArm, rightArm, tear, coldOverlay, warmLight, stressPulse, characterRoot.transform, stateText, reactionText, timeText, endingText, debugText, whiteboxValuesText);
        }

        private Camera EnsureMainCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = GameObject.Find("Main Camera");
                if (cameraObject == null)
                {
                    cameraObject = new GameObject("Main Camera");
                }

                cameraObject.tag = "MainCamera";
                camera = cameraObject.GetComponent<Camera>();
                if (camera == null)
                {
                    camera = cameraObject.AddComponent<Camera>();
                }

                cameraObject.transform.position = new Vector3(0f, 0f, -10f);
                cameraObject.transform.rotation = Quaternion.identity;
            }

            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.backgroundColor = new Color(0.09f, 0.085f, 0.08f);

            if (camera.GetComponent<AudioListener>() == null)
            {
                camera.gameObject.AddComponent<AudioListener>();
            }

            return camera;
        }

        private void CreateEnvironment()
        {
            CreateSprite("Cave Bedroom Back Wall", new Vector3(0f, 0f, 2.5f), new Vector3(18f, 10f, 1f), new Color(0.18f, 0.16f, 0.14f), -10);
            CreateSprite("Floor", new Vector3(0f, -3.75f, 1f), new Vector3(18f, 2f, 1f), new Color(0.25f, 0.2f, 0.16f), -8);
            CreateSprite("Bed Base", new Vector3(0f, -2.15f, 0f), new Vector3(5.4f, 0.85f, 1f), new Color(0.35f, 0.2f, 0.12f), -2);
            CreateSprite("Window", new Vector3(5.9f, 1.8f, 0f), new Vector3(1.7f, 3.7f, 1f), new Color(0.13f, 0.18f, 0.22f), -4);
            CreateSprite("Window Glow", new Vector3(5.9f, 1.8f, -0.1f), new Vector3(1.25f, 3.25f, 1f), new Color(0.98f, 0.8f, 0.42f, 0.25f), -3);
        }

        private GameObject CreateCharacter(out SpriteRenderer body, out SpriteRenderer blanket, out SpriteRenderer face, out SpriteRenderer leftArm, out SpriteRenderer rightArm, out SpriteRenderer tear)
        {
            var root = CreateChild("Little Caveheart Character");
            root.transform.position = new Vector3(0f, -0.4f, -0.2f);

            body = CreateSprite("Body", new Vector3(0f, -0.15f, 0f), new Vector3(1.25f, 0.88f, 1f), new Color(0.93f, 0.72f, 0.48f), 3, root.transform);
            face = CreateSprite("Face Mood Mark", new Vector3(0.22f, 0.1f, -0.02f), new Vector3(0.45f, 0.18f, 1f), new Color(0.12f, 0.08f, 0.07f), 4, root.transform);
            leftArm = CreateSprite("Left Arm", new Vector3(-0.8f, -0.08f, -0.03f), new Vector3(0.85f, 0.18f, 1f), new Color(0.86f, 0.61f, 0.42f), 4, root.transform);
            rightArm = CreateSprite("Right Arm", new Vector3(0.8f, -0.08f, -0.03f), new Vector3(0.85f, 0.18f, 1f), new Color(0.86f, 0.61f, 0.42f), 4, root.transform);
            tear = CreateSprite("Tear Marker", new Vector3(0.56f, -0.05f, -0.04f), new Vector3(0.12f, 0.35f, 1f), new Color(0.38f, 0.72f, 1f), 5, root.transform);
            blanket = CreateSprite("Blanket", new Vector3(0.1f, -0.18f, -0.05f), new Vector3(1.95f, 1.05f, 1f), new Color(0.47f, 0.66f, 0.9f), 6, root.transform);
            return root;
        }

        private void CreateInteractable(CaveheartGameController controller, CaveheartInteractionType type, string label, Vector3 position, Vector2 size, Color color)
        {
            var item = CreateSprite($"Interactable - {label}", position, new Vector3(size.x, size.y, 1f), color, 10).gameObject;
            item.AddComponent<BoxCollider2D>().size = Vector2.one;
            item.AddComponent<CaveheartClickable>().Configure(controller, type);
            CreateWorldLabel(label, position + new Vector3(0f, -size.y * 0.62f, -0.1f), item.transform);
        }

        private SpriteRenderer CreateSprite(string name, Vector3 position, Vector3 scale, Color color, int sortingOrder, Transform parent = null)
        {
            var obj = CreateChild(name, parent);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private void CreateWorldLabel(string text, Vector3 position, Transform parent)
        {
            var label = CreateChild("Label", parent);
            label.transform.position = position;
            var mesh = label.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = 0.28f;
            mesh.fontSize = 42;
            mesh.color = Color.white;
            label.GetComponent<MeshRenderer>().sortingOrder = 30;
        }

        private void CreateUi(Camera camera, out Text stateText, out Text reactionText, out Text timeText, out Text endingText, out Text debugText, out Text whiteboxValuesText)
        {
            var canvasObject = CreateChild("Whitebox UI");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1280f, 720f);
            canvasObject.AddComponent<GraphicRaycaster>();

            stateText = CreateUiText("State Text", canvasObject.transform, new Vector2(24f, -24f), new Vector2(360f, 48f), TextAnchor.UpperLeft, 24, "State: Sleeping");
            reactionText = CreateUiText("Reaction Text", canvasObject.transform, new Vector2(24f, -76f), new Vector2(660f, 150f), TextAnchor.UpperLeft, 21, "Last: none\nClick an object to test an interaction.");
            timeText = CreateUiText("Time Text", canvasObject.transform, new Vector2(-24f, -24f), new Vector2(220f, 44f), TextAnchor.UpperRight, 22, "Time 00:00");
            whiteboxValuesText = CreateUiText("Whitebox Values Text", canvasObject.transform, new Vector2(-24f, -72f), new Vector2(240f, 120f), TextAnchor.UpperRight, 20, "WHITEBOX VALUES\nAwake  0\nTrust  1\nStress 1");
            debugText = CreateUiText("Debug Text Toggle Mirror", canvasObject.transform, new Vector2(-24f, -200f), new Vector2(240f, 110f), TextAnchor.UpperRight, 16, string.Empty);
            debugText.gameObject.SetActive(true);
            endingText = CreateUiText("Ending Text", canvasObject.transform, Vector2.zero, new Vector2(760f, 160f), TextAnchor.MiddleCenter, 26, "Thank you... this way I am less afraid.\n\nSometimes, slower is not failure.\nIt is standing with yourself again.");
            endingText.gameObject.SetActive(false);
        }

        private Text CreateUiText(string name, Transform parent, Vector2 anchoredPosition, Vector2 sizeDelta, TextAnchor anchor, int fontSize, string text)
        {
            var obj = CreateChild(name, parent);
            var rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
            if (anchor == TextAnchor.UpperLeft)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
            }
            else if (anchor == TextAnchor.UpperRight)
            {
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
            }

            var uiText = obj.AddComponent<Text>();
            uiText.text = text;
            uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            uiText.fontSize = fontSize;
            uiText.alignment = anchor;
            uiText.color = Color.white;
            uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
            uiText.verticalOverflow = VerticalWrapMode.Overflow;
            return uiText;
        }

        private GameObject CreateChild(string name, Transform parent = null)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent == null ? transform : parent, false);
            return obj;
        }

        private void ClearChildren()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                DestroyImmediate(child);
            }
        }

        private static Sprite CreateSquareSprite()
        {
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            var pixels = new Color[16 * 16];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 16f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}

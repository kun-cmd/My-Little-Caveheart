using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MyLittleCaveheart
{
    public sealed class ExamEndScreenPresenter : MonoBehaviour
    {
        private const string IconRoot = "Sprites/Caveheart/UI/ExamEnd/";

        [Header("Scene Flow")]
        [SerializeField] private string menuSceneName;

        [Header("Copy")]
        [SerializeField] private string victoryTitle = "I can stay with it.";
        [SerializeField, TextArea] private string victoryBody = "One question at a time.";
        [SerializeField] private string failureTitle = "The room got too loud.";
        [SerializeField, TextArea] private string failureBody = "Try staying with one thought first.";

        [Header("Artwork")]
        [SerializeField] private Sprite victoryIcon;
        [SerializeField] private Sprite failureIcon;
        [SerializeField] private Sprite retryIcon;
        [SerializeField] private Sprite menuIcon;

        [Header("Colors")]
        [SerializeField] private Color victoryPanelColor = new Color(0.15f, 0.18f, 0.17f, 0.88f);
        [SerializeField] private Color failurePanelColor = new Color(0.08f, 0.11f, 0.14f, 0.9f);
        [SerializeField] private Color titleColor = new Color(0.93f, 0.95f, 0.92f, 1f);
        [SerializeField] private Color bodyColor = new Color(0.78f, 0.83f, 0.86f, 1f);
        [SerializeField] private Color buttonColor = new Color(0.22f, 0.28f, 0.29f, 0.95f);
        [SerializeField] private Color buttonHoverColor = new Color(0.31f, 0.38f, 0.37f, 0.98f);

        [Header("Runtime References")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image panelImage;
        [SerializeField] private Image stateIconImage;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Button primaryButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Text primaryButtonText;
        [SerializeField] private Text retryButtonText;
        [SerializeField] private Text menuButtonText;
        [SerializeField] private Image primaryButtonIcon;
        [SerializeField] private Image retryButtonIcon;
        [SerializeField] private Image menuButtonIcon;

        private bool built;

        // Builds the runtime UI once and starts hidden so the level opens directly on the scene.
        private void Awake()
        {
            EnsureBuilt();
            HideImmediate();
        }

        // Keeps authored and generated UI text on the same Short Stack font in the editor.
        private void OnValidate()
        {
            ApplyTypography();
        }

        // Shows the grounded final-level ending with only Retry and Menu choices.
        public void ShowVictory(string overrideBody)
        {
            EnsureBuilt();
            Show(
                victoryPanelColor,
                victoryIcon != null ? victoryIcon : LoadIcon("exam_end_victory_icon"),
                victoryTitle,
                string.IsNullOrWhiteSpace(overrideBody) ? victoryBody : overrideBody,
                "Retry",
                retryIcon != null ? retryIcon : LoadIcon("exam_end_retry_icon"),
                ReloadCurrentScene);
            retryButton.gameObject.SetActive(false);
            menuButton.gameObject.SetActive(true);
        }

        // Shows the overwhelmed timeout ending and makes retry the main action.
        public void ShowFailure(string overrideBody)
        {
            EnsureBuilt();
            Show(
                failurePanelColor,
                failureIcon != null ? failureIcon : LoadIcon("exam_end_failure_icon"),
                failureTitle,
                string.IsNullOrWhiteSpace(overrideBody) ? failureBody : overrideBody,
                "Try Again",
                retryIcon != null ? retryIcon : LoadIcon("exam_end_retry_icon"),
                ReloadCurrentScene);
            retryButton.gameObject.SetActive(false);
            menuButton.gameObject.SetActive(true);
        }

        // Hides the whole ending layer immediately when a fresh run starts.
        public void HideImmediate()
        {
            EnsureBuilt();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvas.gameObject.SetActive(false);
        }

        // Creates the screen-space canvas, text, icon, and button hierarchy when the scene has not authored one.
        private void EnsureBuilt()
        {
            if (built)
            {
                return;
            }

            built = true;
            LoadMissingIcons();
            canvas = canvas != null ? canvas : CreateCanvas();
            canvasGroup = canvasGroup != null ? canvasGroup : GetOrAdd<CanvasGroup>(canvas.gameObject);
            CreatePanel(canvas.transform);
            ApplyTypography();
            EnsureEventSystem();
        }

        // Loads default generated art from Resources while still allowing Inspector overrides.
        private void LoadMissingIcons()
        {
            victoryIcon = victoryIcon != null ? victoryIcon : LoadIcon("exam_end_victory_icon");
            failureIcon = failureIcon != null ? failureIcon : LoadIcon("exam_end_failure_icon");
            retryIcon = retryIcon != null ? retryIcon : LoadIcon("exam_end_retry_icon");
            menuIcon = menuIcon != null ? menuIcon : LoadIcon("exam_end_menu_icon");
        }

        // Creates an overlay canvas that does not require a separate scene object.
        private static Canvas CreateCanvas()
        {
            var root = new GameObject("Exam End Screen");
            var createdCanvas = root.AddComponent<Canvas>();
            createdCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            createdCanvas.sortingOrder = 400;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();
            return createdCanvas;
        }

        // Builds the translucent in-scene ending panel without nesting decorative cards.
        private void CreatePanel(Transform parent)
        {
            var dimmer = CreateImage(parent, "Dimmer", new Color(0f, 0f, 0f, 0.26f));
            Stretch(dimmer.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            panelImage = CreateImage(parent, "Ending Panel", victoryPanelColor);
            var panelRect = panelImage.rectTransform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(720f, 420f);
            panelRect.anchoredPosition = new Vector2(0f, 20f);

            stateIconImage = CreateImage(panelRect, "State Icon", victoryIcon);
            stateIconImage.rectTransform.sizeDelta = new Vector2(104f, 104f);
            stateIconImage.rectTransform.anchoredPosition = new Vector2(0f, 132f);

            titleText = CreateText(panelRect, "Title", 52, titleColor, TextAnchor.MiddleCenter);
            titleText.rectTransform.sizeDelta = new Vector2(620f, 74f);
            titleText.rectTransform.anchoredPosition = new Vector2(0f, 52f);

            bodyText = CreateText(panelRect, "Body", 31, bodyColor, TextAnchor.UpperCenter);
            bodyText.rectTransform.sizeDelta = new Vector2(610f, 110f);
            bodyText.rectTransform.anchoredPosition = new Vector2(0f, -36f);

            var buttonRow = CreateLayout(panelRect, "Button Row");
            var buttonRowRect = buttonRow.GetComponent<RectTransform>();
            buttonRowRect.sizeDelta = new Vector2(620f, 78f);
            buttonRowRect.anchoredPosition = new Vector2(0f, -146f);

            primaryButton = CreateButton(buttonRow.transform, "Primary Button", out primaryButtonText, out primaryButtonIcon);
            retryButton = CreateButton(buttonRow.transform, "Retry Button", out retryButtonText, out retryButtonIcon);
            menuButton = CreateButton(buttonRow.transform, "Menu Button", out menuButtonText, out menuButtonIcon);
            retryButtonText.text = "Retry";
            menuButtonText.text = "Menu";
            SetButtonIcon(retryButtonIcon, retryIcon);
            SetButtonIcon(menuButtonIcon, menuIcon);
            retryButton.onClick.AddListener(ReloadCurrentScene);
            menuButton.onClick.AddListener(LoadMenuScene);
        }

        // Applies the current ending content, button callback, and button visibility.
        private void Show(Color panelColor, Sprite stateIcon, string title, string body, string primaryLabel, Sprite primaryIcon, UnityEngine.Events.UnityAction primaryAction)
        {
            canvas.gameObject.SetActive(true);
            panelImage.color = panelColor;
            stateIconImage.sprite = stateIcon;
            stateIconImage.enabled = stateIcon != null;
            titleText.text = title;
            bodyText.text = body;
            primaryButtonText.text = primaryLabel;
            SetButtonIcon(primaryButtonIcon, primaryIcon);
            SetButtonIcon(retryButtonIcon, retryIcon != null ? retryIcon : LoadIcon("exam_end_retry_icon"));
            SetButtonIcon(menuButtonIcon, menuIcon != null ? menuIcon : LoadIcon("exam_end_menu_icon"));
            primaryButton.onClick.RemoveAllListeners();
            primaryButton.onClick.AddListener(primaryAction);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        // Assigns a button icon and explicitly enables the Image after it was created from a null sprite.
        private static void SetButtonIcon(Image targetImage, Sprite targetSprite)
        {
            if (targetImage == null)
            {
                return;
            }

            targetImage.sprite = targetSprite;
            targetImage.enabled = targetSprite != null;
        }

        // Reloads the active scene for the Try Again and Retry actions.
        private void ReloadCurrentScene()
        {
            var activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.name);
        }

        // Loads the configured menu scene, or reloads the current scene while the project has no menu assigned.
        private void LoadMenuScene()
        {
            if (string.IsNullOrWhiteSpace(menuSceneName))
            {
                Debug.Log("[Level3 Ending] Menu scene name is empty on ExamEndScreenPresenter.", this);
                ReloadCurrentScene();
                return;
            }

            SceneManager.LoadScene(menuSceneName);
        }

        // Assigns the shared storybook font to every UI text field managed by the presenter.
        private void ApplyTypography()
        {
            CaveheartTypography.ApplyTo(titleText);
            CaveheartTypography.ApplyTo(bodyText);
            CaveheartTypography.ApplyTo(primaryButtonText);
            CaveheartTypography.ApplyTo(retryButtonText);
            CaveheartTypography.ApplyTo(menuButtonText);
        }

        // Creates an Image with either a flat color or a sprite.
        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var image = CreateUiObject<Image>(parent, name);
            image.color = color;
            return image;
        }

        // Creates an Image assigned to a sprite without changing the sprite's original colors.
        private static Image CreateImage(Transform parent, string name, Sprite sprite)
        {
            var image = CreateUiObject<Image>(parent, name);
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            image.enabled = sprite != null;
            return image;
        }

        // Creates a fixed-size UI text field using legacy UI Text for this Unity 2022 project.
        private static Text CreateText(Transform parent, string name, int fontSize, Color color, TextAnchor anchor)
        {
            var text = CreateUiObject<Text>(parent, name);
            text.fontSize = fontSize;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(12, fontSize - 12);
            text.resizeTextMaxSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            CaveheartTypography.ApplyTo(text);
            return text;
        }

        // Creates the horizontal row that keeps ending buttons evenly spaced.
        private static HorizontalLayoutGroup CreateLayout(Transform parent, string name)
        {
            var layout = CreateUiObject<HorizontalLayoutGroup>(parent, name);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.spacing = 18f;
            return layout;
        }

        // Creates an icon-and-label button with stable dimensions for all ending actions.
        private Button CreateButton(Transform parent, string name, out Text label, out Image icon)
        {
            var buttonImage = CreateUiObject<Image>(parent, name);
            buttonImage.color = buttonColor;

            var layoutElement = buttonImage.gameObject.AddComponent<LayoutElement>();
            layoutElement.minWidth = 180f;
            layoutElement.preferredWidth = 190f;
            layoutElement.minHeight = 70f;

            var button = buttonImage.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = buttonHoverColor;
            colors.pressedColor = new Color(buttonHoverColor.r * 0.82f, buttonHoverColor.g * 0.82f, buttonHoverColor.b * 0.82f, 1f);
            colors.selectedColor = buttonHoverColor;
            button.colors = colors;

            var row = CreateLayout(buttonImage.transform, "Content");
            row.padding = new RectOffset(18, 18, 8, 8);
            Stretch(row.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            icon = CreateImage(row.transform, "Icon", (Sprite)null);
            icon.rectTransform.sizeDelta = new Vector2(42f, 42f);
            var iconLayout = icon.gameObject.AddComponent<LayoutElement>();
            iconLayout.minWidth = 42f;
            iconLayout.preferredWidth = 42f;

            label = CreateText(row.transform, "Label", 27, titleColor, TextAnchor.MiddleLeft);
            var labelLayout = label.gameObject.AddComponent<LayoutElement>();
            labelLayout.minWidth = 80f;
            labelLayout.preferredWidth = 120f;

            return button;
        }

        // Creates a UI GameObject with a RectTransform and the requested component.
        private static T CreateUiObject<T>(Transform parent, string name) where T : Component
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj.AddComponent<T>();
        }

        // Stretches a RectTransform to the requested anchor box.
        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        // Adds or returns an existing component on one GameObject.
        private static T GetOrAdd<T>(GameObject obj) where T : Component
        {
            var existing = obj.GetComponent<T>();
            return existing != null ? existing : obj.AddComponent<T>();
        }

        // Loads a PNG from Resources and turns it into a Sprite if Unity has not imported it as one.
        private static Sprite LoadIcon(string resourceName)
        {
            var sprite = Resources.Load<Sprite>(IconRoot + resourceName);
            if (sprite != null)
            {
                return sprite;
            }

            var texture = Resources.Load<Texture2D>(IconRoot + resourceName);
            return texture == null
                ? null
                : Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 128f);
        }

        // Creates an EventSystem so dynamically generated buttons work in a minimal whitebox scene.
        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MyLittleCaveheart
{
    public sealed class CaveheartGameController : MonoBehaviour
    {
        private enum MorningOutcome
        {
            Bad,
            Good,
            VeryGood
        }

        [SerializeField] private CaveheartStats stats = CaveheartStats.Starting;
        [SerializeField] private CaveheartState currentState = CaveheartState.Sleeping;
        [SerializeField] private bool lockAfterEnding = true;
        [SerializeField] private bool logWhiteboxInteractions = true;
        [SerializeField] private bool showWhiteboxHud = true;
        [SerializeField] private int morningTimeLimitMinutes = 14;

        public event Action<CaveheartInteractionResult> InteractionResolved;
        public event Action<CaveheartState, CaveheartStats> StateChanged;

        private Text hudValuesText;
        private Text hudReactionText;
        private Text hudTimeText;
        private RectTransform hudAwakeFill;
        private RectTransform hudTrustFill;
        private RectTransform hudStressFill;
        private Text hudOutcomeText;
        private Image hudOutcomePanel;
        private float elapsedSeconds;
        private int usedMorningMinutes;
        private int blanketUseCount;
        private int gentleTouchUseCount;
        private int waterUseCount;
        private int curtainUseCount;
        private int scratchUseCount;
        private int forcefulUseCount;
        private bool hasAcceptedWater;
        private bool hasFailed;
        private CaveheartCharacterView characterView;
        private CaveheartInteractionType lastInteraction = CaveheartInteractionType.Wait;
        private int waitStreak;
        private bool lastInteractionWasForceful;
        private bool lastInteractionWasRejected;
        private CaveheartSpriteAnimator spriteAnimator;
        private CaveheartEnvironmentFeedback environmentFeedback;

        public CaveheartStats Stats => stats;
        public CaveheartState CurrentState => currentState;
        public int UsedMorningMinutes => usedMorningMinutes;
        public int MorningTimeLimitMinutes => morningTimeLimitMinutes;
        public int RemainingMorningMinutes => Mathf.Max(0, morningTimeLimitMinutes - usedMorningMinutes);
        public bool HasFailed => hasFailed;
        public bool IsEnded => currentState == CaveheartState.SittingUp || hasFailed;

        private void Start()
        {
            EnsureClickFeedbackObject();
            PruneRetiredInteractables();
            EnsureCharacterView();
            EnsureSpriteAnimator();
            EnsureEnvironmentFeedback();
            EnsureWhiteboxHud();
            PublishState();
            UpdateWhiteboxHudValues();
            characterView?.ApplyState(currentState, stats);
            spriteAnimator?.Play(currentState);
            UpdateOutcomeUi();
        }

        private void Update()
        {
            elapsedSeconds += Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.F3) || Input.inputString.ToLowerInvariant().Contains("d"))
            {
                showWhiteboxHud = !showWhiteboxHud;
                Debug.Log($"[Caveheart Whitebox] HUD visible: {showWhiteboxHud}", this);
            }

            UpdateWhiteboxHudValues();
        }

        public CaveheartInteractionResult Interact(CaveheartInteractionType interactionType)
        {
            if (lockAfterEnding && IsEnded)
            {
                var endedMessage = hasFailed ? "The morning window has closed." : "The morning has already become possible.";
                var endedReaction = hasFailed
                    ? "He stays curled up. There will need to be another gentler try."
                    : "He stays seated, moving at his own pace.";
                var ended = new CaveheartInteractionResult(
                    interactionType,
                    stats,
                    stats,
                    currentState,
                    false,
                    endedMessage,
                    endedReaction);
                InteractionResolved?.Invoke(ended);
                return ended;
            }

            var actionMinutes = CaveheartRules.GetTimeCost(interactionType);
            if (usedMorningMinutes + actionMinutes > morningTimeLimitMinutes)
            {
                usedMorningMinutes = morningTimeLimitMinutes;
                hasFailed = true;
                var timedOut = new CaveheartInteractionResult(
                    interactionType,
                    stats,
                    stats,
                    currentState,
                    false,
                    "There is not enough morning left for that.",
                    "The room goes quiet. He is still curled up, and the chance for this attempt passes.");
                LogWhiteboxInteraction(timedOut, currentState);
                UpdateWhiteboxHudReaction(timedOut);
                UpdateOutcomeUi();
                InteractionResolved?.Invoke(timedOut);
                StateChanged?.Invoke(currentState, stats);
                return timedOut;
            }

            var previousState = currentState;
            var context = new CaveheartInteractionContext(
                blanketUseCount,
                gentleTouchUseCount,
                waterUseCount,
                curtainUseCount,
                lastInteraction,
                waitStreak,
                lastInteractionWasForceful,
                lastInteractionWasRejected,
                scratchUseCount);
            var result = CaveheartRules.Apply(stats, currentState, interactionType, context);
            switch (interactionType)
            {
                case CaveheartInteractionType.TuckBlanket:
                    blanketUseCount++;
                    break;
                case CaveheartInteractionType.GentleTouch:
                    gentleTouchUseCount++;
                    break;
                case CaveheartInteractionType.OfferWater:
                    waterUseCount++;
                    if (result.accepted)
                    {
                        hasAcceptedWater = true;
                    }
                    break;
                case CaveheartInteractionType.OpenCurtain:
                    curtainUseCount++;
                    break;
                case CaveheartInteractionType.Scratch:
                    scratchUseCount++;
                    break;
            }

            stats = result.after;
            currentState = result.state;
            usedMorningMinutes += actionMinutes;
            if (interactionType == CaveheartInteractionType.Alarm || interactionType == CaveheartInteractionType.ShakeBed)
            {
                forcefulUseCount++;
            }

            if (currentState != CaveheartState.SittingUp && usedMorningMinutes >= morningTimeLimitMinutes)
            {
                hasFailed = true;
            }

            lastInteractionWasForceful = interactionType == CaveheartInteractionType.Alarm || interactionType == CaveheartInteractionType.ShakeBed;
            lastInteractionWasRejected = !result.accepted;
            waitStreak = interactionType == CaveheartInteractionType.Wait ? waitStreak + 1 : 0;
            lastInteraction = interactionType;
            LogWhiteboxInteraction(result, previousState);
            UpdateWhiteboxHudReaction(result);
            UpdateOutcomeUi();
            characterView?.ApplyReaction(result);
            spriteAnimator?.Play(currentState);
            InteractionResolved?.Invoke(result);

            if (currentState != previousState)
            {
                PublishState();
            }
            else
            {
                StateChanged?.Invoke(currentState, stats);
            }

            return result;
        }

        [ContextMenu("Reset Morning")]
        public void ResetMorning()
        {
            stats = CaveheartStats.Starting;
            currentState = CaveheartState.Sleeping;
            blanketUseCount = 0;
            gentleTouchUseCount = 0;
            waterUseCount = 0;
            curtainUseCount = 0;
            scratchUseCount = 0;
            forcefulUseCount = 0;
            usedMorningMinutes = 0;
            hasAcceptedWater = false;
            hasFailed = false;
            lastInteraction = CaveheartInteractionType.Wait;
            waitStreak = 0;
            lastInteractionWasForceful = false;
            lastInteractionWasRejected = false;
            PublishState();
            characterView?.SetHasAcceptedWater(hasAcceptedWater);
            characterView?.ApplyState(currentState, stats);
            spriteAnimator?.Play(currentState);
            UpdateOutcomeUi();
        }

        private void PublishState()
        {
            StateChanged?.Invoke(currentState, stats);
        }

        private void EnsureClickFeedbackObject()
        {
            if (Application.isPlaying)
            {
                CaveheartClickFeedback.GetOrCreateActive();
                return;
            }

            if (FindObjectOfType<CaveheartClickFeedback>() == null)
            {
                new GameObject("ClickFeedback").AddComponent<CaveheartClickFeedback>();
            }
        }

        private void PruneRetiredInteractables()
        {
            var clickables = FindObjectsOfType<CaveheartClickable>(true);
            for (var i = 0; i < clickables.Length; i++)
            {
                var clickable = clickables[i];
                if (clickable == null)
                {
                    continue;
                }

                if (clickable.InteractionType == CaveheartInteractionType.Scratch)
                {
                    clickable.gameObject.SetActive(false);
                }
                else if (clickable.InteractionType == CaveheartInteractionType.ShakeBed || clickable.InteractionType == CaveheartInteractionType.TuckBlanket)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(clickable.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(clickable.gameObject);
                    }
                }
            }
        }

        private static Sprite FindReusableWhiteboxSprite()
        {
            var renderers = FindObjectsOfType<SpriteRenderer>();
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].sprite != null)
                {
                    return renderers[i].sprite;
                }
            }

            return null;
        }

        private static Sprite CreateRuntimeSquareSprite()
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

        private static void CreateRuntimeWorldLabel(string text, Vector3 position, Transform parent)
        {
            var label = new GameObject("Label");
            label.transform.SetParent(parent, true);
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

        private void EnsureCharacterView()
        {
            characterView = GetComponent<CaveheartCharacterView>();
            if (characterView == null)
            {
                characterView = gameObject.AddComponent<CaveheartCharacterView>();
            }

            characterView.BindExistingScene();
            characterView.SetHasAcceptedWater(hasAcceptedWater);
        }

        private void EnsureSpriteAnimator()
        {
            spriteAnimator = GetComponent<CaveheartSpriteAnimator>();
            if (spriteAnimator == null)
            {
                spriteAnimator = gameObject.AddComponent<CaveheartSpriteAnimator>();
            }

            spriteAnimator.BindOrCreateRenderer();
            spriteAnimator.LoadClips();
            spriteAnimator.Play(currentState);
        }

        private void EnsureEnvironmentFeedback()
        {
            environmentFeedback = GetComponent<CaveheartEnvironmentFeedback>();
            if (environmentFeedback == null)
            {
                environmentFeedback = gameObject.AddComponent<CaveheartEnvironmentFeedback>();
            }
        }

        private void EnsureWhiteboxHud()
        {
            var uiRoot = GameObject.Find("Whitebox UI");
            if (uiRoot == null)
            {
                uiRoot = new GameObject("Whitebox UI");
            }

            var canvas = uiRoot.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = uiRoot.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var scaler = uiRoot.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = uiRoot.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);

            if (uiRoot.GetComponent<GraphicRaycaster>() == null)
            {
                uiRoot.AddComponent<GraphicRaycaster>();
            }

            EnsureEventSystem();

            hudValuesText = FindOrCreateHudText(uiRoot.transform, "Whitebox Values Text", new Vector2(-24f, -72f), new Vector2(260f, 128f), TextAnchor.UpperRight, 20);
            FindOrCreateSignalBars(uiRoot.transform);
            hudReactionText = FindOrCreateHudText(uiRoot.transform, "Whitebox Runtime Reaction Text", new Vector2(24f, -76f), new Vector2(720f, 150f), TextAnchor.UpperLeft, 21);
            hudTimeText = FindOrCreateHudText(uiRoot.transform, "Whitebox Runtime Time Text", new Vector2(-24f, -24f), new Vector2(300f, 52f), TextAnchor.UpperRight, 22);
            FindOrCreateOutcomeUi(uiRoot.transform);
            EnsureBottomActionBar(uiRoot.transform);
            hudReactionText.text = "Last: none\nClick an object to test an interaction.";
        }

        private void EnsureBottomActionBar(Transform parent)
        {
            const float spacing = 12f;
            const float buttonWidth = 136f;
            const float step = buttonWidth + spacing;
            const float y = 24f;
            const float startX = -2f * step;

            EnsureActionButton(parent, "Action Button - Alarm", "Urge", new Vector2(startX + step * 0f, y), CaveheartInteractionType.Alarm);
            EnsureActionButton(parent, "Action Button - Touch", "Touch", new Vector2(startX + step * 1f, y), CaveheartInteractionType.GentleTouch);
            EnsureActionButton(parent, "Action Button - Water", "Water", new Vector2(startX + step * 2f, y), CaveheartInteractionType.OfferWater);
            EnsureActionButton(parent, "Action Button - Curtain", "Window", new Vector2(startX + step * 3f, y), CaveheartInteractionType.OpenCurtain);
            EnsureActionButton(parent, "Action Button - Wait", "Observe", new Vector2(startX + step * 4f, y), CaveheartInteractionType.Wait);

            RemoveLegacyButton(parent, "Wait Observe Button");
            RemoveLegacyButton(parent, "Scratch Button");
            RemoveLegacyButton(parent, "Action Button - Shake Bed");
            RemoveLegacyButton(parent, "Action Button - Blanket");
            RemoveLegacyButton(parent, "Action Button - Scratch");
        }

        private static void RemoveLegacyButton(Transform parent, string objectName)
        {
            var legacy = parent.Find(objectName);
            if (legacy != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(legacy.gameObject);
                }
                else
                {
                    DestroyImmediate(legacy.gameObject);
                }
            }
        }

        private void FindOrCreateOutcomeUi(Transform parent)
        {
            var panelTransform = parent.Find("Whitebox Outcome Panel");
            var panelObject = panelTransform == null ? new GameObject("Whitebox Outcome Panel") : panelTransform.gameObject;
            panelObject.transform.SetParent(parent, false);

            var panelRect = panelObject.GetComponent<RectTransform>();
            if (panelRect == null)
            {
                panelRect = panelObject.AddComponent<RectTransform>();
            }

            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(800f, 260f);
            panelRect.anchoredPosition = Vector2.zero;

            hudOutcomePanel = panelObject.GetComponent<Image>();
            if (hudOutcomePanel == null)
            {
                hudOutcomePanel = panelObject.AddComponent<Image>();
            }

            hudOutcomePanel.color = new Color(0.07f, 0.08f, 0.08f, 0.88f);

            var textTransform = panelObject.transform.Find("Outcome Text");
            var textObject = textTransform == null ? new GameObject("Outcome Text") : textTransform.gameObject;
            textObject.transform.SetParent(panelObject.transform, false);

            var textRect = textObject.GetComponent<RectTransform>();
            if (textRect == null)
            {
                textRect = textObject.AddComponent<RectTransform>();
            }

            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(32f, 24f);
            textRect.offsetMax = new Vector2(-32f, -24f);

            hudOutcomeText = textObject.GetComponent<Text>();
            if (hudOutcomeText == null)
            {
                hudOutcomeText = textObject.AddComponent<Text>();
            }

            hudOutcomeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hudOutcomeText.fontSize = 28;
            hudOutcomeText.alignment = TextAnchor.MiddleCenter;
            hudOutcomeText.color = Color.white;
            hudOutcomeText.horizontalOverflow = HorizontalWrapMode.Wrap;
            hudOutcomeText.verticalOverflow = VerticalWrapMode.Overflow;
            panelObject.SetActive(false);
        }

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

        private void EnsureActionButton(Transform parent, string objectName, string labelText, Vector2 anchoredPosition, CaveheartInteractionType interactionType)
        {
            var existing = parent.Find(objectName);
            var obj = existing == null ? new GameObject(objectName) : existing.gameObject;
            obj.transform.SetParent(parent, false);

            var rect = obj.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = obj.AddComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(136f, 50f);
            rect.anchoredPosition = anchoredPosition;

            var image = obj.GetComponent<Image>();
            if (image == null)
            {
                image = obj.AddComponent<Image>();
            }

            image.color = new Color(0.12f, 0.15f, 0.18f, 0.82f);

            var button = obj.GetComponent<Button>();
            if (button == null)
            {
                button = obj.AddComponent<Button>();
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => Interact(interactionType));

            var labelTransform = obj.transform.Find("Label");
            var labelObject = labelTransform == null ? new GameObject("Label") : labelTransform.gameObject;
            labelObject.transform.SetParent(obj.transform, false);

            var labelRect = labelObject.GetComponent<RectTransform>();
            if (labelRect == null)
            {
                labelRect = labelObject.AddComponent<RectTransform>();
            }

            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = labelObject.GetComponent<Text>();
            if (label == null)
            {
                label = labelObject.AddComponent<Text>();
            }

            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = labelText;
            label.fontSize = 20;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;

            var feedback = obj.GetComponent<CaveheartActionButtonFeedback>();
            if (feedback == null)
            {
                feedback = obj.AddComponent<CaveheartActionButtonFeedback>();
            }

            feedback.Configure(image, label, interactionType);
            obj.SetActive(true);
        }

        private void FindOrCreateSignalBars(Transform parent)
        {
            hudAwakeFill = FindOrCreateSignalBar(parent, "Awake Signal Bar", "Awake", new Vector2(-24f, -112f), new Color(0.92f, 0.78f, 0.38f, 0.92f));
            hudTrustFill = FindOrCreateSignalBar(parent, "Trust Signal Bar", "Trust", new Vector2(-24f, -158f), new Color(0.36f, 0.78f, 0.62f, 0.92f));
            hudStressFill = FindOrCreateSignalBar(parent, "Stress Signal Bar", "Stress", new Vector2(-24f, -204f), new Color(0.54f, 0.68f, 0.94f, 0.92f));
        }

        private static RectTransform FindOrCreateSignalBar(Transform parent, string objectName, string labelText, Vector2 anchoredPosition, Color fillColor)
        {
            var existing = parent.Find(objectName);
            var obj = existing == null ? new GameObject(objectName) : existing.gameObject;
            obj.transform.SetParent(parent, false);

            var rect = obj.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = obj.AddComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(220f, 30f);
            rect.anchoredPosition = anchoredPosition;

            var label = FindOrCreateHudText(obj.transform, "Label", new Vector2(0f, 0f), new Vector2(76f, 28f), TextAnchor.MiddleLeft, 16);
            var labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0.5f);
            labelRect.anchorMax = new Vector2(0f, 0.5f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = new Vector2(0f, 0f);
            label.text = labelText;

            var background = obj.transform.Find("Track") == null ? new GameObject("Track") : obj.transform.Find("Track").gameObject;
            background.transform.SetParent(obj.transform, false);
            var backgroundRect = background.GetComponent<RectTransform>();
            if (backgroundRect == null)
            {
                backgroundRect = background.AddComponent<RectTransform>();
            }

            backgroundRect.anchorMin = new Vector2(1f, 0.5f);
            backgroundRect.anchorMax = new Vector2(1f, 0.5f);
            backgroundRect.pivot = new Vector2(1f, 0.5f);
            backgroundRect.sizeDelta = new Vector2(138f, 14f);
            backgroundRect.anchoredPosition = new Vector2(0f, 0f);

            var backgroundImage = background.GetComponent<Image>();
            if (backgroundImage == null)
            {
                backgroundImage = background.AddComponent<Image>();
            }

            backgroundImage.color = new Color(0.03f, 0.04f, 0.045f, 0.72f);

            var fill = background.transform.Find("Fill") == null ? new GameObject("Fill") : background.transform.Find("Fill").gameObject;
            fill.transform.SetParent(background.transform, false);
            var fillRect = fill.GetComponent<RectTransform>();
            if (fillRect == null)
            {
                fillRect = fill.AddComponent<RectTransform>();
            }

            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = new Vector2(0f, 0f);
            fillRect.sizeDelta = new Vector2(0f, 0f);

            var fillImage = fill.GetComponent<Image>();
            if (fillImage == null)
            {
                fillImage = fill.AddComponent<Image>();
            }

            fillImage.type = Image.Type.Simple;
            fillImage.color = fillColor;
            obj.SetActive(true);
            return fillRect;
        }

        private static Text FindOrCreateHudText(Transform parent, string name, Vector2 anchoredPosition, Vector2 sizeDelta, TextAnchor anchor, int fontSize)
        {
            var existing = parent.Find(name);
            var obj = existing == null ? new GameObject(name) : existing.gameObject;
            obj.transform.SetParent(parent, false);

            var rect = obj.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = obj.AddComponent<RectTransform>();
            }

            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;

            if (anchor == TextAnchor.UpperLeft)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
            }
            else
            {
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
            }

            var text = obj.GetComponent<Text>();
            if (text == null)
            {
                text = obj.AddComponent<Text>();
            }

            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var shadow = obj.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = obj.AddComponent<Shadow>();
            }

            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(2f, -2f);
            obj.SetActive(true);
            return text;
        }

        private void UpdateWhiteboxHudValues()
        {
            if (hudValuesText == null || hudTimeText == null)
            {
                EnsureWhiteboxHud();
            }

            if (hudValuesText != null)
            {
                hudValuesText.gameObject.SetActive(showWhiteboxHud);
                hudValuesText.text = "BODY SIGNALS";
            }

            SetSignalBar(hudAwakeFill, stats.awake, showWhiteboxHud);
            SetSignalBar(hudTrustFill, stats.trust, showWhiteboxHud);
            SetSignalBar(hudStressFill, stats.stress, showWhiteboxHud);

            if (hudTimeText != null)
            {
                hudTimeText.text = $"Time Left {RemainingMorningMinutes:00}m\nUsed {usedMorningMinutes:00}/{morningTimeLimitMinutes:00}m";
            }
        }

        private static void SetSignalBar(RectTransform fill, int value, bool visible)
        {
            if (fill == null)
            {
                return;
            }

            fill.gameObject.transform.parent.parent.gameObject.SetActive(visible);
            var ratio = Mathf.Clamp01(value / 8f);
            fill.sizeDelta = new Vector2(138f * ratio, 0f);
        }

        private void UpdateWhiteboxHudReaction(CaveheartInteractionResult result)
        {
            if (hudReactionText == null)
            {
                EnsureWhiteboxHud();
            }

            if (hudReactionText == null)
            {
                return;
            }

            var acceptance = result.accepted ? "Accepted" : "Rejected";
            var actionMinutes = CaveheartRules.GetTimeCost(result.interactionType);
            hudReactionText.text =
                $"Last: {result.interactionType} ({acceptance})  Time -{actionMinutes}m  Left {RemainingMorningMinutes}m\n" +
                result.observedReaction;
        }

        private void UpdateOutcomeUi()
        {
            if (hudOutcomePanel == null || hudOutcomeText == null)
            {
                return;
            }

            var showSuccess = currentState == CaveheartState.SittingUp && !hasFailed;
            var showFailure = hasFailed;
            hudOutcomePanel.gameObject.SetActive(showSuccess || showFailure);

            if (showSuccess)
            {
                var outcome = GetMorningOutcome();
                if (outcome == MorningOutcome.VeryGood)
                {
                    hudOutcomePanel.color = new Color(0.06f, 0.11f, 0.08f, 0.9f);
                    hudOutcomeText.text =
                        "A soft morning\n\n" +
                        "\"Thank you... this way I am less afraid.\"\n\n" +
                        "Slower was not failure.\nIt was you and your body standing together.";
                }
                else if (outcome == MorningOutcome.Good)
                {
                    hudOutcomePanel.color = new Color(0.08f, 0.1f, 0.08f, 0.9f);
                    hudOutcomeText.text =
                        "He sits up\n\n" +
                        "\"I can start slowly.\"\n\n" +
                        "The morning still presses, but he is willing to move with you.";
                }
                else
                {
                    hudOutcomePanel.color = new Color(0.11f, 0.09f, 0.07f, 0.9f);
                    hudOutcomeText.text =
                        "He gets up, tense\n\n" +
                        "He is awake, but his shoulders stay high and the room feels sharp.\n\n" +
                        "It worked. It did not feel safe.";
                }
            }
            else if (showFailure)
            {
                hudOutcomePanel.color = new Color(0.1f, 0.08f, 0.08f, 0.9f);
                hudOutcomeText.text =
                    "The morning passes\n\n" +
                    "He is more awake, maybe, but not ready to move with you.\n\n" +
                    "Next time, notice the body before asking it for more.";
            }
        }

        private MorningOutcome GetMorningOutcome()
        {
            if (hasFailed)
            {
                return MorningOutcome.Bad;
            }

            if (forcefulUseCount >= 3 || stats.trust <= CaveheartRules.SitUpTrust || stats.stress >= CaveheartRules.SitUpMaxStress)
            {
                return MorningOutcome.Bad;
            }

            if (forcefulUseCount <= 1 && stats.trust >= 5 && stats.stress <= 3 && usedMorningMinutes <= 12)
            {
                return MorningOutcome.VeryGood;
            }

            if (stats.trust >= CaveheartRules.SitUpTrust && stats.stress <= CaveheartRules.SitUpMaxStress)
            {
                return MorningOutcome.Good;
            }

            return MorningOutcome.Bad;
        }

        private void LogWhiteboxInteraction(CaveheartInteractionResult result, CaveheartState previousState)
        {
            if (!logWhiteboxInteractions)
            {
                return;
            }

            Debug.Log(
                $"[Caveheart Whitebox] {result.interactionType} | " +
                $"accepted={result.accepted} | " +
                $"state {previousState} -> {result.state} | " +
                $"time {usedMorningMinutes}/{morningTimeLimitMinutes}m | " +
                $"awake {result.before.awake}->{result.after.awake}, " +
                $"trust {result.before.trust}->{result.after.trust}, " +
                $"stress {result.before.stress}->{result.after.stress} | " +
                $"{result.observedReaction}",
                this);
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MyLittleCaveheart
{
    public sealed class CaveheartGameController : MonoBehaviour
    {
        private const string CursorResourceRoot = "Sprites/Caveheart/CursorIcons/Size64/";
        private const string OpeningGoalText = "Help him sit up gently. Watch if he is awake, safe, and calm.";

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
        [SerializeField] private bool showWhiteboxHud = false;
        [SerializeField] private int morningTimeLimitMinutes = 14;

        [Header("World Interaction Zones")]
        [SerializeField] private Collider2D urgeZone;
        [SerializeField] private Collider2D touchHeadZone;
        [SerializeField] private Collider2D scratchFeetZone;
        [SerializeField] private Collider2D waterZone;
        [SerializeField] private Collider2D windowZone;
        [SerializeField] private Collider2D observeZone;

        [Header("Curtain Background")]
        [SerializeField] private SpriteRenderer curtainBackgroundRenderer;
        [SerializeField] private Sprite closedCurtainBackground;
        [SerializeField] private Sprite openCurtainBackground;
        [SerializeField] private bool resetCurtainOnMorning = true;

        [Header("Cursor Images")]
        [SerializeField] private Texture2D defaultCursor;
        [SerializeField] private Texture2D urgeCursor;
        [SerializeField] private Texture2D touchCursor;
        [SerializeField] private Texture2D waterCursor;
        [SerializeField] private Texture2D windowCursor;
        [SerializeField] private Texture2D scratchCursor;
        [SerializeField] private Texture2D observeCursor;
        [SerializeField, Min(8f)] private float cursorImageSize = 37f;
        [SerializeField] private Vector2 cursorImageOffset = Vector2.zero;

        [Header("Music")]
        [SerializeField] private AudioClip backgroundMusicClip;
        [SerializeField, Range(0f, 1f)] private float backgroundMusicVolume = 0.28f;

        [Header("UI Images")]
        [SerializeField] private Sprite outcomePanelSprite;
        [SerializeField] private Sprite restartButtonSprite;
        [SerializeField] private Sprite signalBarTrackSprite;

        [Header("HUD Layout")]
        [SerializeField] private Vector2 reactionTextPosition = new Vector2(0f, -72f);
        [SerializeField] private Vector2 reactionTextSize = new Vector2(760f, 190f);
        [SerializeField] private Vector2 timeTextPosition = new Vector2(-24f, -24f);
        [SerializeField] private Vector2 timeTextSize = new Vector2(300f, 52f);
        [SerializeField] private Vector2 debugValuesPosition = new Vector2(-24f, -72f);
        [SerializeField] private Vector2 debugValuesSize = new Vector2(260f, 128f);
        [SerializeField] private Vector2 awakeBarPosition = new Vector2(-24f, -112f);
        [SerializeField] private Vector2 trustBarPosition = new Vector2(-24f, -158f);
        [SerializeField] private Vector2 stressBarPosition = new Vector2(-24f, -204f);

        [Header("Hover Label")]
        [SerializeField] private Vector2 hoverLabelSize = new Vector2(260f, 58f);
        [SerializeField] private Vector2 hoverLabelOffset = new Vector2(18f, -18f);
        [SerializeField, Min(0f)] private float hoverLabelDelay = 0.35f;

        [Header("Observe Hold Ring")]
        [SerializeField] private bool holdToObserve = true;
        [SerializeField, Min(0.05f)] private float observeHoldDuration = 1f;
        [SerializeField, Min(0.1f)] private float observeHoldReturnSpeed = 3.5f;
        [SerializeField, Min(4f)] private float observeHoldRingRadius = 48f;
        [SerializeField, Min(1f)] private float observeHoldRingWidth = 8f;
        [SerializeField] private Vector2 observeHoldRingOffset = Vector2.zero;
        [SerializeField] private Color observeHoldIdleRingColor = new Color(0f, 0f, 0f, 0.34f);
        [SerializeField] private Color observeHoldWarmStartColor = new Color(1f, 0.48f, 0.14f, 0.88f);
        [SerializeField] private Color observeHoldWarmEndColor = new Color(1f, 0.76f, 0.32f, 0.96f);

        [Header("Outcome Layout")]
        [SerializeField] private Vector2 outcomePanelPosition = Vector2.zero;
        [SerializeField] private Vector2 outcomePanelSize = new Vector2(800f, 260f);
        [SerializeField] private Vector2 restartButtonPosition = new Vector2(0f, 18f);
        [SerializeField] private Vector2 restartButtonSize = new Vector2(160f, 44f);

        public event Action<CaveheartInteractionResult> InteractionResolved;
        public event Action<CaveheartState, CaveheartStats> StateChanged;
        public event Action<CaveheartInteractionType?, Vector3> InteractionHoverChanged;

        private Text hudValuesText;
        private Text hudReactionText;
        private Text hudTimeText;
        private RectTransform hudAwakeFill;
        private RectTransform hudTrustFill;
        private RectTransform hudStressFill;
        private Text hudOutcomeText;
        private Image hudOutcomePanel;
        private Button hudRestartButton;
        private Text hudHoverText;
        private RectTransform hudHoverRect;
        private RectTransform cursorImageRect;
        private RawImage cursorImage;
        private RectTransform observeHoldRingRect;
        private RawImage observeHoldIdleRingImage;
        private RawImage observeHoldProgressRingImage;
        private Texture2D observeHoldIdleTexture;
        private Texture2D observeHoldProgressTexture;
        private int observeHoldTextureSize;
        private float elapsedSeconds;
        private int usedMorningMinutes;
        private int blanketUseCount;
        private int gentleTouchUseCount;
        private int waterUseCount;
        private int curtainUseCount;
        private int scratchUseCount;
        private int forcefulUseCount;
        private int rejectedActionCount;
        private bool hasAcceptedWater;
        private bool hasMorningClosed;
        private CaveheartCharacterView characterView;
        private CaveheartInteractionType lastInteraction = CaveheartInteractionType.Wait;
        private int waitStreak;
        private bool lastInteractionWasForceful;
        private bool lastInteractionWasRejected;
        private CaveheartSpriteAnimator spriteAnimator;
        private CaveheartEnvironmentFeedback environmentFeedback;
        private CaveheartClickable hoveredInteractionZone;
        private Vector3 hoveredInteractionWorldPosition;
        private CaveheartInteractionType? hoveredInteraction;
        private bool hoveredInteractionAvailable;
        private float hoverStartedAt;
        private float observeHoldProgress;
        private bool observeHoldTriggeredThisPress;
        public CaveheartStats Stats => stats;
        public CaveheartState CurrentState => currentState;
        public int UsedMorningMinutes => usedMorningMinutes;
        public int MorningTimeLimitMinutes => morningTimeLimitMinutes;
        public int RemainingMorningMinutes => Mathf.Max(0, morningTimeLimitMinutes - usedMorningMinutes);
        public bool HasMorningClosed => hasMorningClosed;
        public bool IsEnded => currentState == CaveheartState.SittingUp || hasMorningClosed;
        public int RejectedActionCount => rejectedActionCount;
        public CaveheartInteractionType? CurrentHoveredInteraction => hoveredInteraction;
        public Vector3 HoveredInteractionWorldPosition => hoveredInteractionWorldPosition;

        // Reclaims the native cursor when this controller is re-enabled during play.
        private void OnEnable()
        {
            CaveheartCursorVisibilityGuard.SetOverrideActive(this, true);
        }

        private void Start()
        {
            BeginMorning();
            EnsureCurtainBackground();
            if (resetCurtainOnMorning)
            {
                SetCurtainBackground(false);
            }

            EnsureClickFeedbackObject();
            PruneRetiredInteractables();
            EnsureWorldInteractionZones();
            EnsureCharacterView();
            EnsureSpriteAnimator();
            EnsureEnvironmentFeedback();
            CaveheartBackgroundMusic.Ensure(backgroundMusicClip, backgroundMusicVolume);
            EnsureWhiteboxHud();
            CaveheartCursorVisibilityGuard.SetOverrideActive(this, true);
            ApplyInteractionCursor(null, false);
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

            UpdateWorldInteractionInput();
            UpdateHoverTooltip();
            UpdateWhiteboxHudValues();
        }

        private void LateUpdate()
        {
            UpdateCursorImageVisibility();
            PositionCursorImage();
        }

        public bool IsInteractionAvailable(CaveheartInteractionType interactionType)
        {
            if (lockAfterEnding && IsEnded)
            {
                return false;
            }

            switch (interactionType)
            {
                case CaveheartInteractionType.GentleTouch:
                    return gentleTouchUseCount < CaveheartRules.HelpfulGentleTouchUses;
                case CaveheartInteractionType.OfferWater:
                    return waterUseCount < CaveheartRules.HelpfulWaterUses;
                case CaveheartInteractionType.OpenCurtain:
                    return curtainUseCount < CaveheartRules.HelpfulCurtainUses;
                case CaveheartInteractionType.Scratch:
                    return scratchUseCount < CaveheartRules.HelpfulScratchUses;
                default:
                    return true;
            }
        }

        public CaveheartInteractionResult Interact(CaveheartInteractionType interactionType)
        {
            if (lockAfterEnding && IsEnded)
            {
                var endedMessage = hasMorningClosed ? "Today is already over." : "He is already sitting up.";
                var endedReaction = hasMorningClosed
                    ? "He stays curled up."
                    : "He stays seated.";
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

            if (!IsInteractionAvailable(interactionType))
            {
                var unavailable = new CaveheartInteractionResult(
                    interactionType,
                    stats,
                    stats,
                    currentState,
                    false,
                    InteractionUnavailableText(interactionType),
                    "You leave it there.");
                UpdateWhiteboxHudReaction(unavailable);
                InteractionResolved?.Invoke(unavailable);
                return unavailable;
            }

            var actionMinutes = CaveheartRules.GetTimeCost(interactionType);
            if (usedMorningMinutes + actionMinutes > morningTimeLimitMinutes)
            {
                usedMorningMinutes = morningTimeLimitMinutes;
                hasMorningClosed = true;
                var timedOut = new CaveheartInteractionResult(
                    interactionType,
                    stats,
                    stats,
                    currentState,
                    false,
                    "There is not enough morning left.",
                    "Today can stop here. He stays curled up.");
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
                    if (result.accepted)
                    {
                        gentleTouchUseCount++;
                    }
                    break;
                case CaveheartInteractionType.OfferWater:
                    if (result.accepted)
                    {
                        waterUseCount++;
                        hasAcceptedWater = true;
                    }
                    break;
                case CaveheartInteractionType.OpenCurtain:
                    curtainUseCount++;
                    SetCurtainBackground(true);
                    break;
                case CaveheartInteractionType.Scratch:
                    if (result.accepted)
                    {
                        scratchUseCount++;
                    }
                    break;
            }

            stats = result.after;
            currentState = result.state;
            usedMorningMinutes += actionMinutes;
            if (!result.accepted)
            {
                rejectedActionCount++;
            }

            if (interactionType == CaveheartInteractionType.Alarm || interactionType == CaveheartInteractionType.ShakeBed)
            {
                forcefulUseCount++;
            }

            if (currentState != CaveheartState.SittingUp && usedMorningMinutes >= morningTimeLimitMinutes)
            {
                hasMorningClosed = true;
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
            BeginMorning();
            EnsureCurtainBackground();
            if (resetCurtainOnMorning)
            {
                SetCurtainBackground(false);
            }

            SetHoveredInteraction(null, Vector3.zero);
            PublishState();
            characterView?.SetHasAcceptedWater(hasAcceptedWater);
            characterView?.ApplyState(currentState, stats);
            spriteAnimator?.Play(currentState);
            if (hudReactionText != null)
            {
                hudReactionText.text = OpeningGoalText;
            }

            UpdateOutcomeUi();
        }

        private void BeginMorning()
        {
            stats = CaveheartStats.Starting;
            currentState = CaveheartRules.EvaluateState(stats, CaveheartState.Sleeping);
            blanketUseCount = 0;
            gentleTouchUseCount = 0;
            waterUseCount = 0;
            curtainUseCount = 0;
            scratchUseCount = 0;
            forcefulUseCount = 0;
            rejectedActionCount = 0;
            usedMorningMinutes = 0;
            hasAcceptedWater = false;
            hasMorningClosed = false;
            lastInteraction = CaveheartInteractionType.Wait;
            waitStreak = 0;
            lastInteractionWasForceful = false;
            lastInteractionWasRejected = false;
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
                    clickable.gameObject.SetActive(true);
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

        private void EnsureWorldInteractionZones()
        {
            EnsureInteractionZone(ref urgeZone, CaveheartInteractionType.Alarm, "Alarm", new Vector2(-6.25f, -1.35f), new Vector2(1.8f, 2.2f));
            EnsureInteractionZone(ref touchHeadZone, CaveheartInteractionType.GentleTouch, "Touch Head", new Vector2(-0.35f, -0.55f), new Vector2(2.4f, 1.5f));
            EnsureInteractionZone(ref scratchFeetZone, CaveheartInteractionType.Scratch, "Scratch Feet", new Vector2(0.35f, -3.25f), new Vector2(2.3f, 0.9f));
            EnsureInteractionZone(ref waterZone, CaveheartInteractionType.OfferWater, "Water", new Vector2(6.15f, -2.65f), new Vector2(2.5f, 1.55f));
            EnsureInteractionZone(ref windowZone, CaveheartInteractionType.OpenCurtain, "Curtain", new Vector2(5.55f, 1.65f), new Vector2(2.4f, 3.6f));
            EnsureInteractionZone(ref observeZone, CaveheartInteractionType.Wait, "Observe", Vector2.zero, new Vector2(18f, 10f));
        }

        private void EnsureInteractionZone(
            ref Collider2D zoneCollider,
            CaveheartInteractionType interactionType,
            string label,
            Vector2 fallbackPosition,
            Vector2 fallbackSize)
        {
            var zone = zoneCollider == null ? FindInteractionZone(interactionType) : zoneCollider.GetComponent<CaveheartClickable>();
            if (zoneCollider == null && zone != null)
            {
                zoneCollider = zone.GetComponent<Collider2D>();
            }

            if (zoneCollider == null)
            {
                var zoneObject = new GameObject($"Interactable - {label}");
                zoneObject.transform.position = new Vector3(fallbackPosition.x, fallbackPosition.y, 0f);
                var boxCollider = zoneObject.AddComponent<BoxCollider2D>();
                boxCollider.size = fallbackSize;
                zoneCollider = boxCollider;
                zone = zoneObject.AddComponent<CaveheartClickable>();
            }
            else if (zone == null)
            {
                zone = zoneCollider.gameObject.AddComponent<CaveheartClickable>();
            }

            zone.gameObject.SetActive(true);
            zone.Configure(this, interactionType);
        }

        private static CaveheartClickable FindInteractionZone(CaveheartInteractionType interactionType)
        {
            var clickables = FindObjectsOfType<CaveheartClickable>(true);
            for (var i = 0; i < clickables.Length; i++)
            {
                if (clickables[i] != null && clickables[i].InteractionType == interactionType)
                {
                    return clickables[i];
                }
            }

            return null;
        }

        private void UpdateWorldInteractionInput()
        {
            if (!Application.isPlaying || (lockAfterEnding && IsEnded))
            {
                SetHoveredInteraction(null, Vector3.zero);
                UpdateObserveHoldInput(null);
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                SetHoveredInteraction(null, Vector3.zero);
                UpdateObserveHoldInput(null);
                return;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                SetHoveredInteraction(null, Vector3.zero);
                UpdateObserveHoldInput(null);
                return;
            }

            var screenPoint = Input.mousePosition;
            var worldPoint3D = camera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, -camera.transform.position.z));
            var worldPoint = new Vector2(worldPoint3D.x, worldPoint3D.y);
            var hits = Physics2D.OverlapPointAll(worldPoint);
            CaveheartClickable bestZone = null;

            for (var i = 0; i < hits.Length; i++)
            {
                var candidate = hits[i].GetComponent<CaveheartClickable>();
                if (candidate == null)
                {
                    continue;
                }

                if (bestZone == null || candidate.HoverPriority > bestZone.HoverPriority)
                {
                    bestZone = candidate;
                }
            }

            SetHoveredInteraction(bestZone, worldPoint3D);
            UpdateObserveHoldInput(bestZone);
            if (bestZone != null
                && IsInteractionAvailable(bestZone.InteractionType)
                && Input.GetMouseButtonDown(0)
                && !RequiresHoldToObserve(bestZone.InteractionType))
            {
                bestZone.TryInteract();
            }
        }

        private bool RequiresHoldToObserve(CaveheartInteractionType interactionType)
        {
            return holdToObserve && interactionType == CaveheartInteractionType.Wait;
        }

        private void UpdateObserveHoldInput(CaveheartClickable zone)
        {
            var canHoldObserve = zone != null
                && RequiresHoldToObserve(zone.InteractionType)
                && IsInteractionAvailable(zone.InteractionType);

            if (!canHoldObserve)
            {
                observeHoldTriggeredThisPress = false;
                observeHoldProgress = 0f;
                UpdateObserveHoldIndicator(false);
                return;
            }

            if (!Input.GetMouseButton(0))
            {
                observeHoldTriggeredThisPress = false;
                observeHoldProgress = Mathf.MoveTowards(
                    observeHoldProgress,
                    0f,
                    Time.unscaledDeltaTime * observeHoldReturnSpeed);
            }
            else if (!observeHoldTriggeredThisPress)
            {
                var duration = Mathf.Max(0.05f, observeHoldDuration);
                observeHoldProgress = Mathf.MoveTowards(
                    observeHoldProgress,
                    1f,
                    Time.unscaledDeltaTime / duration);

                if (observeHoldProgress >= 1f)
                {
                    observeHoldProgress = 1f;
                    observeHoldTriggeredThisPress = true;
                    zone.TryInteract();
                }
            }

            UpdateObserveHoldIndicator(true);
        }

        private void SetHoveredInteraction(CaveheartClickable zone, Vector3 worldPosition)
        {
            var interactionChanged = hoveredInteractionZone != zone;
            var positionChanged = (hoveredInteractionWorldPosition - worldPosition).sqrMagnitude > 0.0001f;
            hoveredInteractionZone = zone;
            hoveredInteractionWorldPosition = worldPosition;
            var interaction = zone == null ? (CaveheartInteractionType?)null : zone.InteractionType;
            var available = interaction.HasValue && IsInteractionAvailable(interaction.Value);

            if (interactionChanged || hoveredInteraction != interaction || hoveredInteractionAvailable != available)
            {
                hoveredInteraction = interaction;
                hoveredInteractionAvailable = available;
                hoverStartedAt = Time.unscaledTime;
                ApplyInteractionCursor(interaction, available);
                InteractionHoverChanged?.Invoke(CurrentHoveredInteraction, worldPosition);
            }
            else if (zone != null && positionChanged)
            {
                InteractionHoverChanged?.Invoke(CurrentHoveredInteraction, worldPosition);
            }
        }

        private void OnValidate()
        {
            BindExistingZone(ref urgeZone, CaveheartInteractionType.Alarm);
            BindExistingZone(ref touchHeadZone, CaveheartInteractionType.GentleTouch);
            BindExistingZone(ref scratchFeetZone, CaveheartInteractionType.Scratch);
            BindExistingZone(ref waterZone, CaveheartInteractionType.OfferWater);
            BindExistingZone(ref windowZone, CaveheartInteractionType.OpenCurtain);
            BindExistingZone(ref observeZone, CaveheartInteractionType.Wait);
            if (observeZone != null)
            {
                observeZone.enabled = true;
            }
        }

        private static void BindExistingZone(ref Collider2D collider, CaveheartInteractionType interactionType)
        {
            if (collider != null)
            {
                return;
            }

            var zone = FindInteractionZone(interactionType);
            if (zone != null)
            {
                collider = zone.GetComponent<Collider2D>();
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
            CaveheartTypography.ApplyTo(mesh);
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
                Debug.LogWarning("CaveheartSpriteAnimator is not configured on GameController. Use the editor installer before entering play mode.", this);
                return;
            }

            if (!spriteAnimator.BindConfiguredRenderer())
            {
                return;
            }

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

        private void EnsureCurtainBackground()
        {
            if (curtainBackgroundRenderer == null || closedCurtainBackground == null || openCurtainBackground == null)
            {
                Debug.LogWarning("Caveheart curtain backgrounds are not fully configured on GameController.", this);
            }
        }

        private void SetCurtainBackground(bool isOpen)
        {
            if (curtainBackgroundRenderer == null)
            {
                return;
            }

            var targetSprite = isOpen ? openCurtainBackground : closedCurtainBackground;
            if (targetSprite != null)
            {
                curtainBackgroundRenderer.sprite = targetSprite;
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

            RemoveObsoleteUiElement(uiRoot.transform, "Whitebox Runtime State Text");
            RemoveObsoleteUiElement(uiRoot.transform, "State Text");
            RemoveObsoleteUiElement(uiRoot.transform, "Whitebox Runtime Reaction Text");
            RemoveObsoleteUiElement(uiRoot.transform, "Whitebox Runtime Time Text");
            RemoveObsoleteUiElement(uiRoot.transform, "Debug Text Toggle Mirror");
            RemoveObsoleteUiElement(uiRoot.transform, "Ending Text");
            RemoveObsoleteUiElement(uiRoot.transform, "Observe Button");
            RemoveObsoleteUiElement(uiRoot.transform, "Observe Hold Ring");

            EnsureCursorImage(EnsureCursorUiRoot());
            hudValuesText = FindOrCreateHudText(uiRoot.transform, "Whitebox Values Text", debugValuesPosition, debugValuesSize, TextAnchor.UpperRight, 20);
            FindOrCreateSignalBars(uiRoot.transform);
            hudReactionText = FindOrCreateHudText(uiRoot.transform, "Reaction Text", reactionTextPosition, reactionTextSize, TextAnchor.UpperCenter, 20);
            hudTimeText = FindOrCreateHudText(uiRoot.transform, "Time Text", timeTextPosition, timeTextSize, TextAnchor.UpperRight, 22);
            EnsureObserveHoldIndicator(uiRoot.transform);
            EnsureHoverTooltip(uiRoot.transform);
            FindOrCreateOutcomeUi(uiRoot.transform);
            RemoveActionButtons(uiRoot.transform);
            hudReactionText.text = OpeningGoalText;
        }

        // Creates a pixel-exact cursor canvas so cursor icons are not scaled by the HUD canvas.
        private static Transform EnsureCursorUiRoot()
        {
            var uiRoot = GameObject.Find("Caveheart Cursor UI");
            if (uiRoot == null)
            {
                uiRoot = new GameObject("Caveheart Cursor UI");
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

            return uiRoot.transform;
        }

        private void EnsureCursorImage(Transform parent)
        {
            var cursorTransform = parent.Find("Caveheart Cursor Image");
            var cursorObject = cursorTransform == null ? new GameObject("Caveheart Cursor Image") : cursorTransform.gameObject;
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

        private void EnsureObserveHoldIndicator(Transform parent)
        {
            var ringTransform = parent.Find("Observe Cursor Ring");
            var ringObject = ringTransform == null ? new GameObject("Observe Cursor Ring") : ringTransform.gameObject;
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

            RemoveObsoleteUiElement(ringObject.transform, "Observe Icon");
            observeHoldIdleRingImage = FindOrCreateRingImage(ringObject.transform, "Idle Ring");
            observeHoldProgressRingImage = FindOrCreateRingImage(ringObject.transform, "Progress Ring");
            UpdateObserveHoldIndicator(false);
        }

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

        private void UpdateObserveHoldIndicator(bool visible)
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
            DrawRingTexture(observeHoldIdleTexture, 1f, observeHoldIdleRingColor, observeHoldIdleRingColor);
            DrawRingTexture(observeHoldProgressTexture, observeHoldProgress, observeHoldWarmStartColor, observeHoldWarmEndColor);

            if (visible)
            {
                PositionObserveHoldIndicator();
            }

            observeHoldRingRect.gameObject.SetActive(visible);
            cursorImageRect?.SetAsLastSibling();
        }

        private void EnsureObserveHoldTextures(int size)
        {
            size = Mathf.Clamp(size, 16, 512);
            if (observeHoldTextureSize == size && observeHoldIdleTexture != null && observeHoldProgressTexture != null)
            {
                return;
            }

            observeHoldTextureSize = size;
            observeHoldIdleTexture = CreateRingTexture(size, "Caveheart Observe Hold Idle Ring");
            observeHoldProgressTexture = CreateRingTexture(size, "Caveheart Observe Hold Progress Ring");
            observeHoldIdleRingImage.texture = observeHoldIdleTexture;
            observeHoldProgressRingImage.texture = observeHoldProgressTexture;
        }

        private static Texture2D CreateRingTexture(int size, string textureName)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = textureName;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }

        private void DrawRingTexture(Texture2D texture, float amount, Color startColor, Color endColor)
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

                    var t = progressDegrees <= 0f ? 0f : Mathf.Clamp01(clockwiseFromTop / Mathf.Max(0.001f, progressDegrees));
                    var color = WarmRingColor(startColor, endColor, t, distance, clockwiseFromTop);
                    color.a *= ringAlpha;
                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        private static Color WarmRingColor(Color startColor, Color endColor, float t, float distance, float angle)
        {
            var color = Color.Lerp(startColor, endColor, t);
            var paperVariation = Mathf.Sin(angle * 0.19f + distance * 0.31f) * 0.035f;
            color.r = Mathf.Clamp01(color.r + paperVariation);
            color.g = Mathf.Clamp01(color.g + paperVariation * 0.55f);
            color.b = Mathf.Clamp01(color.b - paperVariation * 0.15f);
            return color;
        }

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

        private void EnsureHoverTooltip(Transform parent)
        {
            hudHoverText = FindOrCreateHudText(
                parent,
                "Interaction Hover Text",
                Vector2.zero,
                hoverLabelSize,
                TextAnchor.UpperLeft,
                18);
            hudHoverRect = hudHoverText.rectTransform;
            hudHoverRect.anchorMin = new Vector2(0.5f, 0.5f);
            hudHoverRect.anchorMax = new Vector2(0.5f, 0.5f);
            hudHoverRect.pivot = new Vector2(0f, 1f);
            hudHoverText.raycastTarget = false;
            hudHoverText.gameObject.SetActive(false);
        }

        private void UpdateHoverTooltip()
        {
            if (hudHoverText == null || hudHoverRect == null)
            {
                return;
            }

            if (!hoveredInteraction.HasValue || Time.unscaledTime - hoverStartedAt < hoverLabelDelay)
            {
                hudHoverText.gameObject.SetActive(false);
                return;
            }

            var interaction = hoveredInteraction.Value;
            hudHoverText.text = hoveredInteractionAvailable
                ? $"{InteractionDisplayName(interaction)}  {CaveheartRules.GetTimeCost(interaction)}m"
                : InteractionUnavailableText(interaction);
            PositionHoverTooltip();
            hudHoverText.gameObject.SetActive(true);
        }

        private void PositionHoverTooltip()
        {
            var canvasRect = hudHoverRect.parent as RectTransform;
            if (canvasRect == null
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    Input.mousePosition,
                    null,
                    out var localPoint))
            {
                return;
            }

            var canvasBounds = canvasRect.rect;
            localPoint += hoverLabelOffset;
            localPoint.x = Mathf.Clamp(localPoint.x, canvasBounds.xMin + 8f, canvasBounds.xMax - hudHoverRect.rect.width - 8f);
            localPoint.y = Mathf.Clamp(localPoint.y, canvasBounds.yMin + hudHoverRect.rect.height + 8f, canvasBounds.yMax - 8f);
            hudHoverRect.anchoredPosition = localPoint;
        }

        private static string InteractionDisplayName(CaveheartInteractionType interaction)
        {
            switch (interaction)
            {
                case CaveheartInteractionType.Alarm:
                    return "Urge";
                case CaveheartInteractionType.GentleTouch:
                    return "Touch";
                case CaveheartInteractionType.OfferWater:
                    return "Offer water";
                case CaveheartInteractionType.OpenCurtain:
                    return "Open curtain";
                case CaveheartInteractionType.Scratch:
                    return "Scratch";
                case CaveheartInteractionType.Wait:
                    return "Observe";
                default:
                    return interaction.ToString();
            }
        }

        private string InteractionUnavailableText(CaveheartInteractionType interaction)
        {
            switch (interaction)
            {
                case CaveheartInteractionType.GentleTouch:
                    return "He does not want more touch.";
                case CaveheartInteractionType.OfferWater:
                    return "He has had enough water.";
                case CaveheartInteractionType.OpenCurtain:
                    return "The curtain is already open.";
                case CaveheartInteractionType.Scratch:
                    return "He tucks his feet away.";
                default:
                    return "Not available now.";
            }
        }

        private void ApplyInteractionCursor(CaveheartInteractionType? interaction, bool available)
        {
            var texture = interaction.HasValue && available
                ? CursorTexture(interaction.Value)
                : DefaultCursorTexture();
            ApplyCursorImage(texture);
        }

        private Texture2D DefaultCursorTexture()
        {
            return defaultCursor = ResolveCursorTexture(defaultCursor, "cursor_default_flint_64");
        }

        private Texture2D CursorTexture(CaveheartInteractionType interaction)
        {
            switch (interaction)
            {
                case CaveheartInteractionType.Alarm:
                    return urgeCursor = ResolveCursorTexture(urgeCursor, "cursor_rooster_crow_64");
                case CaveheartInteractionType.GentleTouch:
                    return touchCursor = ResolveCursorTexture(touchCursor, "cursor_touch_64");
                case CaveheartInteractionType.OfferWater:
                    return waterCursor = ResolveCursorTexture(waterCursor, "cursor_wooden_cup_64");
                case CaveheartInteractionType.OpenCurtain:
                    return windowCursor = ResolveCursorTexture(windowCursor, "cursor_curtain_open_64");
                case CaveheartInteractionType.Scratch:
                    return scratchCursor = ResolveCursorTexture(scratchCursor, "cursor_tickled_64");
                case CaveheartInteractionType.Wait:
                    return observeCursor = ResolveCursorTexture(observeCursor, "cursor_observe_eye_64");
                default:
                    return DefaultCursorTexture();
            }
        }

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

        private void OnDisable()
        {
            HideCursorImage();
            CaveheartCursorVisibilityGuard.SetOverrideActive(this, false);
        }

        private void HideCursorImage()
        {
            if (cursorImage == null)
            {
                return;
            }

            cursorImage.enabled = false;
            cursorImage.gameObject.SetActive(false);
        }

        private static void RemoveObsoleteUiElement(Transform parent, string objectName)
        {
            var obsolete = parent.Find(objectName);
            if (obsolete == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(obsolete.gameObject);
            }
            else
            {
                DestroyImmediate(obsolete.gameObject);
            }
        }

        private static void RemoveActionButtons(Transform parent)
        {
            var buttonNames = new[]
            {
                "Action Button - Alarm",
                "Action Button - Touch",
                "Action Button - Water",
                "Action Button - Curtain",
                "Action Button - Scratch",
                "Action Button - Wait",
                "Wait Observe Button",
                "Scratch Button",
                "Action Button - Shake Bed",
                "Action Button - Blanket"
            };

            for (var i = 0; i < buttonNames.Length; i++)
            {
                var button = parent.Find(buttonNames[i]);
                if (button == null)
                {
                    continue;
                }

                button.gameObject.SetActive(false);
                if (Application.isPlaying)
                {
                    Destroy(button.gameObject);
                }
                else
                {
                    DestroyImmediate(button.gameObject);
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
            panelRect.sizeDelta = outcomePanelSize;
            panelRect.anchoredPosition = outcomePanelPosition;

            hudOutcomePanel = panelObject.GetComponent<Image>();
            if (hudOutcomePanel == null)
            {
                hudOutcomePanel = panelObject.AddComponent<Image>();
            }

            if (outcomePanelSprite != null)
            {
                hudOutcomePanel.sprite = outcomePanelSprite;
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
            textRect.offsetMin = new Vector2(32f, 82f);
            textRect.offsetMax = new Vector2(-32f, -24f);

            hudOutcomeText = textObject.GetComponent<Text>();
            if (hudOutcomeText == null)
            {
                hudOutcomeText = textObject.AddComponent<Text>();
            }

            CaveheartTypography.ApplyTo(hudOutcomeText);
            hudOutcomeText.fontSize = 28;
            hudOutcomeText.alignment = TextAnchor.MiddleCenter;
            hudOutcomeText.color = Color.white;
            hudOutcomeText.horizontalOverflow = HorizontalWrapMode.Wrap;
            hudOutcomeText.verticalOverflow = VerticalWrapMode.Overflow;
            EnsureRestartButton(panelObject.transform);
            panelObject.SetActive(false);
        }

        private void EnsureRestartButton(Transform parent)
        {
            var buttonTransform = parent.Find("Restart Button");
            var buttonObject = buttonTransform == null ? new GameObject("Restart Button") : buttonTransform.gameObject;
            buttonObject.transform.SetParent(parent, false);

            var rect = buttonObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = buttonObject.AddComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = restartButtonSize;
            rect.anchoredPosition = restartButtonPosition;

            var image = buttonObject.GetComponent<Image>();
            if (image == null)
            {
                image = buttonObject.AddComponent<Image>();
            }

            if (restartButtonSprite != null)
            {
                image.sprite = restartButtonSprite;
            }

            image.color = new Color(0.18f, 0.24f, 0.22f, 0.95f);

            hudRestartButton = buttonObject.GetComponent<Button>();
            if (hudRestartButton == null)
            {
                hudRestartButton = buttonObject.AddComponent<Button>();
            }

            hudRestartButton.onClick.RemoveAllListeners();
            hudRestartButton.onClick.AddListener(ResetMorning);

            var labelTransform = buttonObject.transform.Find("Label");
            var labelObject = labelTransform == null ? new GameObject("Label") : labelTransform.gameObject;
            labelObject.transform.SetParent(buttonObject.transform, false);

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

            CaveheartTypography.ApplyTo(label);
            label.text = "Restart";
            label.fontSize = 20;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
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

        private void FindOrCreateSignalBars(Transform parent)
        {
            hudAwakeFill = FindOrCreateSignalBar(parent, "Awake Signal Bar", "Awake", awakeBarPosition, new Color(0.92f, 0.78f, 0.38f, 0.92f));
            hudTrustFill = FindOrCreateSignalBar(parent, "Trust Signal Bar", "Trust", trustBarPosition, new Color(0.36f, 0.78f, 0.62f, 0.92f));
            hudStressFill = FindOrCreateSignalBar(parent, "Stress Signal Bar", "Stress", stressBarPosition, new Color(0.54f, 0.68f, 0.94f, 0.92f));
        }

        private RectTransform FindOrCreateSignalBar(Transform parent, string objectName, string labelText, Vector2 anchoredPosition, Color fillColor)
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

            if (signalBarTrackSprite != null)
            {
                backgroundImage.sprite = signalBarTrackSprite;
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
            else if (anchor == TextAnchor.UpperCenter)
            {
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
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

            CaveheartTypography.ApplyTo(text);
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

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
                hudTimeText.text = showWhiteboxHud
                    ? $"Morning left {RemainingMorningMinutes:00}m\nUsed {usedMorningMinutes:00}/{morningTimeLimitMinutes:00}m"
                    : $"Morning left {RemainingMorningMinutes:00}m";
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

            hudReactionText.text = string.IsNullOrEmpty(result.message)
                ? CombineFeedbackLines(result.observedReaction, BuildActionImpactText(result))
                : CombineFeedbackLines(result.message, result.observedReaction, BuildActionImpactText(result));
        }

        private void UpdateOutcomeUi()
        {
            if (hudOutcomePanel == null || hudOutcomeText == null)
            {
                return;
            }

            var showSittingUp = currentState == CaveheartState.SittingUp && !hasMorningClosed;
            var showMorningClosed = hasMorningClosed;
            var showOutcome = showSittingUp || showMorningClosed;
            hudOutcomePanel.gameObject.SetActive(showOutcome);

            if (hudRestartButton != null)
            {
                hudRestartButton.gameObject.SetActive(showOutcome);
            }

            if (showSittingUp)
            {
                var outcome = GetMorningOutcome();
                if (outcome == MorningOutcome.VeryGood)
                {
                    hudOutcomePanel.color = new Color(0.06f, 0.11f, 0.08f, 0.9f);
                    hudOutcomeText.text =
                        "A warm morning\n\n" +
                        "\"Stay close. I can get up.\"\n\n" +
                        "He reaches for you before he stands.";
                }
                else if (outcome == MorningOutcome.Good)
                {
                    hudOutcomePanel.color = new Color(0.08f, 0.1f, 0.08f, 0.9f);
                    hudOutcomeText.text =
                        "A good morning\n\n" +
                        "\"I can start from here.\"\n\n" +
                        "He sits up beside you.";
                }
                else
                {
                    hudOutcomePanel.color = new Color(0.11f, 0.09f, 0.07f, 0.9f);
                    hudOutcomeText.text =
                        "He gets up, tense\n\n" +
                        "He is awake, but the morning felt pushed.\n\n" +
                        BuildTenseOutcomeHint();
                }
            }
            else if (showMorningClosed)
            {
                hudOutcomePanel.color = new Color(0.1f, 0.08f, 0.08f, 0.9f);
                hudOutcomeText.text =
                    "Today stops here\n\n" +
                    "He is not ready to move yet.\n\n" +
                    BuildMorningClosedHint();
            }
        }

        private static string CombineFeedbackLines(params string[] lines)
        {
            var text = string.Empty;
            for (var i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrEmpty(lines[i]))
                {
                    continue;
                }

                text = string.IsNullOrEmpty(text) ? lines[i] : text + "\n" + lines[i];
            }

            return text;
        }

        private static string BuildActionImpactText(CaveheartInteractionResult result)
        {
            var awakeDelta = result.after.awake - result.before.awake;
            var trustDelta = result.after.trust - result.before.trust;
            var stressDelta = result.after.stress - result.before.stress;

            if (awakeDelta == 0 && trustDelta == 0 && stressDelta == 0)
            {
                return result.accepted
                    ? "It does not change him much."
                    : "He says no. Nothing really reaches him.";
            }

            var impact = string.Empty;
            AddImpactPart(ref impact, AwakeImpactText(awakeDelta));
            AddImpactPart(ref impact, TrustImpactText(trustDelta));
            AddImpactPart(ref impact, StressImpactText(stressDelta));

            if (!result.accepted && !string.IsNullOrEmpty(impact))
            {
                return "He refuses, and " + LowerFirst(impact);
            }

            return impact;
        }

        private static void AddImpactPart(ref string impact, string part)
        {
            if (string.IsNullOrEmpty(part))
            {
                return;
            }

            impact = string.IsNullOrEmpty(impact) ? part : impact + " " + part;
        }

        private static string AwakeImpactText(int delta)
        {
            if (delta >= 2)
            {
                return "He wakes up a lot.";
            }

            if (delta > 0)
            {
                return "He wakes a little.";
            }

            return string.Empty;
        }

        private static string TrustImpactText(int delta)
        {
            if (delta >= 2)
            {
                return "He really starts to trust you.";
            }

            if (delta > 0)
            {
                return "He lets you in a little.";
            }

            if (delta < 0)
            {
                return "He pulls away from you.";
            }

            return string.Empty;
        }

        private static string StressImpactText(int delta)
        {
            if (delta >= 2)
            {
                return "He tenses up hard.";
            }

            if (delta > 0)
            {
                return "He gets tense.";
            }

            if (delta <= -2)
            {
                return "He relaxes a lot.";
            }

            if (delta < 0)
            {
                return "He relaxes a little.";
            }

            return string.Empty;
        }

        private static string LowerFirst(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            return char.ToLowerInvariant(text[0]) + text.Substring(1);
        }

        private string BuildMorningClosedHint()
        {
            var hint = string.Empty;
            if (stats.awake < CaveheartRules.SitUpAwake)
            {
                AddOutcomeReason(ref hint, "he was still too asleep");
            }

            if (stats.trust < CaveheartRules.SitUpTrust)
            {
                AddOutcomeReason(ref hint, "he did not feel safe enough");
            }

            if (stats.stress > CaveheartRules.SitUpMaxStress)
            {
                AddOutcomeReason(ref hint, "his body was still too tense");
            }

            return string.IsNullOrEmpty(hint)
                ? "The morning ran out."
                : "Mostly, " + hint + ".";
        }

        private string BuildTenseOutcomeHint()
        {
            var hint = string.Empty;
            if (forcefulUseCount >= 3)
            {
                AddOutcomeReason(ref hint, "too many forceful pushes stayed in his body");
            }

            if (rejectedActionCount > 0)
            {
                AddOutcomeReason(ref hint, "some of his signals were missed");
            }

            if (stats.trust < CaveheartRules.SitUpTrust)
            {
                AddOutcomeReason(ref hint, "he got up before he fully trusted the morning");
            }

            if (stats.stress > CaveheartRules.SitUpMaxStress)
            {
                AddOutcomeReason(ref hint, "his body was still too tense");
            }

            return string.IsNullOrEmpty(hint)
                ? "He got up, but it was hard on him."
                : "Mostly, " + hint + ".";
        }

        private static void AddOutcomeReason(ref string hint, string reason)
        {
            if (string.IsNullOrEmpty(hint))
            {
                hint = reason;
                return;
            }

            hint += ", and " + reason;
        }

        private MorningOutcome GetMorningOutcome()
        {
            if (hasMorningClosed)
            {
                return MorningOutcome.Bad;
            }

            if (forcefulUseCount >= 3 || stats.trust < CaveheartRules.SitUpTrust || stats.stress > CaveheartRules.SitUpMaxStress)
            {
                return MorningOutcome.Bad;
            }

            var cleanVeryGood = forcefulUseCount == 0
                && rejectedActionCount == 0
                && stats.trust >= 5
                && stats.stress <= 3
                && usedMorningMinutes <= 12;
            var repairedVeryGood = forcefulUseCount == 1
                && rejectedActionCount == 0
                && stats.trust >= 5
                && stats.stress <= 2
                && usedMorningMinutes <= 12;
            if (cleanVeryGood || repairedVeryGood)
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
                $"rejected {rejectedActionCount} | " +
                $"awake {result.before.awake}->{result.after.awake}, " +
                $"trust {result.before.trust}->{result.after.trust}, " +
                $"stress {result.before.stress}->{result.after.stress} | " +
                $"{result.observedReaction}",
                this);
        }
    }

}

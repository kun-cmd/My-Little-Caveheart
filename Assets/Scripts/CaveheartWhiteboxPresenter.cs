using UnityEngine;
using UnityEngine.UI;

namespace MyLittleCaveheart
{
    public sealed class CaveheartWhiteboxPresenter : MonoBehaviour
    {
        [Header("Controller")]
        [SerializeField] private CaveheartGameController controller;

        [Header("Character Parts")]
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteRenderer blanket;
        [SerializeField] private SpriteRenderer face;
        [SerializeField] private SpriteRenderer leftArm;
        [SerializeField] private SpriteRenderer rightArm;
        [SerializeField] private SpriteRenderer tear;

        [Header("Environment")]
        [SerializeField] private SpriteRenderer coldOverlay;
        [SerializeField] private SpriteRenderer warmLight;
        [SerializeField] private SpriteRenderer stressPulse;
        [SerializeField] private Transform characterRoot;

        [Header("Text")]
        [SerializeField] private Text stateText;
        [SerializeField] private Text reactionText;
        [SerializeField] private Text timeText;
        [SerializeField] private Text endingText;
        [SerializeField] private Text debugText;
        [SerializeField] private Text whiteboxValuesText;
        [SerializeField] private bool showDebugValues = true;

        private float pulseTime;
        private float elapsedSeconds;

        public void Configure(
            CaveheartGameController targetController,
            SpriteRenderer bodyRenderer,
            SpriteRenderer blanketRenderer,
            SpriteRenderer faceRenderer,
            SpriteRenderer leftArmRenderer,
            SpriteRenderer rightArmRenderer,
            SpriteRenderer tearRenderer,
            SpriteRenderer coldOverlayRenderer,
            SpriteRenderer warmLightRenderer,
            SpriteRenderer stressPulseRenderer,
            Transform targetCharacterRoot,
            Text stateLabel,
            Text reactionLabel,
            Text timeLabel,
            Text endingLabel,
            Text debugLabel,
            Text whiteboxValuesLabel = null)
        {
            controller = targetController;
            body = bodyRenderer;
            blanket = blanketRenderer;
            face = faceRenderer;
            leftArm = leftArmRenderer;
            rightArm = rightArmRenderer;
            tear = tearRenderer;
            coldOverlay = coldOverlayRenderer;
            warmLight = warmLightRenderer;
            stressPulse = stressPulseRenderer;
            characterRoot = targetCharacterRoot;
            stateText = stateLabel;
            reactionText = reactionLabel;
            timeText = timeLabel;
            endingText = endingLabel;
            debugText = debugLabel;
            whiteboxValuesText = whiteboxValuesLabel;
        }

        private void OnEnable()
        {
            if (controller == null)
            {
                controller = FindObjectOfType<CaveheartGameController>();
            }

            if (controller != null)
            {
                controller.StateChanged += ApplyState;
                controller.InteractionResolved += ApplyReaction;
            }
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.StateChanged -= ApplyState;
                controller.InteractionResolved -= ApplyReaction;
            }
        }

        private void Update()
        {
            elapsedSeconds += Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.F3) || Input.inputString.ToLowerInvariant().Contains("d"))
            {
                showDebugValues = !showDebugValues;
                Debug.Log($"[Caveheart Whitebox] Debug values visible: {showDebugValues}", this);
            }

            if (controller != null)
            {
                ApplyState(controller.CurrentState, controller.Stats);
                UpdateDebug(controller.Stats);
            }

            UpdateTime();

            pulseTime += Time.deltaTime;
            if (stressPulse != null && controller != null)
            {
                var alpha = controller.Stats.stress >= CaveheartRules.StartledStress
                    ? 0.08f + Mathf.PingPong(pulseTime * 0.45f, 0.08f)
                    : 0f;
                SetAlpha(stressPulse, alpha);
            }
        }

        private void ApplyReaction(CaveheartInteractionResult result)
        {
            if (reactionText != null)
            {
                var acceptance = result.accepted ? "Accepted" : "Rejected";
                reactionText.text =
                    $"Last: {ActionName(result.interactionType)} ({acceptance})  " +
                    $"Time -{CaveheartRules.GetTimeCost(result.interactionType)}m  " +
                    $"Left {(controller == null ? 0 : controller.RemainingMorningMinutes)}m\n" +
                    result.observedReaction;
            }

            ApplyState(result.state, result.after);
        }

        private void ApplyState(CaveheartState state, CaveheartStats stats)
        {
            if (stateText != null)
            {
                stateText.text = $"State: {StateName(state)}";
            }

            if (endingText != null)
            {
                endingText.gameObject.SetActive(false);
            }

            SetPose(state, stats);
            UpdateDebug(stats);
        }

        private void SetPose(CaveheartState state, CaveheartStats stats)
        {
            if (characterRoot != null)
            {
                characterRoot.localPosition = state == CaveheartState.SittingUp ? new Vector3(0f, -0.15f, 0f) : new Vector3(0f, -0.55f, 0f);
                characterRoot.localRotation = Quaternion.Euler(0f, 0f, state == CaveheartState.SittingUp ? 0f : -8f);
                characterRoot.localScale = state == CaveheartState.Sleeping ? new Vector3(0.95f, 0.82f, 1f) : Vector3.one;
            }

            if (body != null)
            {
                body.color = state == CaveheartState.Resisting ? new Color(0.95f, 0.54f, 0.48f) : new Color(0.93f, 0.72f, 0.48f);
                body.transform.localScale = state == CaveheartState.SittingUp ? new Vector3(1.05f, 1.45f, 1f) : new Vector3(1.25f, 0.88f, 1f);
            }

            if (blanket != null)
            {
                blanket.transform.localPosition = state == CaveheartState.SittingUp ? new Vector3(0f, -0.95f, 0f) : new Vector3(0.1f, -0.18f, 0f);
                blanket.transform.localScale = state == CaveheartState.SittingUp ? new Vector3(1.7f, 0.45f, 1f) : new Vector3(1.95f, 1.05f, 1f);
                SetAlpha(blanket, state == CaveheartState.SittingUp ? 0.7f : 1f);
            }

            if (face != null)
            {
                face.color = state == CaveheartState.Settled || state == CaveheartState.SittingUp
                    ? new Color(0.24f, 0.15f, 0.12f)
                    : new Color(0.1f, 0.07f, 0.08f);
                face.transform.localScale = state == CaveheartState.Resisting ? new Vector3(0.78f, 0.2f, 1f) : new Vector3(0.45f, 0.18f, 1f);
            }

            if (leftArm != null)
            {
                leftArm.transform.localRotation = Quaternion.Euler(0f, 0f, state == CaveheartState.Resisting ? 45f : state == CaveheartState.Startled ? 70f : 12f);
            }

            if (rightArm != null)
            {
                rightArm.transform.localRotation = Quaternion.Euler(0f, 0f, state == CaveheartState.Resisting ? -45f : state == CaveheartState.Startled ? -70f : -12f);
            }

            if (tear != null)
            {
                tear.gameObject.SetActive(state == CaveheartState.Resisting || (state == CaveheartState.Startled && stats.stress >= CaveheartRules.StartledStress));
            }

            if (coldOverlay != null)
            {
                SetAlpha(coldOverlay, Mathf.Lerp(0.05f, 0.35f, stats.stress / 8f));
            }

            if (warmLight != null)
            {
                var warm = state == CaveheartState.Settled || state == CaveheartState.SittingUp ? 0.35f : Mathf.Clamp01(stats.trust / 12f);
                SetAlpha(warmLight, warm);
            }
        }

        private void UpdateDebug(CaveheartStats stats)
        {
            if (debugText == null)
            {
                UpdateWhiteboxValues(stats);
                return;
            }

            debugText.gameObject.SetActive(showDebugValues);
            debugText.text = $"Debug (D/F3)\nAwake {stats.awake}\nTrust {stats.trust}\nStress {stats.stress}";
            UpdateWhiteboxValues(stats);
        }

        private void UpdateWhiteboxValues(CaveheartStats stats)
        {
            if (whiteboxValuesText == null)
            {
                return;
            }

            whiteboxValuesText.gameObject.SetActive(true);
            whiteboxValuesText.text = "BODY SIGNALS";
        }

        private void UpdateTime()
        {
            if (timeText == null)
            {
                return;
            }

            var minutes = Mathf.FloorToInt(elapsedSeconds / 60f);
            var seconds = Mathf.FloorToInt(elapsedSeconds % 60f);
            timeText.text = controller == null
                ? $"Time {minutes:00}:{seconds:00}"
                : $"Time Left {controller.RemainingMorningMinutes:00}m\nUsed {controller.UsedMorningMinutes:00}/{controller.MorningTimeLimitMinutes:00}m";
        }

        private static void SetAlpha(SpriteRenderer spriteRenderer, float alpha)
        {
            var color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }

        private static string StateName(CaveheartState state)
        {
            switch (state)
            {
                case CaveheartState.Sleeping: return "Sleeping";
                case CaveheartState.Startled: return "Startled";
                case CaveheartState.Resisting: return "Resisting";
                case CaveheartState.Settled: return "Settled";
                case CaveheartState.SittingUp: return "Sitting Up";
                default: return state.ToString();
            }
        }

        private static string ActionName(CaveheartInteractionType interactionType)
        {
            switch (interactionType)
            {
                case CaveheartInteractionType.Alarm: return "Alarm";
                case CaveheartInteractionType.ShakeBed: return "Shake bed";
                case CaveheartInteractionType.GentleTouch: return "Gentle touch";
                case CaveheartInteractionType.OfferWater: return "Offer water";
                case CaveheartInteractionType.OpenCurtain: return "Open curtain";
                case CaveheartInteractionType.TuckBlanket: return "Tuck blanket";
                case CaveheartInteractionType.Scratch: return "Scratch";
                case CaveheartInteractionType.Wait: return "Wait / observe";
                default: return interactionType.ToString();
            }
        }

    }
}

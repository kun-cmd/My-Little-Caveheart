using System.Collections;
using UnityEngine;

namespace MyLittleCaveheart
{
    public sealed class ExamThoughtText : MonoBehaviour
    {
        [SerializeField] private float holdSeconds = 1f;
        [SerializeField] private TextMesh textMesh;
        [SerializeField] private BoxCollider2D hitbox;

        private float heldFor;
        private bool pointerHovering;
        private bool pointerHolding;
        private Vector3 baseScale;
        private Vector3 basePosition;
        private TextAnchor normalAnchor = TextAnchor.MiddleCenter;
        private TextAlignment normalAlignment = TextAlignment.Center;
        private float normalCharacterSize = 0.19f;
        private int normalFontSize = 38;
        private bool centeredHitbox;
        private float joltTimer;
        private Coroutine eraseRoutine;

        public string Text => textMesh == null ? string.Empty : textMesh.text;
        public TextMesh TextComponent => textMesh == null ? textMesh = GetComponent<TextMesh>() : textMesh;

        // Keeps editor-visible text on the same Short Stack font/material used at runtime.
        private void OnValidate()
        {
            textMesh = textMesh == null ? GetComponent<TextMesh>() : textMesh;
            hitbox = hitbox == null ? GetComponent<BoxCollider2D>() : hitbox;
            CaveheartTypography.ApplyTo(textMesh);
            RefreshHitbox();
        }

        // Prepares the floating thought for hold observation and stress-based shaking.
        private void Awake()
        {
            textMesh = textMesh == null ? GetComponent<TextMesh>() : textMesh;
            hitbox = hitbox == null ? GetComponent<BoxCollider2D>() : hitbox;
            if (hitbox == null)
            {
                hitbox = gameObject.AddComponent<BoxCollider2D>();
                hitbox.isTrigger = true;
            }

            baseScale = transform.localScale;
            basePosition = transform.position;
            CaveheartTypography.ApplyTo(textMesh);
            RefreshHitbox();
        }

        // Shakes the catastrophe thought while the pointer tracker controls hover and hold attention.
        private void Update()
        {
            var slowAttention = pointerHovering || pointerHolding;
            var shakeSpeed = slowAttention ? 7f : 24f;
            var shakeAmount = slowAttention ? 0.018f : 0.055f;
            var shake = Mathf.Sin(Time.time * shakeSpeed) * shakeAmount * Mathf.Max(0.5f, transform.localScale.x);
            var jolt = 0f;
            if (joltTimer > 0f)
            {
                joltTimer = Mathf.Max(0f, joltTimer - Time.deltaTime);
                jolt = Mathf.Sin(Time.time * 80f) * 0.11f * (joltTimer / 0.24f);
            }

            transform.position = basePosition + new Vector3(shake + jolt, 0f, 0f);
            transform.localScale = baseScale * Mathf.Lerp(1f, 1.08f, heldFor / holdSeconds);
        }

        // Connects this thought text to the active level controller.
        public void Bind(ExamLevelController targetController)
        {
        }

        // Lets the level controller use the TextMesh explicitly assigned in the stage layout Inspector.
        public void UseTextMesh(TextMesh targetTextMesh)
        {
            if (targetTextMesh != null)
            {
                textMesh = targetTextMesh;
            }

            CaveheartTypography.ApplyTo(textMesh);
            RefreshHitbox();
        }

        // Replaces the thought sentence for the current stage and resets hold state.
        public void SetText(string value)
        {
            if (textMesh != null)
            {
                textMesh.text = value;
            }

            pointerHolding = false;
            pointerHovering = false;
            heldFor = 0f;
            joltTimer = 0f;
            basePosition = transform.position;
            RefreshHitbox();
        }

        // Applies the one shared visible style used by all splittable thought sentences.
        public void SetStandardPresentation()
        {
            if (textMesh == null)
            {
                return;
            }

            centeredHitbox = true;
            textMesh.anchor = normalAnchor;
            textMesh.alignment = normalAlignment;
            textMesh.characterSize = normalCharacterSize;
            textMesh.fontSize = normalFontSize;

            CaveheartTypography.ApplyTo(textMesh);
            RefreshHitbox();
        }

        // Scales the thought to make higher stress feel visually heavier.
        public void SetPressure(int stress)
        {
            baseScale = Vector3.one * Mathf.Lerp(0.86f, 1.15f, Mathf.Clamp01(stress / 8f));
            if (!pointerHolding)
            {
                transform.localScale = baseScale;
            }
        }

        // Lets the pointer tracker slow the shake and scale the thought during a hold.
        public void SetPointerAttention(bool hovering, bool holding, float progress01)
        {
            pointerHovering = hovering;
            pointerHolding = holding;
            heldFor = Mathf.Clamp01(progress01) * holdSeconds;
            if (!holding)
            {
                transform.localScale = baseScale;
            }
        }

        // Marks this thought as observed so it stops responding to hover and hold visuals.
        public void MarkObserved()
        {
            pointerHovering = false;
            pointerHolding = false;
            heldFor = 0f;
        }

        // Makes the thought rebound larger after the eraser fails to remove it.
        public void PulseLouder()
        {
            baseScale *= 1.04f;
            transform.localScale = baseScale;
            Jolt();
            RefreshHitbox();
        }

        // Produces the short side-to-side wobble used when clicking or dragging fails.
        public void Jolt()
        {
            joltTimer = 0.24f;
        }

        // Briefly hides the thought after an erase attempt, then snaps it back louder.
        public void FlickerAwayThenReturn()
        {
            if (eraseRoutine != null)
            {
                StopCoroutine(eraseRoutine);
            }

            eraseRoutine = StartCoroutine(FlickerAwayRoutine());
        }

        // Keeps the pointer hitbox roughly aligned to the current thought text.
        private void RefreshHitbox()
        {
            if (hitbox == null || textMesh == null)
            {
                return;
            }

            var longestLineLength = LongestLineLength(textMesh.text);
            hitbox.size = new Vector2(Mathf.Max(2.4f, longestLineLength * 0.105f), 0.68f);
            if (centeredHitbox)
            {
                hitbox.size = new Vector2(Mathf.Max(3.1f, longestLineLength * 0.12f), 0.86f);
                hitbox.offset = Vector2.zero;
            }
            else
            {
                hitbox.offset = new Vector2(hitbox.size.x * 0.5f, 0f);
            }
        }

        // Measures text width by its longest visual line so multiline thoughts keep sensible hitboxes.
        private static int LongestLineLength(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            var longest = 0;
            var current = 0;
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] == '\n')
                {
                    longest = Mathf.Max(longest, current);
                    current = 0;
                    continue;
                }

                if (value[i] != '\r')
                {
                    current++;
                }
            }

            return Mathf.Max(longest, current);
        }

        // Runs the visual rebound for the eraser: gone for a blink, then present again.
        private IEnumerator FlickerAwayRoutine()
        {
            var renderer = textMesh == null ? null : textMesh.gameObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }

            yield return new WaitForSeconds(0.16f);

            if (renderer != null)
            {
                renderer.enabled = true;
            }

            PulseLouder();
            eraseRoutine = null;
        }
    }
}

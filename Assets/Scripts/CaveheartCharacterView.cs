using UnityEngine;

namespace MyLittleCaveheart
{
    public sealed class CaveheartCharacterView : MonoBehaviour
    {
        [SerializeField] private Transform characterRoot;
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteRenderer blanket;
        [SerializeField] private SpriteRenderer face;
        [SerializeField] private SpriteRenderer leftArm;
        [SerializeField] private SpriteRenderer rightArm;
        [SerializeField] private SpriteRenderer tear;
        [SerializeField] private bool hideLegacyBodyWhenSpriteAnimatorIsPresent = true;

        private Sprite squareSprite;
        private SpriteRenderer leftEye;
        private SpriteRenderer rightEye;
        private SpriteRenderer mouth;
        private SpriteRenderer browLeft;
        private SpriteRenderer browRight;
        private SpriteRenderer blush;
        private TextMesh speechText;
        private float stateWiggle;
        private bool hasAcceptedWater;

        public void SetHasAcceptedWater(bool value)
        {
            hasAcceptedWater = value;
        }

        public void BindExistingScene()
        {
            characterRoot = FindTransform("Little Caveheart Character");
            body = FindRenderer("Body");
            blanket = FindRenderer("Blanket");
            face = FindRenderer("Face Mood Mark");
            leftArm = FindRenderer("Left Arm");
            rightArm = FindRenderer("Right Arm");
            tear = FindRenderer("Tear Marker");

            squareSprite = body != null ? body.sprite : CreateSquareSprite();
            EnsureExpressionParts();
        }

        public void ApplyState(CaveheartState state, CaveheartStats stats)
        {
            if (characterRoot == null)
            {
                BindExistingScene();
            }

            stateWiggle = state == CaveheartState.Startled || state == CaveheartState.Resisting ? 1f : 0f;

            switch (state)
            {
                case CaveheartState.Sleeping:
                    ApplySleepingPose();
                    ApplySleepyExpression();
                    SetSpeech("...");
                    break;
                case CaveheartState.Startled:
                    ApplyStartledPose();
                    ApplyScaredExpression();
                    SetSpeech("too loud...");
                    break;
                case CaveheartState.Resisting:
                    ApplyResistingPose();
                    ApplyScaredExpression();
                    SetSpeech("no... stop");
                    break;
                case CaveheartState.Settled:
                    ApplySettledPose();
                    ApplySafeExpression();
                    SetSpeech("maybe... slowly");
                    break;
                case CaveheartState.SittingUp:
                    ApplySittingPose();
                    ApplySafeExpression();
                    SetSpeech("I can go slowly.");
                    break;
            }

            if (!hasAcceptedWater && state != CaveheartState.SittingUp && stats.trust >= CaveheartRules.WaterTrustGate)
            {
                SetSpeech("...");
            }

            if (tear != null)
            {
                tear.gameObject.SetActive(state == CaveheartState.Resisting || (state == CaveheartState.Startled && stats.stress >= CaveheartRules.StartledStress));
            }

            if (hideLegacyBodyWhenSpriteAnimatorIsPresent && GameObject.Find("Little Caveheart Animated Sprite") != null)
            {
                HideLegacyBodyRenderers();
            }
        }

        public void ApplyReaction(CaveheartInteractionResult result)
        {
            ApplyState(result.state, result.after);

            if (!result.accepted)
            {
                if (result.interactionType == CaveheartInteractionType.TuckBlanket)
                {
                    SetSpeech("too hot...");
                }
                else if (result.interactionType == CaveheartInteractionType.OfferWater)
                {
                    SetSpeech("not yet...");
                }
                else
                {
                    SetSpeech("please wait");
                }
            }
        }

        private void Update()
        {
            if (characterRoot == null || stateWiggle <= 0f)
            {
                return;
            }

            var shake = Mathf.Sin(Time.time * 30f) * 0.035f * stateWiggle;
            characterRoot.localPosition += new Vector3(shake, 0f, 0f);
            stateWiggle = Mathf.Max(0f, stateWiggle - Time.deltaTime * 2.5f);
        }

        private void EnsureExpressionParts()
        {
            if (characterRoot == null || squareSprite == null)
            {
                return;
            }

            leftEye = EnsurePart("Expression - Left Eye", new Color(0.08f, 0.04f, 0.03f), 12);
            rightEye = EnsurePart("Expression - Right Eye", new Color(0.08f, 0.04f, 0.03f), 12);
            mouth = EnsurePart("Expression - Mouth", new Color(0.08f, 0.04f, 0.03f), 12);
            browLeft = EnsurePart("Expression - Left Brow", new Color(0.08f, 0.04f, 0.03f), 13);
            browRight = EnsurePart("Expression - Right Brow", new Color(0.08f, 0.04f, 0.03f), 13);
            blush = EnsurePart("Expression - Warm Cheek", new Color(1f, 0.5f, 0.42f, 0.45f), 11);

            if (speechText == null)
            {
                var speech = new GameObject("Speech - Little Caveheart");
                speech.transform.SetParent(characterRoot, false);
                speech.transform.localPosition = new Vector3(0.3f, 1.35f, -0.3f);
                speechText = speech.AddComponent<TextMesh>();
                speechText.anchor = TextAnchor.MiddleCenter;
                speechText.alignment = TextAlignment.Center;
                speechText.characterSize = 0.22f;
                speechText.fontSize = 48;
                speechText.color = Color.white;
                speech.GetComponent<MeshRenderer>().sortingOrder = 60;
            }
        }

        private void HideLegacyBodyRenderers()
        {
            SetActive(body, false);
            SetActive(blanket, false);
            SetActive(face, false);
            SetActive(leftArm, false);
            SetActive(rightArm, false);
            SetActive(tear, false);
            SetActive(leftEye, false);
            SetActive(rightEye, false);
            SetActive(mouth, false);
            SetActive(browLeft, false);
            SetActive(browRight, false);
            SetActive(blush, false);
        }

        private SpriteRenderer EnsurePart(string name, Color color, int sortingOrder)
        {
            var existing = characterRoot.Find(name);
            var obj = existing == null ? new GameObject(name) : existing.gameObject;
            obj.transform.SetParent(characterRoot, false);

            var renderer = obj.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = obj.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = squareSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private void ApplySleepingPose()
        {
            SetRoot(new Vector3(0f, -0.55f, 0f), -12f, new Vector3(0.92f, 0.78f, 1f));
            SetPart(body, new Vector3(0f, -0.18f, 0f), new Vector3(1.35f, 0.72f, 1f), 0f);
            SetPart(blanket, new Vector3(0.1f, -0.18f, -0.05f), new Vector3(2.05f, 1.1f, 1f), 0f);
            SetPart(leftArm, new Vector3(-0.74f, -0.05f, -0.03f), new Vector3(0.75f, 0.16f, 1f), 20f);
            SetPart(rightArm, new Vector3(0.74f, -0.05f, -0.03f), new Vector3(0.75f, 0.16f, 1f), -20f);
        }

        private void ApplyStartledPose()
        {
            SetRoot(new Vector3(0f, -0.45f, 0f), -6f, Vector3.one);
            SetPart(body, new Vector3(0f, -0.1f, 0f), new Vector3(1.15f, 0.95f, 1f), 0f);
            SetPart(blanket, new Vector3(0.12f, -0.28f, -0.05f), new Vector3(1.9f, 0.95f, 1f), 0f);
            SetPart(leftArm, new Vector3(-0.72f, 0.08f, -0.03f), new Vector3(0.78f, 0.16f, 1f), 78f);
            SetPart(rightArm, new Vector3(0.72f, 0.08f, -0.03f), new Vector3(0.78f, 0.16f, 1f), -78f);
        }

        private void ApplyResistingPose()
        {
            SetRoot(new Vector3(-0.15f, -0.48f, 0f), -18f, new Vector3(1f, 0.92f, 1f));
            SetPart(body, new Vector3(-0.05f, -0.1f, 0f), new Vector3(1.1f, 0.9f, 1f), 0f);
            SetPart(blanket, new Vector3(0.22f, -0.2f, -0.05f), new Vector3(2.1f, 0.85f, 1f), -8f);
            SetPart(leftArm, new Vector3(-0.82f, 0.08f, -0.03f), new Vector3(0.9f, 0.18f, 1f), 145f);
            SetPart(rightArm, new Vector3(0.78f, 0.08f, -0.03f), new Vector3(0.9f, 0.18f, 1f), -145f);
        }

        private void ApplySettledPose()
        {
            SetRoot(new Vector3(0f, -0.38f, 0f), -3f, Vector3.one);
            SetPart(body, new Vector3(0f, -0.05f, 0f), new Vector3(1.15f, 0.95f, 1f), 0f);
            SetPart(blanket, new Vector3(0.05f, -0.42f, -0.05f), new Vector3(1.85f, 0.75f, 1f), 0f);
            SetPart(leftArm, new Vector3(-0.67f, -0.02f, -0.03f), new Vector3(0.7f, 0.15f, 1f), 5f);
            SetPart(rightArm, new Vector3(0.67f, -0.02f, -0.03f), new Vector3(0.7f, 0.15f, 1f), -5f);
        }

        private void ApplySittingPose()
        {
            SetRoot(new Vector3(0f, -0.12f, 0f), 0f, Vector3.one);
            SetPart(body, new Vector3(0f, 0.08f, 0f), new Vector3(1.05f, 1.45f, 1f), 0f);
            SetPart(blanket, new Vector3(0f, -0.95f, -0.05f), new Vector3(1.7f, 0.45f, 1f), 0f);
            SetPart(leftArm, new Vector3(-0.7f, -0.05f, -0.03f), new Vector3(0.75f, 0.16f, 1f), -25f);
            SetPart(rightArm, new Vector3(0.7f, -0.05f, -0.03f), new Vector3(0.75f, 0.16f, 1f), 25f);
        }

        private void ApplySleepyExpression()
        {
            SetPart(leftEye, new Vector3(0.02f, 0.18f, -0.08f), new Vector3(0.18f, 0.035f, 1f), -8f);
            SetPart(rightEye, new Vector3(0.42f, 0.15f, -0.08f), new Vector3(0.18f, 0.035f, 1f), -8f);
            SetPart(mouth, new Vector3(0.24f, -0.05f, -0.08f), new Vector3(0.22f, 0.04f, 1f), 0f);
            SetBrows(false, 0f);
            SetActive(blush, false);
        }

        private void ApplyScaredExpression()
        {
            SetPart(leftEye, new Vector3(0.02f, 0.2f, -0.08f), new Vector3(0.16f, 0.16f, 1f), 0f);
            SetPart(rightEye, new Vector3(0.42f, 0.2f, -0.08f), new Vector3(0.16f, 0.16f, 1f), 0f);
            SetPart(mouth, new Vector3(0.23f, -0.08f, -0.08f), new Vector3(0.26f, 0.12f, 1f), 0f);
            SetBrows(true, 18f);
            SetActive(blush, false);
        }

        private void ApplySafeExpression()
        {
            SetPart(leftEye, new Vector3(0.04f, 0.18f, -0.08f), new Vector3(0.2f, 0.055f, 1f), 8f);
            SetPart(rightEye, new Vector3(0.42f, 0.18f, -0.08f), new Vector3(0.2f, 0.055f, 1f), -8f);
            SetPart(mouth, new Vector3(0.24f, -0.05f, -0.08f), new Vector3(0.28f, 0.055f, 1f), 0f);
            SetBrows(false, 0f);
            SetPart(blush, new Vector3(0.23f, 0.02f, -0.09f), new Vector3(0.72f, 0.12f, 1f), 0f);
            SetActive(blush, true);
        }

        private void SetBrows(bool active, float tilt)
        {
            SetActive(browLeft, active);
            SetActive(browRight, active);
            if (!active)
            {
                return;
            }

            SetPart(browLeft, new Vector3(0.03f, 0.36f, -0.09f), new Vector3(0.28f, 0.045f, 1f), tilt);
            SetPart(browRight, new Vector3(0.42f, 0.36f, -0.09f), new Vector3(0.28f, 0.045f, 1f), -tilt);
        }

        private void SetSpeech(string text)
        {
            if (speechText != null)
            {
                speechText.text = text;
            }
        }

        private void SetRoot(Vector3 localPosition, float zRotation, Vector3 localScale)
        {
            if (characterRoot == null)
            {
                return;
            }

            characterRoot.localPosition = localPosition;
            characterRoot.localRotation = Quaternion.Euler(0f, 0f, zRotation);
            characterRoot.localScale = localScale;
        }

        private static void SetPart(SpriteRenderer part, Vector3 localPosition, Vector3 localScale, float zRotation)
        {
            if (part == null)
            {
                return;
            }

            part.gameObject.SetActive(true);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);
        }

        private static void SetActive(SpriteRenderer part, bool active)
        {
            if (part != null)
            {
                part.gameObject.SetActive(active);
            }
        }

        private static Transform FindTransform(string objectName)
        {
            var obj = GameObject.Find(objectName);
            return obj == null ? null : obj.transform;
        }

        private static SpriteRenderer FindRenderer(string objectName)
        {
            var obj = GameObject.Find(objectName);
            return obj == null ? null : obj.GetComponent<SpriteRenderer>();
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

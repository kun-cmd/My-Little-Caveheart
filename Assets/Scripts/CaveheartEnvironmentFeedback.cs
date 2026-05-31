using UnityEngine;

namespace MyLittleCaveheart
{
    [DisallowMultipleComponent]
    public sealed class CaveheartEnvironmentFeedback : MonoBehaviour
    {
        [SerializeField] private CaveheartGameController controller;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private SpriteRenderer coldOverlay;
        [SerializeField] private SpriteRenderer warmLight;
        [SerializeField] private SpriteRenderer stressPulse;
        [SerializeField] private SpriteRenderer doorPressureShadow;
        [SerializeField] private SpriteRenderer alarmPressureLines;

        private Sprite squareSprite;
        private Vector3 cameraBasePosition;
        private Color cameraBaseColor;
        private float pressureKick;
        private float pulseTime;
        private bool hasCameraBase;

        private void Awake()
        {
            BindScene();
        }

        private void OnEnable()
        {
            BindScene();
            if (controller != null)
            {
                controller.InteractionResolved += OnInteractionResolved;
            }
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.InteractionResolved -= OnInteractionResolved;
            }
        }

        private void Update()
        {
            BindScene();
            if (controller == null)
            {
                return;
            }

            pulseTime += Time.deltaTime;
            pressureKick = Mathf.Max(0f, pressureKick - Time.deltaTime * 1.8f);

            var stats = controller.Stats;
            var stress01 = Mathf.Clamp01(stats.stress / 8f);
            var trust01 = Mathf.Clamp01(stats.trust / 8f);
            var time01 = controller.MorningTimeLimitMinutes <= 0
                ? 0f
                : 1f - Mathf.Clamp01(controller.RemainingMorningMinutes / (float)controller.MorningTimeLimitMinutes);
            var isSettled = controller.CurrentState == CaveheartState.Settled || controller.CurrentState == CaveheartState.SittingUp;

            SetAlpha(coldOverlay, Mathf.Lerp(0.04f, 0.34f, stress01) + time01 * 0.08f);
            SetAlpha(stressPulse, stats.stress >= CaveheartRules.ResistStress
                ? 0.08f + Mathf.PingPong(pulseTime * 0.75f, 0.16f)
                : 0f);
            SetAlpha(warmLight, isSettled ? Mathf.Lerp(0.22f, 0.48f, trust01) : Mathf.Lerp(0.08f, 0.22f, trust01));
            SetAlpha(doorPressureShadow, Mathf.Clamp01(time01 * 0.3f + stress01 * 0.18f));
            SetAlpha(alarmPressureLines, Mathf.Clamp01(stress01 * 0.45f + pressureKick * 0.35f));

            if (targetCamera != null)
            {
                var coldColor = new Color(0.075f, 0.085f, 0.105f);
                var warmColor = new Color(0.12f, 0.095f, 0.07f);
                targetCamera.backgroundColor = Color.Lerp(coldColor, warmColor, Mathf.Clamp01(trust01 - stress01 * 0.45f + 0.25f));

                var shakeStrength = Mathf.Max(stress01 - 0.35f, 0f) * 0.08f + pressureKick * 0.06f;
                if (controller.CurrentState == CaveheartState.SittingUp)
                {
                    shakeStrength = 0f;
                }

                var shake = new Vector3(
                    Mathf.Sin(Time.time * 39f) * shakeStrength,
                    Mathf.Cos(Time.time * 43f) * shakeStrength * 0.75f,
                    0f);
                targetCamera.transform.position = cameraBasePosition + shake;
            }
        }

        private void OnInteractionResolved(CaveheartInteractionResult result)
        {
            if (result.interactionType == CaveheartInteractionType.Alarm || result.interactionType == CaveheartInteractionType.ShakeBed || !result.accepted)
            {
                pressureKick = 1f;
            }
        }

        private void BindScene()
        {
            if (controller == null)
            {
                controller = FindObjectOfType<CaveheartGameController>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera != null && !hasCameraBase)
            {
                cameraBasePosition = targetCamera.transform.position;
                cameraBaseColor = targetCamera.backgroundColor;
                hasCameraBase = true;
            }

            squareSprite = squareSprite == null ? FindReusableSprite() ?? CreateSquareSprite() : squareSprite;
            coldOverlay = coldOverlay == null ? FindRenderer("Cold Stress Overlay") : coldOverlay;
            warmLight = warmLight == null ? FindRenderer("Warm Morning Light") : warmLight;
            stressPulse = stressPulse == null ? FindRenderer("Stress Pulse Overlay") : stressPulse;
            doorPressureShadow = doorPressureShadow == null
                ? EnsureSprite("Door Time Pressure Shadow", new Vector3(-6.6f, 0.8f, 4.6f), new Vector3(1.5f, 5.6f, 1f), new Color(0.02f, 0.025f, 0.03f, 0f), 18)
                : doorPressureShadow;
            alarmPressureLines = alarmPressureLines == null
                ? EnsureSprite("Alarm Pressure Lines", new Vector3(-6.1f, -0.25f, 4.7f), new Vector3(2.0f, 1.6f, 1f), new Color(1f, 0.2f, 0.18f, 0f), 22)
                : alarmPressureLines;
        }

        private SpriteRenderer EnsureSprite(string objectName, Vector3 position, Vector3 scale, Color color, int sortingOrder)
        {
            var existing = GameObject.Find(objectName);
            var obj = existing == null ? new GameObject(objectName) : existing;
            obj.transform.position = position;
            obj.transform.localScale = scale;

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

        private static SpriteRenderer FindRenderer(string objectName)
        {
            var obj = GameObject.Find(objectName);
            return obj == null ? null : obj.GetComponent<SpriteRenderer>();
        }

        private static void SetAlpha(SpriteRenderer renderer, float alpha)
        {
            if (renderer == null)
            {
                return;
            }

            var color = renderer.color;
            color.a = Mathf.Clamp01(alpha);
            renderer.color = color;
        }

        private static Sprite FindReusableSprite()
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

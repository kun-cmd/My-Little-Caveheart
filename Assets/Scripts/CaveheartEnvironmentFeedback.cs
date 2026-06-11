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

        private Vector3 cameraBasePosition;
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
                hasCameraBase = true;
            }

            coldOverlay = coldOverlay == null ? FindRenderer("Cold Stress Overlay") : coldOverlay;
            warmLight = warmLight == null ? FindRenderer("Warm Morning Light") : warmLight;
            stressPulse = stressPulse == null ? FindRenderer("Stress Pulse Overlay") : stressPulse;
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
    }
}

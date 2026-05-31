using UnityEngine;

namespace MyLittleCaveheart
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class CaveheartAudioFeedback : MonoBehaviour
    {
        [SerializeField] private CaveheartGameController controller;
        [SerializeField] private AudioSource audioSource;

        private AudioClip alarmClip;
        private AudioClip softClip;
        private AudioClip waterClip;
        private AudioClip rustleClip;
        private AudioClip upsetClip;
        private AudioClip lightClip;

        private void Awake()
        {
            audioSource = audioSource == null ? GetComponent<AudioSource>() : audioSource;
            alarmClip = CreateTone("WhiteboxAlarm", 880f, 0.18f, 0.35f);
            softClip = CreateTone("WhiteboxTouch", 330f, 0.15f, 0.16f);
            waterClip = CreateTone("WhiteboxWater", 520f, 0.12f, 0.12f);
            rustleClip = CreateNoise("WhiteboxBlanket", 0.2f, 0.1f);
            upsetClip = CreateTone("WhiteboxUpset", 180f, 0.28f, 0.22f);
            lightClip = CreateTone("WhiteboxLight", 660f, 0.4f, 0.12f);
        }

        private void OnEnable()
        {
            if (controller == null)
            {
                controller = FindObjectOfType<CaveheartGameController>();
            }

            if (controller != null)
            {
                controller.InteractionResolved += PlayFeedback;
            }
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.InteractionResolved -= PlayFeedback;
            }
        }

        private void PlayFeedback(CaveheartInteractionResult result)
        {
            if (audioSource == null)
            {
                return;
            }

            AudioClip clip;
            switch (result.interactionType)
            {
                case CaveheartInteractionType.Alarm:
                    clip = alarmClip;
                    break;
                case CaveheartInteractionType.ShakeBed:
                    clip = upsetClip;
                    break;
                case CaveheartInteractionType.GentleTouch:
                    clip = result.accepted ? softClip : upsetClip;
                    break;
                case CaveheartInteractionType.OfferWater:
                    clip = result.accepted ? waterClip : upsetClip;
                    break;
                case CaveheartInteractionType.OpenCurtain:
                    clip = lightClip;
                    break;
                case CaveheartInteractionType.TuckBlanket:
                    clip = rustleClip;
                    break;
                case CaveheartInteractionType.Scratch:
                    clip = result.accepted ? softClip : upsetClip;
                    break;
                default:
                    clip = softClip;
                    break;
            }

            audioSource.PlayOneShot(clip);
        }

        private static AudioClip CreateTone(string name, float frequency, float duration, float volume)
        {
            const int sampleRate = 44100;
            var sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var samples = new float[sampleCount];
            for (var i = 0; i < samples.Length; i++)
            {
                var fade = 1f - (i / (float)samples.Length);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * volume * fade;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateNoise(string name, float duration, float volume)
        {
            const int sampleRate = 44100;
            var sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var samples = new float[sampleCount];
            for (var i = 0; i < samples.Length; i++)
            {
                var fade = 1f - (i / (float)samples.Length);
                samples[i] = Random.Range(-volume, volume) * fade;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}

using System.Collections;
using UnityEngine;

namespace MyLittleCaveheart
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class CaveheartAudioFeedback : MonoBehaviour
    {
        [SerializeField] private CaveheartGameController controller;
        [SerializeField] private AudioSource audioSource;

        private AudioClip alarmClip;
        private AudioClip touchClip;
        private AudioClip waterClip;
        private AudioClip observeClip;
        private AudioClip scratchClip;
        private AudioClip upsetClip;
        private AudioClip lightClip;
        private AudioClip positiveTwoToneClip;
        private AudioClip positiveThreeToneClip;
        private AudioClip negativeThreeToneClip;
        private AudioClip rejectTwoClip;
        private AudioClip rejectThreeClip;
        private AudioClip highToneClip;
        private AudioClip lowToneClip;
        private Coroutine feedbackRoutine;

        private void Awake()
        {
            audioSource = audioSource == null ? GetComponent<AudioSource>() : audioSource;
            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
                audioSource.volume = 0.9f;
            }

            alarmClip = LoadClip("action_urge") ?? CreateTone("CaveheartActionUrge", 880f, 0.14f, 0.32f);
            touchClip = LoadClip("action_click") ?? CreateNoise("CaveheartActionTouch", 0.12f, 0.055f);
            waterClip = LoadClip("action_water") ?? CreateTone("CaveheartActionWater", 520f, 0.13f, 0.13f);
            observeClip = LoadClip("action_observe_breath") ?? LoadClip("action_click") ?? CreateTone("CaveheartActionObserve", 290f, 0.16f, 0.075f);
            scratchClip = LoadClip("action_click") ?? CreateTwoTone("CaveheartActionScratch", 470f, 610f, 0.16f, 0.12f);
            upsetClip = LoadClip("action_urge") ?? CreateTone("CaveheartResultReject", 170f, 0.18f, 0.2f);
            lightClip = LoadClip("action_window") ?? CreateTwoTone("CaveheartActionWindow", 580f, 760f, 0.22f, 0.11f);
            positiveTwoToneClip = LoadClip("feedback_positive_2") ?? CreateSequenceTone("CaveheartFeedbackPositive2", new[] { 440f, 660f }, 0.08f, 0.11f);
            positiveThreeToneClip = LoadClip("feedback_positive_3") ?? CreateSequenceTone("CaveheartFeedbackPositive3", new[] { 392f, 523f, 659f }, 0.075f, 0.11f);
            negativeThreeToneClip = LoadClip("feedback_negative_3") ?? CreateSequenceTone("CaveheartFeedbackNegative3", new[] { 360f, 240f, 160f }, 0.075f, 0.12f);
            rejectTwoClip = LoadClip("feedback_reject_2") ?? CreateSequenceTone("CaveheartFeedbackReject2", new[] { 240f, 170f }, 0.08f, 0.12f);
            rejectThreeClip = LoadClip("feedback_reject_3") ?? CreateSequenceTone("CaveheartFeedbackReject3", new[] { 300f, 220f, 160f }, 0.075f, 0.12f);
            highToneClip = CreateTone("CaveheartFeedbackHigh", 520f, 0.12f, 0.08f);
            lowToneClip = CreateTone("CaveheartFeedbackLow", 145f, 0.12f, 0.1f);

            PrepareClip(alarmClip);
            PrepareClip(touchClip);
            PrepareClip(waterClip);
            PrepareClip(observeClip);
            PrepareClip(scratchClip);
            PrepareClip(upsetClip);
            PrepareClip(lightClip);
            PrepareClip(positiveTwoToneClip);
            PrepareClip(positiveThreeToneClip);
            PrepareClip(negativeThreeToneClip);
            PrepareClip(rejectTwoClip);
            PrepareClip(rejectThreeClip);
        }

        private static AudioClip LoadClip(string clipName)
        {
            var clip = Resources.Load<AudioClip>("Audio/Caveheart/" + clipName);
            if (clip == null)
            {
                Debug.LogWarning($"Caveheart audio clip could not be loaded: {clipName}");
            }

            return clip;
        }

        private static void PrepareClip(AudioClip clip)
        {
            if (clip != null && clip.loadState == AudioDataLoadState.Unloaded)
            {
                clip.LoadAudioData();
            }
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

            if (feedbackRoutine != null)
            {
                StopCoroutine(feedbackRoutine);
                feedbackRoutine = null;
            }
        }

        private void PlayFeedback(CaveheartInteractionResult result)
        {
            if (audioSource == null)
            {
                return;
            }

            var actionClip = GetActionClip(result.interactionType);
            var feedbackClip = GetFeedbackClip(result);
            if (feedbackRoutine != null)
            {
                StopCoroutine(feedbackRoutine);
            }

            audioSource.Stop();
            feedbackRoutine = StartCoroutine(PlayFeedbackSequence(actionClip, feedbackClip));
        }

        private IEnumerator PlayFeedbackSequence(AudioClip actionClip, AudioClip feedbackClip)
        {
            if (actionClip != null)
            {
                yield return WaitForAudioData(actionClip);
                if (actionClip.loadState == AudioDataLoadState.Loaded)
                {
                    audioSource.PlayOneShot(actionClip, 0.8f);
                    yield return new WaitForSeconds(actionClip.length);
                }
            }

            if (feedbackClip != null)
            {
                yield return WaitForAudioData(feedbackClip);
                if (feedbackClip.loadState == AudioDataLoadState.Loaded)
                {
                    audioSource.PlayOneShot(feedbackClip, 0.72f);
                }
            }

            feedbackRoutine = null;
        }

        private static IEnumerator WaitForAudioData(AudioClip clip)
        {
            PrepareClip(clip);
            while (clip != null && clip.loadState == AudioDataLoadState.Loading)
            {
                yield return null;
            }

            if (clip != null && clip.loadState == AudioDataLoadState.Failed)
            {
                Debug.LogWarning($"Caveheart audio data failed to load: {clip.name}");
            }
        }

        private AudioClip GetActionClip(CaveheartInteractionType interactionType)
        {
            switch (interactionType)
            {
                case CaveheartInteractionType.Alarm:
                    return alarmClip;
                case CaveheartInteractionType.ShakeBed:
                    return upsetClip;
                case CaveheartInteractionType.GentleTouch:
                    return touchClip;
                case CaveheartInteractionType.OfferWater:
                    return waterClip;
                case CaveheartInteractionType.OpenCurtain:
                    return lightClip;
                case CaveheartInteractionType.TuckBlanket:
                    return touchClip;
                case CaveheartInteractionType.Scratch:
                    return scratchClip;
                case CaveheartInteractionType.Wait:
                    return observeClip;
                default:
                    return null;
            }
        }

        private AudioClip GetFeedbackClip(CaveheartInteractionResult result)
        {
            var awakeDelta = result.after.awake - result.before.awake;
            var trustDelta = result.after.trust - result.before.trust;
            var stressDelta = result.after.stress - result.before.stress;

            if (!result.accepted)
            {
                return Random.value < 0.5f ? rejectTwoClip : rejectThreeClip;
            }

            if (trustDelta < 0 || stressDelta >= 2)
            {
                return negativeThreeToneClip != null ? negativeThreeToneClip : lowToneClip;
            }

            if (stressDelta > 0)
            {
                return negativeThreeToneClip != null ? negativeThreeToneClip : lowToneClip;
            }

            if (trustDelta >= 2 || stressDelta <= -2)
            {
                return positiveThreeToneClip != null ? positiveThreeToneClip : highToneClip;
            }

            if (trustDelta > 0 || stressDelta < 0)
            {
                return positiveTwoToneClip != null ? positiveTwoToneClip : highToneClip;
            }

            if (awakeDelta > 0)
            {
                return highToneClip;
            }

            return null;
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

        private static AudioClip CreateSequenceTone(string name, float[] frequencies, float noteDuration, float volume)
        {
            const int sampleRate = 44100;
            var gapDuration = noteDuration * 0.28f;
            var totalDuration = frequencies.Length * noteDuration + (frequencies.Length - 1) * gapDuration;
            var sampleCount = Mathf.CeilToInt(sampleRate * totalDuration);
            var samples = new float[sampleCount];
            var noteStride = noteDuration + gapDuration;

            for (var i = 0; i < samples.Length; i++)
            {
                var time = i / (float)sampleRate;
                var noteIndex = Mathf.FloorToInt(time / noteStride);
                if (noteIndex < 0 || noteIndex >= frequencies.Length)
                {
                    continue;
                }

                var noteTime = time - noteIndex * noteStride;
                if (noteTime > noteDuration)
                {
                    continue;
                }

                var ratio = noteTime / noteDuration;
                var attack = Mathf.Clamp01(ratio / 0.18f);
                var release = Mathf.Clamp01((1f - ratio) / 0.28f);
                var envelope = Mathf.Min(attack, release);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequencies[noteIndex] * noteTime) * volume * envelope;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateTwoTone(string name, float firstFrequency, float secondFrequency, float duration, float volume)
        {
            const int sampleRate = 44100;
            var sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var samples = new float[sampleCount];
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)sampleRate;
                var ratio = i / (float)samples.Length;
                var frequency = ratio < 0.48f ? firstFrequency : secondFrequency;
                var attack = Mathf.Clamp01(ratio / 0.12f);
                var release = 1f - ratio;
                var envelope = Mathf.Min(attack, release);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * volume * envelope;
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

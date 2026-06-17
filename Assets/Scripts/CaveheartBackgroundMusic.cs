using UnityEngine;

namespace MyLittleCaveheart
{
    public sealed class CaveheartBackgroundMusic : MonoBehaviour
    {
        private const string MusicResourcePath = "Audio/Caveheart/bgm_pale_sunrise";

        [Header("Music")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip musicClip;
        [SerializeField, Range(0f, 1f)] private float volume = 0.28f;
        private Coroutine playWhenReadyRoutine;

        public static CaveheartBackgroundMusic Ensure(AudioClip preferredClip = null, float preferredVolume = 0.28f)
        {
            var existing = FindObjectOfType<CaveheartBackgroundMusic>();
            if (existing != null)
            {
                existing.ApplyPreferences(preferredClip, preferredVolume);
                existing.Play();
                return existing;
            }

            var obj = new GameObject("Caveheart Background Music");
            DontDestroyOnLoad(obj);
            var music = obj.AddComponent<CaveheartBackgroundMusic>();
            music.ApplyPreferences(preferredClip, preferredVolume);
            music.Play();
            return music;
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Play();
        }

        public void Play()
        {
            audioSource = audioSource == null ? GetComponent<AudioSource>() : audioSource;
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.spatialBlend = 0f;
            audioSource.volume = volume;

            if (musicClip == null)
            {
                musicClip = Resources.Load<AudioClip>(MusicResourcePath);
            }

            if (audioSource.clip == null)
            {
                audioSource.clip = musicClip;
            }

            if (audioSource.clip == null)
            {
                Debug.LogWarning($"Caveheart background music could not be loaded: {MusicResourcePath}", this);
                return;
            }

            if (audioSource.clip.loadState == AudioDataLoadState.Unloaded)
            {
                audioSource.clip.LoadAudioData();
            }

            if (playWhenReadyRoutine != null)
            {
                StopCoroutine(playWhenReadyRoutine);
            }

            playWhenReadyRoutine = StartCoroutine(PlayWhenReady());
        }

        private void ApplyPreferences(AudioClip preferredClip, float preferredVolume)
        {
            if (preferredClip != null)
            {
                musicClip = preferredClip;
            }

            volume = Mathf.Clamp01(preferredVolume);
            if (audioSource != null)
            {
                audioSource.volume = volume;
                if (musicClip != null && audioSource.clip != musicClip)
                {
                    audioSource.clip = musicClip;
                }
            }
        }

        private System.Collections.IEnumerator PlayWhenReady()
        {
            while (audioSource != null
                   && audioSource.clip != null
                   && audioSource.clip.loadState == AudioDataLoadState.Loading)
            {
                yield return null;
            }

            if (audioSource == null || audioSource.clip == null)
            {
                playWhenReadyRoutine = null;
                yield break;
            }

            if (audioSource.clip.loadState == AudioDataLoadState.Failed)
            {
                Debug.LogWarning($"Caveheart background music data failed to load: {audioSource.clip.name}", this);
            }
            else if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }

            playWhenReadyRoutine = null;
        }
    }
}

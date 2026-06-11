using UnityEngine;

namespace MyLittleCaveheart
{
    public sealed class CaveheartBackgroundMusic : MonoBehaviour
    {
        private const string MusicResourcePath = "Audio/Caveheart/bgm_pale_sunrise";

        [SerializeField] private AudioSource audioSource;
        [SerializeField, Range(0f, 1f)] private float volume = 0.28f;
        private Coroutine playWhenReadyRoutine;

        public static CaveheartBackgroundMusic Ensure()
        {
            var existing = FindObjectOfType<CaveheartBackgroundMusic>();
            if (existing != null)
            {
                existing.Play();
                return existing;
            }

            var obj = new GameObject("Caveheart Background Music");
            DontDestroyOnLoad(obj);
            var music = obj.AddComponent<CaveheartBackgroundMusic>();
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

            if (audioSource.clip == null)
            {
                audioSource.clip = Resources.Load<AudioClip>(MusicResourcePath);
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

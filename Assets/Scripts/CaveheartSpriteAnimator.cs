using System.Collections.Generic;
using UnityEngine;

namespace MyLittleCaveheart
{
    public sealed class CaveheartSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private float framesPerSecond = 6f;
        [SerializeField] private string resourceRoot = "Sprites/Caveheart";

        private readonly Dictionary<CaveheartState, Sprite[]> clips = new Dictionary<CaveheartState, Sprite[]>();
        private CaveheartState currentState = CaveheartState.Sleeping;
        private float frameTimer;
        private int frameIndex;

        public void BindOrCreateRenderer()
        {
            if (targetRenderer != null)
            {
                return;
            }

            var existing = GameObject.Find("Little Caveheart Animated Sprite");
            if (existing == null)
            {
                existing = new GameObject("Little Caveheart Animated Sprite");
                existing.transform.position = new Vector3(0f, -0.35f, -0.55f);
                existing.transform.localScale = new Vector3(2.2f, 2.2f, 1f);
            }

            targetRenderer = existing.GetComponent<SpriteRenderer>();
            if (targetRenderer == null)
            {
                targetRenderer = existing.AddComponent<SpriteRenderer>();
            }

            targetRenderer.sortingOrder = 80;
        }

        public void LoadClips()
        {
            clips[CaveheartState.Sleeping] = LoadClip("sleeping");
            clips[CaveheartState.Startled] = LoadClip("startled");
            clips[CaveheartState.Resisting] = LoadClip("resisting");
            clips[CaveheartState.Settled] = LoadClip("settled");
            clips[CaveheartState.SittingUp] = LoadClip("sitting");
        }

        public void Play(CaveheartState state)
        {
            if (currentState != state)
            {
                currentState = state;
                frameIndex = 0;
                frameTimer = 0f;
            }

            ApplyCurrentFrame();
        }

        private void Update()
        {
            if (targetRenderer == null || !clips.TryGetValue(currentState, out var clip) || clip == null || clip.Length == 0)
            {
                return;
            }

            frameTimer += Time.deltaTime;
            var frameDuration = 1f / Mathf.Max(1f, framesPerSecond);
            if (frameTimer < frameDuration)
            {
                return;
            }

            frameTimer -= frameDuration;
            frameIndex = (frameIndex + 1) % clip.Length;
            ApplyCurrentFrame();
        }

        private void ApplyCurrentFrame()
        {
            if (targetRenderer == null || !clips.TryGetValue(currentState, out var clip) || clip == null || clip.Length == 0)
            {
                return;
            }

            targetRenderer.sprite = clip[Mathf.Clamp(frameIndex, 0, clip.Length - 1)];
        }

        private Sprite[] LoadClip(string clipName)
        {
            var frames = new List<Sprite>();
            for (var i = 0; i < 4; i++)
            {
                var sprite = Resources.Load<Sprite>($"{resourceRoot}/{clipName}_{i}");
                if (sprite != null)
                {
                    frames.Add(sprite);
                }
            }

            return frames.ToArray();
        }
    }
}

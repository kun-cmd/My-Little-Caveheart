using System.Collections.Generic;
using UnityEngine;

namespace MyLittleCaveheart
{
    public sealed class CaveheartSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private int sortingOrder = 80;
        [SerializeField] private bool applySortingOrder = true;
        [SerializeField] private bool animateFrames;
        [SerializeField] private float framesPerSecond = 6f;
        [SerializeField] private string resourceRoot = "Sprites/Caveheart";
        [SerializeField] private int frameCount = 4;
        [SerializeField] private string sleepingClipName = "sleeping";
        [SerializeField] private string startledClipName = "startled";
        [SerializeField] private string resistingClipName = "resisting";
        [SerializeField] private string settledClipName = "settled";
        [SerializeField] private string sittingUpClipName = "sitting";

        private readonly Dictionary<CaveheartState, Sprite[]> clips = new Dictionary<CaveheartState, Sprite[]>();
        private CaveheartState currentState = CaveheartState.Sleeping;
        private float frameTimer;
        private int frameIndex;

        public bool BindConfiguredRenderer()
        {
            if (targetRenderer == null)
            {
                Debug.LogWarning("CaveheartSpriteAnimator needs a scene SpriteRenderer assigned before play mode.", this);
                return false;
            }

            ApplyRendererSettings();
            return true;
        }

        public void Configure(SpriteRenderer renderer, string root = null, int? order = null)
        {
            targetRenderer = renderer;
            if (!string.IsNullOrEmpty(root))
            {
                resourceRoot = root;
            }

            if (order.HasValue)
            {
                sortingOrder = order.Value;
            }

            ApplyRendererSettings();
        }

        public void LoadClips()
        {
            clips[CaveheartState.Sleeping] = LoadClip(sleepingClipName);
            clips[CaveheartState.Startled] = LoadClip(startledClipName);
            clips[CaveheartState.Resisting] = LoadClip(resistingClipName);
            clips[CaveheartState.Settled] = LoadClip(settledClipName);
            clips[CaveheartState.SittingUp] = LoadClip(sittingUpClipName);
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
            if (!animateFrames)
            {
                return;
            }

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
            for (var i = 0; i < Mathf.Max(1, frameCount); i++)
            {
                var sprite = Resources.Load<Sprite>($"{resourceRoot}/{clipName}_{i}");
                if (sprite != null)
                {
                    frames.Add(sprite);
                }
            }

            return frames.ToArray();
        }

        private void ApplyRendererSettings()
        {
            if (targetRenderer != null && applySortingOrder)
            {
                targetRenderer.sortingOrder = sortingOrder;
            }
        }

        private void OnValidate()
        {
            frameCount = Mathf.Max(1, frameCount);
            framesPerSecond = Mathf.Max(0.1f, framesPerSecond);
            ApplyRendererSettings();
        }
    }
}

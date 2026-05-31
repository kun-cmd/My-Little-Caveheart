using System.Collections;
using UnityEngine;

namespace MyLittleCaveheart
{
    public sealed class CaveheartClickFeedback : MonoBehaviour
    {
        [SerializeField] private Color flashColor = new Color(1f, 0.95f, 0.35f, 1f);
        [SerializeField] private float flashSeconds = 0.28f;
        [SerializeField] private float scaleBump = 1.16f;

        private Material flashMaterial;

        public Color FlashColor
        {
            get => flashColor;
            set => flashColor = value;
        }

        public void Play(SpriteRenderer target)
        {
            if (target == null)
            {
                return;
            }

            if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            {
                var activeFeedback = GetOrCreateActive();
                if (activeFeedback != this)
                {
                    activeFeedback.Play(target);
                }

                return;
            }

            StartCoroutine(FlashRoutine(target));
            Debug.Log($"[Caveheart Whitebox] Click flash: {target.name}", target);
        }

        public static CaveheartClickFeedback GetOrCreateActive()
        {
            var feedbacks = FindObjectsOfType<CaveheartClickFeedback>(true);
            for (var i = 0; i < feedbacks.Length; i++)
            {
                var feedback = feedbacks[i];
                if (feedback == null || !feedback.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (!feedback.gameObject.activeSelf)
                {
                    feedback.gameObject.SetActive(true);
                }

                if (!feedback.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!feedback.enabled)
                {
                    feedback.enabled = true;
                }

                return feedback;
            }

            return new GameObject("ClickFeedback").AddComponent<CaveheartClickFeedback>();
        }

        private IEnumerator FlashRoutine(SpriteRenderer target)
        {
            EnsureFlashMaterial();
            var originalMaterial = target.sharedMaterial;
            var baseScale = target.transform.localScale;
            var baseColor = target.color;
            var propertyBlock = new MaterialPropertyBlock();

            target.sharedMaterial = flashMaterial;
            target.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_Color", baseColor);
            propertyBlock.SetColor("_FlashColor", flashColor);
            propertyBlock.SetFloat("_FlashAmount", 1f);
            target.SetPropertyBlock(propertyBlock);
            target.transform.localScale = baseScale * scaleBump;

            yield return new WaitForSeconds(flashSeconds);

            if (target == null)
            {
                yield break;
            }

            target.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat("_FlashAmount", 0f);
            target.SetPropertyBlock(propertyBlock);
            target.sharedMaterial = originalMaterial;
            target.transform.localScale = baseScale;
        }

        private void EnsureFlashMaterial()
        {
            if (flashMaterial != null)
            {
                return;
            }

            var shader = Shader.Find("MyLittleCaveheart/WhiteboxFlash");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
                Debug.LogWarning("[Caveheart Whitebox] WhiteboxFlash shader was not found; falling back to Sprites/Default.", this);
            }

            flashMaterial = new Material(shader)
            {
                name = "Runtime Whitebox Flash Material",
                hideFlags = HideFlags.HideAndDontSave
            };
        }
    }
}

using UnityEngine;

namespace MyLittleCaveheart
{
    [ExecuteAlways]
    [RequireComponent(typeof(Collider2D))]
    public sealed class CaveheartClickable : MonoBehaviour
    {
        [SerializeField] private CaveheartGameController controller;
        [SerializeField] private CaveheartInteractionType interactionType;
        [SerializeField] private CaveheartClickFeedback clickFeedback;
        [SerializeField] private SpriteRenderer targetRenderer;

        public CaveheartInteractionType InteractionType
        {
            get => interactionType;
            set => interactionType = value;
        }

        public SpriteRenderer TargetRenderer
        {
            get
            {
                if (targetRenderer == null)
                {
                    targetRenderer = GetComponent<SpriteRenderer>();
                }

                return targetRenderer;
            }
        }

        public void Configure(CaveheartGameController targetController, CaveheartInteractionType type)
        {
            controller = targetController;
            interactionType = type;
            EnsureClickFeedback();
        }

        private void OnEnable()
        {
            EnsureClickFeedback();
        }

        private void Reset()
        {
            EnsureClickFeedback();
        }

        private void EnsureClickFeedback()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<SpriteRenderer>();
            }

            if (clickFeedback == null)
            {
                clickFeedback = Application.isPlaying
                    ? CaveheartClickFeedback.GetOrCreateActive()
                    : FindObjectOfType<CaveheartClickFeedback>();
            }

            if (clickFeedback != null && Application.isPlaying && !clickFeedback.gameObject.activeInHierarchy)
            {
                clickFeedback = CaveheartClickFeedback.GetOrCreateActive();
            }
        }

        private void OnMouseDown()
        {
            // Scene props are visual signals now. Actions are taken from the bottom bar.
        }
    }
}

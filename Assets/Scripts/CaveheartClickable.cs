using UnityEngine;

namespace MyLittleCaveheart
{
    [ExecuteAlways]
    [RequireComponent(typeof(Collider2D))]
    public sealed class CaveheartClickable : MonoBehaviour
    {
        [SerializeField] private CaveheartGameController controller;
        [SerializeField] private CaveheartInteractionType interactionType;
        [SerializeField] private SpriteRenderer targetRenderer;

        public CaveheartInteractionType InteractionType
        {
            get => interactionType;
            set => interactionType = value;
        }

        public int HoverPriority
        {
            get
            {
                switch (interactionType)
                {
                    case CaveheartInteractionType.Wait:
                        return 0;
                    case CaveheartInteractionType.GentleTouch:
                    case CaveheartInteractionType.Scratch:
                        return 20;
                    default:
                        return 10;
                }
            }
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
            HideDebugVisuals();
        }

        private void OnEnable()
        {
            HideDebugVisuals();
        }

        private void Reset()
        {
            HideDebugVisuals();
        }

        public bool TryInteract()
        {
            if (controller == null)
            {
                controller = FindObjectOfType<CaveheartGameController>();
            }

            if (controller == null || !controller.IsInteractionAvailable(interactionType))
            {
                return false;
            }

            controller.Interact(interactionType);
            return true;
        }

        public void HideDebugVisuals()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<SpriteRenderer>();
            }

            if (targetRenderer != null)
            {
                targetRenderer.enabled = false;
            }

            var labels = GetComponentsInChildren<MeshRenderer>(true);
            for (var i = 0; i < labels.Length; i++)
            {
                labels[i].enabled = false;
            }
        }
    }
}

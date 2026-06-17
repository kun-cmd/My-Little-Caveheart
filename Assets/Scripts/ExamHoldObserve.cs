using UnityEngine;

namespace MyLittleCaveheart
{
    public sealed class ExamHoldObserve : MonoBehaviour
    {
        [SerializeField] private ExamLevelController controller;
        [SerializeField] private float holdSeconds = 1f;

        private float heldFor;
        private bool holding;

        // Adds a hold target to the caveheart body if the scene object has no collider yet.
        private void Awake()
        {
            var hitbox = GetComponent<BoxCollider2D>();
            if (hitbox == null)
            {
                hitbox = gameObject.AddComponent<BoxCollider2D>();
                hitbox.isTrigger = true;
                hitbox.size = new Vector2(1.7f, 1.9f);
            }
        }

        // Connects this hold target to the active level controller.
        public void Bind(ExamLevelController targetController)
        {
            controller = targetController;
        }

        // Counts a steady hold and triggers observation once the player stays long enough.
        private void Update()
        {
            if (!holding)
            {
                return;
            }

            heldFor += Time.deltaTime;
            transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.06f, heldFor / holdSeconds);
            if (heldFor >= holdSeconds)
            {
                holding = false;
                heldFor = 0f;
                transform.localScale = Vector3.one;
                controller?.ObserveActiveThought();
            }
        }

        // Starts measuring deliberate attention on the caveheart body.
        private void OnMouseDown()
        {
            if (controller == null)
            {
                return;
            }

            holding = true;
            heldFor = 0f;
        }

        // Cancels the hold if the player releases before the observation threshold.
        private void OnMouseUp()
        {
            holding = false;
            heldFor = 0f;
            transform.localScale = Vector3.one;
        }
    }
}

using UnityEngine;

namespace MyLittleCaveheart
{
    public sealed class ExamEraser : MonoBehaviour
    {
        [SerializeField] private ExamLevelController controller;

        private Vector3 startPosition;
        private Vector3 grabOffset;
        private SpriteRenderer visualRenderer;
        private BoxCollider2D dragHitbox;
        private float dragZ;
        private bool erasedThisDrag;
        private bool dragging;

        // Records the eraser's desk position and fits its drag hitbox to the authored sprite.
        private void Awake()
        {
            startPosition = transform.position;
            visualRenderer = GetComponentInChildren<SpriteRenderer>(true);
            dragHitbox = GetComponent<BoxCollider2D>();
            if (dragHitbox == null)
            {
                dragHitbox = gameObject.AddComponent<BoxCollider2D>();
            }

            dragHitbox.isTrigger = false;
            FitHitboxToVisual(dragHitbox, new Vector2(1.1f, 0.55f));
        }

        // Connects this draggable prop to the active level controller.
        public void Bind(ExamLevelController targetController)
        {
            controller = targetController;
        }

        // Drives dragging from explicit 2D hit tests so large drop-zone triggers cannot steal the eraser click.
        private void Update()
        {
            if (Camera.main == null)
            {
                return;
            }

            if (!dragging && Input.GetMouseButtonDown(0) && IsPointerOverThisEraser())
            {
                BeginDrag();
            }

            if (!dragging)
            {
                return;
            }

            if (Input.GetMouseButton(0))
            {
                DragWithPointer();
                TryEraseThoughtUnderVisual();
            }

            if (Input.GetMouseButtonUp(0))
            {
                EndDrag();
            }
        }

        // Starts a drag pass and allows one thought-erasing attempt for this drag.
        private void BeginDrag()
        {
            dragging = true;
            dragZ = transform.position.z;
            grabOffset = transform.position - PointerWorldPosition(dragZ);
            erasedThisDrag = false;
        }

        // Follows the mouse while preserving where the player grabbed the visible eraser.
        private void DragWithPointer()
        {
            var world = PointerWorldPosition(dragZ) + grabOffset;
            transform.position = new Vector3(world.x, world.y, dragZ);
        }

        // Asks the controller to resolve erasing when the visible eraser crosses a thought.
        private void TryEraseThoughtUnderVisual()
        {
            if (erasedThisDrag)
            {
                return;
            }

            var hits = Physics2D.OverlapPointAll(EraseProbePosition());
            for (var i = 0; i < hits.Length; i++)
            {
                var thought = hits[i].GetComponent<ExamThoughtText>();
                if (thought != null && controller != null)
                {
                    erasedThisDrag = controller.TryEraseThought(thought);
                    break;
                }
            }
        }

        // Returns the eraser to the desk after the player releases it.
        private void EndDrag()
        {
            dragging = false;
            transform.position = startPosition;
        }

        // Matches the clickable area to the current child sprite without changing the sprite itself.
        private void FitHitboxToVisual(BoxCollider2D hitbox, Vector2 fallbackSize)
        {
            if (visualRenderer == null || visualRenderer.sprite == null)
            {
                hitbox.offset = Vector2.zero;
                hitbox.size = fallbackSize;
                return;
            }

            var bounds = visualRenderer.bounds;
            var localMin = transform.InverseTransformPoint(bounds.min);
            var localMax = transform.InverseTransformPoint(bounds.max);
            var center = (localMin + localMax) * 0.5f;
            var size = localMax - localMin;
            hitbox.offset = new Vector2(center.x, center.y);
            hitbox.size = new Vector2(Mathf.Max(Mathf.Abs(size.x), 0.2f), Mathf.Max(Mathf.Abs(size.y), 0.2f));
        }

        // Uses the visible eraser center when checking whether it crosses a thought.
        private Vector3 EraseProbePosition()
        {
            if (visualRenderer == null)
            {
                return transform.position;
            }

            var center = visualRenderer.bounds.center;
            return new Vector3(center.x, center.y, dragZ);
        }

        // Converts the mouse to the same world plane used by this draggable prop.
        private static Vector3 PointerWorldPosition(float targetZ)
        {
            var mouse = Input.mousePosition;
            mouse.z = Mathf.Abs(Camera.main.transform.position.z + targetZ);
            return Camera.main.ScreenToWorldPoint(mouse);
        }

        // Checks every collider under the cursor and only begins dragging when this eraser is among them.
        private bool IsPointerOverThisEraser()
        {
            var pointer = PointerWorldPosition(transform.position.z);
            var hits = Physics2D.OverlapPointAll(pointer);
            for (var i = 0; i < hits.Length; i++)
            {
                if (hits[i] == dragHitbox)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

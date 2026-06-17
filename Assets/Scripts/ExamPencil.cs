using UnityEngine;

namespace MyLittleCaveheart
{
    public sealed class ExamPencil : MonoBehaviour
    {
        [SerializeField] private ExamLevelController controller;

        private Vector3 startPosition;
        private Vector3 grabOffset;
        private SpriteRenderer visualRenderer;
        private BoxCollider2D dragHitbox;
        private float dragZ;
        private bool dragging;

        // Records the pencil's starting desk position and fits its drag hitbox to the authored sprite.
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
            FitHitboxToVisual(dragHitbox, new Vector2(1.2f, 0.45f));
        }

        // Connects this draggable pencil to the active level controller.
        public void Bind(ExamLevelController targetController)
        {
            controller = targetController;
        }

        // Returns the pencil to its authored starting position after an invalid attempt.
        public void ResetPosition()
        {
            transform.position = startPosition;
        }

        // Drives dragging from explicit 2D hit tests so large drop-zone triggers cannot steal the pencil click.
        private void Update()
        {
            if (Camera.main == null)
            {
                return;
            }

            if (!dragging && Input.GetMouseButtonDown(0) && IsPointerOverThisPencil())
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
            }

            if (Input.GetMouseButtonUp(0))
            {
                EndDrag();
            }
        }

        // Captures the current depth so mouse dragging stays on the scene plane.
        private void BeginDrag()
        {
            dragging = true;
            dragZ = transform.position.z;
            grabOffset = transform.position - PointerWorldPosition(dragZ);
        }

        // Moves the pencil with the cursor in world space.
        private void DragWithPointer()
        {
            var world = PointerWorldPosition(dragZ) + grabOffset;
            transform.position = new Vector3(world.x, world.y, dragZ);
        }

        // Lets the controller decide whether dropping the pencil is too early or completes the level.
        private void EndDrag()
        {
            dragging = false;
            if (controller == null || !controller.TryPlacePencil(this))
            {
                ResetPosition();
            }
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

        // Converts the mouse to the same world plane used by this draggable prop.
        private static Vector3 PointerWorldPosition(float targetZ)
        {
            var mouse = Input.mousePosition;
            mouse.z = Mathf.Abs(Camera.main.transform.position.z + targetZ);
            return Camera.main.ScreenToWorldPoint(mouse);
        }

        // Checks every collider under the cursor and only begins dragging when this pencil is among them.
        private bool IsPointerOverThisPencil()
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

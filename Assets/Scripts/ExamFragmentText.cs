using UnityEngine;

namespace MyLittleCaveheart
{
    public sealed class ExamFragmentText : MonoBehaviour
    {
        [SerializeField] private TextMesh textMesh;
        [SerializeField] private BoxCollider2D hitbox;
        [SerializeField] private ExamFragmentKind fragmentKind;
        [SerializeField, Min(0.05f)] private float driftSeconds = 0.55f;
        [SerializeField, Min(0f)] private float floatAmplitude = 0.045f;
        [SerializeField, Min(0.1f)] private float floatSpeed = 1.25f;

        private ExamLevelController controller;
        private Vector3 sourcePosition;
        private Vector3 floatPosition;
        private float driftProgress;
        private float floatSeed;
        private float dragZ;
        private int mistakeCount;
        private bool connected;
        private bool dragging;

        public ExamFragmentKind FragmentKind => fragmentKind;
        public int MistakeCount => mistakeCount;
        public bool IsConnected => connected;
        public string Text => textMesh == null ? string.Empty : textMesh.text;
        public TextMesh TextComponent => textMesh == null ? textMesh = GetComponent<TextMesh>() : textMesh;

        // Keeps editor-visible split text on the same Short Stack font/material used at runtime.
        private void OnValidate()
        {
            textMesh = textMesh == null ? GetComponent<TextMesh>() : textMesh;
            hitbox = hitbox == null ? GetComponent<BoxCollider2D>() : hitbox;
            CaveheartTypography.ApplyTo(textMesh);
        }

        // Caches text and collider references for a runtime-spawned fragment.
        private void Awake()
        {
            textMesh = textMesh == null ? GetComponent<TextMesh>() : textMesh;
            hitbox = hitbox == null ? GetComponent<BoxCollider2D>() : hitbox;
            CaveheartTypography.ApplyTo(textMesh);
        }

        // Keeps unresolved fragments gently floating once they finish drifting out of the split thought.
        private void Update()
        {
            if (connected || dragging)
            {
                return;
            }

            if (driftProgress < 1f)
            {
                driftProgress = Mathf.MoveTowards(driftProgress, 1f, Time.deltaTime / Mathf.Max(0.05f, driftSeconds));
                var eased = 1f - Mathf.Pow(1f - driftProgress, 3f);
                transform.position = Vector3.Lerp(sourcePosition, floatPosition, eased);
                return;
            }

            var time = Time.time * floatSpeed + floatSeed;
            var floatOffset = new Vector3(Mathf.Sin(time) * floatAmplitude, Mathf.Cos(time * 0.73f) * floatAmplitude * 0.55f, 0f);
            transform.position = floatPosition + floatOffset;
        }

        // Initializes fragment text, category, hitbox, drift path, and reset position after a thought splits.
        public void Configure(
            ExamLevelController targetController,
            ExamFragmentKind kind,
            string value,
            Vector3 splitSourcePosition,
            Vector3 targetFloatPosition,
            int slotIndex)
        {
            textMesh = textMesh == null ? GetComponent<TextMesh>() : textMesh;
            hitbox = hitbox == null ? GetComponent<BoxCollider2D>() : hitbox;
            controller = targetController;
            fragmentKind = kind;
            connected = false;
            dragging = false;
            mistakeCount = 0;
            sourcePosition = splitSourcePosition;
            floatPosition = targetFloatPosition;
            driftProgress = 0f;
            floatSeed = slotIndex * 1.77f + splitSourcePosition.x * 0.23f + splitSourcePosition.y * 0.11f;
            transform.position = sourcePosition;

            if (textMesh != null)
            {
                textMesh.text = value;
            }

            if (hitbox != null)
            {
                hitbox.enabled = true;
                hitbox.size = new Vector2(Mathf.Max(1.8f, LongestLineLength(value) * 0.09f), 0.44f);
                hitbox.offset = Vector2.zero;
            }
        }

        // Lets the level controller use the TextMesh explicitly assigned in a split-fragment Inspector row.
        public void UseTextMesh(TextMesh targetTextMesh)
        {
            if (targetTextMesh != null)
            {
                textMesh = targetTextMesh;
            }

            CaveheartTypography.ApplyTo(textMesh);
        }

        // Starts a pointer-driven drag so fragments can be moved even when larger zone colliders overlap them.
        public void BeginPointerDrag()
        {
            if (connected)
            {
                return;
            }

            dragging = true;
            driftProgress = 1f;
            dragZ = transform.position.z;
            controller?.PlayFragmentPickUp();
        }

        // Moves the fragment to the pointer position while preserving its depth in the 2D scene.
        public void DragToWorld(Vector3 worldPosition)
        {
            if (connected || !dragging)
            {
                return;
            }

            transform.position = new Vector3(worldPosition.x, worldPosition.y, dragZ);
        }

        // Finishes a pointer-driven drag and asks the controller to evaluate the drop once.
        public void EndPointerDrag()
        {
            if (connected || !dragging)
            {
                return;
            }

            dragging = false;
            controller?.TryDropFragment(this);
        }

        // Counts a failed placement and sends the fragment back to its settled floating position.
        public void RegisterWrongDrop()
        {
            mistakeCount++;
            dragging = false;
            driftProgress = 1f;
            transform.position = floatPosition;
        }

        // Locks the fragment into an accepted zone and optionally replaces it with a reframe.
        public void Connect(Vector3 targetPosition, string transformedText)
        {
            connected = true;
            dragging = false;
            transform.position = targetPosition;

            if (!string.IsNullOrEmpty(transformedText) && textMesh != null)
            {
                textMesh.text = transformedText;
            }

            if (hitbox != null)
            {
                hitbox.enabled = false;
            }
        }

        // Begins dragging only while this fragment is still unresolved.
        private void OnMouseDown()
        {
            BeginPointerDrag();
        }

        // Tracks the cursor in world space while preserving the fragment's authored depth.
        private void OnMouseDrag()
        {
            if (connected || Camera.main == null)
            {
                return;
            }

            DragToWorld(PointerWorldPosition());
        }

        // Asks the controller to evaluate the drop target when the drag ends.
        private void OnMouseUp()
        {
            EndPointerDrag();
        }

        // Converts the mouse to world coordinates for the legacy OnMouse fallback path.
        private static Vector3 PointerWorldPosition()
        {
            if (Camera.main == null)
            {
                return Vector3.zero;
            }

            var mouse = Input.mousePosition;
            mouse.z = -Camera.main.transform.position.z;
            return Camera.main.ScreenToWorldPoint(mouse);
        }

        // Measures text width by its longest visual line so multiline fragments keep sensible hitboxes.
        private static int LongestLineLength(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            var longest = 0;
            var current = 0;
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] == '\n')
                {
                    longest = Mathf.Max(longest, current);
                    current = 0;
                    continue;
                }

                if (value[i] != '\r')
                {
                    current++;
                }
            }

            return Mathf.Max(longest, current);
        }
    }
}

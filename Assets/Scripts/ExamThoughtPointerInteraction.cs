using UnityEngine;

namespace MyLittleCaveheart
{
    public sealed class ExamThoughtPointerInteraction : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float holdSeconds = 1f;
        [SerializeField, Min(0.01f)] private float dragThreshold = 0.18f;

        private ExamLevelController controller;
        private ExamCursorVisuals cursorVisuals;
        private ExamThoughtText hoveredThought;
        private ExamThoughtText pressedThought;
        private ExamThoughtText attentionThought;
        private ExamFragmentText hoveredFragment;
        private ExamFragmentText draggedFragment;
        private float holdProgress;
        private bool pressingThought;
        private bool dragAttempted;
        private Vector3 pressWorldPosition;

        // Connects the tracker to the controller and cursor visuals it coordinates.
        public void Bind(ExamLevelController targetController, ExamCursorVisuals targetCursorVisuals)
        {
            controller = targetController;
            cursorVisuals = targetCursorVisuals;
            ResetThoughtPress();
            SetThoughtAttention(null, false, 0f);
        }

        // Polls the cursor every frame so thought interactions do not depend on Unity OnMouse callbacks.
        private void Update()
        {
            if (controller == null || cursorVisuals == null)
            {
                return;
            }

            var pointerWorld = PointerWorldPosition();
            hoveredThought = controller.FindThoughtAt(pointerWorld);
            hoveredFragment = controller.FindFragmentAt(pointerWorld);
            var pointerOverAnyInteractive = hoveredThought != null
                || hoveredFragment != null
                || IsPointerOverSecondaryInteractive(pointerWorld);

            if (HandleFragmentDrag(pointerWorld, pointerOverAnyInteractive))
            {
                return;
            }

            if (!controller.HasInteractableThoughts)
            {
                ResetThoughtPress();
                SetThoughtAttention(null, false, 0f);
                cursorVisuals.SetCursorMode(pointerOverAnyInteractive ? ExamCursorMode.Hand : ExamCursorMode.Default);
                cursorVisuals.SetObserveHold(false, 0f);
                controller.SetObserveVisualActive(false);
                controller.ShowThoughtCursorHint(false, 0f);
                return;
            }

            HandleThoughtPress(pointerWorld);
            UpdateThoughtVisuals(pointerOverAnyInteractive);
        }

        // Runs fragment dragging from the same pointer scanner that drives cursor hover feedback.
        private bool HandleFragmentDrag(Vector3 pointerWorld, bool pointerOverAnyInteractive)
        {
            if (draggedFragment == null && hoveredFragment != null && Input.GetMouseButtonDown(0))
            {
                draggedFragment = hoveredFragment;
                draggedFragment.BeginPointerDrag();
                ResetThoughtPress();
                SetThoughtAttention(null, false, 0f);
            }

            if (draggedFragment == null)
            {
                return false;
            }

            controller.ShowThoughtCursorHint(false, 0f);
            cursorVisuals.SetObserveHold(false, 0f);
            controller.SetObserveVisualActive(false);

            if (Input.GetMouseButton(0))
            {
                draggedFragment.DragToWorld(pointerWorld);
                cursorVisuals.SetCursorMode(ExamCursorMode.Grab);
                return true;
            }

            draggedFragment.EndPointerDrag();
            draggedFragment = null;
            cursorVisuals.SetCursorMode(pointerOverAnyInteractive ? ExamCursorMode.Hand : ExamCursorMode.Default);
            return true;
        }

        // Advances the click, drag, and hold state machine for the thought under the cursor.
        private void HandleThoughtPress(Vector3 pointerWorld)
        {
            if (!pressingThought && hoveredThought != null && Input.GetMouseButtonDown(0))
            {
                pressingThought = true;
                pressedThought = hoveredThought;
                dragAttempted = false;
                holdProgress = 0f;
                pressWorldPosition = pointerWorld;
                controller.PlayObserveHoldStart();
            }

            if (!pressingThought)
            {
                holdProgress = Mathf.MoveTowards(holdProgress, 0f, Time.unscaledDeltaTime * 3.5f);
                return;
            }

            if (pressedThought == null || !controller.IsThoughtInteractable(pressedThought))
            {
                ResetThoughtPress();
                return;
            }

            if (Input.GetMouseButton(0))
            {
                if (!dragAttempted && Vector2.Distance(pressWorldPosition, pointerWorld) > dragThreshold)
                {
                    dragAttempted = true;
                    controller.RegisterThoughtDragAttempt(pressedThought);
                    ResetThoughtPress();
                    return;
                }

                var duration = Mathf.Max(0.05f, holdSeconds);
                holdProgress = Mathf.MoveTowards(holdProgress, 1f, Time.unscaledDeltaTime / duration);
                if (holdProgress >= 1f)
                {
                    controller.ObserveThought(pressedThought);
                    ResetThoughtPress();
                }

                return;
            }

            if (!dragAttempted && holdProgress < 1f)
            {
                controller.RegisterThoughtClickAttempt(pressedThought);
            }

            ResetThoughtPress();
        }

        // Updates the visual affordances for cursor icon, hold ring, hint text, and thought shake.
        private void UpdateThoughtVisuals(bool pointerOverAnyInteractive)
        {
            var holding = pressingThought && Input.GetMouseButton(0);
            var activeThought = pressingThought ? pressedThought : hoveredThought;
            var attentive = activeThought != null;
            SetThoughtAttention(activeThought, holding, holdProgress);
            controller.ShowThoughtCursorHint(attentive, holdProgress);
            cursorVisuals.SetObserveHold(attentive, holdProgress);
            controller.SetObserveVisualActive(holding && attentive);

            if (holding)
            {
                cursorVisuals.SetCursorMode(ExamCursorMode.Grab);
            }
            else if (pointerOverAnyInteractive)
            {
                cursorVisuals.SetCursorMode(ExamCursorMode.Hand);
            }
            else
            {
                cursorVisuals.SetCursorMode(ExamCursorMode.Default);
            }
        }

        // Moves pointer attention between thoughts without leaving old hover shake on inactive text.
        private void SetThoughtAttention(ExamThoughtText targetThought, bool holding, float progress01)
        {
            if (attentionThought != null && attentionThought != targetThought)
            {
                attentionThought.SetPointerAttention(false, false, 0f);
            }

            attentionThought = targetThought;
            if (attentionThought != null)
            {
                attentionThought.SetPointerAttention(attentionThought == hoveredThought, holding, progress01);
            }
        }

        // Clears the active thought press without changing hover state.
        private void ResetThoughtPress()
        {
            pressingThought = false;
            dragAttempted = false;
            pressedThought = null;
            holdProgress = 0f;
        }

        // Checks other draggable second-level objects so the cursor still becomes a hand over them.
        private static bool IsPointerOverSecondaryInteractive(Vector3 pointerWorld)
        {
            var hits = Physics2D.OverlapPointAll(pointerWorld);
            for (var i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.GetComponent<ExamPencil>() != null || hit.GetComponent<ExamEraser>() != null)
                {
                    return true;
                }

                var fragment = hit.GetComponent<ExamFragmentText>();
                if (fragment != null && !fragment.IsConnected)
                {
                    return true;
                }
            }

            return false;
        }

        // Converts the cursor into the same world coordinates used by the first-level hover scanner.
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
    }
}

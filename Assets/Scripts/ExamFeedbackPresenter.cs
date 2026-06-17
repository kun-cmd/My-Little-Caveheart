using UnityEngine;

namespace MyLittleCaveheart
{
    public sealed class ExamFeedbackPresenter : MonoBehaviour
    {
        [SerializeField] private TextMesh feedbackText;
        [SerializeField] private TextMesh statsText;
        [SerializeField] private TextMesh timeText;
        [SerializeField] private TextMesh titleText;

        // Keeps feedback text visible in the editor when designers type placeholder copy before Play Mode.
        private void OnValidate()
        {
            BindTextReferences();
            ApplyFont(feedbackText);
            ApplyFont(statsText);
            ApplyFont(timeText);
            ApplyFont(titleText);
        }

        // Finds the scene's whitebox text fields and applies the shared storybook font.
        public void BindExistingScene()
        {
            BindTextReferences();
            ApplyFont(feedbackText);
            ApplyFont(statsText);
            ApplyFont(timeText);
            ApplyFont(titleText);
        }

        // Updates the level title shown at the top of the scene.
        public void SetTitle(string value)
        {
            if (titleText != null)
            {
                titleText.text = value;
            }
        }

        // Toggles the level title without affecting the rest of the whitebox feedback text.
        public void SetTitleVisible(bool visible)
        {
            if (titleText != null)
            {
                titleText.gameObject.SetActive(visible);
            }
        }

        // Shows the main player-facing feedback line for the latest interaction.
        public void Show(string value)
        {
            if (feedbackText != null)
            {
                feedbackText.text = value;
            }
        }

        // Prints hidden-state values for whitebox tuning and debugging.
        public void SetStats(int grounding, int trust, int stress)
        {
            if (statsText != null)
            {
                statsText.text = $"grounding {grounding}/8   trust {trust}/8   stress {stress}/8";
            }
        }

        // Shows only the remaining exam time, leaving mood and pressure to scene visuals.
        public void SetTime(int usedMinutes, int timeoutMinutes)
        {
            if (timeText == null)
            {
                return;
            }

            var remaining = Mathf.Max(0, timeoutMinutes - usedMinutes);
            timeText.text = $"exam closes in {remaining}m";
        }

        // Looks up a named TextMesh in the scene without requiring serialized references.
        private static TextMesh FindText(string objectName)
        {
            var obj = GameObject.Find(objectName);
            return obj == null ? null : obj.GetComponent<TextMesh>();
        }

        // Finds scene text objects once so both editor validation and runtime binding use the same references.
        private void BindTextReferences()
        {
            feedbackText = feedbackText == null ? FindText("FeedbackText") : feedbackText;
            statsText = statsText == null ? FindText("StatsText") : statsText;
            timeText = timeText == null ? FindText("TimeText") : timeText;
            titleText = titleText == null ? FindText("LevelTitleText") : titleText;
        }

        // Applies typography only when the target text exists.
        private static void ApplyFont(TextMesh text)
        {
            if (text != null)
            {
                CaveheartTypography.ApplyTo(text);
            }
        }
    }
}

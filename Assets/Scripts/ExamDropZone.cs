using UnityEngine;

namespace MyLittleCaveheart
{
    public enum ExamFragmentKind
    {
        Body,
        Reality,
        Future
    }

    public sealed class ExamDropZone : MonoBehaviour
    {
        [SerializeField] private ExamFragmentKind fragmentKind;
        [SerializeField] private SpriteRenderer zoneRenderer;

        private Color baseColor;

        public ExamFragmentKind FragmentKind => fragmentKind;

        // Keeps editor-authored zone labels and child backplates ready before entering Play Mode.
        private void OnValidate()
        {
            CaveheartTypography.ApplyTo(GetComponent<TextMesh>());
            ResolveZoneRenderer();
        }

        // Ensures the whitebox zone can receive drops even when the scene only has a label object.
        private void Awake()
        {
            ResolveZoneRenderer();

            var hitbox = GetComponent<BoxCollider2D>();
            if (hitbox == null)
            {
                hitbox = gameObject.AddComponent<BoxCollider2D>();
                hitbox.isTrigger = true;
            }

            hitbox.size = GetDefaultSize();

            baseColor = zoneRenderer != null ? zoneRenderer.color : Color.white;
        }

        // Gives each authored drop area a practical hitbox without scaling its visible text label.
        private Vector2 GetDefaultSize()
        {
            if (name.Contains("Reality"))
            {
                return new Vector2(6.8f, 2.45f);
            }

            if (name.Contains("Future"))
            {
                return new Vector2(12.9f, 2.7f);
            }

            if (name.Contains("Body"))
            {
                return new Vector2(5.5f, 4.7f);
            }

            return Vector2.one;
        }

        // Finds the visible zone backplate whether it lives on this object or under the authored child.
        private void ResolveZoneRenderer()
        {
            if (zoneRenderer == null)
            {
                zoneRenderer = GetComponent<SpriteRenderer>();
            }

            if (zoneRenderer == null)
            {
                zoneRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }
        }

        // Sets which fragment category this zone accepts after the controller discovers it.
        public void Configure(ExamFragmentKind kind)
        {
            fragmentKind = kind;
        }

        // Briefly tints the zone to acknowledge a correct or incorrect drop.
        public void Flash(bool accepted)
        {
            if (zoneRenderer == null)
            {
                return;
            }

            zoneRenderer.color = accepted
                ? Color.Lerp(baseColor, new Color(0.6f, 1f, 0.78f, 0.55f), 0.65f)
                : Color.Lerp(baseColor, new Color(1f, 0.42f, 0.36f, 0.55f), 0.65f);
            CancelInvoke(nameof(RestoreColor));
            Invoke(nameof(RestoreColor), 0.28f);
        }

        // Restores the zone color after a feedback flash.
        private void RestoreColor()
        {
            if (zoneRenderer != null)
            {
                zoneRenderer.color = baseColor;
            }
        }
    }
}

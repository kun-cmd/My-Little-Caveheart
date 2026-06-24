using UnityEngine;

namespace MyLittleCaveheart
{
    public enum FireLevelInteractableKind
    {
        Fire,
        Kindling,
        Ash,
        Stone,
        WindStonesTarget,
        StoneShelterTarget,
        Thought,
        LionShadow,
        WarmLight,
        AshPoker,
        DryingSlot
    }

    public sealed class FireLevelInteractable : MonoBehaviour
    {
        public FireLevelInteractableKind kind;
        public string id;
        public bool dryKindling = true;

        [Header("Pointer Hit Shape")]
        [SerializeField] private bool usePreciseHitSegment;
        [SerializeField] private Vector2 hitSegmentStart = new Vector2(-0.85f, -0.5f);
        [SerializeField] private Vector2 hitSegmentEnd = new Vector2(0.85f, 0.5f);
        [SerializeField, Min(0.01f)] private float hitSegmentRadius = 0.24f;

        public bool ContainsPointer(Vector3 worldPoint)
        {
            if (!usePreciseHitSegment)
            {
                var collider = GetComponent<Collider2D>();
                return collider != null && collider.OverlapPoint(worldPoint);
            }

            var localPoint = (Vector2)transform.InverseTransformPoint(worldPoint);
            return IsPointInsideSegment(localPoint, hitSegmentStart, hitSegmentEnd, hitSegmentRadius);
        }

        public static bool IsPointInsideSegment(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd, float radius)
        {
            var segment = segmentEnd - segmentStart;
            var segmentLengthSquared = segment.sqrMagnitude;
            if (segmentLengthSquared <= Mathf.Epsilon)
            {
                return Vector2.Distance(point, segmentStart) <= radius;
            }

            var t = Mathf.Clamp01(Vector2.Dot(point - segmentStart, segment) / segmentLengthSquared);
            var closest = segmentStart + segment * t;
            return Vector2.Distance(point, closest) <= radius;
        }

        private void OnDrawGizmosSelected()
        {
            if (!usePreciseHitSegment)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.82f, 0.25f, 0.85f);
            var start = transform.TransformPoint(hitSegmentStart);
            var end = transform.TransformPoint(hitSegmentEnd);
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(start, hitSegmentRadius);
            Gizmos.DrawWireSphere(end, hitSegmentRadius);
        }
    }
}

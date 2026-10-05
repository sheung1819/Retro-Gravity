using System.Collections;
using UnityEngine;

namespace Pulse
{
    /// <summary>
    /// Core V1 mechanic: catcher slides along the current "floor" edge via swipe,
    /// and gravity flips at random intervals, remapping which edge is the floor
    /// and which direction the swipe axis maps to.
    /// </summary>
    public class CatcherController : MonoBehaviour
    {
        [Header("Shape")]
        [SerializeField] private float length = 2.0f;
        [SerializeField] private float thickness = 0.45f;
        [Tooltip("Distance of the catcher's line from the floor edge (world units).")]
        [SerializeField] private float screenMargin = 1.0f;

        [Header("Gravity Flip")]
        [SerializeField] private float minFlipInterval = 8f;
        [SerializeField] private float maxFlipInterval = 16f;
        [SerializeField] private float flipTransitionDuration = 0.4f;
        [Tooltip("How long before a flip the upcoming floor is telegraphed.")]
        [SerializeField] private float flipWarningLead = 1.0f;

        private FloorEdge currentFloor = FloorEdge.Bottom;
        private FloorEdge pendingFloor;
        private float flipTimer;
        private bool warned;
        private bool isFlipping;
        private bool flipsEnabled;
        private float along; // position on the axis parallel to the floor

        public FloorEdge CurrentFloor => currentFloor;
        public bool IsFlipping => isFlipping;
        public float Length => length;
        public float Thickness => thickness;
        public float FlipTransitionDuration => flipTransitionDuration;
        /// <summary>Coordinate of the catcher's line on the fall axis.</summary>
        public float FloorLine => Playfield.EdgeCoordinate(currentFloor) - Playfield.AxisSign(currentFloor) * screenMargin;
        /// <summary>Seconds until the next flip (only meaningful while flips are enabled).</summary>
        public float TimeUntilFlip => flipTimer;

        private void Start()
        {
            along = 0f;
            SnapToFloor();
            Playfield.Changed += SnapToFloor;
        }

        private void OnDestroy()
        {
            Playfield.Changed -= SnapToFloor;
        }

        private void Update()
        {
            HandleSwipeInput();
            CheckForFlip();
        }

        // ---------- Public control (GameManager) ----------

        /// <summary>Back to the bottom floor, centred, no flip in progress.</summary>
        public void ResetForRun()
        {
            StopAllCoroutines();
            isFlipping = false;
            warned = false;
            currentFloor = FloorEdge.Bottom;
            along = 0f;
            SnapToFloor();
        }

        public void SetFlipsEnabled(bool enabled)
        {
            flipsEnabled = enabled;
            if (enabled) ScheduleNextFlip();
        }

        /// <summary>Difficulty ramp hook: tighten the flip interval as the run goes on.</summary>
        public void SetFlipIntervalRange(float min, float max)
        {
            minFlipInterval = min;
            maxFlipInterval = Mathf.Max(min, max);
        }

        // ---------- Input ----------

        private void HandleSwipeInput()
        {
            if (isFlipping || Time.timeScale == 0f) return;
            if (PulseInput.IsPressed) DragCatcher(PulseInput.Position);
        }

        private void DragCatcher(Vector2 screenPos)
        {
            Vector2 worldPos = Playfield.ScreenToWorld(screenPos);

            // Movement is constrained to the axis parallel to the current floor edge.
            along = ClampAlong(currentFloor.IsHorizontal() ? worldPos.x : worldPos.y, currentFloor);
            transform.position = GetFloorAnchorPosition(currentFloor, along);
        }

        private float ClampAlong(float value, FloorEdge edge)
        {
            Vector2 range = Playfield.AlongRange(edge);
            float half = length * 0.5f + 0.1f;
            if (range.y - range.x <= half * 2f) return (range.x + range.y) * 0.5f;
            return Mathf.Clamp(value, range.x + half, range.y - half);
        }

        // ---------- Gravity Flip ----------

        private void ScheduleNextFlip()
        {
            flipTimer = Random.Range(minFlipInterval, maxFlipInterval);
            warned = false;
        }

        private void CheckForFlip()
        {
            if (!flipsEnabled || isFlipping) return;

            flipTimer -= Time.deltaTime;

            if (!warned && flipTimer <= flipWarningLead)
            {
                warned = true;
                pendingFloor = GetRandomDifferentEdge(currentFloor);
                GameEvents.RaiseFlipWarning(pendingFloor, Mathf.Max(0f, flipTimer));
            }

            if (flipTimer <= 0f)
            {
                TriggerFlip();
            }
        }

        private void TriggerFlip()
        {
            FloorEdge newFloor = warned ? pendingFloor : GetRandomDifferentEdge(currentFloor);
            StartCoroutine(FlipRoutine(newFloor));
            ScheduleNextFlip();
        }

        private FloorEdge GetRandomDifferentEdge(FloorEdge exclude)
        {
            FloorEdge result;
            do
            {
                result = (FloorEdge)Random.Range(0, 4);
            } while (result == exclude);
            return result;
        }

        private IEnumerator FlipRoutine(FloorEdge newFloor)
        {
            isFlipping = true;
            FloorEdge oldFloor = currentFloor;
            GameEvents.RaiseFlipStart(oldFloor, newFloor);

            Vector3 startPos = transform.position;
            float startAngle = oldFloor.UprightAngle();

            // Keep the catcher's screen position as continuous as possible:
            // carry the coordinate that lies on the new floor's axis.
            currentFloor = newFloor;
            along = ClampAlong(newFloor.IsHorizontal() ? startPos.x : startPos.y, newFloor);
            Vector3 endPos = GetFloorAnchorPosition(newFloor, along);
            float endAngle = newFloor.UprightAngle();

            float elapsed = 0f;
            while (elapsed < flipTransitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / flipTransitionDuration);
                float e = 1f - Mathf.Pow(1f - t, 3f); // ease-out cubic
                transform.position = Vector3.Lerp(startPos, endPos, e);
                transform.rotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(startAngle, endAngle, e));
                yield return null;
            }

            transform.position = endPos;
            transform.rotation = Quaternion.Euler(0f, 0f, endAngle);
            isFlipping = false;

            GameEvents.RaiseFlipEnd(currentFloor);
        }

        private void SnapToFloor()
        {
            along = ClampAlong(along, currentFloor);
            transform.position = GetFloorAnchorPosition(currentFloor, along);
            transform.rotation = Quaternion.Euler(0f, 0f, currentFloor.UprightAngle());
        }

        private Vector3 GetFloorAnchorPosition(FloorEdge edge, float alongAxis)
        {
            Vector2 p = Playfield.PointOnEdge(edge, alongAxis, screenMargin);
            return new Vector3(p.x, p.y, transform.position.z);
        }

        /// <summary>
        /// The direction "down" currently falls, for the object spawner to use.
        /// </summary>
        public Vector2 GetFallDirection() => currentFloor.FallDirection();

        /// <summary>World-space box objects must overlap to be caught.</summary>
        public Rect GetCatchRect()
        {
            Vector3 p = transform.position;
            Vector2 size = currentFloor.IsHorizontal() ? new Vector2(length, thickness) : new Vector2(thickness, length);
            return new Rect((Vector2)p - size * 0.5f, size);
        }
    }
}

using System;
using UnityEngine;

namespace Pulse
{
    public enum FloorEdge { Bottom, Top, Left, Right }

    /// <summary>
    /// Global event hub. Gameplay code raises events here; audio, VFX, haptics
    /// and UI listen, so no system needs a direct reference to the others.
    /// </summary>
    public static class GameEvents
    {
        /// <summary>Raised shortly before a flip so the player can see it coming. (next floor, seconds until flip)</summary>
        public static event Action<FloorEdge, float> FlipWarning;
        /// <summary>(from, to)</summary>
        public static event Action<FloorEdge, FloorEdge> FlipStart;
        public static event Action<FloorEdge> FlipEnd;

        /// <summary>(world position, combo count after the catch)</summary>
        public static event Action<Vector3, int> Caught;
        /// <summary>A bad object hit the catcher. (world position, lives remaining)</summary>
        public static event Action<Vector3, int> Hit;
        /// <summary>A good object reached the floor without being caught.</summary>
        public static event Action<Vector3> Missed;

        public static event Action RunStarted;
        /// <summary>(final score, is new best)</summary>
        public static event Action<int, bool> RunEnded;
        public static event Action<bool> PauseChanged;

        public static void RaiseFlipWarning(FloorEdge next, float lead) => FlipWarning?.Invoke(next, lead);
        public static void RaiseFlipStart(FloorEdge from, FloorEdge to) => FlipStart?.Invoke(from, to);
        public static void RaiseFlipEnd(FloorEdge floor) => FlipEnd?.Invoke(floor);
        public static void RaiseCaught(Vector3 pos, int combo) => Caught?.Invoke(pos, combo);
        public static void RaiseHit(Vector3 pos, int livesLeft) => Hit?.Invoke(pos, livesLeft);
        public static void RaiseMissed(Vector3 pos) => Missed?.Invoke(pos);
        public static void RaiseRunStarted() => RunStarted?.Invoke();
        public static void RaiseRunEnded(int score, bool newBest) => RunEnded?.Invoke(score, newBest);
        public static void RaisePauseChanged(bool paused) => PauseChanged?.Invoke(paused);

        /// <summary>Drop every listener. Called on bootstrap so domain-reload-disabled play mode starts clean.</summary>
        public static void Clear()
        {
            FlipWarning = null; FlipStart = null; FlipEnd = null;
            Caught = null; Hit = null; Missed = null;
            RunStarted = null; RunEnded = null; PauseChanged = null;
        }
    }

    public static class FloorEdgeExtensions
    {
        /// <summary>The direction "down" points for this floor.</summary>
        public static Vector2 FallDirection(this FloorEdge edge)
        {
            switch (edge)
            {
                case FloorEdge.Top: return Vector2.up;
                case FloorEdge.Left: return Vector2.left;
                case FloorEdge.Right: return Vector2.right;
                default: return Vector2.down;
            }
        }

        /// <summary>True when the floor runs horizontally (catcher moves along X).</summary>
        public static bool IsHorizontal(this FloorEdge edge) => edge == FloorEdge.Bottom || edge == FloorEdge.Top;

        public static FloorEdge Opposite(this FloorEdge edge)
        {
            switch (edge)
            {
                case FloorEdge.Top: return FloorEdge.Bottom;
                case FloorEdge.Left: return FloorEdge.Right;
                case FloorEdge.Right: return FloorEdge.Left;
                default: return FloorEdge.Top;
            }
        }

        /// <summary>Z rotation that turns a shape authored for the bottom floor so it faces into the playfield.</summary>
        public static float UprightAngle(this FloorEdge edge)
        {
            switch (edge)
            {
                case FloorEdge.Top: return 180f;
                case FloorEdge.Left: return -90f;
                case FloorEdge.Right: return 90f;
                default: return 0f;
            }
        }
    }
}

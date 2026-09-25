using System;
using UnityEngine;

namespace Pulse
{
    /// <summary>
    /// World-space rectangle of the playable area: the camera view shrunk to the
    /// device safe area (notches, rounded corners, home indicator).
    /// Screen→world conversion here ignores camera shake/roll, so input and
    /// layout stay stable while flip effects move the camera.
    /// </summary>
    public static class Playfield
    {
        public static Rect Rect { get; private set; }
        /// <summary>Full camera view (ignores safe area) — used for background and flashes.</summary>
        public static Rect ViewRect { get; private set; }
        public static event Action Changed;

        private static Camera cam;
        private static Vector2 center;
        private static int lastWidth, lastHeight;
        private static Rect lastSafeArea;

        public static void Init(Camera camera)
        {
            cam = camera;
            center = camera.transform.position;
            Recompute(force: true);
        }

        /// <summary>Call once per frame; recomputes and raises <see cref="Changed"/> if the screen changed.</summary>
        public static void Poll() => Recompute(force: false);

        private static void Recompute(bool force)
        {
            if (cam == null) return;
            if (!force && Screen.width == lastWidth && Screen.height == lastHeight && Screen.safeArea == lastSafeArea) return;

            lastWidth = Screen.width;
            lastHeight = Screen.height;
            lastSafeArea = Screen.safeArea;

            Vector2 viewMin = ScreenToWorld(Vector2.zero);
            Vector2 viewMax = ScreenToWorld(new Vector2(Screen.width, Screen.height));
            ViewRect = Rect.MinMaxRect(viewMin.x, viewMin.y, viewMax.x, viewMax.y);

            Rect safe = Screen.safeArea;
            Vector2 min = ScreenToWorld(safe.min);
            Vector2 max = ScreenToWorld(safe.max);
            Rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);

            if (!force) Changed?.Invoke();
        }

        public static Vector2 ScreenToWorld(Vector2 screen)
        {
            float h = cam.orthographicSize * 2f;
            float w = h * (Screen.width / (float)Mathf.Max(1, Screen.height));
            float nx = screen.x / Mathf.Max(1, Screen.width) - 0.5f;
            float ny = screen.y / Mathf.Max(1, Screen.height) - 0.5f;
            return center + new Vector2(nx * w, ny * h);
        }

        /// <summary>Coordinate (on the fall axis) of the given edge of the safe playfield.</summary>
        public static float EdgeCoordinate(FloorEdge edge)
        {
            switch (edge)
            {
                case FloorEdge.Top: return Rect.yMax;
                case FloorEdge.Left: return Rect.xMin;
                case FloorEdge.Right: return Rect.xMax;
                default: return Rect.yMin;
            }
        }

        /// <summary>Coordinate of the given edge of the full camera view (beyond the safe area).</summary>
        public static float ViewEdgeCoordinate(FloorEdge edge)
        {
            switch (edge)
            {
                case FloorEdge.Top: return ViewRect.yMax;
                case FloorEdge.Left: return ViewRect.xMin;
                case FloorEdge.Right: return ViewRect.xMax;
                default: return ViewRect.yMin;
            }
        }

        /// <summary>Point on the edge, `inset` units inward, at `along` on the edge's axis.</summary>
        public static Vector2 PointOnEdge(FloorEdge edge, float along, float inset)
        {
            float c = EdgeCoordinate(edge) - Mathf.Sign(AxisSign(edge)) * inset;
            return edge.IsHorizontal() ? new Vector2(along, c) : new Vector2(c, along);
        }

        /// <summary>+1 if the edge sits on the positive side of its axis (Top/Right), -1 otherwise.</summary>
        public static float AxisSign(FloorEdge edge) => (edge == FloorEdge.Top || edge == FloorEdge.Right) ? 1f : -1f;

        /// <summary>Min/max of the axis the catcher slides along for this floor.</summary>
        public static Vector2 AlongRange(FloorEdge edge) =>
            edge.IsHorizontal() ? new Vector2(Rect.xMin, Rect.xMax) : new Vector2(Rect.yMin, Rect.yMax);
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Pulse
{
    /// <summary>Faint neon grid that scrolls in the current direction of gravity.</summary>
    public class BackgroundGrid : MonoBehaviour
    {
        [SerializeField] private float spacing = 1.2f;
        [SerializeField] private float scrollSpeed = 0.6f;

        private CatcherController catcher;
        private readonly List<GameObject> lines = new List<GameObject>();
        private Vector2 offset;
        private Vector2 scrollDir = Vector2.down;
        private float speedMultiplier = 1f;

        public void Init(CatcherController c)
        {
            catcher = c;
            Build();
            Playfield.Changed += Build;
        }

        private void OnDestroy() => Playfield.Changed -= Build;

        public void SetSpeedMultiplier(float m) => speedMultiplier = m;

        private void Build()
        {
            foreach (var l in lines) Destroy(l);
            lines.Clear();

            Rect v = Playfield.ViewRect;
            // Oversize by a few cells so scrolling and camera roll never reveal the edges.
            float pad = spacing * 3f;
            float xMin = Mathf.Floor((v.xMin - pad) / spacing) * spacing;
            float xMax = Mathf.Ceil((v.xMax + pad) / spacing) * spacing;
            float yMin = Mathf.Floor((v.yMin - pad) / spacing) * spacing;
            float yMax = Mathf.Ceil((v.yMax + pad) / spacing) * spacing;

            for (float x = xMin; x <= xMax + 0.001f; x += spacing)
                lines.Add(GlowKit.CreateLineObject("V", transform,
                    new[] { new Vector3(x, yMin), new Vector3(x, yMax) }, GlowKit.Grid, 0.08f, false, -10));
            for (float y = yMin; y <= yMax + 0.001f; y += spacing)
                lines.Add(GlowKit.CreateLineObject("H", transform,
                    new[] { new Vector3(xMin, y), new Vector3(xMax, y) }, GlowKit.Grid, 0.08f, false, -10));
        }

        private void Update()
        {
            if (catcher != null)
                scrollDir = Vector2.Lerp(scrollDir, catcher.GetFallDirection(), Time.deltaTime * 4f);

            offset += scrollDir * scrollSpeed * speedMultiplier * Time.deltaTime;
            offset.x = Mathf.Repeat(offset.x, spacing);
            offset.y = Mathf.Repeat(offset.y, spacing);
            transform.localPosition = new Vector3(offset.x, offset.y, 5f);
        }
    }
}

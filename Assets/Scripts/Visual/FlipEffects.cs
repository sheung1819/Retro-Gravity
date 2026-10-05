using UnityEngine;

namespace Pulse
{
    /// <summary>
    /// Everything that makes a gravity flip read clearly: a steady glow on the
    /// current floor, a pulsing telegraph on the upcoming floor, and on the flip
    /// itself a screen flash, camera roll/shake and a spark burst.
    /// Also handles hit/catch feedback bursts.
    /// </summary>
    public class FlipEffects : MonoBehaviour
    {
        private CatcherController catcher;
        private CameraRig rig;
        private ParticleSystem sparks;

        private LineRenderer floorLine;
        private LineRenderer warnLine;
        private SpriteRenderer flash;
        private Color flashColor;
        private float flashAlpha;

        private FloorEdge shownFloor;
        private float floorFade = 1f;
        private bool warning;
        private FloorEdge warnFloor;
        private float warnTime;

        public void Init(CatcherController c, CameraRig cameraRig, Transform cameraTransform)
        {
            catcher = c;
            rig = cameraRig;

            floorLine = GlowKit.CreateLineObject("FloorLine", transform, new Vector3[2], GlowKit.Floor, 0.28f, false, 0)
                .GetComponent<LineRenderer>();
            floorLine.useWorldSpace = true;
            warnLine = GlowKit.CreateLineObject("WarnLine", transform, new Vector3[2], GlowKit.Warning, 0.45f, false, 0)
                .GetComponent<LineRenderer>();
            warnLine.useWorldSpace = true;
            warnLine.enabled = false;

            sparks = GlowKit.CreateParticles("Bursts", transform, 600);

            // Full-screen flash parented to the camera so it always covers the view.
            flash = GlowKit.CreateQuad("Flash", cameraTransform, Color.clear, 50);
            flash.transform.localPosition = new Vector3(0f, 0f, 1f);
            LayoutFlash();

            shownFloor = catcher.CurrentFloor;
            PlaceEdgeLine(floorLine, shownFloor);
            Playfield.Changed += OnPlayfieldChanged;
        }

        private void OnEnable()
        {
            GameEvents.FlipWarning += OnFlipWarning;
            GameEvents.FlipStart += OnFlipStart;
            GameEvents.FlipEnd += OnFlipEnd;
            GameEvents.Caught += OnCaught;
            GameEvents.Hit += OnHit;
            GameEvents.Missed += OnMissed;
            GameEvents.RunStarted += OnRunStarted;
            GameEvents.RunEnded += OnRunEnded;
        }

        private void OnDisable()
        {
            GameEvents.FlipWarning -= OnFlipWarning;
            GameEvents.FlipStart -= OnFlipStart;
            GameEvents.FlipEnd -= OnFlipEnd;
            GameEvents.Caught -= OnCaught;
            GameEvents.Hit -= OnHit;
            GameEvents.Missed -= OnMissed;
            GameEvents.RunStarted -= OnRunStarted;
            GameEvents.RunEnded -= OnRunEnded;
        }

        private void OnDestroy() => Playfield.Changed -= OnPlayfieldChanged;

        private void OnPlayfieldChanged()
        {
            LayoutFlash();
            PlaceEdgeLine(floorLine, shownFloor);
            if (warning) PlaceEdgeLine(warnLine, warnFloor);
        }

        private void LayoutFlash()
        {
            Rect v = Playfield.ViewRect;
            // Oversized so camera roll never shows its corners.
            flash.transform.localScale = new Vector3(v.width * 1.6f, v.height * 1.6f, 1f);
        }

        private static void PlaceEdgeLine(LineRenderer lr, FloorEdge edge)
        {
            Rect r = Playfield.Rect;
            const float inset = 0.25f;
            Vector2 a, b;
            switch (edge)
            {
                case FloorEdge.Top: a = new Vector2(r.xMin, r.yMax - inset); b = new Vector2(r.xMax, r.yMax - inset); break;
                case FloorEdge.Left: a = new Vector2(r.xMin + inset, r.yMin); b = new Vector2(r.xMin + inset, r.yMax); break;
                case FloorEdge.Right: a = new Vector2(r.xMax - inset, r.yMin); b = new Vector2(r.xMax - inset, r.yMax); break;
                default: a = new Vector2(r.xMin, r.yMin + inset); b = new Vector2(r.xMax, r.yMin + inset); break;
            }
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
        }

        // ---------- Events ----------

        private void OnFlipWarning(FloorEdge next, float lead)
        {
            warning = true;
            warnFloor = next;
            warnTime = 0f;
            PlaceEdgeLine(warnLine, next);
            warnLine.enabled = true;
        }

        private void OnFlipStart(FloorEdge from, FloorEdge to)
        {
            warning = false;
            warnLine.enabled = false;

            Flash(Color.white, 0.35f);
            rig.Roll(Random.value < 0.5f ? -140f : 140f);
            rig.Shake(0.45f);
            GlowKit.Burst(sparks, catcher.transform.position, GlowKit.Catcher, 40, 1.3f);

            shownFloor = to;
            floorFade = 0f;
            PlaceEdgeLine(floorLine, to);
        }

        private void OnFlipEnd(FloorEdge floor)
        {
            GlowKit.Burst(sparks, catcher.transform.position, GlowKit.Floor, 16, 0.7f);
        }

        private void OnCaught(Vector3 pos, int combo) =>
            GlowKit.Burst(sparks, pos, GlowKit.Good, 12 + Mathf.Min(combo, 20));

        private void OnHit(Vector3 pos, int lives)
        {
            GlowKit.Burst(sparks, pos, GlowKit.Bad, 45, 1.5f);
            Flash(GlowKit.Bad, 0.45f);
            rig.Shake(0.8f);
        }

        private void OnMissed(Vector3 pos) => GlowKit.Burst(sparks, pos, GlowKit.Good * 0.5f, 6, 0.5f);

        private void OnRunStarted()
        {
            warning = false;
            warnLine.enabled = false;
            shownFloor = catcher.CurrentFloor;
            PlaceEdgeLine(floorLine, shownFloor);
            floorFade = 0f;
        }

        private void OnRunEnded(int score, bool newBest)
        {
            warning = false;
            warnLine.enabled = false;
        }

        private void Flash(Color c, float alpha)
        {
            flashColor = c;
            flashAlpha = Mathf.Max(flashAlpha, alpha);
        }

        // ---------- Per-frame ----------

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, dt * 1.6f);
            flash.color = new Color(flashColor.r, flashColor.g, flashColor.b, flashAlpha);

            floorFade = Mathf.MoveTowards(floorFade, 1f, dt * 3f);
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 3f);
            Color fc = GlowKit.Floor * (floorFade * pulse);
            fc.a = floorFade * pulse;
            floorLine.startColor = floorLine.endColor = fc;

            if (warning)
            {
                warnTime += Time.deltaTime;
                float blink = 0.5f + 0.5f * Mathf.Sin(warnTime * Mathf.PI * 2f * 5f);
                Color wc = GlowKit.Warning;
                wc.a = Mathf.Lerp(0.25f, 1f, blink);
                warnLine.startColor = warnLine.endColor = wc;
                warnLine.widthMultiplier = Mathf.Lerp(0.35f, 0.6f, blink);
            }
        }
    }
}

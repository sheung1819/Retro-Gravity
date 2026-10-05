using UnityEngine;

namespace Pulse
{
    /// <summary>Score/lives/combo readout plus the title, pause and game-over overlays.</summary>
    public class Hud : MonoBehaviour
    {
        private PixelText score;
        private PixelText combo;
        private PixelText lives;
        private PixelText title;
        private PixelText subtitle;
        private PixelText info;
        private PixelText detail;

        private bool blinkSubtitle;

        public void Init()
        {
            score = PixelText.Create("Score", transform, 0.09f, PixelText.Align.Left, GlowKit.Text);
            lives = PixelText.Create("Lives", transform, 0.09f, PixelText.Align.Right, GlowKit.Bad);
            combo = PixelText.Create("Combo", transform, 0.08f, PixelText.Align.Right, GlowKit.Good);
            title = PixelText.Create("Title", transform, 0.2f, PixelText.Align.Center, GlowKit.Title);
            subtitle = PixelText.Create("Subtitle", transform, 0.08f, PixelText.Align.Center, GlowKit.Text);
            info = PixelText.Create("Info", transform, 0.075f, PixelText.Align.Center, GlowKit.Warning);
            detail = PixelText.Create("Detail", transform, 0.075f, PixelText.Align.Center, GlowKit.Text);
            Layout();
            Playfield.Changed += Layout;
        }

        private void OnDestroy() => Playfield.Changed -= Layout;

        private void Layout()
        {
            Rect r = Playfield.Rect;
            const float z = 10f; // in front of the camera (HUD is parented to it)
            const float pad = 0.45f;
            float top = r.yMax - pad - 0.35f;
            score.transform.localPosition = new Vector3(r.xMin + pad, top, z);
            lives.transform.localPosition = new Vector3(r.xMax - pad, top, z);
            combo.transform.localPosition = new Vector3(r.xMax - pad, top - 0.9f, z);

            title.transform.localPosition = new Vector3(r.center.x, r.center.y + 2.2f, z);
            subtitle.transform.localPosition = new Vector3(r.center.x, r.center.y - 0.2f, z);
            info.transform.localPosition = new Vector3(r.center.x, r.center.y - 1.3f, z);
            detail.transform.localPosition = new Vector3(r.center.x, r.center.y - 2.1f, z);
            FitTitle();
        }

        private void SetTitle(string text)
        {
            title.Text = text;
            FitTitle();
        }

        // Shrink the big title so it always fits the width of narrow phones.
        private void FitTitle()
        {
            float w = title.MeasureWidth(title.Text);
            float max = Playfield.Rect.width * 0.85f;
            title.transform.localScale = Vector3.one * (w > max ? max / w : 1f);
        }

        public void SetScore(int value) => score.Text = value.ToString();

        public void SetLives(int value) => lives.Text = new string('*', Mathf.Max(0, value));

        public void SetCombo(int multiplier) => combo.Text = multiplier > 1 ? "X" + multiplier : "";

        public void ShowTitle(int best)
        {
            SetGameplayVisible(false);
            SetTitle("PULSE");
            subtitle.Text = "TAP TO START";
            info.Text = "DRAG TO MOVE";
            detail.Text = best > 0 ? "BEST " + best : "";
            blinkSubtitle = true;
        }

        public void ShowPlaying()
        {
            SetGameplayVisible(true);
            SetTitle("");
            subtitle.Text = "";
            info.Text = "";
            detail.Text = "";
            blinkSubtitle = false;
        }

        public void ShowPaused()
        {
            SetTitle("PAUSED");
            subtitle.Text = "TAP TO RESUME";
            info.Text = "";
            detail.Text = "";
            blinkSubtitle = true;
        }

        public void ShowGameOver(int finalScore, int best, bool newBest)
        {
            SetTitle("GAME OVER");
            subtitle.Text = "";
            info.Text = "SCORE " + finalScore;
            detail.Text = newBest ? "NEW BEST!" : "BEST " + best;
            blinkSubtitle = false;
        }

        public void ShowRetryPrompt()
        {
            subtitle.Text = "TAP TO RETRY";
            blinkSubtitle = true;
        }

        private void SetGameplayVisible(bool visible)
        {
            score.Visible = visible;
            lives.Visible = visible;
            combo.Visible = visible;
        }

        private void Update()
        {
            subtitle.Visible = !blinkSubtitle || Mathf.Repeat(Time.unscaledTime, 1f) < 0.7f;
        }
    }
}

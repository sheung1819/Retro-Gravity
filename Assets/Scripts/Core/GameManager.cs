using UnityEngine;

namespace Pulse
{
    /// <summary>
    /// Run state machine (title → playing ⇄ paused → game over), scoring, lives
    /// and the difficulty ramp.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public enum State { Title, Playing, Paused, GameOver }

        private const string BestKey = "pulse.best";
        private const float RetryDelay = 1.0f;

        private PulseConfig config;
        private CatcherController catcher;
        private CatcherVisual catcherVisual;
        private ObjectSpawner spawner;
        private BackgroundGrid grid;
        private Hud hud;

        public State CurrentState { get; private set; } = State.Title;

        private float elapsed;
        private float catchPoints;
        private int combo;
        private int lives;
        private int best;
        private float invulnerableUntil;
        private float stateTime;

        public int Score => Mathf.FloorToInt(catchPoints + elapsed * config.pointsPerSecond);
        public int Multiplier => Mathf.Min(config.maxMultiplier, 1 + combo / Mathf.Max(1, config.catchesPerMultiplier));

        public void Init(PulseConfig cfg, CatcherController c, CatcherVisual cv, ObjectSpawner s, BackgroundGrid g, Hud h)
        {
            config = cfg;
            catcher = c;
            catcherVisual = cv;
            spawner = s;
            grid = g;
            hud = h;
            best = PlayerPrefs.GetInt(BestKey, 0);
        }

        private void OnEnable() => GameEvents.FlipEnd += OnFlipEnd;
        private void OnDisable() => GameEvents.FlipEnd -= OnFlipEnd;

        private void Start() => EnterTitle();

        private void EnterTitle()
        {
            CurrentState = State.Title;
            stateTime = 0f;
            catcher.SetFlipsEnabled(false);
            spawner.SetSpawning(false);
            grid.SetSpeedMultiplier(0.5f);
            hud.ShowTitle(best);
        }

        private void StartRun()
        {
            CurrentState = State.Playing;
            stateTime = 0f;
            elapsed = 0f;
            catchPoints = 0f;
            combo = 0;
            lives = config.startingLives;
            invulnerableUntil = 0f;

            spawner.ClearAll();
            catcher.ResetForRun();
            catcher.SetFlipIntervalRange(config.flipIntervalStart.x, config.flipIntervalStart.y);
            catcher.SetFlipsEnabled(true);
            spawner.SetSpawning(true);
            ApplyDifficulty();

            hud.ShowPlaying();
            hud.SetLives(lives);
            hud.SetCombo(1);
            hud.SetScore(0);
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            GameEvents.RaiseRunStarted();
        }

        private void EndRun()
        {
            CurrentState = State.GameOver;
            stateTime = 0f;
            catcher.SetFlipsEnabled(false);
            spawner.SetSpawning(false);
            spawner.ClearAll();

            int finalScore = Score;
            bool newBest = finalScore > best;
            if (newBest)
            {
                best = finalScore;
                PlayerPrefs.SetInt(BestKey, best);
                PlayerPrefs.Save();
            }

            hud.ShowGameOver(finalScore, best, newBest);
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
            GameEvents.RaiseRunEnded(finalScore, newBest);
        }

        private void SetPaused(bool paused)
        {
            if (paused && CurrentState != State.Playing) return;
            if (!paused && CurrentState != State.Paused) return;

            CurrentState = paused ? State.Paused : State.Playing;
            Time.timeScale = paused ? 0f : 1f;
            if (paused) hud.ShowPaused();
            else hud.ShowPlaying();
            GameEvents.RaisePauseChanged(paused);
        }

        // Pause automatically when the app is backgrounded (call, notification, home button).
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SetPaused(true);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && !Application.isEditor) SetPaused(true);
        }

        private void Update()
        {
            Playfield.Poll();
            stateTime += Time.unscaledDeltaTime;

            switch (CurrentState)
            {
                case State.Title:
                    if (PulseInput.PressedThisFrame) StartRun();
                    break;

                case State.Playing:
                    elapsed += Time.deltaTime;
                    ApplyDifficulty();
                    hud.SetScore(Score);
                    break;

                case State.Paused:
                    if (PulseInput.PressedThisFrame) SetPaused(false);
                    break;

                case State.GameOver:
                    if (stateTime >= RetryDelay)
                    {
                        if (stateTime - Time.unscaledDeltaTime < RetryDelay) hud.ShowRetryPrompt();
                        if (PulseInput.PressedThisFrame) StartRun();
                    }
                    break;
            }
        }

        private void ApplyDifficulty()
        {
            float t = config.Ramp(elapsed);
            spawner.SetDifficulty(
                Mathf.Lerp(config.spawnInterval.x, config.spawnInterval.y, t),
                Mathf.Lerp(config.travelTime.x, config.travelTime.y, t),
                Mathf.Lerp(config.badChance.x, config.badChance.y, t));
            catcher.SetFlipIntervalRange(
                Mathf.Lerp(config.flipIntervalStart.x, config.flipIntervalEnd.x, t),
                Mathf.Lerp(config.flipIntervalStart.y, config.flipIntervalEnd.y, t));
            grid.SetSpeedMultiplier(1f + t * 2f);
        }

        // ---------- Called by ObjectSpawner ----------

        public void OnGoodCaught(Vector3 pos)
        {
            if (CurrentState != State.Playing) return;
            combo++;
            catchPoints += config.pointsPerCatch * Multiplier;
            hud.SetCombo(Multiplier);
            GameEvents.RaiseCaught(pos, combo);
        }

        public void OnGoodMissed(Vector3 pos)
        {
            if (CurrentState != State.Playing) return;
            combo = 0;
            hud.SetCombo(1);
            GameEvents.RaiseMissed(pos);
        }

        /// <summary>A hazard touched the catcher. Returns false if it was ignored (invulnerable).</summary>
        public bool OnBadTouched(Vector3 pos)
        {
            if (CurrentState != State.Playing) return true;
            if (catcher.IsFlipping || Time.time < invulnerableUntil) return false;

            lives--;
            combo = 0;
            hud.SetLives(lives);
            hud.SetCombo(1);
            Haptics.Heavy();
            GameEvents.RaiseHit(pos, lives);

            if (lives <= 0)
            {
                EndRun();
            }
            else
            {
                invulnerableUntil = Time.time + config.hitInvulnerability;
                catcherVisual.Blink(config.hitInvulnerability);
            }
            return true;
        }

        private void OnFlipEnd(FloorEdge floor)
        {
            // Grace window after reorienting, so a flip never causes a cheap death.
            invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + config.flipGracePeriod);
            if (CurrentState == State.Playing) catcherVisual.Blink(config.flipGracePeriod);
        }
    }
}

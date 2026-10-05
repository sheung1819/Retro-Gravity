using UnityEngine;

namespace Pulse
{
    /// <summary>
    /// Plays the music loop and SFX in response to <see cref="GameEvents"/>.
    /// Assign authored clips in the Inspector to override the synthesized ones.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Optional overrides (synthesized if empty)")]
        [SerializeField] private AudioClip music;
        [SerializeField] private AudioClip catchClip;
        [SerializeField] private AudioClip hitClip;
        [SerializeField] private AudioClip missClip;
        [SerializeField] private AudioClip flipClip;
        [SerializeField] private AudioClip warningClip;
        [SerializeField] private AudioClip startClip;
        [SerializeField] private AudioClip gameOverClip;

        [Header("Mix")]
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.55f;
        [Range(0f, 1f)] [SerializeField] private float musicVolumeIdle = 0.3f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.9f;

        private AudioSource musicSource;
        private AudioSource[] sfxSources;
        private int nextSfx;
        private float musicTarget;

        private void Awake()
        {
            if (music == null) music = ChiptuneSynth.Music();
            if (catchClip == null) catchClip = ChiptuneSynth.Catch();
            if (hitClip == null) hitClip = ChiptuneSynth.Hit();
            if (missClip == null) missClip = ChiptuneSynth.Miss();
            if (flipClip == null) flipClip = ChiptuneSynth.Flip();
            if (warningClip == null) warningClip = ChiptuneSynth.Warning();
            if (startClip == null) startClip = ChiptuneSynth.Start();
            if (gameOverClip == null) gameOverClip = ChiptuneSynth.GameOver();

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.clip = music;
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            musicSource.volume = musicTarget = musicVolumeIdle;

            sfxSources = new AudioSource[6];
            for (int i = 0; i < sfxSources.Length; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                sfxSources[i] = s;
            }
        }

        private void OnEnable()
        {
            GameEvents.FlipWarning += OnFlipWarning;
            GameEvents.FlipStart += OnFlipStart;
            GameEvents.Caught += OnCaught;
            GameEvents.Hit += OnHit;
            GameEvents.Missed += OnMissed;
            GameEvents.RunStarted += OnRunStarted;
            GameEvents.RunEnded += OnRunEnded;
            GameEvents.PauseChanged += OnPauseChanged;
        }

        private void OnDisable()
        {
            GameEvents.FlipWarning -= OnFlipWarning;
            GameEvents.FlipStart -= OnFlipStart;
            GameEvents.Caught -= OnCaught;
            GameEvents.Hit -= OnHit;
            GameEvents.Missed -= OnMissed;
            GameEvents.RunStarted -= OnRunStarted;
            GameEvents.RunEnded -= OnRunEnded;
            GameEvents.PauseChanged -= OnPauseChanged;
        }

        private void Start() => musicSource.Play();

        private void Update()
        {
            musicSource.volume = Mathf.MoveTowards(musicSource.volume, musicTarget, Time.unscaledDeltaTime * 0.8f);
        }

        private void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            AudioSource s = sfxSources[nextSfx];
            nextSfx = (nextSfx + 1) % sfxSources.Length;
            s.pitch = pitch;
            s.PlayOneShot(clip, volume * sfxVolume);
        }

        private void OnFlipWarning(FloorEdge next, float lead)
        {
            Play(warningClip, 0.8f);
            Invoke(nameof(PlayWarningEcho), Mathf.Max(0.05f, lead * 0.5f));
        }

        private void PlayWarningEcho() => Play(warningClip, 0.8f, 1.12f);

        private void OnFlipStart(FloorEdge from, FloorEdge to) => Play(flipClip, 1f, Random.Range(0.95f, 1.05f));

        // Pitch climbs with the combo so streaks feel rewarding.
        private void OnCaught(Vector3 pos, int combo) => Play(catchClip, 0.8f, 1f + Mathf.Min(combo, 20) * 0.03f);

        private void OnHit(Vector3 pos, int lives) => Play(hitClip, 1f);

        private void OnMissed(Vector3 pos) => Play(missClip, 0.6f);

        private void OnRunStarted()
        {
            Play(startClip, 0.8f);
            musicTarget = musicVolume;
        }

        private void OnRunEnded(int score, bool newBest)
        {
            Play(gameOverClip, 0.9f);
            musicTarget = musicVolumeIdle;
        }

        private void OnPauseChanged(bool paused)
        {
            if (paused) musicSource.Pause();
            else musicSource.UnPause();
        }
    }
}

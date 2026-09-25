using UnityEngine;

namespace Pulse
{
    /// <summary>
    /// Tiny offline synthesizer: renders the chiptune loop and all SFX into
    /// AudioClips at startup, so the project ships with zero audio files.
    /// Swap any of these for authored clips later via <see cref="AudioManager"/>.
    /// </summary>
    public static class ChiptuneSynth
    {
        public const int SampleRate = 22050;

        private static System.Random rng = new System.Random(1234);
        private static float Noise() => (float)(rng.NextDouble() * 2.0 - 1.0);

        private static float Midi(float note) => 440f * Mathf.Pow(2f, (note - 69f) / 12f);

        private static AudioClip ToClip(string name, float[] data)
        {
            // Gentle soft-clip so stacked voices never hard-clip.
            for (int i = 0; i < data.Length; i++) data[i] = (float)System.Math.Tanh(data[i] * 1.2f) * 0.9f;
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Square(float phase, float duty) => (phase - Mathf.Floor(phase)) < duty ? 1f : -1f;
        private static float Triangle(float phase)
        {
            float p = phase - Mathf.Floor(phase);
            return 4f * Mathf.Abs(p - 0.5f) - 1f;
        }

        private enum Wave { Square12, Square25, Square50, Triangle }

        private static void Tone(float[] buf, int start, int length, float freq, Wave wave, float volume, float decayPower = 1.5f)
        {
            float phase = 0f;
            float step = freq / SampleRate;
            int attack = Mathf.Min(length, SampleRate / 400);
            for (int i = 0; i < length && start + i < buf.Length; i++)
            {
                float env = i < attack ? i / (float)attack : Mathf.Pow(1f - (i - attack) / (float)Mathf.Max(1, length - attack), decayPower);
                float s;
                switch (wave)
                {
                    case Wave.Square12: s = Square(phase, 0.125f); break;
                    case Wave.Square25: s = Square(phase, 0.25f); break;
                    case Wave.Square50: s = Square(phase, 0.5f); break;
                    default: s = Triangle(phase); break;
                }
                buf[start + i] += s * env * volume;
                phase += step;
            }
        }

        // ---------- Music ----------

        /// <summary>8-bar A-minor loop at 150 BPM: triangle bass, square arpeggio, pulse lead, noise drums.</summary>
        public static AudioClip Music()
        {
            const float bpm = 150f;
            const int bars = 8;
            int sixteenth = Mathf.RoundToInt(SampleRate * 60f / bpm / 4f);
            int barLen = sixteenth * 16;
            var buf = new float[barLen * bars];

            // (root midi note, is minor) per bar
            int[] roots = { 45, 41, 48, 43, 45, 41, 43, 40 };
            bool[] minor = { true, false, false, false, true, false, false, false };

            // Lead melody: one entry per 8th note (16 per bar pair), -1 = rest. Scale degrees as midi.
            int[] lead =
            {
                76, -1, 72, 74, 76, -1, 79, 76,   77, -1, 76, 74, 72, -1, 69, -1,
                72, -1, 76, 79, 81, -1, 79, 76,   74, -1, 71, 74, 79, -1, 74, -1,
                76, -1, 72, 74, 76, -1, 79, 81,   77, -1, 81, 84, 81, -1, 77, -1,
                79, -1, 76, 79, 83, -1, 79, 74,   80, -1, 76, 71, 68, -1, 71, -1,
            };

            for (int bar = 0; bar < bars; bar++)
            {
                int barStart = bar * barLen;
                int root = roots[bar];
                int third = minor[bar] ? 3 : 4;
                int[] arp = { 0, third, 7, 12, 7, third, 0, 12 };

                for (int s = 0; s < 16; s++)
                {
                    int t = barStart + s * sixteenth;

                    // Bass: octave-jumping 8ths.
                    if (s % 2 == 0)
                        Tone(buf, t, sixteenth * 2 - 40, Midi(root + ((s / 2) % 2 == 0 ? 0 : 12)), Wave.Triangle, 0.32f, 0.6f);

                    // Arpeggio: 16ths, two octaves up.
                    Tone(buf, t, sixteenth - 20, Midi(root + 24 + arp[s % 8]), Wave.Square12, 0.07f, 2.5f);

                    // Drums.
                    if (s % 8 == 0) Kick(buf, t);
                    if (s % 8 == 4) Snare(buf, t);
                    if (s % 2 == 1) Hat(buf, t, 0.05f);
                }

                // Lead: 8 eighth notes per bar; the first half of the loop plays it an octave down and softer.
                bool firstHalf = bar < bars / 2;
                for (int e = 0; e < 8; e++)
                {
                    int n = lead[bar * 8 + e];
                    if (n < 0) continue;
                    Tone(buf, barStart + e * sixteenth * 2, sixteenth * 2 - 60, Midi(firstHalf ? n - 12 : n),
                        Wave.Square25, firstHalf ? 0.06f : 0.09f, 1.2f);
                }
            }

            return ToClip("PulseMusic", buf);
        }

        private static void Kick(float[] buf, int start)
        {
            int len = SampleRate / 8;
            float phase = 0f;
            for (int i = 0; i < len && start + i < buf.Length; i++)
            {
                float t = i / (float)len;
                float f = Mathf.Lerp(160f, 40f, Mathf.Sqrt(t));
                phase += f / SampleRate;
                buf[start + i] += Mathf.Sin(phase * Mathf.PI * 2f) * (1f - t) * 0.5f;
            }
        }

        private static void Snare(float[] buf, int start)
        {
            int len = SampleRate / 7;
            for (int i = 0; i < len && start + i < buf.Length; i++)
            {
                float env = Mathf.Pow(1f - i / (float)len, 2f);
                buf[start + i] += (Noise() * 0.22f + Mathf.Sin(i * 190f / SampleRate * Mathf.PI * 2f) * 0.12f) * env;
            }
        }

        private static void Hat(float[] buf, int start, float vol)
        {
            int len = SampleRate / 30;
            float prev = 0f;
            for (int i = 0; i < len && start + i < buf.Length; i++)
            {
                float n = Noise();
                float hp = n - prev; // crude high-pass
                prev = n;
                buf[start + i] += hp * vol * (1f - i / (float)len);
            }
        }

        // ---------- SFX ----------

        public static AudioClip Catch()
        {
            int step = SampleRate / 22;
            var buf = new float[step * 2];
            Tone(buf, 0, step, Midi(84), Wave.Square25, 0.35f);
            Tone(buf, step, step, Midi(91), Wave.Square25, 0.35f);
            return ToClip("SfxCatch", buf);
        }

        public static AudioClip Hit()
        {
            int len = (int)(SampleRate * 0.45f);
            var buf = new float[len];
            float phase = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)len;
                float f = Mathf.Lerp(420f, 60f, t);
                phase += f / SampleRate;
                float env = Mathf.Pow(1f - t, 1.4f);
                buf[i] = (Square(phase, 0.5f) * 0.35f + Noise() * 0.45f * (1f - t)) * env;
            }
            return ToClip("SfxHit", buf);
        }

        public static AudioClip Miss()
        {
            int len = (int)(SampleRate * 0.18f);
            var buf = new float[len];
            float phase = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)len;
                phase += Mathf.Lerp(330f, 160f, t) / SampleRate;
                buf[i] = Triangle(phase) * 0.35f * (1f - t);
            }
            return ToClip("SfxMiss", buf);
        }

        /// <summary>The flip "whoosh": filtered-noise swell with a rising then falling sweep.</summary>
        public static AudioClip Flip()
        {
            int len = (int)(SampleRate * 0.6f);
            var buf = new float[len];
            float lp = 0f, phase = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)len;
                float swell = Mathf.Sin(t * Mathf.PI);            // 0 → 1 → 0
                float cutoff = Mathf.Lerp(0.02f, 0.35f, swell);   // one-pole LPF coefficient
                lp += (Noise() - lp) * cutoff;
                phase += Mathf.Lerp(180f, 900f, swell) / SampleRate;
                buf[i] = (lp * 0.9f + Square(phase, 0.125f) * 0.12f) * swell;
            }
            return ToClip("SfxFlip", buf);
        }

        public static AudioClip Warning()
        {
            int len = (int)(SampleRate * 0.07f);
            var buf = new float[len];
            Tone(buf, 0, len, Midi(96), Wave.Square50, 0.22f, 3f);
            return ToClip("SfxWarning", buf);
        }

        public static AudioClip Start()
        {
            int step = SampleRate / 14;
            int[] notes = { 69, 72, 76, 81 };
            var buf = new float[step * notes.Length + step];
            for (int i = 0; i < notes.Length; i++)
                Tone(buf, i * step, i == notes.Length - 1 ? step * 2 : step, Midi(notes[i]), Wave.Square25, 0.3f);
            return ToClip("SfxStart", buf);
        }

        public static AudioClip GameOver()
        {
            int step = SampleRate / 7;
            int[] notes = { 76, 72, 69, 64, 57 };
            var buf = new float[step * notes.Length + step * 2];
            for (int i = 0; i < notes.Length; i++)
                Tone(buf, i * step, i == notes.Length - 1 ? step * 3 : step, Midi(notes[i]), Wave.Square50, 0.28f, 1f);
            return ToClip("SfxGameOver", buf);
        }
    }
}

using System;
using UnityEngine;

namespace Pulse
{
    /// <summary>
    /// All gameplay tuning in one place. Lives on the scene's GameBootstrap object,
    /// so it can be tweaked in the Inspector (including during Play mode).
    /// </summary>
    [Serializable]
    public class PulseConfig
    {
        [Header("Run")]
        [Tooltip("Hits from bad objects allowed before game over.")]
        public int startingLives = 3;
        [Tooltip("Points for catching a good object, before the combo multiplier.")]
        public int pointsPerCatch = 10;
        [Tooltip("Points per second survived.")]
        public float pointsPerSecond = 1f;
        [Tooltip("Consecutive catches needed per +1 multiplier step.")]
        public int catchesPerMultiplier = 5;
        public int maxMultiplier = 5;
        [Tooltip("Invulnerability after taking a hit.")]
        public float hitInvulnerability = 1.2f;
        [Tooltip("Extra invulnerability after a flip finishes, so reorienting never costs a cheap life.")]
        public float flipGracePeriod = 0.5f;

        [Header("Difficulty ramp (0 → rampDuration seconds)")]
        public float rampDuration = 150f;
        public Vector2 spawnInterval = new Vector2(1.0f, 0.38f);
        [Tooltip("Seconds for an object to cross the playfield to the floor.")]
        public Vector2 travelTime = new Vector2(3.0f, 1.4f);
        [Tooltip("Chance a spawned object is a hazard.")]
        public Vector2 badChance = new Vector2(0.25f, 0.45f);
        [Tooltip("Flip interval range (min,max) at the start of a run.")]
        public Vector2 flipIntervalStart = new Vector2(8f, 16f);
        [Tooltip("Flip interval range (min,max) once fully ramped.")]
        public Vector2 flipIntervalEnd = new Vector2(5f, 9f);

        [Header("Objects")]
        public float goodRadius = 0.32f;
        public float badRadius = 0.36f;
        [Tooltip("How quickly in-flight objects swing onto the new gravity after a flip (units/s²).")]
        public float gravityTurnRate = 28f;

        public float Ramp(float elapsed) => Mathf.Clamp01(elapsed / Mathf.Max(1f, rampDuration));
    }
}

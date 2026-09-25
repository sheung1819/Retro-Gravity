using UnityEngine;

namespace Pulse
{
    /// <summary>Glowing cup shape, motion sparks and invulnerability blink for the catcher.</summary>
    [RequireComponent(typeof(CatcherController))]
    public class CatcherVisual : MonoBehaviour
    {
        private CatcherController catcher;
        private LineRenderer outline;
        private LineRenderer core;
        private ParticleSystem sparks;
        private float blinkUntil;

        private void Awake()
        {
            catcher = GetComponent<CatcherController>();
        }

        private void Start()
        {
            float h = catcher.Length * 0.5f;
            float t = catcher.Thickness * 0.5f;
            var cup = new[]
            {
                new Vector3(-h, t), new Vector3(-h * 0.82f, -t), new Vector3(h * 0.82f, -t), new Vector3(h, t)
            };
            outline = GlowKit.AddLine(gameObject, cup, GlowKit.Catcher, 0.32f, false, 2);

            var inner = new[] { new Vector3(-h * 0.55f, -t * 0.2f), new Vector3(h * 0.55f, -t * 0.2f) };
            core = GlowKit.CreateLineObject("Core", transform, inner, GlowKit.Catcher * 0.6f, 0.18f, false, 2)
                .GetComponent<LineRenderer>();

            sparks = GlowKit.CreateParticles("Sparks", transform, 200);
            var main = sparks.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 1.2f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = GlowKit.Catcher;
            var shape = sparks.shape;
            shape.shapeType = ParticleSystemShapeType.SingleSidedEdge;
            shape.radius = h;
            var emission = sparks.emission;
            emission.rateOverDistance = 12f;
        }

        public void Blink(float duration) => blinkUntil = Time.time + duration;

        private void Update()
        {
            bool visible = Time.time >= blinkUntil || Mathf.Repeat(Time.time * 12f, 1f) > 0.35f;
            if (outline != null) outline.enabled = visible;
            if (core != null) core.enabled = visible;
        }
    }
}

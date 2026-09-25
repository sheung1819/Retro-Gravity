using UnityEngine;

namespace Pulse
{
    /// <summary>
    /// Procedural materials, textures and palette for the retro vector-glow style.
    /// Nothing is loaded from disk except the additive shader.
    /// </summary>
    public static class GlowKit
    {
        // ---------- Palette ----------
        public static readonly Color Background = new Color(0.02f, 0.012f, 0.06f);
        public static readonly Color Grid = new Color(0.35f, 0.12f, 0.75f, 0.16f);
        public static readonly Color Catcher = new Color(0.25f, 1f, 1f);
        public static readonly Color Good = new Color(0.45f, 1f, 0.35f);
        public static readonly Color Bad = new Color(1f, 0.18f, 0.45f);
        public static readonly Color Warning = new Color(1f, 0.75f, 0.15f);
        public static readonly Color Floor = new Color(0.25f, 0.85f, 1f);
        public static readonly Color Text = new Color(0.85f, 0.95f, 1f);
        public static readonly Color Title = new Color(1f, 0.3f, 0.85f);

        public static Material LineMaterial { get; private set; }
        public static Material DotMaterial { get; private set; }
        public static Material SolidMaterial { get; private set; }
        public static Sprite WhiteSprite { get; private set; }

        private static bool initialized;

        public static void Init()
        {
            if (initialized) return;
            initialized = true;

            Shader shader = Shader.Find("Pulse/Additive");
            if (shader == null)
            {
                Debug.LogWarning("Pulse/Additive shader not found; falling back to Sprites/Default (no additive glow).");
                shader = Shader.Find("Sprites/Default");
            }

            LineMaterial = new Material(shader) { name = "PulseLine", mainTexture = CreateLineTexture() };
            DotMaterial = new Material(shader) { name = "PulseDot", mainTexture = CreateDotTexture(64) };
            SolidMaterial = new Material(shader) { name = "PulseSolid", mainTexture = Texture2D.whiteTexture };

            var white = new Texture2D(4, 4, TextureFormat.RGBA32, false) { name = "PulseWhite" };
            var px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            white.SetPixels32(px);
            white.Apply();
            WhiteSprite = Sprite.Create(white, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        /// <summary>Cross-section of a glowing line: thin hot core, soft halo. U runs along the line, V across it.</summary>
        private static Texture2D CreateLineTexture()
        {
            const int h = 64;
            var tex = new Texture2D(4, h, TextureFormat.RGBA32, false)
            {
                name = "PulseLineGlow", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
            };
            var px = new Color[4 * h];
            for (int y = 0; y < h; y++)
            {
                float d = Mathf.Abs((y + 0.5f) / h - 0.5f) * 2f; // 0 centre → 1 edge
                float core = 1f - Mathf.SmoothStep(0.12f, 0.28f, d);
                float halo = Mathf.Pow(1f - d, 2.2f) * 0.55f;
                float a = Mathf.Clamp01(core + halo);
                // Core bleeds toward white so tinted lines keep a hot centre.
                float w = Mathf.Lerp(0.75f, 1f, core);
                for (int x = 0; x < 4; x++) px[y * 4 + x] = new Color(w, w, w, a);
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateDotTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "PulseDot", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
            };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float r = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                float core = 1f - Mathf.SmoothStep(0.15f, 0.45f, r);
                float halo = Mathf.Pow(1f - r, 2f) * 0.5f;
                px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(core + halo));
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        // ---------- Builders ----------

        /// <summary>Adds a glowing polyline to `go` using local-space points.</summary>
        public static LineRenderer AddLine(GameObject go, Vector3[] points, Color color, float width, bool loop, int sortingOrder = 0)
        {
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = loop;
            lr.positionCount = points.Length;
            lr.SetPositions(points);
            lr.widthMultiplier = width;
            lr.numCornerVertices = 3;
            lr.numCapVertices = 3;
            lr.textureMode = LineTextureMode.Stretch;
            lr.alignment = LineAlignment.View;
            lr.sharedMaterial = LineMaterial;
            lr.startColor = lr.endColor = color;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.sortingOrder = sortingOrder;
            return lr;
        }

        public static GameObject CreateLineObject(string name, Transform parent, Vector3[] points, Color color, float width, bool loop, int sortingOrder = 0)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            AddLine(go, points, color, width, loop, sortingOrder);
            return go;
        }

        public static TrailRenderer AddTrail(GameObject go, Color color, float width, float time)
        {
            var tr = go.AddComponent<TrailRenderer>();
            tr.time = time;
            tr.minVertexDistance = 0.08f;
            tr.widthMultiplier = width;
            tr.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0f, 1f) });
            tr.colorGradient = g;
            tr.textureMode = LineTextureMode.Stretch;
            tr.sharedMaterial = LineMaterial;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.receiveShadows = false;
            tr.sortingOrder = -1;
            return tr;
        }

        public static SpriteRenderer CreateQuad(string name, Transform parent, Color color, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = WhiteSprite;
            sr.sharedMaterial = SolidMaterial;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return sr;
        }

        /// <summary>A world-space particle system for glowing sparks. Emits nothing until told to.</summary>
        public static ParticleSystem CreateParticles(string name, Transform parent, int maxParticles)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.gravityModifier = 0f;

            var emission = ps.emission;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.05f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = g;

            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 20f;
            limit.dampen = 0.12f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = DotMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 5;

            // Keep it "playing" with zero emission so Emit() bursts simulate immediately.
            ps.Play();
            return ps;
        }

        /// <summary>Emit a radial spark burst at a world position.</summary>
        public static void Burst(ParticleSystem ps, Vector3 position, Color color, int count, float speedScale = 1f)
        {
            if (ps == null) return;
            for (int i = 0; i < count; i++)
            {
                Vector2 dir = Random.insideUnitCircle.normalized;
                var ep = new ParticleSystem.EmitParams
                {
                    position = position,
                    velocity = dir * Random.Range(2f, 7f) * speedScale,
                    startColor = color,
                    startSize = Random.Range(0.12f, 0.3f),
                    startLifetime = Random.Range(0.3f, 0.75f),
                };
                ps.Emit(ep, 1);
            }
        }

        // ---------- Shapes (local space, authored for the bottom floor) ----------

        public static Vector3[] RegularPolygon(int sides, float radius, float rotationDeg = 0f)
        {
            var pts = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = (rotationDeg + 360f * i / sides) * Mathf.Deg2Rad;
                pts[i] = new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * radius;
            }
            return pts;
        }

        public static Vector3[] Star(int points, float outer, float inner, float rotationDeg = 90f)
        {
            var pts = new Vector3[points * 2];
            for (int i = 0; i < pts.Length; i++)
            {
                float r = (i % 2 == 0) ? outer : inner;
                float a = (rotationDeg + 180f * i / points) * Mathf.Deg2Rad;
                pts[i] = new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * r;
            }
            return pts;
        }
    }
}

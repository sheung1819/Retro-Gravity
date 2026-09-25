using UnityEngine;

namespace Pulse
{
    /// <summary>A good (catch it) or bad (dodge it) object. Moved by <see cref="ObjectSpawner"/>.</summary>
    public class FallingObject : MonoBehaviour
    {
        public bool IsGood { get; private set; }
        public float Radius { get; private set; }
        public Vector2 Velocity;

        private LineRenderer line;
        private TrailRenderer trail;
        private float spin;

        public static FallingObject Create(Transform parent, bool good, float radius)
        {
            var go = new GameObject(good ? "Good" : "Bad");
            go.transform.SetParent(parent, false);
            var obj = go.AddComponent<FallingObject>();
            obj.IsGood = good;
            obj.Radius = radius;

            Color c = good ? GlowKit.Good : GlowKit.Bad;
            // Distinct silhouettes as well as colours: diamond = good, spiky star = bad.
            Vector3[] shape = good
                ? GlowKit.RegularPolygon(4, radius, 90f)
                : GlowKit.Star(5, radius * 1.1f, radius * 0.5f);
            obj.line = GlowKit.AddLine(go, shape, c, 0.22f, true, 1);
            obj.trail = GlowKit.AddTrail(go, c, radius * 1.1f, 0.25f);
            return obj;
        }

        public void Launch(Vector3 position, Vector2 velocity)
        {
            transform.position = position;
            transform.rotation = Quaternion.identity;
            Velocity = velocity;
            spin = Random.Range(90f, 220f) * (Random.value < 0.5f ? -1f : 1f);
            gameObject.SetActive(true);
            trail.Clear();
        }

        public void Tick(float dt)
        {
            transform.position += (Vector3)(Velocity * dt);
            transform.Rotate(0f, 0f, spin * dt);
        }
    }
}

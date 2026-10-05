using UnityEngine;

namespace Pulse
{
    /// <summary>Trauma-style screen shake plus a damped roll "kick" used to sell gravity flips.</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraRig : MonoBehaviour
    {
        [SerializeField] private float maxShake = 0.35f;
        [SerializeField] private float shakeDecay = 2.5f;
        [SerializeField] private float rollSpring = 90f;
        [SerializeField] private float rollDamping = 9f;

        private Vector3 basePosition;
        private float trauma;
        private float roll, rollVelocity;

        private void Awake() => basePosition = transform.position;

        public void Shake(float amount) => trauma = Mathf.Clamp01(trauma + amount);

        /// <summary>Kick the camera roll; it springs back to level.</summary>
        public void Roll(float degreesPerSecond) => rollVelocity += degreesPerSecond;

        private void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            trauma = Mathf.Max(0f, trauma - shakeDecay * dt);
            float s = trauma * trauma * maxShake;
            float t = Time.unscaledTime * 30f;
            Vector3 offset = new Vector3(Mathf.PerlinNoise(t, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f, 0f) * 2f * s;

            // Damped spring back to zero roll.
            rollVelocity += (-roll * rollSpring - rollVelocity * rollDamping) * dt;
            roll += rollVelocity * dt;

            transform.position = basePosition + offset;
            transform.rotation = Quaternion.Euler(0f, 0f, roll);
        }
    }
}

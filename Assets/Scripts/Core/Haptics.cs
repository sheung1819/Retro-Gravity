using UnityEngine;

namespace Pulse
{
    /// <summary>Minimal cross-platform haptics. Uses the OS default vibration on phones; no-op elsewhere.</summary>
    public static class Haptics
    {
        public static bool Enabled = true;

        public static void Heavy()
        {
            if (!Enabled) return;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}

using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Pulse
{
    /// <summary>
    /// Single-pointer input that works with either the new Input System or the
    /// legacy Input Manager (whichever the project's Active Input Handling uses),
    /// covering touch on device and mouse in the editor.
    /// </summary>
    public static class PulseInput
    {
        public static bool IsPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var p = Pointer.current;
                return p != null && p.press.isPressed;
#else
                if (Input.touchCount > 0) return true;
                return Input.GetMouseButton(0);
#endif
            }
        }

        public static bool PressedThisFrame
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var p = Pointer.current;
                return p != null && p.press.wasPressedThisFrame;
#else
                if (Input.touchCount > 0) return Input.GetTouch(0).phase == TouchPhase.Began;
                return Input.GetMouseButtonDown(0);
#endif
            }
        }

        public static Vector2 Position
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var p = Pointer.current;
                return p != null ? p.position.ReadValue() : Vector2.zero;
#else
                if (Input.touchCount > 0) return Input.GetTouch(0).position;
                return Input.mousePosition;
#endif
            }
        }
    }
}

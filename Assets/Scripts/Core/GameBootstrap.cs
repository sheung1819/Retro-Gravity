using UnityEngine;

namespace Pulse
{
    /// <summary>
    /// Entry point. The Main scene only contains this component (and a camera);
    /// everything else — catcher, spawner, HUD, audio, effects — is built here in
    /// code, so the game has no prefab/scene wiring to break.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private PulseConfig config = new PulseConfig();
        [SerializeField] private float cameraSize = 8f;
        [SerializeField] private int targetFrameRate = 60;

        private void Awake()
        {
            GameEvents.Clear();
            Application.targetFrameRate = targetFrameRate;
            Time.timeScale = 1f;

            Camera cam = SetupCamera();
            Playfield.Init(cam);
            GlowKit.Init();

            var rig = cam.GetComponent<CameraRig>();
            if (rig == null) rig = cam.gameObject.AddComponent<CameraRig>();

            // Catcher
            var catcherGo = new GameObject("Catcher");
            var catcher = catcherGo.AddComponent<CatcherController>();
            var catcherVisual = catcherGo.AddComponent<CatcherVisual>();

            // Background + effects
            var grid = new GameObject("BackgroundGrid").AddComponent<BackgroundGrid>();
            grid.Init(catcher);

            var effects = new GameObject("FlipEffects").AddComponent<FlipEffects>();
            effects.Init(catcher, rig, cam.transform);

            // Audio
            new GameObject("Audio").AddComponent<AudioManager>();

            // HUD lives under the camera so it stays screen-fixed during roll/shake.
            var hudGo = new GameObject("HUD");
            hudGo.transform.SetParent(cam.transform, false);
            var hud = hudGo.AddComponent<Hud>();
            hud.Init();

            // Gameplay
            var game = new GameObject("GameManager").AddComponent<GameManager>();
            var spawner = new GameObject("Objects").AddComponent<ObjectSpawner>();
            spawner.Init(config, catcher, game);
            game.Init(config, catcher, catcherVisual, spawner, grid, hud);
        }

        private Camera SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }

            cam.orthographic = true;
            cam.orthographicSize = cameraSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = GlowKit.Background;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cam.transform.SetPositionAndRotation(new Vector3(0f, 0f, -10f), Quaternion.identity);
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            return cam;
        }
    }
}

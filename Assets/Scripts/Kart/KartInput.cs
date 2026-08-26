using UnityEngine;

namespace RoguelikeRacing.Kart
{
    /// <summary>
    /// Reads local keyboard/gamepad input and feeds it into KartController.SetInput.
    /// Kept separate from KartController on purpose: an AI driver or a networked input
    /// source can later drive the same controller by calling SetInput directly, without
    /// this component being involved.
    ///
    /// Throttle/steer come from the "Vertical"/"Horizontal" axes, which
    /// ProjectSettings/InputManager.asset binds to WASD/arrow keys AND the first
    /// joystick's left stick (deliberately not the triggers — see
    /// docs/DESIGN_DECISIONS.md for why). Drift is polled directly via KeyCode so it
    /// doesn't depend on that file: KeyCode.JoystickButton0/5 mean "button 0/5 on any
    /// connected joystick", which is the South face button / right shoulder on the
    /// standard Xbox-layout mapping every controller (including Steam Deck's, via Steam
    /// Input's default gamepad template) presents itself as.
    /// </summary>
    [RequireComponent(typeof(KartController))]
    public class KartInput : MonoBehaviour
    {
        KartController _controller;

        void Awake()
        {
            _controller = GetComponent<KartController>();
        }

        void Update()
        {
            float throttle = Input.GetAxisRaw("Vertical");
            float steer = Input.GetAxisRaw("Horizontal");
            bool drift = IsDriftHeld();

            _controller.SetInput(throttle, steer, drift);
        }

        static bool IsDriftHeld()
        {
            return Input.GetKey(KeyCode.LeftShift)
                || Input.GetKey(KeyCode.Space)
                || Input.GetKey(KeyCode.JoystickButton0)
                || Input.GetKey(KeyCode.JoystickButton5);
        }
    }
}

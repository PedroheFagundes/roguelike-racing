using RoguelikeRacing.Race;
using UnityEngine;

namespace RoguelikeRacing.Kart
{
    /// <summary>
    /// Reads local keyboard/gamepad input and feeds it into KartController.SetInput
    /// (and KartInventory.UseHeldItem for the item button). Kept separate from
    /// KartController on purpose: an AI driver or a networked input source can later
    /// drive the same controller by calling SetInput directly, without this component
    /// being involved.
    ///
    /// Throttle/steer come from the "Vertical"/"Horizontal" axes, which
    /// ProjectSettings/InputManager.asset binds to WASD/arrow keys AND the first
    /// joystick's left stick (deliberately not the triggers — see
    /// docs/DESIGN_DECISIONS.md for why). Drift/item buttons are polled directly via
    /// KeyCode so they don't depend on that file: KeyCode.JoystickButtonN (with no
    /// joystick number) means "button N on any connected joystick", which is South/
    /// right-shoulder/West on the standard Xbox-layout mapping every controller
    /// (including Steam Deck's, via Steam Input's default gamepad template) presents
    /// itself as.
    /// </summary>
    [RequireComponent(typeof(KartController))]
    public class KartInput : MonoBehaviour
    {
        KartController _controller;
        KartInventory _inventory;

        void Awake()
        {
            _controller = GetComponent<KartController>();
            _inventory = GetComponent<KartInventory>();
        }

        void Update()
        {
            float throttle = Input.GetAxisRaw("Vertical");
            float steer = Input.GetAxisRaw("Horizontal");
            bool drift = IsDriftHeld();

            _controller.SetInput(throttle, steer, drift);

            if (_inventory != null && IsUseItemPressed())
            {
                _inventory.UseHeldItem();
            }
        }

        static bool IsDriftHeld()
        {
            return Input.GetKey(KeyCode.LeftShift)
                || Input.GetKey(KeyCode.Space)
                || Input.GetKey(KeyCode.JoystickButton0)
                || Input.GetKey(KeyCode.JoystickButton5);
        }

        static bool IsUseItemPressed()
        {
            return Input.GetKeyDown(KeyCode.E)
                || Input.GetKeyDown(KeyCode.LeftControl)
                || Input.GetKeyDown(KeyCode.JoystickButton2);
        }
    }
}

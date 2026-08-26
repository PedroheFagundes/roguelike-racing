using UnityEngine;

namespace RoguelikeRacing.Kart
{
    /// <summary>
    /// Reads local keyboard/gamepad input and feeds it into KartController.SetInput.
    /// Kept separate from KartController on purpose: an AI driver or a networked input
    /// source can later drive the same controller by calling SetInput directly, without
    /// this component being involved.
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
            bool drift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.Space);

            _controller.SetInput(throttle, steer, drift);
        }
    }
}

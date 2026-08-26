using UnityEngine;

namespace RoguelikeRacing.Kart
{
    /// <summary>
    /// Arcade kart movement driven by a Rigidbody. Velocity is authored each FixedUpdate
    /// from input (not accumulated via AddForce), which is the common "arcade kart" trick:
    /// it gives precise, predictable control and turns wall contacts into smooth sliding
    /// instead of chaotic bouncing. Gravity is preserved on the Y axis so the Rigidbody
    /// still falls/rests naturally on the track collider.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class KartController : MonoBehaviour
    {
        [Header("Engine")]
        public float maxForwardSpeed = 24f;
        public float maxReverseSpeed = 10f;
        public float acceleration = 18f;
        public float brakeDeceleration = 30f;
        public float engineBrakeDeceleration = 10f;

        [Header("Steering")]
        public float baseTurnRateDegPerSec = 140f;
        public float minSpeedFactorForFullTurn = 0.25f;
        public float lowSpeedTurnFactor = 0.4f;

        [Header("Drift")]
        public float driftTurnMultiplier = 1.6f;
        public float driftLateralSlip = 6f;
        public float minDriftSecondsForBoost = 0.6f;
        public float driftBoostSpeedBonus = 6f;
        public float maxDriftBoostSpeedBonus = 14f;
        public float driftBoostDuration = 1.2f;

        [Header("Ground")]
        public LayerMask groundMask = ~0;
        public float groundCheckDistance = 1.2f;

        Rigidbody _rb;

        float _throttleInput;
        float _steerInput;
        bool _driftHeld;

        float _forwardSpeed;
        float _driftHeldSeconds;
        float _boostSpeed;
        float _boostTimeRemaining;
        bool _grounded;

        public float CurrentSpeedKmh => _forwardSpeed * 3.6f;
        public bool IsDrifting => _driftHeld;
        public bool IsGrounded => _grounded;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        /// <summary>
        /// Feeds input into the controller. Called once per frame by whatever produces
        /// input for this kart (KartInput for the local player today; an AI driver or a
        /// networked input source later can call the same method).
        /// </summary>
        public void SetInput(float throttle, float steer, bool drift)
        {
            _throttleInput = Mathf.Clamp(throttle, -1f, 1f);
            _steerInput = Mathf.Clamp(steer, -1f, 1f);
            _driftHeld = drift && Mathf.Abs(_steerInput) > 0.1f;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            CheckGround();
            UpdateDrift(dt);
            UpdateSpeed(dt);
            UpdateSteering(dt);
            ApplyVelocity();
        }

        void CheckGround()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            _grounded = Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.5f, groundMask);
        }

        void UpdateDrift(float dt)
        {
            if (_driftHeld)
            {
                _driftHeldSeconds += dt;
            }
            else
            {
                if (_driftHeldSeconds > 0f)
                {
                    float bonus = KartPhysicsMath.ComputeDriftBoostSpeed(
                        _driftHeldSeconds, minDriftSecondsForBoost, driftBoostSpeedBonus, maxDriftBoostSpeedBonus);

                    if (bonus > 0f)
                    {
                        _boostSpeed = bonus;
                        _boostTimeRemaining = driftBoostDuration;
                    }
                }
                _driftHeldSeconds = 0f;
            }

            if (_boostTimeRemaining > 0f)
            {
                _boostTimeRemaining -= dt;
                if (_boostTimeRemaining <= 0f)
                {
                    _boostTimeRemaining = 0f;
                    _boostSpeed = 0f;
                }
            }
        }

        void UpdateSpeed(float dt)
        {
            float effectiveMax = maxForwardSpeed + (_boostTimeRemaining > 0f ? _boostSpeed : 0f);
            _forwardSpeed = KartPhysicsMath.IntegrateSpeed(
                _forwardSpeed, _throttleInput, effectiveMax, maxReverseSpeed,
                acceleration, brakeDeceleration, engineBrakeDeceleration, dt);
        }

        void UpdateSteering(float dt)
        {
            if (!_grounded) return;

            float turnRate = KartPhysicsMath.ComputeTurnRateDegPerSec(
                _forwardSpeed, maxForwardSpeed, baseTurnRateDegPerSec, minSpeedFactorForFullTurn, lowSpeedTurnFactor);

            if (_driftHeld) turnRate *= driftTurnMultiplier;

            float speedSign = _forwardSpeed >= 0f ? 1f : -1f;
            float yaw = _steerInput * turnRate * speedSign * dt;

            _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0f, yaw, 0f));
        }

        void ApplyVelocity()
        {
            Vector3 forwardVelocity = transform.forward * _forwardSpeed;

            Vector3 lateralVelocity = Vector3.zero;
            if (_driftHeld)
            {
                lateralVelocity = transform.right * (_steerInput * driftLateralSlip);
            }

            Vector3 targetVelocity = forwardVelocity + lateralVelocity;
            targetVelocity.y = _rb.velocity.y;

            _rb.velocity = targetVelocity;
        }
    }
}

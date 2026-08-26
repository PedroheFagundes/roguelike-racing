using System.Collections.Generic;
using UnityEngine;

namespace RoguelikeRacing.Kart
{
    /// <summary>
    /// Arcade kart movement driven by a Rigidbody. Velocity is authored each FixedUpdate
    /// from input (not accumulated via AddForce), which is the common "arcade kart" trick:
    /// it gives precise, predictable control. Gravity is preserved on the Y axis so the
    /// Rigidbody still falls/rests naturally on the track collider.
    ///
    /// Authoring velocity directly means the physics engine's own collision response
    /// gets overwritten every single FixedUpdate before it can do anything useful — hit
    /// a wall and the kart just re-aims itself straight back into it next step, which
    /// reads as being stuck/vibrating in place no matter how hard you steer away. The
    /// fix (see ApplyWallSlide) is the standard "collide and slide" technique: track
    /// wall contact normals and clip the into-wall component of the desired velocity
    /// each frame, same as Unity's own Character Controller docs recommend for wall
    /// sliding. See docs/DESIGN_DECISIONS.md for the research behind this.
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

        [Header("Resistances")]
        [Tooltip("0 = ApplySlow (oil slick/shockwave) applies at full strength, 1 = fully immune. Raised by the Blindagem level-up.")]
        public float slowResistance = 0f;

        [Header("Wall Collision")]
        [Tooltip("Contact normals steeper than this (dot with world up) count as ground/ramp, not a wall, and are ignored for wall sliding.")]
        public float wallNormalMaxVerticalComponent = 0.5f;

        Rigidbody _rb;
        readonly Dictionary<Collider, Vector3> _wallContactNormals = new Dictionary<Collider, Vector3>();

        float _throttleInput;
        float _steerInput;
        bool _driftHeld;

        float _forwardSpeed;
        float _driftHeldSeconds;
        float _boostSpeed;
        float _boostTimeRemaining;
        bool _grounded;

        // Item effects (step 5): separate from the drift mini-turbo boost above so a
        // Nitro item and a mini-turbo can stack instead of overwriting each other.
        float _itemBoostSpeed;
        float _itemBoostTimeRemaining;
        float _shieldTimeRemaining;
        float _slowMultiplier = 1f;
        float _slowTimeRemaining;

        public float CurrentSpeedKmh => _forwardSpeed * 3.6f;
        public bool IsDrifting => _driftHeld;
        public bool IsGrounded => _grounded;
        public bool IsShielded => _shieldTimeRemaining > 0f;

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

        /// <summary>Nitro-style item: temporary top-speed bonus, stacks with the drift boost.</summary>
        public void ApplyItemBoost(float extraSpeed, float duration)
        {
            _itemBoostSpeed = extraSpeed;
            _itemBoostTimeRemaining = duration;
        }

        /// <summary>Blocks ApplySlow for the given duration (refreshes, doesn't stack).</summary>
        public void ApplyShield(float duration)
        {
            _shieldTimeRemaining = Mathf.Max(_shieldTimeRemaining, duration);
        }

        /// <summary>
        /// Offensive item effect (oil slick, shockwave, ...). No-ops while shielded;
        /// otherwise softened towards "no effect" by slowResistance (0 = full effect,
        /// 1 = fully immune).
        /// </summary>
        public void ApplySlow(float multiplier, float duration)
        {
            if (IsShielded) return;

            float effectiveMultiplier = Mathf.Lerp(multiplier, 1f, Mathf.Clamp01(slowResistance));
            _slowMultiplier = Mathf.Clamp01(effectiveMultiplier);
            _slowTimeRemaining = duration;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            CheckGround();
            UpdateDrift(dt);
            UpdateItemEffects(dt);
            UpdateSpeed(dt);
            UpdateSteering(dt);
            ApplyVelocity();
        }

        void CheckGround()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            _grounded = Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.5f, groundMask);
        }

        // Track which colliders we're currently touching that count as a "wall" (a
        // contact whose normal is mostly horizontal, as opposed to the ground/a ramp),
        // and their normal, so ApplyVelocity can clip against them. Keyed by the other
        // collider so touching two walls at once (a corner) and leaving one doesn't
        // wipe out the other's contact.
        void OnCollisionEnter(Collision collision) => UpdateWallContact(collision);
        void OnCollisionStay(Collision collision) => UpdateWallContact(collision);

        void OnCollisionExit(Collision collision)
        {
            _wallContactNormals.Remove(collision.collider);
        }

        void UpdateWallContact(Collision collision)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;

            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector3 normal = collision.GetContact(i).normal;
                if (Mathf.Abs(normal.y) < wallNormalMaxVerticalComponent)
                {
                    sum += normal;
                    count++;
                }
            }

            if (count > 0)
            {
                _wallContactNormals[collision.collider] = (sum / count).normalized;
            }
            else
            {
                _wallContactNormals.Remove(collision.collider);
            }
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

        void UpdateItemEffects(float dt)
        {
            if (_itemBoostTimeRemaining > 0f)
            {
                _itemBoostTimeRemaining -= dt;
                if (_itemBoostTimeRemaining <= 0f)
                {
                    _itemBoostTimeRemaining = 0f;
                    _itemBoostSpeed = 0f;
                }
            }

            if (_shieldTimeRemaining > 0f)
            {
                _shieldTimeRemaining -= dt;
                if (_shieldTimeRemaining < 0f) _shieldTimeRemaining = 0f;
            }

            if (_slowTimeRemaining > 0f)
            {
                _slowTimeRemaining -= dt;
                if (_slowTimeRemaining <= 0f)
                {
                    _slowTimeRemaining = 0f;
                    _slowMultiplier = 1f;
                }
            }
        }

        void UpdateSpeed(float dt)
        {
            float driftBoost = _boostTimeRemaining > 0f ? _boostSpeed : 0f;
            float itemBoost = _itemBoostTimeRemaining > 0f ? _itemBoostSpeed : 0f;
            float effectiveMax = (maxForwardSpeed + driftBoost + itemBoost) * _slowMultiplier;

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
            targetVelocity = ApplyWallSlide(targetVelocity);
            targetVelocity.y = _rb.linearVelocity.y;

            _rb.linearVelocity = targetVelocity;
        }

        /// <summary>
        /// "Collide and slide": if the desired velocity points into a wall we're
        /// currently touching, remove exactly that inward component and keep the rest
        /// (the part along the wall's surface). If it's already pointing away from the
        /// wall (e.g. the player just steered off it), this is a no-op — nothing holds
        /// the kart back once it's not driving into the wall anymore.
        /// </summary>
        Vector3 ApplyWallSlide(Vector3 desiredVelocity)
        {
            if (_wallContactNormals.Count == 0) return desiredVelocity;

            Vector3 combinedNormal = Vector3.zero;
            foreach (Vector3 normal in _wallContactNormals.Values)
            {
                combinedNormal += normal;
            }

            if (combinedNormal.sqrMagnitude < 0.0001f) return desiredVelocity;
            combinedNormal.Normalize();

            float intoWall = Vector3.Dot(desiredVelocity, -combinedNormal);
            if (intoWall > 0f)
            {
                desiredVelocity += combinedNormal * intoWall;
            }

            return desiredVelocity;
        }
    }
}

using RoguelikeRacing.Kart;
using UnityEngine;

namespace RoguelikeRacing.Race
{
    /// <summary>
    /// Flies towards the kart it was fired at, curving gradually rather than snapping
    /// (a hard turn-rate cap, so it can be dodged), applies KartController.ApplySlow on
    /// contact and destroys itself. Expires harmlessly on its own after a lifetime if it
    /// never catches up. No Rigidbody -- it's a simple kinematic mover with a trigger
    /// collider, driven from Update rather than physics.
    /// </summary>
    public class HomingMissile : MonoBehaviour
    {
        KartController _target;
        KartController _shooter;
        float _speed;
        float _turnRateDegPerSec;
        float _slowMultiplier;
        float _slowDuration;

        public void Initialize(KartController target, KartController shooter, float speed, float turnRateDegPerSec, float lifetimeSeconds, float slowMultiplier, float slowDuration)
        {
            _target = target;
            _shooter = shooter;
            _speed = speed;
            _turnRateDegPerSec = turnRateDegPerSec;
            _slowMultiplier = slowMultiplier;
            _slowDuration = slowDuration;

            Destroy(gameObject, lifetimeSeconds);
        }

        void Update()
        {
            if (_target == null)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 toTarget = _target.transform.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude > 0.01f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, _turnRateDegPerSec * Time.deltaTime);
            }

            transform.position += transform.forward * _speed * Time.deltaTime;
        }

        void OnTriggerEnter(Collider other)
        {
            var kart = other.GetComponentInParent<KartController>();
            if (kart == null || kart == _shooter) return;

            kart.ApplySlow(_slowMultiplier, _slowDuration);
            Destroy(gameObject);
        }
    }
}

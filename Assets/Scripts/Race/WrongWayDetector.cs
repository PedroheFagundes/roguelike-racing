using System.Collections.Generic;
using UnityEngine;

namespace RoguelikeRacing.Race
{
    /// <summary>
    /// Flags whether the kart is currently moving against the track's intended
    /// direction, by comparing its velocity to the direction of the nearest centerline
    /// segment. Player-only: AI already only ever drives the correct way (KartAIDriver
    /// follows waypoints in order), so there's nothing useful to flag for it.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class WrongWayDetector : MonoBehaviour
    {
        [Tooltip("Dot product between velocity and track direction below this counts as wrong-way.")]
        public float wrongWayDotThreshold = -0.2f;

        public bool IsWrongWay { get; private set; }

        List<Vector3> _centerline;
        Rigidbody _rb;

        public void Initialize(List<Vector3> centerline)
        {
            _centerline = centerline;
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        void Update()
        {
            if (_centerline == null || _centerline.Count < 2) return;

            Vector3 velocity = _rb.velocity;
            velocity.y = 0f;

            // Direction is noise at near-zero speed, so don't flag while basically stopped.
            if (velocity.sqrMagnitude < 1f)
            {
                IsWrongWay = false;
                return;
            }

            int nearestIndex = FindNearestPointIndex();
            Vector3 a = _centerline[nearestIndex];
            Vector3 b = _centerline[(nearestIndex + 1) % _centerline.Count];
            Vector3 trackDir = (b - a).normalized;

            IsWrongWay = Vector3.Dot(velocity.normalized, trackDir) < wrongWayDotThreshold;
        }

        int FindNearestPointIndex()
        {
            int nearest = 0;
            float bestDistSq = float.MaxValue;
            Vector3 position = transform.position;

            for (int i = 0; i < _centerline.Count; i++)
            {
                float distSq = (position - _centerline[i]).sqrMagnitude;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    nearest = i;
                }
            }

            return nearest;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace RoguelikeRacing.Kart
{
    /// <summary>
    /// Simple waypoint-following AI: steers towards the next point on the track's
    /// centerline, advancing to the next one once close enough, looping forever. Feeds
    /// KartController.SetInput exactly like KartInput does for the local player, so from
    /// the controller's point of view an AI kart and a human kart are indistinguishable.
    ///
    /// Deliberately dumb for now (no rubber-banding, no avoidance, no lap awareness) —
    /// step 2 is only about having something to race against on the track built in step 1.
    /// </summary>
    [RequireComponent(typeof(KartController))]
    public class KartAIDriver : MonoBehaviour
    {
        [Header("Waypoint following")]
        public float waypointReachedDistance = 5f;

        [Header("Steering")]
        [Tooltip("Angle (degrees) to the target waypoint that maps to full steering lock.")]
        public float steerAngleForFullLock = 45f;

        [Header("Throttle")]
        [Tooltip("Angle (degrees) to the target waypoint at or beyond which throttle is cut to minThrottleOnSharpTurns.")]
        public float angleForMinThrottle = 90f;
        public float minThrottleOnSharpTurns = 0.3f;

        [Header("Drift")]
        [Tooltip("Angle (degrees) to the target waypoint beyond which the AI holds drift.")]
        public float driftAngleThreshold = 30f;

        KartController _controller;
        IReadOnlyList<Vector3> _waypoints;
        int _currentWaypointIndex;

        void Awake()
        {
            _controller = GetComponent<KartController>();
        }

        /// <summary>
        /// Must be called once after spawning, before the AI can drive. startWaypointIndex
        /// lets a kart start further along the track (e.g. staggered grid spawns).
        /// </summary>
        public void Initialize(IReadOnlyList<Vector3> waypoints, int startWaypointIndex = 0)
        {
            _waypoints = waypoints;
            _currentWaypointIndex = waypoints.Count > 0 ? ((startWaypointIndex % waypoints.Count) + waypoints.Count) % waypoints.Count : 0;
        }

        void Update()
        {
            if (_waypoints == null || _waypoints.Count == 0) return;

            Vector3 toTarget = GetVectorToCurrentWaypoint();
            if (toTarget.magnitude < waypointReachedDistance)
            {
                _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Count;
                toTarget = GetVectorToCurrentWaypoint();
            }

            float angleToTarget = Vector3.SignedAngle(transform.forward, toTarget.normalized, Vector3.up);

            float steer = Mathf.Clamp(angleToTarget / steerAngleForFullLock, -1f, 1f);

            float turnSeverity = Mathf.Clamp01(Mathf.Abs(angleToTarget) / angleForMinThrottle);
            float throttle = Mathf.Lerp(1f, minThrottleOnSharpTurns, turnSeverity);

            bool drift = Mathf.Abs(angleToTarget) > driftAngleThreshold;

            _controller.SetInput(throttle, steer, drift);
        }

        Vector3 GetVectorToCurrentWaypoint()
        {
            Vector3 toTarget = _waypoints[_currentWaypointIndex] - transform.position;
            toTarget.y = 0f;
            return toTarget;
        }
    }
}

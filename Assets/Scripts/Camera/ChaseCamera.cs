using UnityEngine;

namespace RoguelikeRacing.CameraRig
{
    /// <summary>
    /// Standard 3rd-person kart-racer chase camera: follows behind and above the target,
    /// looking slightly ahead of it. Smoothing is split into position (SmoothDamp, feels
    /// weighty) and rotation (Slerp, avoids camera snapping on sharp turns).
    /// </summary>
    public class ChaseCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 3.5f, -6.5f);
        public float positionSmoothTime = 0.15f;
        public float lookAheadDistance = 4f;
        public float rotationSmoothSpeed = 8f;

        Vector3 _velocity;

        void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.TransformPoint(offset);
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, positionSmoothTime);

            Vector3 lookTarget = target.position + target.forward * lookAheadDistance + Vector3.up * 1f;
            Vector3 lookDirection = lookTarget - transform.position;

            if (lookDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSmoothSpeed * Time.deltaTime);
            }
        }
    }
}

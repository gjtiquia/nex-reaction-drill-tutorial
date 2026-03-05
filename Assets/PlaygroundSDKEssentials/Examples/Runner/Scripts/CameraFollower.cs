#nullable enable

using UnityEngine;

namespace Nex.Essentials.Examples.Runner
{
    public class CameraFollower : MonoBehaviour
    {
        public Transform? target;
        public Vector3 positionOffset = new(0, 6, -10);
        public Vector2 lookAtOffset = new(0, 10);
        public float smoothTime = 0.125f;
        private Vector3 smoothVelocity;

        private void LateUpdate()
        {
            if (target is null) return;
            var desiredPosition = target.position + positionOffset;
            var newPosition =
                Vector3.SmoothDamp(transform.position, desiredPosition, ref smoothVelocity, smoothTime);

            var lookDirection = new Vector2(0 - newPosition.x, 0 - newPosition.z) + lookAtOffset;
            var angle = Vector2.SignedAngle(lookDirection, Vector2.up);
            var rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, angle, transform.rotation.eulerAngles.z);

            transform.SetPositionAndRotation(newPosition, rotation);
        }
    }
}

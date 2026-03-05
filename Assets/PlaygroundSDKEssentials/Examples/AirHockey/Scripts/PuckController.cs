#nullable enable
using UnityEngine;

namespace Nex.Essentials.Examples.AirHockey
{
    public class PuckController : MonoBehaviour
    {
        [SerializeField] private float maxSpeed;
        private new Rigidbody rigidbody = null!;

        private void Start()
        {
            rigidbody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
#if UNITY_6000_0_OR_NEWER
            rigidbody.linearVelocity = Vector3.ClampMagnitude(rigidbody.linearVelocity, maxSpeed);
#else
            rigidbody.velocity = Vector3.ClampMagnitude(rigidbody.velocity, maxSpeed);
#endif
        }
    }
}

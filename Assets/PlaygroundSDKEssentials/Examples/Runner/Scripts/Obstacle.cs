using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nex.Essentials.Examples.Runner
{
    public class Obstacle : MonoBehaviour
    {
        [SerializeField] private float speed;
        [SerializeField] private float outOfBound;
        [SerializeField] private Rigidbody rb;

        private void Start()
        {
            MaybeSelfDestroy(this.GetCancellationTokenOnDestroy()).Forget();

#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = new Vector3(0, 0, -speed);
#else
            rb.velocity = new Vector3(0, 0, -speed);
#endif
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                RunnerDriver.Instance.OnCollidedWithObstacles();
            }
        }

        private async UniTaskVoid MaybeSelfDestroy(CancellationToken cancellationToken)
        {
            while (isActiveAndEnabled)
            {
                if (transform.position.z < outOfBound)
                {
                    Destroy(gameObject);
                    return;
                }

                await UniTask.Delay(1000, cancellationToken: cancellationToken);
            }
        }

        public void SetSpeed(float aSpeed)
        {
            speed = aSpeed;
        }
    }
}

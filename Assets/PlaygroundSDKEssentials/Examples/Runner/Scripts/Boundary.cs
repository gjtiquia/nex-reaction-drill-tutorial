using UnityEngine;

namespace Nex.Essentials.Examples.Runner
{
    public class Boundary : MonoBehaviour
    {
        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                RunnerDriver.Instance.OnCollidedWithObstacles();
            }
        }
    }
}

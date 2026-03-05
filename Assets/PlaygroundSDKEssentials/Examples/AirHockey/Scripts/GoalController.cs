using UnityEngine;

namespace Nex.Essentials.Examples.AirHockey
{
    public class GoalController : MonoBehaviour
    {
        [SerializeField] private int goalIndex;
        [SerializeField] private AirHockeyGameDriver gameDriver;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.gameObject.CompareTag("AirHockeyPuck")) return;

            _ = gameDriver.OnGoalScore(goalIndex);
        }
    }
}

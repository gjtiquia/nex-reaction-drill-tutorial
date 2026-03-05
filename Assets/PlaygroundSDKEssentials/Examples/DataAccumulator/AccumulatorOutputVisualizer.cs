using System.Text;
using TMPro;
using UnityEngine;

namespace Nex.Essentials.Examples.DataAccumulator
{
    public class AccumulatorOutputVisualizer : MonoBehaviour
    {
        [SerializeField] private DistanceAccumulator distanceAccumulator;
        [SerializeField] private DirectionChangeAccumulator directionChangeAccumulator;
        [SerializeField] private TMP_Text label;
        [SerializeField] private string labelPrefix;

        private void Update()
        {
            var sb = new StringBuilder();
            sb.AppendLine(labelPrefix);
            sb.Append("DistanceX: ").AppendLine(distanceAccumulator.DistanceX.ToString("0.00"));
            sb.Append("DistanceY: ").AppendLine(distanceAccumulator.DistanceY.ToString("0.00"));
            sb.Append("DistanceTotal: ").AppendLine(distanceAccumulator.DistanceTotal.ToString("0.00"));
            sb.Append("RepsX: ").AppendLine(directionChangeAccumulator.RepsX.ToString());
            sb.Append("RepsY: ").AppendLine(directionChangeAccumulator.RepsY.ToString());
            sb.Append("Cumulated RepsX: ").AppendLine(directionChangeAccumulator.CumulatedRepsX.ToString());
            sb.Append("Cumulated RepsY: ").AppendLine(directionChangeAccumulator.CumulatedRepsY.ToString());
            
            label.text = sb.ToString();
        }
    }
}

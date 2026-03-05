using UnityEngine;
using TMPro;

namespace Nex.Essentials.Examples.SignalProcessor
{
    public class DemoWheel : MonoBehaviour
    {
        [SerializeField] private SignalProducer wheelSignalProducer;
        [SerializeField] private RectTransform wheel;
        [SerializeField] private TMP_Text label;

        private void Update()
        {
            wheel.eulerAngles = new Vector3(0, 0, wheelSignalProducer.Signal);
            label.text = wheelSignalProducer.Signal.ToString("0.00");
        }
    }
}

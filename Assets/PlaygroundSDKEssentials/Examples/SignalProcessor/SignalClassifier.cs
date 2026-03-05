#nullable enable

using Cysharp.Threading.Tasks.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Nex.Essentials.Examples.SignalProcessor
{
    public class SignalClassifier : MonoBehaviour
    {
        [SerializeField] private GameObject positiveIndicator = null!;
        [SerializeField] private GameObject negativeIndicator = null!;
        [FormerlySerializedAs("signalDetector")] [SerializeField] private SignalPolarityDetector signalPolarityDetector = null!;

        private void Start()
        {
            signalPolarityDetector.SignalStream.Subscribe(HandleDetectedSignal, destroyCancellationToken);
        }

        private void HandleDetectedSignal(SignalPolarityDetector.SignalPolarity signal)
        {
            positiveIndicator.SetActive(signal == SignalPolarityDetector.SignalPolarity.Positive);
            negativeIndicator.SetActive(signal == SignalPolarityDetector.SignalPolarity.Negative);
        }
    }
}

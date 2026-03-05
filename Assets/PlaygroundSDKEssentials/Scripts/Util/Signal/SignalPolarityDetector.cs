#nullable enable

using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using UnityEngine;

namespace Nex.Essentials
{
    // Based on algorithm from:
    // Brakel, J.P.G. van (2014). "Robust peak detection algorithm using z-scores". Stack Overflow.
    // Available at:
    // https://stackoverflow.com/questions/22583391/peak-signal-detection-in-realtime-timeseries-data/22640362#22640362
    // (version: 2020-11-08)
    public class SignalPolarityDetector : MonoBehaviour
    {
        [SerializeField] private SignalProducer signalProducer = null!;

        [Header("Configuration")]
        [SerializeField] [Tooltip("Duration (in seconds) of data to retain.")]
        private float storageDuration = 10f;

        [SerializeField] [Tooltip("Duration (in seconds) of the moving window for standard deviation calculation.")]
        private float lag = 2f; // moving window size

        // minimum moving window size for signal calculation
        [SerializeField] [Tooltip("Minimum duration (in seconds) of data required before signal detection begin.")]
        private float minDurationForSignalCalculation = 2f;

        [SerializeField]
        [HideInInspector]
        [Tooltip("Boolean flag to indicate if the signal detection should be asymmetric")]
        private bool isAsymmetric;

        // alpha of the single exponential moving average
        [SerializeField] [HideInInspector] [Tooltip("Controls the responsiveness of the signal filter to new values.")]
        private float influenceTau = 2f;

        // alpha of the single exponential moving average for positive signals
        [SerializeField]
        [HideInInspector]
        [Tooltip("Controls the responsiveness of the positive signal filter to new values.")]
        private float influenceTauPositive = 2f;

        // alpha of the single exponential moving average for negative signals
        [SerializeField]
        [HideInInspector]
        [Tooltip("Controls the responsiveness of the negative signal filter to new values.")]
        private float influenceTauNegative = 2f;

        // threshold for z-score (symmetric mode)
        [SerializeField]
        [HideInInspector]
        [Tooltip("Multiplier of the standard deviation as the signal detection threshold.")]
        private float threshold = 1f;

        // threshold for positive z-score (asymmetric mode)
        [SerializeField]
        [HideInInspector]
        [Tooltip("Multiplier of the standard deviation as the positive signal detection threshold.")]
        private float thresholdPositive = 1f;

        // threshold for negative z-score (asymmetric mode)
        [SerializeField]
        [HideInInspector]
        [Tooltip("Multiplier of the standard deviation as the negative signal detection threshold.")]
        private float thresholdNegative = 1f;

        // absolute margin for signal detection (symmetric mode)
        [SerializeField] [HideInInspector] [Tooltip("Minimum absolute value for signal detection.")]
        public float signalMargin = 0.1f;

        // absolute margin for positive signal detection (asymmetric mode)
        [SerializeField] [HideInInspector] [Tooltip("Minimum absolute value for positive signal detection.")]
        public float signalMarginPositive = 0.1f;

        // absolute margin for negative signal detection (asymmetric mode)
        [SerializeField] [HideInInspector] [Tooltip("Minimum absolute value for negative signal detection.")]
        public float signalMarginNegative = 0.1f;

        private History<float> inputData = null!; // raw data
        private History<SignalPolarity> signals = null!; // 1, -1, 0
        private History<float> filteredData = null!; // single exponential moving average
        private History<float> avgFilter = null!; // time windowed mean
        private History<float> stdFilter = null!; // time windowed standard deviation

        private float lastFrameTime;
        private float initialDetectionFrameTime = -1f;

        private readonly AsyncReactiveProperty<SignalPolarity> signalStream = new(0);
        public IUniTaskAsyncEnumerable<SignalPolarity> SignalStream => signalStream.AsUniTaskAsyncEnumerable();

        public SignalPolarity Signal => signalStream.Value;

        public enum SignalPolarity
        {
            Negative = -1,
            Neutral = 0,
            Positive = 1
        }

        private void Awake()
        {
            inputData = new History<float>(storageDuration);
            signals = new History<SignalPolarity>(storageDuration);
            filteredData = new History<float>(storageDuration);
            avgFilter = new History<float>(storageDuration);
            stdFilter = new History<float>(storageDuration);
        }

        private void Start()
        {
            signalProducer.SignalStream.Subscribe(HandleRawSignal, destroyCancellationToken);
        }

        private void OnDestroy()
        {
            signalStream.Dispose();
        }

        private void HandleRawSignal(float signal)
        {
            ProcessSignal(signal, Time.time);
        }

        /// <summary>
        /// Process a signal with a time. Will update signalStream when done.
        /// When there is no signal, we should still call this method with null.
        /// </summary>
        /// <param name="signal">The signal value when there is one.</param>
        /// <param name="time">The time when the signal is detected</param>
        private void ProcessSignal(float? signal, float time)
        {
            if (initialDetectionFrameTime < 0)
            {
                initialDetectionFrameTime = time;
            }

            if (!signal.HasValue) return;
            AddDataPoint(signal.Value, time);
            signalStream.Value = signals.LatestValue;
        }

        private void AddDataPoint(float newValue, float frameTime)
        {
            var deltaTime = frameTime - lastFrameTime;
            lastFrameTime = frameTime;
            inputData.Add(newValue, frameTime);

            var hasEnoughData = inputData.TimeSpan > Mathf.Min(minDurationForSignalCalculation, lag);

            if (!hasEnoughData)
            {
                filteredData.Add(newValue, frameTime);
                signals.Add(0, frameTime);

                var (newAvg, newStd) = filteredData.Select(i => i.value).MeanAndDeviation();
                avgFilter.Add(newAvg, frameTime);
                stdFilter.Add(newStd, frameTime);
            }
            else
            {
                var lastAvg = avgFilter.LatestValue;
                var lastStd = stdFilter.LatestValue;

                var signal = SignalPolarity.Neutral; // No signal by default

                if (isAsymmetric)
                {
                    if (newValue > lastAvg && newValue - lastAvg > thresholdPositive * lastStd + signalMarginPositive)
                    {
                        signal = SignalPolarity.Positive;
                    }
                    else if (newValue < lastAvg &&
                             (lastAvg - newValue) > thresholdNegative * lastStd + signalMarginNegative)
                    {
                        signal = SignalPolarity.Negative;
                    }
                }
                else
                {
                    if (Math.Abs(newValue - lastAvg) > threshold * lastStd + signalMargin)
                    {
                        signal = newValue > lastAvg ? SignalPolarity.Positive : SignalPolarity.Negative;
                    }
                }

                signals.Add(signal, frameTime);

                if (signal != SignalPolarity.Neutral)
                {
                    var lastFilteredData = filteredData.LatestValue;
                    var semaInfluence = signal == SignalPolarity.Positive
                        ? ComputeSemaInfluence(isAsymmetric ? influenceTauPositive : influenceTau, deltaTime)
                        : ComputeSemaInfluence(isAsymmetric ? influenceTauNegative : influenceTau, deltaTime);
                    filteredData.Add(semaInfluence * newValue + (1 - semaInfluence) * lastFilteredData, frameTime);
                }
                else
                {
                    filteredData.Add(newValue, frameTime);
                }

                // Update the filters
                // Get the filtered data from the last lag points
                var windowedFilteredData = filteredData
                    .TakeWhile(i => i.timestamp > frameTime - lag)
                    .Select(i => i.value);
                var (newAvg, newStd) = windowedFilteredData.MeanAndDeviation();

                avgFilter.Add(newAvg, frameTime);
                stdFilter.Add(newStd, frameTime);
            }

            UpdateCurrentFrameTimes(frameTime);
        }

        private void UpdateCurrentFrameTimes(float frameTime)
        {
            inputData.CleanUp(frameTime);
            filteredData.CleanUp(frameTime);
            signals.CleanUp(frameTime);
            avgFilter.CleanUp(frameTime);
            stdFilter.CleanUp(frameTime);
        }

        private static float ComputeSemaInfluence(float tau, float deltaTime)
        {
            return 1 - Mathf.Exp(-deltaTime / tau);
        }

        public void ClearData()
        {
            inputData.Clear();
            filteredData.Clear();
            signals.Clear();
            avgFilter.Clear();
            stdFilter.Clear();
        }
    }
}

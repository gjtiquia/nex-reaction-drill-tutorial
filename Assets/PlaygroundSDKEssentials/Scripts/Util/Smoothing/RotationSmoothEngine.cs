using UnityEngine;

namespace Nex.Essentials
{
    /**
     * This is a wrapper around the OneEuroFilterQuaternion struct
     * So that this creates an object and its reference can be stored in the list
     */
    public sealed class RotationSmoothEngine
    {
        private OneEuroFilterConfig config;
        private TickDebouncedBoolean.DebounceConfig debounceConfig;
        private TickDebouncedBoolean isDetected;

        public OneEuroFilterConfig Config
        {
            get => config;
            set
            {
                value.Validate();
                config = value;
                smoothing.Config = config;
            }
        }

        public TickDebouncedBoolean.DebounceConfig DebounceConfig
        {
            get => debounceConfig;
            set
            {
                debounceConfig = value;
                isDetected.Config = debounceConfig;
            }
        }

        private OneEuroFilterQuaternion smoothing;

        public bool IsDetected => isDetected.Value;
        public Quaternion? LatestRawValue { get; private set; }
        public Quaternion? LatestSmoothedValue => smoothing.FilteredValue;

        public RotationSmoothEngine(TickDebouncedBoolean.DebounceConfig aDebounceConfig, OneEuroFilterConfig config)
        {
            Config = config;
            debounceConfig = aDebounceConfig;
            isDetected = new TickDebouncedBoolean(debounceConfig);
            smoothing = new OneEuroFilterQuaternion(config);
        }

        public Quaternion? Update(Quaternion? raw, float deltaTime)
        {
            LatestRawValue = raw;

            // Update whether the rotation is detected
            isDetected.Update(raw.HasValue);

            return smoothing.Update(raw, deltaTime);
        }
    }
}

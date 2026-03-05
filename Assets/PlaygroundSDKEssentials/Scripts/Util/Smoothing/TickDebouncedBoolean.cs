#nullable enable

using System;

namespace Nex.Essentials
{
    // This provides some debouncing of a Boolean variable, guarding against noise.
    public struct TickDebouncedBoolean
    {
        [Serializable]
        public struct DebounceConfig
        {
            public int activateTicks;  // The number of positive ticks to go from off to on.
            public int deactivateTicks;  // The number of negative ticks to go from on to off.

            public DebounceConfig(int activateTicks, int deactivateTicks)
            {
                this.activateTicks = activateTicks;
                this.deactivateTicks = deactivateTicks;
            }
        }

        public TickDebouncedBoolean(DebounceConfig config)
        {
            Config = config;
            Value = false;
            Progress = 0;
        }

        public DebounceConfig Config { get; set; }

        public bool Value { get; private set; }

        // How long it will take before we flip Value.
        public int Progress { get; private set; }

        public bool Update(bool signal)
        {
            if (signal == Value) return Value;

            var threshold = Value ? Config.deactivateTicks : Config.activateTicks;
            if (++Progress != threshold) return Value;

            Value = !Value;
            Progress = 0;
            return Value;
        }

        public void Reset()
        {
            Value = false;
            Progress = 0;
        }
    }
}

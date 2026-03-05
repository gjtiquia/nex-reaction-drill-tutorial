#nullable enable

using System;
using UnityEngine;

namespace Nex.Essentials
{
    public class TimeDebouncedBoolean
    {
        [Serializable]
        public struct DebounceConfig
        {
            public float activateDuration;  // The number of positive ticks to go from off to on.
            public float deactivateDuration;  // The number of negative ticks to go from on to off.

            public DebounceConfig(float activateDuration, float deactivateDuration)
            {
                this.activateDuration = activateDuration;
                this.deactivateDuration = deactivateDuration;
            }

            // How long would it take to flip the signal.
            public float GetExpiryDuration(bool signal) => signal ? deactivateDuration : activateDuration;
        }

        public TimeDebouncedBoolean(DebounceConfig config)
        {
            Config = config;
            Value = false;
            Expiry = Time.unscaledTime + config.GetExpiryDuration(false);
        }

        public DebounceConfig Config { get; set; }

        public bool Value { get; private set; }

        public float Expiry { get; private set; }

        public bool Update(bool signal)
        {
            var currTime = Time.unscaledTime;
            if (signal == Value)
            {
                Expiry = currTime + Config.GetExpiryDuration(signal);
                return signal;
            }

            // The signal is different from the current value.
            if (currTime < Expiry) return Value;  // This is still too soon for us to believe the value has flipped.

            // We can flip now.
            Expiry = currTime + Config.GetExpiryDuration(signal);
            return Value = signal;
        }
    }
}

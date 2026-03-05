#nullable enable

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Nex.Essentials
{
    [Serializable]
    public class OneEuroFilterConfig : ISerializationCallbackReceiver
    {
        [SerializeField] [Min(0)] private float minCutoff;
        [SerializeField] [Min(0)] private float beta;
        [SerializeField] [Min(0)] private float derivativeCutoff;

        public float MinCutoff
        {
            get => minCutoff;
            set
            {
                minCutoff = value;
                Validate();
            }
        }

        public float Beta
        {
            get => beta;
            set
            {
                beta = value;
                Validate();
            }
        }

        public float DerivativeCutoff
        {
            get => derivativeCutoff;
            set
            {
                derivativeCutoff = value;
                Validate();
            }
        }

        public OneEuroFilterConfig(float minCutoff, float beta, float derivativeCutoff)
        {
            this.minCutoff = minCutoff;
            this.beta = beta;
            this.derivativeCutoff = derivativeCutoff;
            Validate();
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            Validate();
        }

        public void Validate()
        {
            if (minCutoff < 0)
            {
                minCutoff = 0;
            }

            if (beta < 0)
            {
                beta = 0;
            }

            if (derivativeCutoff < 0)
            {
                derivativeCutoff = 0;
            }
        }
    }

    public interface IOneEuroFilter<T> where T : struct
    {
        public OneEuroFilterConfig Config { get; set; }
        public float MinCutoff { get; set; }
        public float Beta { get; set; }
        public float DerivativeCutoff { get; set; }

        public T? RawValue { get; }
        public T? FilteredValue { get; }
        public T? FilteredDerivativeValue { get; }

        public T? Update(T? newRawValue, float deltaTime, float speedNormalizeFactor = 1);
        public T Update(T newRawValue, float deltaTime, float speedNormalizeFactor = 1);

        public T? Set(T? newRawValue);
        public T Set(T newRawValue);
    }

    [Serializable]
    public class OneEuroFilterFloat : IOneEuroFilter<float>
    {
        #region Config

        [SerializeField] private OneEuroFilterConfig config;

        public OneEuroFilterConfig Config
        {
            get => config;
            set
            {
                value.Validate();
                config = value;
            }
        }

        public float MinCutoff
        {
            get => config.MinCutoff;
            set => config.MinCutoff = value;
        }

        public float Beta
        {
            get => config.Beta;
            set => config.Beta = value;
        }

        public float DerivativeCutoff
        {
            get => config.DerivativeCutoff;
            set => config.DerivativeCutoff = value;
        }

        #endregion

        #region Fields / Properties

        [SerializeField] private bool hasValue;

        [SerializeField]
        private float rawValue;

        [SerializeField]
        private float filteredValue;

        [SerializeField]
        private float filteredDerivativeValue;

        public float? RawValue => hasValue ? rawValue : null;
        public float? FilteredValue => hasValue ? filteredValue : null;
        public float? FilteredDerivativeValue => hasValue ? filteredDerivativeValue : null;

        #endregion

        #region Constructor

        public OneEuroFilterFloat(OneEuroFilterConfig config)
        {
            config.Validate();
            this.config = config;
            hasValue = false;
            rawValue = default;
            filteredValue = default;
            filteredDerivativeValue = default;
        }

        #endregion

        #region Update

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float? Update(float? newRawValue, float deltaTime, float speedNormalizeFactor = 1)
        {
            if ((newRawValue ?? RawValue) is not { } rawValueNotNull)
            {
                return null;
            }

            return Update(rawValueNotNull, deltaTime, speedNormalizeFactor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Update(float newRawValue, float deltaTime, float speedNormalizeFactor = 1)
        {
            if (deltaTime <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Delta time must be positive");
            }

            rawValue = newRawValue;

            var derivativeValue = hasValue ? ComputeDerivative(rawValue, filteredValue, deltaTime, speedNormalizeFactor) : default;
            var derivativeAlpha = OneEuroFilterUtils.ComputeAlpha(deltaTime, config.DerivativeCutoff);
            filteredDerivativeValue = hasValue ? Lerp(filteredDerivativeValue, derivativeValue, derivativeAlpha) : default;

            var cutoff = config.MinCutoff + config.Beta * ComputeMagnitude(filteredDerivativeValue, deltaTime, speedNormalizeFactor);

            var alpha = OneEuroFilterUtils.ComputeAlpha(deltaTime, cutoff);
            filteredValue = hasValue ? Lerp(filteredValue, rawValue, alpha) : rawValue;

            hasValue = true;

            return filteredValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Lerp(float a, float b, float t)
        {
            return Mathf.Lerp(a, b, t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ComputeDerivative(float rawValue, float filteredValue, float deltaTime, float speedNormalizeFactor)
        {
            return (rawValue - filteredValue) / (deltaTime * speedNormalizeFactor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ComputeMagnitude(float a, float deltaTime, float speedNormalizeFactor)
        {
            return Mathf.Abs(a);
        }

        #endregion

        #region Set

        public float? Set(float? newRawValue)
        {
            hasValue = newRawValue is not null;
            rawValue = newRawValue ?? default;
            filteredValue = newRawValue ?? default;
            filteredDerivativeValue = default;
            return newRawValue;
        }

        public float Set(float newRawValue)
        {
            hasValue = true;
            rawValue = newRawValue;
            filteredValue = newRawValue;
            filteredDerivativeValue = default;
            return newRawValue;
        }

        #endregion
    }

    [Serializable]
    public class OneEuroFilterVector2 : IOneEuroFilter<Vector2>
    {
        #region Config

        [SerializeField] private OneEuroFilterConfig config;

        public OneEuroFilterConfig Config
        {
            get => config;
            set
            {
                value.Validate();
                config = value;
            }
        }

        public float MinCutoff
        {
            get => config.MinCutoff;
            set => config.MinCutoff = value;
        }

        public float Beta
        {
            get => config.Beta;
            set => config.Beta = value;
        }

        public float DerivativeCutoff
        {
            get => config.DerivativeCutoff;
            set => config.DerivativeCutoff = value;
        }

        #endregion

        #region Fields / Properties

        [SerializeField] private bool hasValue;

        [SerializeField]
        private Vector2 rawValue;

        [SerializeField]
        private Vector2 filteredValue;

        [SerializeField]
        private Vector2 filteredDerivativeValue;

        public Vector2? RawValue => hasValue ? rawValue : null;
        public Vector2? FilteredValue => hasValue ? filteredValue : null;
        public Vector2? FilteredDerivativeValue => hasValue ? filteredDerivativeValue : null;

        #endregion

        #region Constructor

        public OneEuroFilterVector2(OneEuroFilterConfig config)
        {
            config.Validate();
            this.config = config;
            hasValue = false;
            rawValue = default;
            filteredValue = default;
            filteredDerivativeValue = default;
        }

        #endregion

        #region Update

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2? Update(Vector2? newRawValue, float deltaTime, float speedNormalizeFactor = 1)
        {
            if ((newRawValue ?? RawValue) is not { } rawValueNotNull)
            {
                return null;
            }

            return Update(rawValueNotNull, deltaTime, speedNormalizeFactor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 Update(Vector2 newRawValue, float deltaTime, float speedNormalizeFactor = 1)
        {
            if (deltaTime <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Delta time must be positive");
            }

            rawValue = newRawValue;

            var derivativeValue = hasValue ? ComputeDerivative(rawValue, filteredValue, deltaTime, speedNormalizeFactor) : default;
            var derivativeAlpha = OneEuroFilterUtils.ComputeAlpha(deltaTime, config.DerivativeCutoff);
            filteredDerivativeValue = hasValue ? Lerp(filteredDerivativeValue, derivativeValue, derivativeAlpha) : default;

            var cutoff = config.MinCutoff + config.Beta * ComputeMagnitude(filteredDerivativeValue, deltaTime, speedNormalizeFactor);

            var alpha = OneEuroFilterUtils.ComputeAlpha(deltaTime, cutoff);
            filteredValue = hasValue ? Lerp(filteredValue, rawValue, alpha) : rawValue;

            hasValue = true;

            return filteredValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector2 Lerp(Vector2 a, Vector2 b, float t)
        {
            return Vector2.Lerp(a, b, t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector2 ComputeDerivative(Vector2 rawValue, Vector2 filteredValue, float deltaTime, float speedNormalizeFactor)
        {
            return (rawValue - filteredValue) / (deltaTime * speedNormalizeFactor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ComputeMagnitude(Vector2 a, float deltaTime, float speedNormalizeFactor)
        {
            return a.magnitude;
        }

        #endregion

        #region Set

        public Vector2? Set(Vector2? newRawValue)
        {
            hasValue = newRawValue is not null;
            rawValue = newRawValue ?? default;
            filteredValue = newRawValue ?? default;
            filteredDerivativeValue = default;
            return newRawValue;
        }

        public Vector2 Set(Vector2 newRawValue)
        {
            hasValue = true;
            rawValue = newRawValue;
            filteredValue = newRawValue;
            filteredDerivativeValue = default;
            return newRawValue;
        }

        #endregion
    }

    [Serializable]
    public class OneEuroFilterVector3 : IOneEuroFilter<Vector3>
    {
        #region Config

        [SerializeField] private OneEuroFilterConfig config;

        public OneEuroFilterConfig Config
        {
            get => config;
            set
            {
                value.Validate();
                config = value;
            }
        }

        public float MinCutoff
        {
            get => config.MinCutoff;
            set => config.MinCutoff = value;
        }

        public float Beta
        {
            get => config.Beta;
            set => config.Beta = value;
        }

        public float DerivativeCutoff
        {
            get => config.DerivativeCutoff;
            set => config.DerivativeCutoff = value;
        }

        #endregion

        #region Fields / Properties

        [SerializeField] private bool hasValue;

        [SerializeField]
        private Vector3 rawValue;

        [SerializeField]
        private Vector3 filteredValue;

        [SerializeField]
        private Vector3 filteredDerivativeValue;

        public Vector3? RawValue => hasValue ? rawValue : null;
        public Vector3? FilteredValue => hasValue ? filteredValue : null;
        public Vector3? FilteredDerivativeValue => hasValue ? filteredDerivativeValue : null;

        #endregion

        #region Constructor

        public OneEuroFilterVector3(OneEuroFilterConfig config)
        {
            config.Validate();
            this.config = config;
            hasValue = false;
            rawValue = default;
            filteredValue = default;
            filteredDerivativeValue = default;
        }

        #endregion

        #region Update

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3? Update(Vector3? newRawValue, float deltaTime, float speedNormalizeFactor = 1)
        {
            if ((newRawValue ?? RawValue) is not { } rawValueNotNull)
            {
                return null;
            }

            return Update(rawValueNotNull, deltaTime, speedNormalizeFactor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 Update(Vector3 newRawValue, float deltaTime, float speedNormalizeFactor = 1)
        {
            if (deltaTime <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Delta time must be positive");
            }

            rawValue = newRawValue;

            var derivativeValue = hasValue ? ComputeDerivative(rawValue, filteredValue, deltaTime, speedNormalizeFactor) : default;
            var derivativeAlpha = OneEuroFilterUtils.ComputeAlpha(deltaTime, config.DerivativeCutoff);
            filteredDerivativeValue = hasValue ? Lerp(filteredDerivativeValue, derivativeValue, derivativeAlpha) : default;

            var cutoff = config.MinCutoff + config.Beta * ComputeMagnitude(filteredDerivativeValue, deltaTime, speedNormalizeFactor);

            var alpha = OneEuroFilterUtils.ComputeAlpha(deltaTime, cutoff);
            filteredValue = hasValue ? Lerp(filteredValue, rawValue, alpha) : rawValue;

            hasValue = true;

            return filteredValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3 Lerp(Vector3 a, Vector3 b, float t)
        {
            return Vector3.Lerp(a, b, t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3 ComputeDerivative(Vector3 rawValue, Vector3 filteredValue, float deltaTime, float speedNormalizeFactor)
        {
            return (rawValue - filteredValue) / (deltaTime * speedNormalizeFactor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ComputeMagnitude(Vector3 a, float deltaTime, float speedNormalizeFactor)
        {
            return a.magnitude;
        }

        #endregion

        #region Set

        public Vector3? Set(Vector3? newRawValue)
        {
            hasValue = newRawValue is not null;
            rawValue = newRawValue ?? default;
            filteredValue = newRawValue ?? default;
            filteredDerivativeValue = default;
            return newRawValue;
        }

        public Vector3 Set(Vector3 newRawValue)
        {
            hasValue = true;
            rawValue = newRawValue;
            filteredValue = newRawValue;
            filteredDerivativeValue = default;
            return newRawValue;
        }

        #endregion
    }

    [Serializable]
    public class OneEuroFilterVector4 : IOneEuroFilter<Vector4>
    {
        #region Config

        [SerializeField] private OneEuroFilterConfig config;

        public OneEuroFilterConfig Config
        {
            get => config;
            set
            {
                value.Validate();
                config = value;
            }
        }

        public float MinCutoff
        {
            get => config.MinCutoff;
            set => config.MinCutoff = value;
        }

        public float Beta
        {
            get => config.Beta;
            set => config.Beta = value;
        }

        public float DerivativeCutoff
        {
            get => config.DerivativeCutoff;
            set => config.DerivativeCutoff = value;
        }

        #endregion

        #region Fields / Properties

        [SerializeField] private bool hasValue;

        [SerializeField]
        private Vector4 rawValue;

        [SerializeField]
        private Vector4 filteredValue;

        [SerializeField]
        private Vector4 filteredDerivativeValue;

        public Vector4? RawValue => hasValue ? rawValue : null;
        public Vector4? FilteredValue => hasValue ? filteredValue : null;
        public Vector4? FilteredDerivativeValue => hasValue ? filteredDerivativeValue : null;

        #endregion

        #region Constructor

        public OneEuroFilterVector4(OneEuroFilterConfig config)
        {
            config.Validate();
            this.config = config;
            hasValue = false;
            rawValue = default;
            filteredValue = default;
            filteredDerivativeValue = default;
        }

        #endregion

        #region Update

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4? Update(Vector4? newRawValue, float deltaTime, float speedNormalizeFactor = 1)
        {
            if ((newRawValue ?? RawValue) is not { } rawValueNotNull)
            {
                return null;
            }

            return Update(rawValueNotNull, deltaTime, speedNormalizeFactor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4 Update(Vector4 newRawValue, float deltaTime, float speedNormalizeFactor = 1)
        {
            if (deltaTime <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Delta time must be positive");
            }

            rawValue = newRawValue;

            var derivativeValue = hasValue ? ComputeDerivative(rawValue, filteredValue, deltaTime, speedNormalizeFactor) : default;
            var derivativeAlpha = OneEuroFilterUtils.ComputeAlpha(deltaTime, config.DerivativeCutoff);
            filteredDerivativeValue = hasValue ? Lerp(filteredDerivativeValue, derivativeValue, derivativeAlpha) : default;

            var cutoff = config.MinCutoff + config.Beta * ComputeMagnitude(filteredDerivativeValue, deltaTime, speedNormalizeFactor);

            var alpha = OneEuroFilterUtils.ComputeAlpha(deltaTime, cutoff);
            filteredValue = hasValue ? Lerp(filteredValue, rawValue, alpha) : rawValue;

            hasValue = true;

            return filteredValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector4 Lerp(Vector4 a, Vector4 b, float t)
        {
            return Vector4.Lerp(a, b, t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector4 ComputeDerivative(Vector4 rawValue, Vector4 filteredValue, float deltaTime, float speedNormalizeFactor)
        {
            return (rawValue - filteredValue) / (deltaTime * speedNormalizeFactor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ComputeMagnitude(Vector4 a, float deltaTime, float speedNormalizeFactor)
        {
            return a.magnitude;
        }

        #endregion

        #region Set

        public Vector4? Set(Vector4? newRawValue)
        {
            hasValue = newRawValue is not null;
            rawValue = newRawValue ?? default;
            filteredValue = newRawValue ?? default;
            filteredDerivativeValue = default;
            return newRawValue;
        }

        public Vector4 Set(Vector4 newRawValue)
        {
            hasValue = true;
            rawValue = newRawValue;
            filteredValue = newRawValue;
            filteredDerivativeValue = default;
            return newRawValue;
        }

        #endregion
    }

    [Serializable]
    public class OneEuroFilterQuaternion : IOneEuroFilter<Quaternion>
    {
        #region Config

        [SerializeField] private OneEuroFilterConfig config;

        public OneEuroFilterConfig Config
        {
            get => config;
            set
            {
                value.Validate();
                config = value;
            }
        }

        public float MinCutoff
        {
            get => config.MinCutoff;
            set => config.MinCutoff = value;
        }

        public float Beta
        {
            get => config.Beta;
            set => config.Beta = value;
        }

        public float DerivativeCutoff
        {
            get => config.DerivativeCutoff;
            set => config.DerivativeCutoff = value;
        }

        #endregion

        #region Fields / Properties

        [SerializeField] private bool hasValue;

        [SerializeField]
        private Quaternion rawValue;

        [SerializeField]
        private Quaternion filteredValue;

        [SerializeField]
        private Quaternion filteredDerivativeValue;

        public Quaternion? RawValue => hasValue ? rawValue : null;
        public Quaternion? FilteredValue => hasValue ? filteredValue : null;
        public Quaternion? FilteredDerivativeValue => hasValue ? filteredDerivativeValue : null;

        #endregion

        #region Constructor

        public OneEuroFilterQuaternion(OneEuroFilterConfig config)
        {
            config.Validate();
            this.config = config;
            hasValue = false;
            rawValue = default;
            filteredValue = default;
            filteredDerivativeValue = default;
        }

        #endregion

        #region Update

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quaternion? Update(Quaternion? newRawValue, float deltaTime, float speedNormalizeFactor = 1)
        {
            if ((newRawValue ?? RawValue) is not { } rawValueNotNull)
            {
                return null;
            }

            return Update(rawValueNotNull, deltaTime, speedNormalizeFactor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quaternion Update(Quaternion newRawValue, float deltaTime, float speedNormalizeFactor = 1)
        {
            if (deltaTime <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Delta time must be positive");
            }

            rawValue = newRawValue;

            var derivativeValue = hasValue ? ComputeDerivative(rawValue, filteredValue, deltaTime, speedNormalizeFactor) : default;
            var derivativeAlpha = OneEuroFilterUtils.ComputeAlpha(deltaTime, config.DerivativeCutoff);
            filteredDerivativeValue = hasValue ? Lerp(filteredDerivativeValue, derivativeValue, derivativeAlpha) : default;

            var cutoff = config.MinCutoff + config.Beta * ComputeMagnitude(filteredDerivativeValue, deltaTime, speedNormalizeFactor);

            var alpha = OneEuroFilterUtils.ComputeAlpha(deltaTime, cutoff);
            filteredValue = hasValue ? Lerp(filteredValue, rawValue, alpha) : rawValue;

            hasValue = true;

            return filteredValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Quaternion Lerp(Quaternion a, Quaternion b, float t)
        {
            return Quaternion.Slerp(a, b, t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Quaternion ComputeDerivative(Quaternion rawValue, Quaternion filteredValue, float deltaTime, float speedNormalizeFactor)
        {
            // Compute rotation difference: rotation from filteredValue to rawValue
            var rotationDiff = Quaternion.Inverse(filteredValue) * rawValue;
            // Normalize to ensure valid quaternion
            rotationDiff = rotationDiff.normalized;
            return rotationDiff;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ComputeMagnitude(Quaternion a, float deltaTime, float speedNormalizeFactor)
        {
            // Compute angular velocity magnitude in radians per second
            // The quaternion represents a rotation difference, convert to angular velocity
            var angle = Quaternion.Angle(Quaternion.identity, a);
            var angularVelocity = (angle * Mathf.Deg2Rad) / (deltaTime * speedNormalizeFactor);
            return angularVelocity;
        }

        #endregion

        #region Set

        public Quaternion? Set(Quaternion? newRawValue)
        {
            hasValue = newRawValue is not null;
            rawValue = newRawValue ?? default;
            filteredValue = newRawValue ?? default;
            filteredDerivativeValue = default;
            return newRawValue;
        }

        public Quaternion Set(Quaternion newRawValue)
        {
            hasValue = true;
            rawValue = newRawValue;
            filteredValue = newRawValue;
            filteredDerivativeValue = default;
            return newRawValue;
        }

        #endregion
    }

    public static class OneEuroFilterUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ComputeAlpha(float deltaTime, float cutoff)
        {
            var r = 2 * Mathf.PI * cutoff * deltaTime;
            return r / (r + 1);
        }
    }
}

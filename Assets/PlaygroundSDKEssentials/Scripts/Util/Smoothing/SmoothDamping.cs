#nullable enable

using UnityEngine;

namespace Nex.Essentials
{
    public struct SmoothDampedFloat
    {
        public float CurrentValue { get; private set; }
        public float TargetValue { get; private set; }

        private float velocity;
        private readonly float smoothTime;

        public SmoothDampedFloat(float smoothTime = 1f)
        {
            CurrentValue = default;
            TargetValue = default;
            velocity = default;
            this.smoothTime = smoothTime;
        }

        public void Reset(float value)
        {
            CurrentValue = value;
            TargetValue = value;
            velocity = default;
        }

        public void SetTargetValue(float targetValue)
        {
            TargetValue = targetValue;
        }

        public float Update(float? overrideSmoothTime = null)
        {
            return CurrentValue = Mathf.SmoothDamp(
                CurrentValue,
                TargetValue,
                ref velocity,
                overrideSmoothTime ?? smoothTime,
                Mathf.Infinity,
                Time.unscaledTime);
        }
    }

    public struct SmoothDampedVector2
    {
        public Vector2 CurrentValue { get; private set; }
        public Vector2 TargetValue { get; private set; }

        private Vector2 velocity;
        private readonly float smoothTime;

        public SmoothDampedVector2(float smoothTime = 1f)
        {
            CurrentValue = default;
            TargetValue = default;
            velocity = default;
            this.smoothTime = smoothTime;
        }

        public void Reset(Vector2 value)
        {
            CurrentValue = value;
            TargetValue = value;
            velocity = default;
        }

        public void SetTargetValue(Vector2 targetValue)
        {
            TargetValue = targetValue;
        }

        public Vector2 Update(float? overrideSmoothTime = null)
        {
            return CurrentValue = Vector2.SmoothDamp(
                CurrentValue,
                TargetValue,
                ref velocity,
                overrideSmoothTime ?? smoothTime,
                Mathf.Infinity,
                Time.unscaledTime);
        }
    }

    public struct SmoothDampedVector3
    {
        public Vector3 CurrentValue { get; private set; }
        public Vector3 TargetValue { get; private set; }

        private Vector3 velocity;
        private readonly float smoothTime;

        public SmoothDampedVector3(float smoothTime = 1f)
        {
            CurrentValue = default;
            TargetValue = default;
            velocity = default;
            this.smoothTime = smoothTime;
        }

        public void Reset(Vector3 value)
        {
            CurrentValue = value;
            TargetValue = value;
            velocity = default;
        }

        public void SetTargetValue(Vector3 targetValue)
        {
            TargetValue = targetValue;
        }

        public Vector3 Update(float? overrideSmoothTime = null)
        {
            return CurrentValue = Vector3.SmoothDamp(
                CurrentValue,
                TargetValue,
                ref velocity,
                overrideSmoothTime ?? smoothTime,
                Mathf.Infinity,
                Time.unscaledTime);
        }
    }

    public struct SmoothDampedRect
    {
        public Rect CurrentValue { get; private set; }
        public Rect TargetValue { get; private set; }

        private float minXVelocity;
        private float maxXVelocity;
        private float minYVelocity;
        private float maxYVelocity;
        private readonly float smoothTime;

        public SmoothDampedRect(float smoothTime)
        {
            CurrentValue = Rect.zero;
            TargetValue = Rect.zero;
            minXVelocity = maxXVelocity = minYVelocity = maxYVelocity = 0;
            this.smoothTime = smoothTime;
        }

        public void Reset(Rect value)
        {
            CurrentValue = value;
            TargetValue = value;
            minXVelocity = maxXVelocity = minYVelocity = maxYVelocity = 0;
        }

        public void SetTargetValue(Rect targetValue) {
            TargetValue = targetValue;
        }

        public Rect Update(float? overrideSmoothTime = null)
        {
            var minX = Mathf.SmoothDamp(
                CurrentValue.xMin, TargetValue.xMin, ref minXVelocity,
                overrideSmoothTime ?? smoothTime, Mathf.Infinity, Time.unscaledTime);
            var maxX = Mathf.SmoothDamp(
                CurrentValue.xMax, TargetValue.xMax, ref maxXVelocity,
                overrideSmoothTime ?? smoothTime, Mathf.Infinity, Time.unscaledTime);
            var minY = Mathf.SmoothDamp(
                CurrentValue.yMin, TargetValue.yMin, ref minYVelocity,
                overrideSmoothTime ?? smoothTime, Mathf.Infinity, Time.unscaledTime);
            var maxY = Mathf.SmoothDamp(
                CurrentValue.yMax, TargetValue.yMax, ref maxYVelocity,
                overrideSmoothTime ?? smoothTime, Mathf.Infinity, Time.unscaledTime);
            return CurrentValue = Rect.MinMaxRect(minX, minY, maxX, maxY);
        }
    }
}

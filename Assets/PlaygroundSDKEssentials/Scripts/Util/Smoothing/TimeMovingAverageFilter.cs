#nullable enable

using UnityEngine;

namespace Nex.Essentials
{
    public struct TimeMovingAverageFilterFloat
    {
        public float CurrentValue => history.Count > 0 ? sum / history.Count : default;

        private Deque<(float value, float time)> history;
        private float sum;

        public float SmoothWindow { get; set; }

        private const int minimumHistorySize = 8;

        public TimeMovingAverageFilterFloat(float smoothWindow = 1f)
        {
            history = new Deque<(float value, float time)>(minimumHistorySize);
            sum = default;
            SmoothWindow = smoothWindow;
        }

        public void Reset()
        {
            history.Clear();
            sum = default;
        }

        public float Update(float value, float? overrideCurrTime = null)
        {
            var currTime = overrideCurrTime ?? Time.unscaledTime;
            var expiredTime = currTime - SmoothWindow;
            while (history.TryPeekFront(out var item) && item.time < expiredTime)
            {
                sum -= item.value;
                history.PopFront();
            }
            history.PushBack((value, currTime));
            sum += value;
            return sum / history.Count;
        }
    }

    public struct TimeMovingAverageFilterVector2
    {
        public Vector2 CurrentValue => history.Count > 0 ? sum / history.Count : default;

        private Deque<(Vector2 value, float time)> history;
        private Vector2 sum;

        public float SmoothWindow { get; set; }

        private const int minimumHistorySize = 8;

        public TimeMovingAverageFilterVector2(float smoothWindow = 1f)
        {
            history = new Deque<(Vector2 value, float time)>(minimumHistorySize);
            sum = default;
            SmoothWindow = smoothWindow;
        }

        public void Reset()
        {
            history.Clear();
            sum = default;
        }

        public Vector2 Update(Vector2 value, float? overrideCurrTime = null)
        {
            var currTime = overrideCurrTime ?? Time.unscaledTime;
            var expiredTime = currTime - SmoothWindow;
            while (history.TryPeekFront(out var item) && item.time < expiredTime)
            {
                sum -= item.value;
                history.PopFront();
            }
            history.PushBack((value, currTime));
            sum += value;
            return sum / history.Count;
        }
    }

    public struct TimeMovingAverageFilterVector3
    {
        public Vector3 CurrentValue => history.Count > 0 ? sum / history.Count : default;

        private Deque<(Vector3 value, float time)> history;
        private Vector3 sum;

        public float SmoothWindow { get; set; }

        private const int minimumHistorySize = 8;

        public TimeMovingAverageFilterVector3(float smoothWindow = 1f)
        {
            history = new Deque<(Vector3 value, float time)>(minimumHistorySize);
            sum = default;
            SmoothWindow = smoothWindow;
        }

        public void Reset()
        {
            history.Clear();
            sum = default;
        }

        public Vector3 Update(Vector3 value, float? overrideCurrTime = null)
        {
            var currTime = overrideCurrTime ?? Time.unscaledTime;
            var expiredTime = currTime - SmoothWindow;
            while (history.TryPeekFront(out var item) && item.time < expiredTime)
            {
                sum -= item.value;
                history.PopFront();
            }
            history.PushBack((value, currTime));
            sum += value;
            return sum / history.Count;
        }
    }

    public struct TimeMovingAverageFilterVector4
    {
        public Vector4 CurrentValue => history.Count > 0 ? sum / history.Count : default;

        private Deque<(Vector4 value, float time)> history;
        private Vector4 sum;

        public float SmoothWindow { get; set; }

        private const int minimumHistorySize = 8;

        public TimeMovingAverageFilterVector4(float smoothWindow = 1f)
        {
            history = new Deque<(Vector4 value, float time)>(minimumHistorySize);
            sum = default;
            SmoothWindow = smoothWindow;
        }

        public void Reset()
        {
            history.Clear();
            sum = default;
        }

        public Vector4 Update(Vector4 value, float? overrideCurrTime = null)
        {
            var currTime = overrideCurrTime ?? Time.unscaledTime;
            var expiredTime = currTime - SmoothWindow;
            while (history.TryPeekFront(out var item) && item.time < expiredTime)
            {
                sum -= item.value;
                history.PopFront();
            }
            history.PushBack((value, currTime));
            sum += value;
            return sum / history.Count;
        }
    }

    public struct TimeMovingAverageFilterRect {
        // This is similar to moving average for min/max x/y values.
        public Rect CurrentValue {
            get {
                if (history.Count == 0) {
                    return Rect.zero;
                }
                var average = sum / history.Count;
                return Rect.MinMaxRect(average.x, average.y, average.z, average.w);
            }
        }

        private Deque<(Vector4 value, float time)> history;
        private Vector4 sum;

        public float SmoothWindow { get; set; }

        private const int minimumHistorySize = 8;

        public TimeMovingAverageFilterRect(float smoothWindow)
        {
            history = new Deque<(Vector4 value, float time)>(minimumHistorySize);
            sum = default;
            SmoothWindow = smoothWindow;
        }

        public void Reset()
        {
            history.Clear();
            sum = default;
        }

        public Rect Update(Rect rect, float? overrideCurrTime = null)
        {
            var value = new Vector4(rect.xMin, rect.yMin, rect.xMax, rect.yMax);
            var currTime = overrideCurrTime ?? Time.unscaledTime;
            var expiredTime = currTime - SmoothWindow;
            while (history.TryPeekFront(out var item) && item.time < expiredTime) {
                sum -= item.value;
                history.PopFront();
            }
            history.PushBack((value, currTime));
            sum += value;
            var average = sum / history.Count;
            return Rect.MinMaxRect(average.x, average.y, average.z, average.w);
        }
    }
}

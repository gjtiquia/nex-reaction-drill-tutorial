#nullable enable

using System.Collections;
using System.Collections.Generic;

namespace Nex.Essentials
{
    public struct HistoryItem<T>
    {
        public T value;
        public float timestamp;

        public static implicit operator T (HistoryItem<T> item) => item.value;
    }

    public class History<T> : IEnumerable<HistoryItem<T>>
    {
        private readonly float maxAge;
        private readonly Deque<HistoryItem<T>> history = new();

        public HistoryItem<T> LatestItem => history.Count > 0 ? history.PeekFront() : default;
        public HistoryItem<T> EarliestItem => history.Count > 0 ? history.PeekBack() : default;
        public T LatestValue => LatestItem.value;
        public T EarliestValue => EarliestItem.value;
        public int Count => history.Count;

        public float TimeSpan => LatestItem.timestamp - EarliestItem.timestamp;

        public History(float maxAge)
        {
            this.maxAge = maxAge;
        }

        public void Add(T data, float timestamp)
        {
            if (history.Count > 0 && timestamp <= LatestItem.timestamp) return;
            var item = new HistoryItem<T> { timestamp = timestamp, value = data };
            history.PushFront(item);
            CleanUp(timestamp);
        }

        public void Clear()
        {
            history.Clear();
        }

        public void CleanUp(float timestamp)
        {
            var cutoff = timestamp - maxAge;
            while (history.Count > 0 && history.PeekBack().timestamp <= cutoff)
            {
                history.PopBack();
            }
        }

        public IEnumerator<HistoryItem<T>> GetEnumerator() => history.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => history.GetEnumerator();
    }
}

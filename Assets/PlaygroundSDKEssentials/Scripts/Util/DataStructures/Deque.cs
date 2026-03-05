#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;

namespace Nex.Essentials
{
    public class Deque<T> : IList<T>, IReadOnlyList<T>
    {
        private const int MinCapacity = 4;
        private T[] items;

        private int headIndex; // This is where the first item is.
        private int count; // How many items are there in the deque.

        public int Count => count;
        public bool IsReadOnly => false;

        public Deque(int capacity = MinCapacity)
        {
            if (capacity < MinCapacity)
            {
                capacity = MinCapacity;
            }

            items = new T[capacity];
        }

        private void EnlargeIfNeeded()
        {
            if (count < items.Length) return;
            // We want to double the size of the array.
            // However, we want to make sure the items are actually well-ordered.
            var newItems = new T[items.Length * 2];
            Array.Copy(items, headIndex, newItems, 0, count - headIndex);
            Array.Copy(items, 0, newItems, count - headIndex, headIndex);
            items = newItems;
            headIndex = 0;
        }

        public void PushFront(T item)
        {
            EnlargeIfNeeded();
            headIndex = (headIndex - 1 + items.Length) % items.Length;
            items[headIndex] = item;
            ++count;
        }

        public void PushBack(T item)
        {
            EnlargeIfNeeded();
            items[(headIndex + count++) % items.Length] = item;
        }

        public T PopFront()
        {
            if (count == 0)
            {
                throw new InvalidOperationException("Deque is empty.");
            }

            var item = items[headIndex];
            items[headIndex] = default!;
            headIndex = (headIndex + 1) % items.Length;
            --count;
            return item;
        }

        public T PopBack()
        {
            if (count == 0)
            {
                throw new InvalidOperationException("Deque is empty.");
            }

            var holeIndex = (headIndex + count - 1) % items.Length;
            var item = items[holeIndex];
            items[holeIndex] = default!;
            --count;
            return item;
        }

        public T PeekFront()
        {
            if (count == 0)
            {
                throw new InvalidOperationException("Deque is empty.");
            }

            return items[headIndex];
        }

        public bool TryPeekFront(out T item)
        {
            if (count == 0)
            {
                item = default!;
                return false;
            }
            item = items[headIndex];
            return true;
        }

        public T PeekBack()
        {
            if (count == 0)
            {
                throw new InvalidOperationException("Deque is empty.");
            }

            return items[(headIndex + count - 1) % items.Length];
        }

        public bool TryPeekBack(out T item)
        {
            if (count == 0)
            {
                item = default!;
                return false;
            }
            item = items[(headIndex + count - 1) % items.Length];
            return true;
        }

        public void Add(T item)
        {
            PushBack(item);
        }

        public void Clear()
        {
            var index = headIndex;
            var capacity = items.Length;
            for (var i = 0; i < count; ++i)
            {
                items[index] = default!;
                if (++index == capacity) index = 0;
            }

            headIndex = 0;
            count = 0;
        }

        public bool Contains(T item)
        {
            var index = headIndex;
            var capacity = items.Length;
            for (var i = 0; i < count; ++i)
            {
                if (Equals(items[index], item)) return true;
                if (++index == capacity) index = 0;
            }
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            var index = headIndex;
            var capacity = items.Length;
            var outIndex = arrayIndex;
            for (var i = 0; i < count; ++i)
            {
                array[outIndex++] = items[index];
                if (++index == capacity) index = 0;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            var index = headIndex;
            var capacity = items.Length;
            for (var i = 0; i < count; ++i)
            {
                yield return items[index];
                if (++index == capacity) index = 0;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int IndexOf(T item)
        {
            var index = headIndex;
            for (var i = 0; i < count; ++i)
            {
                if (Equals(items[index], item)) return i;
                if (++index == items.Length) index = 0;
            }

            return -1;
        }

        public void Insert(int index, T item)
        {
            if (index < 0 || index > count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            EnlargeIfNeeded();
            var capacity = items.Length;
            int holeIndex;
            if (index <= count / 2)
            {
                // Shift the prefix one space before, to make room at index.
                var currIndex = headIndex;
                headIndex = (headIndex - 1 + capacity) % capacity;
                holeIndex = headIndex;
                for (var i = 0; i < index; ++i)
                {
                    items[holeIndex] = items[currIndex];
                    holeIndex = currIndex;
                    if (++currIndex == capacity) currIndex = 0;
                }
            }
            else
            {
                // Shift the suffix one space after, to make room at index.
                var currIndex = (headIndex + count - 1) % capacity;
                holeIndex = (currIndex + 1) % capacity;
                for (var i = index; i < count; ++i)
                {
                    items[holeIndex] = items[currIndex];
                    holeIndex = currIndex;
                    if (--currIndex == -1) currIndex += capacity;
                }
            }
            items[holeIndex] = item;
            ++count;
        }

        public void RemoveAt(int index)
        {
            var capacity = items.Length;
            var holeIndex = (headIndex + index) % capacity;
            if (index <= count / 2) {
                // Shift the prefix one space after, which means moving the holeIndex to the front.
                var prevIndex = (holeIndex + capacity - 1) % capacity;
                for (var i = index; i > 1; --i)
                {
                    items[holeIndex] = items[prevIndex];
                    holeIndex = prevIndex;
                    if (--prevIndex == -1) prevIndex += capacity;
                }
                headIndex = (headIndex + 1) % items.Length;
            }
            else
            {
                // Shift the suffix one space before, which means moving the holeIndex to the back.
                var nextIndex = (holeIndex + 1) % capacity;
                for (var i = index + 1; i < count; ++i)
                {
                    items[holeIndex] = items[nextIndex];
                    holeIndex = nextIndex;
                    if (++nextIndex == capacity) nextIndex = 0;
                }
            }
            items[holeIndex] = default!;  // Clear the item, since it is removed.
            // Update the count since one element has been removed.
            --count;
        }

        public bool Remove(T item)
        {
            // First we find a matching index.
            var capacity = items.Length;
            var holeIndex = headIndex;
            int i;
            for (i = 0; i < count; ++i)
            {
                if (Equals(items[holeIndex], item)) break;
                if (++holeIndex == capacity) holeIndex = 0;
            }

            if (i == count) return false; // No item found.

            // Shift everything forward by one index now.
            var nextIndex = (holeIndex + 1) % capacity;
            for (++i; i < count; ++i)
            {
                items[holeIndex] = items[nextIndex];
                holeIndex = nextIndex;
                if (++nextIndex == capacity) nextIndex = 0;
            }

            items[holeIndex] = default!;
            --count;

            return true;
        }

        public T this[int index]
        {
            get => items[(headIndex + index) % items.Length];
            set => items[(headIndex + index) % items.Length] = value;
        }
    }
}

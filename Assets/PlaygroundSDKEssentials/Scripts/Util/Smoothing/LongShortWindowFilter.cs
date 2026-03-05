namespace Nex.Essentials
{
    /**
     * A simple temporal filter that converts noisy per-frame boolean signals
     * into a stable state plus clean start/end transition events.
     *
     * Inspired by the Baba Algorithm from https://github.com/PINTO0309/bbalg
     *
     * Uses two sliding windows:
     * - Long window: adds stability and prevents flapping.
     * - Short window: confirms recent trend for fast transitions.
     */
    public class LongShortWindowFilter
    {
        int longWindowLength;
        int shortWindowLength;
        Deque<bool> longWindow;
        Deque<bool> shortWindow;
        int longWindowSum;
        int shortWindowSum;

        public bool Value { get; private set; }

        public LongShortWindowFilter(int longWindowLength = 10, int shortWindowLength = 4)
        {
            this.longWindowLength = longWindowLength;
            this.shortWindowLength = shortWindowLength;
            longWindow = new Deque<bool>(longWindowLength);
            shortWindow = new Deque<bool>(shortWindowLength);
            longWindowSum = 0;
            shortWindowSum = 0;
            Value = false;
        }

        public void Reset()
        {
            longWindow.Clear();
            shortWindow.Clear();
            longWindowSum = 0;
            shortWindowSum = 0;
            Value = false;
        }

        public bool Update(bool signal)
        {
            if (longWindow.Count == longWindowLength)
            {
                var removed = longWindow.PopFront();
                if (removed) longWindowSum--;
            }
            longWindow.PushBack(signal);
            if (signal) longWindowSum++;

            if (shortWindow.Count == shortWindowLength)
            {
                var removed = shortWindow.PopFront();
                if (removed) shortWindowSum--;
            }
            shortWindow.PushBack(signal);
            if (signal) shortWindowSum++;

            if (longWindow.Count < longWindowLength || shortWindow.Count < shortWindowLength)
            {
                // Not enough data yet; maintain current state.
                return Value;
            }

            if (longWindowSum >= longWindowLength / 2 && shortWindowSum >= shortWindowLength - 1)
            {
                Value = true;
            }
            else if (longWindowSum < longWindowLength / 2 && shortWindowSum <= 1)
            {
                Value = false;
            }

            return Value;
        }
    }
}

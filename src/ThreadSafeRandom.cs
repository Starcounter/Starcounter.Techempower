using System;
using System.Threading;

namespace Starcounter.Techempower
{
    public class ThreadSafeRandom : IRandom
    {
        private static int nextSeed = unchecked((int)DateTime.Now.Ticks);
        // System.Random class isn't thread safe.
        private static readonly ThreadLocal<Random> _random = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref nextSeed)));

        public int Next(int minValue, int maxValue)
        {
            return _random.Value.Next(minValue, maxValue);
        }
    }
}

using System;

namespace Toggl.Foundation.Sync
{
    internal sealed class RetryDelayService : IRetryDelayService
    {
        private readonly Random rnd;

        private double lastDelay;

        private const double defaultFastDelay = 10;

        private const double defaultSlowDelay = 60;

        private double randomFastFactor => getRandomNumberBetween(1, 1.5);

        private double randomSlowFactor => getRandomNumberBetween(1.5, 2);

        public RetryDelayService(Random rnd)
        {
            this.rnd = rnd;
            Reset();
        }

        public TimeSpan NextSlowDelay()
            => nextDelay(randomSlowFactor, defaultSlowDelay);

        public TimeSpan NextFastDelay()
            => nextDelay(randomFastFactor, defaultFastDelay);

        public void Reset() => lastDelay = 0;

        private TimeSpan nextDelay(double factor, double defaultDelay)
        {
            lastDelay = Math.Max(Math.Min(lastDelay * factor, TimeSpan.MaxValue.TotalSeconds), defaultDelay);
            return TimeSpan.FromSeconds(lastDelay);
        }

        private double getRandomNumberBetween(double min, double max)
            => rnd.NextDouble() * (max - min) + min;
    }
}

using System;

namespace Octoplug.ResidentDemand.Unity
{
    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random _random;

        public SystemRandomSource(int seed)
        {
            _random = new Random(seed);
        }

        public int Next(int maximumExclusive)
        {
            return _random.Next(maximumExclusive);
        }
    }
}

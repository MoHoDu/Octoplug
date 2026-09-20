namespace Octoplug.ResidentDemand
{
    public sealed class ResidentNumberSequence
    {
        private int _nextValue = 1;

        public ResidentNumber Next()
        {
            return new ResidentNumber(_nextValue++);
        }
    }
}

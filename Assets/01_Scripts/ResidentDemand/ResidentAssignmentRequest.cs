using System;

namespace Octoplug.ResidentDemand
{
    public sealed class ResidentAssignmentRequest
    {
        public ResidentAssignmentRequest(
            ResidentNumber residentNumber,
            int needIndex,
            ResidentNeedType needType,
            long sequence)
        {
            if (needIndex < 0 || needIndex > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(needIndex));
            }

            if (sequence < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sequence));
            }

            ResidentNumber = residentNumber;
            NeedIndex = needIndex;
            NeedType = needType;
            Sequence = sequence;
        }

        public ResidentNumber ResidentNumber { get; }
        public int NeedIndex { get; }
        public ResidentNeedType NeedType { get; }
        public long Sequence { get; }
    }
}

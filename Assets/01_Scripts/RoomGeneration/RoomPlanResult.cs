using System;
using System.Collections.Generic;

namespace Octoplug.RoomGeneration
{
    /// <summary>Result of evaluating a caller-ordered candidate sequence.</summary>
    public sealed class RoomPlanResult
    {
        internal RoomPlanResult(RoomPlan plan, int selectedCandidateIndex, IReadOnlyList<RoomCandidateRejectionRecord> rejections)
        {
            Plan = plan;
            SelectedCandidateIndex = selectedCandidateIndex;
            Rejections = rejections ?? throw new ArgumentNullException(nameof(rejections));
        }

        public bool Success => Plan != null;
        public RoomPlan Plan { get; }
        public int SelectedCandidateIndex { get; }
        public IReadOnlyList<RoomCandidateRejectionRecord> Rejections { get; }
    }
}

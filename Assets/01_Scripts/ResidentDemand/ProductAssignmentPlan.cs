using System.Collections.Generic;

namespace Octoplug.ResidentDemand
{
    public sealed class ProductAssignmentPlan
    {
        public ProductAssignmentPlan(
            IReadOnlyList<ResidentProductAssignment> assignments,
            IReadOnlyList<ResidentAssignmentRequest> waiting)
        {
            Assignments = assignments;
            Waiting = waiting;
        }

        public IReadOnlyList<ResidentProductAssignment> Assignments { get; }
        public IReadOnlyList<ResidentAssignmentRequest> Waiting { get; }
    }
}

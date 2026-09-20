namespace Octoplug.ResidentDemand
{
    public sealed class ResidentProductAssignment
    {
        public ResidentProductAssignment(ResidentAssignmentRequest request, string productId)
        {
            Request = request;
            ProductId = productId;
        }

        public ResidentAssignmentRequest Request { get; }
        public string ProductId { get; }
        public bool IsUsing => ProductId != null;
    }
}

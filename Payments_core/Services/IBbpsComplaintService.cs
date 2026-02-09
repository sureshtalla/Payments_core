namespace Payments_core.Services.BBPSService
{
    public interface IBbpsComplaintService
    {
        Task RegisterComplaint(
            string txnRefId,
            string billerId,
            string complaintType,
            string description
        );

        Task TrackComplaint(string complaintId);
    }
}
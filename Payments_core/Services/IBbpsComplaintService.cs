namespace Payments_core.Services.BBPSService
{
    public interface IBbpsComplaintService
    {
        Task<object> RegisterComplaint(
        string txnRefId,
        string complaintType,
        string description
    );

        Task<object> TrackComplaint(string complaintId);
    }
}
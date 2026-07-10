public class ReplenishmentRequest
{
    public Guid Id { get; set; }

    public string LocationCode { get; set; } = string.Empty;

    public Priority Priority { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Draft;

    public ValidationStatus ValidationStatus { get; set; } = ValidationStatus.Pending;

    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime? FulfilledAt { get; set; }

    public List<RequestItem> Items { get; set; } = [];
}
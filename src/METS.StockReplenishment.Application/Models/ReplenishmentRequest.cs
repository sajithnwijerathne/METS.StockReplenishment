
public class ReplenishmentRequest
{
    public Guid Id { get; set; }

    public string LocationCode { get; set; } = string.Empty;

    public Priority Priority { get; set; }

    public RequestStatus Status { get; set; }

    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<RequestItem> Items { get; set; } = [];
}

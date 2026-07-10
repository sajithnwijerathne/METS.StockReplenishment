public class RequestDto
{
    public Guid Id { get; set; }

    public string LocationCode { get; set; } = string.Empty;

    public Priority Priority { get; set; }

    public RequestStatus Status { get; set; }

    public ValidationStatus ValidationStatus { get; set; }

    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime? FulfilledAt { get; set; }

    public List<RequestItemDto> Items { get; set; } = [];
}

public class RequestItemDto
{
    public Guid Id { get; set; }

    public string ArticleNumber { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal RequestedQuantity { get; set; }

    public decimal FulfilledQuantity { get; set; }
}
public class RequestListItemDto
{
    public Guid Id { get; set; }

    public string LocationCode { get; set; } = string.Empty;

    public Priority Priority { get; set; }

    public RequestStatus Status { get; set; }

    public ValidationStatus ValidationStatus { get; set; }

    public DateTime CreatedAt { get; set; }

    public int ItemCount { get; set; }
}
public class RequestFilterDto
{
    public RequestStatus? Status { get; set; }

    public Priority? Priority { get; set; }

    public string? LocationCode { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
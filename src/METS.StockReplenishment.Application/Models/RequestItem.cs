public class RequestItem
{
    public Guid Id { get; set; }

    public string ArticleNumber { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal RequestedQuantity { get; set; }

    public decimal FulfilledQuantity { get; set; }
}
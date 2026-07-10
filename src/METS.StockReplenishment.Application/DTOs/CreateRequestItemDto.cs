using System.ComponentModel.DataAnnotations;
public class CreateRequestItemDto
{
    [Required]
    public string ArticleNumber { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal RequestedQuantity { get; set; }
}
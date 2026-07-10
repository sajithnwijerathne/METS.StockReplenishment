using System.ComponentModel.DataAnnotations;

public class FulfillRequestDto
{
    [Required]
    [MinLength(1)]
    public List<FulfillRequestItemDto> Items { get; set; } = [];
}

public class FulfillRequestItemDto
{
    [Required]
    public Guid RequestItemId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal FulfilledQuantity { get; set; }
}
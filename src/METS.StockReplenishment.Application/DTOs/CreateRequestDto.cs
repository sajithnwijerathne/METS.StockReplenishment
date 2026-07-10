using System.ComponentModel.DataAnnotations;

public class CreateRequestDto
{
    [Required]
    public string LocationCode { get; set; } = string.Empty;

    [Required]
    public Priority Priority { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateRequestItemDto> Items { get; set; } = [];
}

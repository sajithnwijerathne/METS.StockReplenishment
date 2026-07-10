using System.ComponentModel.DataAnnotations;

public class RejectRequestDto
{
    [Required]
    [MinLength(3)]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}
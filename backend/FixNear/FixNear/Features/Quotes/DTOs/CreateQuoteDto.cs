using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.Quotes.DTOs;

public class CreateQuoteDto
{
    [Range(1, int.MaxValue,
        ErrorMessage = "Ma yeu cau sua chua khong hop le")]
    public int RepairRequestId { get; set; }

    public DateTime? ValidUntil { get; set; }

    [Required]
    [MinLength(1,
        ErrorMessage = "Bao gia phai co it nhat mot hang muc")]
    public List<CreateQuoteDetailDto> Details { get; set; }
        = new();
}

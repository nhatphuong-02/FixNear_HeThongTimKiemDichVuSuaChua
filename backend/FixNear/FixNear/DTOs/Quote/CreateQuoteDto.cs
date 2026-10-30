
using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.Quote;

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

public class CreateQuoteDetailDto
{
    [Range(1, int.MaxValue,
        ErrorMessage = "Loai hang muc khong hop le")]
    public int ItemType { get; set; }

    public int? ServiceRefId { get; set; }

    public int? RepairItemRefId { get; set; }

    public int? RepairPartRefId { get; set; }

    public string? Description { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }
}

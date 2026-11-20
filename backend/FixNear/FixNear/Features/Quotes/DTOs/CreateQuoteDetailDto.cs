using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.Quotes.DTOs
{
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

    }

}

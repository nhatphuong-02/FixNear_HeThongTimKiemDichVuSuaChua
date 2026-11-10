
using FixNear.DTOs.RepairItem;

namespace FixNear.Services.Interfaces
{
    public interface IRepairItemService
    {
        Task<CreateRepairItemResult> CreateAsync(
            CreateRepairItemDto dto);
    }

    public class CreateRepairItemResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public int? RepairItemId { get; set; }
    }
}

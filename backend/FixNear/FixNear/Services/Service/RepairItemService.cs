using FixNear.DTOs.RepairItem;
using FixNear.Models;
using FixNear.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Services.Service
{
    public class RepairItemService : IRepairItemService
    {
        private readonly FixNearDbContext _context;

        public RepairItemService(FixNearDbContext context)
        {
            _context = context;
        }

        // Tao hang muc sua chua trong danh muc
        public async Task<CreateRepairItemResult> CreateAsync(
            CreateRepairItemDto dto)
        {
            // 1. Kiem tra ma va ten hang muc
            if (string.IsNullOrWhiteSpace(dto.ItemCode) ||
                string.IsNullOrWhiteSpace(dto.ItemName))
            {
                return new CreateRepairItemResult
                {
                    Success = false,
                    Message = "Mã và tên hạng mục không được để trống"
                };
            }

            if (dto.DefaultPrice < 0)
            {
                return new CreateRepairItemResult
                {
                    Success = false,
                    Message = "Giá mặc định không được âm"
                };
            }

            string itemCode = dto.ItemCode.Trim();
            string itemName = dto.ItemName.Trim();

            // 2. Kiem tra ma hang muc trung
            bool exists = await _context.RepairItem
                .AnyAsync(x => x.ItemCode == itemCode);

            if (exists)
            {
                return new CreateRepairItemResult
                {
                    Success = false,
                    Message = "Mã hạng mục đã tồn tại"
                };
            }

            // 3. Tao doi tuong RepairItem
            var repairItem = new RepairItem
            {
                ItemCode = itemCode,
                ItemName = itemName,
                Description = dto.Description?.Trim(),
                DefaultPrice = dto.DefaultPrice,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            // 4. Them vao DbContext
            _context.RepairItem.Add(repairItem);

            // 5. Luu vao SQL Server
            await _context.SaveChangesAsync();

            // 6. Tra ket qua
            return new CreateRepairItemResult
            {
                Success = true,
                Message = "Tạo hạng mục sửa chữa thành công",
                RepairItemId = repairItem.RepairItemId
            };
        }
    }
}

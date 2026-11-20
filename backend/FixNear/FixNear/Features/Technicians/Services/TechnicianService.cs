
using FixNear.Domain.Enums;
using FixNear.Domain.Models.Technicians;
using FixNear.Features.Technicians.DTOs;
using FixNear.Features.Technicians.Interfaces;
using FixNear.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Features.Technicians.Services;

public sealed class TechnicianService : ITechnicianService
{
    private readonly FixNearDbContext _db;

    public TechnicianService(FixNearDbContext db)
    {
        _db = db;
    }

    // Lấy hồ sơ của KTV đăng nhập
    public async Task<TechnicianProfileResponseDto?>
        GetProfileAsync(int userId)
    {
        return await _db.TechnicianProfile
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .Select(t => new TechnicianProfileResponseDto
            {
                TechnicianId = t.TechnicianId,
                FullName = t.User.FullName,
                Email = t.User.Email,
                Phone = t.User.Phone,
                AvatarUrl = t.User.AvatarUrl,
                DateOfBirth = t.User.DateOfBirth,
                Gender = t.User.Gender,
                ExperienceYears = t.ExperienceYears,
                Introduction = t.Introduction,
                RatingAverage = t.RatingAverage,

                Specializations = t.TechnicianSpecialization
                    .OrderBy(s => s.Service.ServiceName)
                    .Select(s =>
                        new TechnicianSpecializationResponseDto
                        {
                            ServiceId = s.ServiceId,
                            ServiceName =
                                s.Service.ServiceName,
                            Note = s.Note
                        })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    // Cập nhật thông tin chuyên môn của KTV
    public async Task<bool> UpdateProfileAsync(
        int userId,
        UpdateTechnicianProfileRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ExperienceYears is < 0 or > 60)
            throw new ArgumentException(
                "Số năm kinh nghiệm phải từ 0 đến 60.");

        if (request.Introduction?.Length > 2000)
            throw new ArgumentException(
                "Giới thiệu không vượt quá 2000 ký tự.");

        var technician = await _db.TechnicianProfile
            .Include(t => t.TechnicianSpecialization)
            .FirstOrDefaultAsync(t => t.UserId == userId);

        if (technician == null)
            return false;

        var ids = (request.ServiceIds ?? new List<int>())
            .Distinct()
            .ToList();

        if (ids.Any(id => id <= 0))
            throw new ArgumentException(
                "Danh sách ServiceId không hợp lệ.");

        var validCount = await _db.Service
            .CountAsync(s =>
                ids.Contains(s.ServiceId) && s.IsActive);

        if (validCount != ids.Count)
            throw new ArgumentException(
                "Có dịch vụ không tồn tại hoặc đã ngừng hoạt động.");

        technician.ExperienceYears = request.ExperienceYears;
        technician.Introduction = request.Introduction?.Trim();
        technician.UpdatedAt = DateTime.UtcNow;

        // Chỉ xóa chuyên môn không còn được chọn
        var removed = technician.TechnicianSpecialization
            .Where(s => !ids.Contains(s.ServiceId))
            .ToList();

        _db.TechnicianSpecialization.RemoveRange(removed);

        // Giữ nguyên chuyên môn cũ để không mất ghi chú
        var existingIds = technician.TechnicianSpecialization
            .Select(s => s.ServiceId)
            .ToHashSet();

        foreach (var serviceId in ids)
        {
            if (existingIds.Contains(serviceId))
                continue;

            _db.TechnicianSpecialization.Add(
                new TechnicianSpecialization
                {
                    TechnicianId = technician.TechnicianId,
                    ServiceId = serviceId,
                    CreatedAt = DateTime.UtcNow
                });
        }

        await _db.SaveChangesAsync();

        return true;
    }

    // Danh sách cửa hàng mà KTV đang hợp tác
    public async Task<List<TechnicianShopSummaryDto>>
        GetMyShopsAsync(int userId)
    {
        return await _db.TechnicianShop
            .AsNoTracking()
            .Where(ts =>
                ts.Technician.UserId == userId &&
                ts.Status == TechnicianShopStatus.Active &&
                ts.LeftAt == null)
            .OrderByDescending(ts => ts.IsPrimary)
            .ThenBy(ts => ts.Shop.ShopName)
            .Select(ts => new TechnicianShopSummaryDto
            {
                TechnicianShopId = ts.TechnicianShopId,
                ShopId = ts.ShopId,
                ShopName = ts.Shop.ShopName,
                LogoUrl = ts.Shop.LogoUrl,
                Phone = ts.Shop.Phone,
                Status = (int)ts.Status,
                IsPrimary = ts.IsPrimary,
                JoinedAt = ts.JoinedAt
            })
            .ToListAsync();
    }

    // Xem lịch làm việc của KTV
    public async Task<List<TechnicianScheduleDto>>
        GetMySchedulesAsync(
            int userId,
            DateTime? from,
            DateTime? to)
    {
        var query = _db.RepairSchedule
            .AsNoTracking()
            .Where(s =>
                s.Technician.UserId == userId &&
                s.IsCurrent);

        // Lịch giao với khoảng thời gian cần xem
        if (from.HasValue)
            query = query.Where(s => s.EndTime >= from.Value);

        if (to.HasValue)
            query = query.Where(s => s.StartTime <= to.Value);

        return await query
            .OrderBy(s => s.StartTime)
            .ThenBy(s => s.RepairScheduleId)
            .Select(s => new TechnicianScheduleDto
            {
                RepairScheduleId = s.RepairScheduleId,
                RepairRequestId = s.RepairRequestId,
                ShopId = s.RepairRequest.ShopId,
                ShopName = s.RepairRequest.Shop.ShopName,
                Description = s.RepairRequest.Description,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Status = s.Status,
                IsCurrent = s.IsCurrent
            })
            .ToListAsync();
    }
}

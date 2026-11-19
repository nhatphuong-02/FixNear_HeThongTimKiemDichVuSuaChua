using FixNear.Domain.Enums;
using FixNear.Domain.Models.Repairs;
using FixNear.Domain.Models.Notifications;
using FixNear.Domain.Models.ServiceCatalog;
using FixNear.Domain.Models.Shops;
using FixNear.Domain.Models.Technicians;
using FixNear.Enums;
using FixNear.Features.Manager.DTOs;
using FixNear.Features.Manager.Interfaces;
using FixNear.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Features.Manager.Services;

public class ManagerService : IManagerService
{
    private readonly FixNearDbContext _db;

    public ManagerService(FixNearDbContext db)
    {
        _db = db;
    }

    //Kiểm tra quyền
    private async Task EnsureShopAccessAsync(
    int userId,
    int shopId)
    {
        bool hasPermission = await _db.ShopMember
            .AnyAsync(sm =>
                sm.ShopId == shopId &&
                sm.Manager.UserId == userId &&
                sm.Status == ShopMemberStatus.Active &&
                sm.LeftAt == null &&
                (sm.ShopRole == ShopRole.Owner ||
                 sm.ShopRole == ShopRole.Manager));

        if (!hasPermission)
            throw new UnauthorizedAccessException(
                "Bạn không có quyền quản lý cửa hàng này.");
    }

    #region Quản lý nhiều ảnh cửa hàng

    public async Task<object> GetShopImagesAsync(
        int managerId, int shopId)
    {
        // managerId is the authenticated UserId supplied by the controller.
        await EnsureShopAccessAsync(managerId, shopId);
        return await _db.ShopImage
            .AsNoTracking()
            .Where(x =>
                x.ShopId == shopId)
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.DisplayOrder)
            .Select(x => new
            {
                x.ShopImageId,
                x.ShopId,
                x.ImageUrl,
                x.Caption,
                x.IsPrimary,
                x.DisplayOrder,
                x.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<object?> AddShopImageAsync(
        int managerId,
        int shopId,
        ShopImageDto dto)
    {
        // managerId is the authenticated UserId supplied by the controller.
        await EnsureShopAccessAsync(managerId, shopId);
        var ownsShop = await _db.Shop.AnyAsync(x =>
            x.ShopId == shopId);

        if (!ownsShop)
            return null;

        var maxOrder = await _db.ShopImage
            .Where(x => x.ShopId == shopId)
            .Select(x => (int?)x.DisplayOrder)
            .MaxAsync() ?? -1;

        var image = new ShopImage
        {
            ShopId = shopId,
            ImageUrl = dto.ImageUrl.Trim(),
            Caption = dto.Caption?.Trim(),
            IsPrimary = false,
            DisplayOrder = maxOrder + 1,
            CreatedAt = DateTime.UtcNow
        };

        _db.ShopImage.Add(image);
        await _db.SaveChangesAsync();

        return new
        {
            image.ShopImageId,
            image.ShopId,
            image.ImageUrl,
            image.Caption,
            image.IsPrimary
        };
    }

    public async Task<bool> UpdateShopImageAsync(
        int managerId,
        int shopId,
        int imageId,
        ShopImageDto dto)
    {
        // managerId is the authenticated UserId supplied by the controller.
        await EnsureShopAccessAsync(managerId, shopId);
        var image = await _db.ShopImage
            .FirstOrDefaultAsync(x =>
                x.ShopImageId == imageId &&
                x.ShopId == shopId);

        if (image == null)
            return false;

        image.ImageUrl = dto.ImageUrl.Trim();
        image.Caption = dto.Caption?.Trim();

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteShopImageAsync(
        int managerId,
        int shopId,
        int imageId)
    {
        // managerId is the authenticated UserId supplied by the controller.
        await EnsureShopAccessAsync(managerId, shopId);
        var image = await _db.ShopImage
            .FirstOrDefaultAsync(x =>
                x.ShopImageId == imageId &&
                x.ShopId == shopId);

        if (image == null)
            return false;

        _db.ShopImage.Remove(image);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> SetPrimaryImageAsync(
        int managerId,
        int shopId,
        int imageId)
    {
        // managerId is the authenticated UserId supplied by the controller.
        await EnsureShopAccessAsync(managerId, shopId);
        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        var images = await _db.ShopImage
            .Where(x =>
                x.ShopId == shopId)
            .ToListAsync();

        var selected = images.FirstOrDefault(x =>
            x.ShopImageId == imageId);

        if (selected == null)
            return false;

        if (selected.IsPrimary)
            return true;

        foreach (var image in images.Where(x => x.IsPrimary))
        {
            image.IsPrimary = false;
        }

        await _db.SaveChangesAsync();

        selected.IsPrimary = true;

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return true;
    }

    #endregion

    #region Quản lý dịch vụ của cửa hàng

    public async Task<object> GetSystemServicesAsync()
    {
        return await _db.Service
            .AsNoTracking()
            .Where(s => s.IsActive && s.Category.IsActive)
            .OrderBy(s => s.CategoryId)
            .ThenBy(s => s.ServiceName)
            .Select(s => new
            {
                s.ServiceId,
                s.ServiceName,
                s.Description,
                s.CategoryId,
                s.Category.CategoryName
            })
            .ToListAsync();
    }

    public async Task<object?> GetShopServicesAsync(
        int managerId, int shopId)
    {
        // managerId is the authenticated UserId supplied by the controller.
        await EnsureShopAccessAsync(managerId, shopId);
        bool ownsShop = await _db.Shop
            .AnyAsync(s => s.ShopId == shopId);

        if (!ownsShop)
            return null;

        return await _db.ShopService
            .AsNoTracking()
            .Where(ss => ss.ShopId == shopId)
            .OrderBy(ss => ss.ShopServiceId)
            .Select(ss => new
            {
                ss.ShopServiceId,
                ss.ShopId,
                ss.ServiceId,
                ss.Service.ServiceName,
                CategoryName = ss.Service.Category.CategoryName,
                ss.Description,
                ss.IsActive,
                ss.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<object?> GetShopServiceDetailAsync(
        int managerId, int shopId, int shopServiceId)
    {
        // managerId is the authenticated UserId supplied by the controller.
        await EnsureShopAccessAsync(managerId, shopId);
        return await _db.ShopService
            .AsNoTracking()
            .Where(ss =>
                ss.ShopServiceId == shopServiceId &&
                ss.ShopId == shopId)
            .Select(ss => new
            {
                ss.ShopServiceId,
                ss.ShopId,
                ss.ServiceId,
                ss.Service.ServiceName,
                ServiceDescription = ss.Service.Description,
                CategoryName = ss.Service.Category.CategoryName,
                ShopDescription = ss.Description,
                ss.IsActive,
                ss.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<object?> AddShopServiceAsync(
        int managerId, int shopId, AddShopServiceDto dto)
    {
        // managerId is the authenticated UserId supplied by the controller.
        await EnsureShopAccessAsync(managerId, shopId);
        bool ownsShop = await _db.Shop
            .AnyAsync(s => s.ShopId == shopId);

        if (!ownsShop)
            return null;

        bool serviceExists = await _db.Service
            .AnyAsync(s =>
                s.ServiceId == dto.ServiceId &&
                s.IsActive &&
                s.Category.IsActive);

        if (!serviceExists)
            throw new InvalidOperationException(
                "Dịch vụ không tồn tại hoặc đã ngừng hoạt động.");

        var existing = await _db.ShopService
            .FirstOrDefaultAsync(ss =>
                ss.ShopId == shopId &&
                ss.ServiceId == dto.ServiceId);

        if (existing != null)
        {
            if (existing.IsActive)
                throw new InvalidOperationException(
                    "Cửa hàng đã cung cấp dịch vụ này.");

            existing.IsActive = true;
            existing.Description = dto.Description?.Trim();

            await _db.SaveChangesAsync();

            return new
            {
                existing.ShopServiceId,
                existing.ShopId,
                existing.ServiceId,
                existing.Description,
                existing.IsActive,
                Reactivated = true
            };
        }

        var shopService = new ShopService
        {
            ShopId = shopId,
            ServiceId = dto.ServiceId,
            Description = dto.Description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.ShopService.Add(shopService);
        await _db.SaveChangesAsync();

        return new
        {
            shopService.ShopServiceId,
            shopService.ShopId,
            shopService.ServiceId,
            shopService.Description,
            shopService.IsActive,
            Reactivated = false
        };
    }

    public async Task<bool> UpdateShopServiceAsync(
        int managerId, int shopId, int shopServiceId,
        UpdateShopServiceDto dto)
    {
        // managerId is the authenticated UserId supplied by the controller.
        await EnsureShopAccessAsync(managerId, shopId);
        var shopService = await _db.ShopService
            .FirstOrDefaultAsync(ss =>
                ss.ShopServiceId == shopServiceId &&
                ss.ShopId == shopId);

        if (shopService == null)
            return false;

        shopService.Description = dto.Description?.Trim();

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetShopServiceStatusAsync(
        int managerId, int shopId, int shopServiceId,
        bool isActive)
    {
        // managerId is the authenticated UserId supplied by the controller.
        await EnsureShopAccessAsync(managerId, shopId);
        var shopService = await _db.ShopService
            .FirstOrDefaultAsync(ss =>
                ss.ShopServiceId == shopServiceId &&
                ss.ShopId == shopId);

        if (shopService == null)
            return false;

        if (isActive)
        {
            bool serviceAvailable = await _db.Service
                .AnyAsync(s =>
                    s.ServiceId == shopService.ServiceId &&
                    s.IsActive &&
                    s.Category.IsActive);

            if (!serviceAvailable)
                throw new InvalidOperationException(
                    "Dịch vụ hệ thống không còn hoạt động.");
        }

        shopService.IsActive = isActive;

        await _db.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Quản lý giá dịch vụ

    public async Task<object?> GetServicePricesAsync(
        int managerId, int shopId, bool includeHistory)
    {
        // managerId is the authenticated UserId supplied by the controller.
        await EnsureShopAccessAsync(managerId, shopId);
        bool ownsShop = await _db.Shop.AnyAsync(
            s => s.ShopId == shopId);

        if (!ownsShop)
            return null;

        return await _db.ServicePrice
            .AsNoTracking()
            .Where(p =>
                p.ShopService.ShopId == shopId &&
                (includeHistory || p.IsActive))
            .OrderBy(p => p.ShopServiceId)
            .ThenByDescending(p => p.EffectiveFrom)
            .Select(p => new
            {
                p.ServicePriceId,
                p.ShopServiceId,
                p.ShopService.ServiceId,
                p.ShopService.Service.ServiceName,
                p.Price,
                p.MinPrice,
                p.MaxPrice,
                p.PriceUnit,
                p.EffectiveFrom,
                p.EffectiveTo,
                p.IsActive,
                p.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<object?> GetServicePriceAsync(
        int managerId, int shopId, int servicePriceId)
    {
        // managerId is the authenticated UserId supplied by the controller.
        await EnsureShopAccessAsync(managerId, shopId);
        return await _db.ServicePrice
            .AsNoTracking()
            .Where(p =>
                p.ServicePriceId == servicePriceId &&
                p.ShopService.ShopId == shopId &&
                p.ShopService.ShopId == shopId)
            .Select(p => new
            {
                p.ServicePriceId,
                p.ShopServiceId,
                p.ShopService.ServiceId,
                p.ShopService.Service.ServiceName,
                p.Price,
                p.MinPrice,
                p.MaxPrice,
                p.PriceUnit,
                p.EffectiveFrom,
                p.EffectiveTo,
                p.IsActive,
                p.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<object?> AddServicePriceAsync(
        int managerId, int shopId, ServicePriceDto dto)
    {
        // managerId is the authenticated UserId supplied by the controller.
        await EnsureShopAccessAsync(managerId, shopId);
        var shopService = await _db.ShopService
            .AsNoTracking()
            .FirstOrDefaultAsync(ss =>
                ss.ShopServiceId == dto.ShopServiceId &&
                ss.ShopId == shopId);

        if (shopService == null)
            return null;

        bool available = await _db.Service.AnyAsync(s =>
            s.ServiceId == shopService.ServiceId &&
            s.IsActive &&
            s.Category.IsActive);

        if (!shopService.IsActive || !available)
            throw new InvalidOperationException(
                "Dịch vụ đã ngừng hoạt động, không thể thêm giá.");

        bool hasActivePrice = await _db.ServicePrice
            .AnyAsync(p =>
                p.ShopServiceId == dto.ShopServiceId &&
                p.IsActive);

        if (hasActivePrice)
            throw new InvalidOperationException(
                "Dịch vụ đã có giá hiện hành. Hãy dùng PUT để cập nhật.");

        var now = DateTime.UtcNow;

        var price = new ServicePrice
        {
            ShopServiceId = dto.ShopServiceId,
            Price = dto.Price,
            MinPrice = dto.MinPrice,
            MaxPrice = dto.MaxPrice,
            PriceUnit = dto.PriceUnit?.Trim(),
            EffectiveFrom = now,
            EffectiveTo = null,
            IsActive = true,
            CreatedAt = now
        };

        _db.ServicePrice.Add(price);
        await _db.SaveChangesAsync();

        return new
        {
            price.ServicePriceId,
            price.ShopServiceId,
            price.Price,
            price.MinPrice,
            price.MaxPrice,
            price.PriceUnit,
            price.EffectiveFrom,
            price.IsActive
        };
    }

    public async Task<object?> UpdateServicePriceAsync(
        int managerId,
        int shopId,
        int servicePriceId,
        UpdateServicePriceDto dto)
    {
        // managerId is the authenticated UserId supplied by the controller.
        await EnsureShopAccessAsync(managerId, shopId);
        var oldPrice = await _db.ServicePrice
            .Include(p => p.ShopService)
            .FirstOrDefaultAsync(p =>
                p.ServicePriceId == servicePriceId &&
                p.IsActive &&
                p.ShopService.ShopId == shopId &&
                p.ShopService.ShopId == shopId);

        if (oldPrice == null)
            return null;

        bool available = await _db.Service.AnyAsync(s =>
            s.ServiceId == oldPrice.ShopService.ServiceId &&
            s.IsActive &&
            s.Category.IsActive);

        if (!oldPrice.ShopService.IsActive || !available)
            throw new InvalidOperationException(
                "Dịch vụ đã ngừng hoạt động, không thể cập nhật giá.");

        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        var now = DateTime.UtcNow;

        oldPrice.IsActive = false;
        oldPrice.EffectiveTo = now;
        await _db.SaveChangesAsync();

        var newPrice = new ServicePrice
        {
            ShopServiceId = oldPrice.ShopServiceId,
            Price = dto.Price,
            MinPrice = dto.MinPrice,
            MaxPrice = dto.MaxPrice,
            PriceUnit = dto.PriceUnit?.Trim(),
            EffectiveFrom = now,
            EffectiveTo = null,
            IsActive = true,
            CreatedAt = now
        };

        _db.ServicePrice.Add(newPrice);
        await _db.SaveChangesAsync();

        await transaction.CommitAsync();

        return new
        {
            OldServicePriceId = oldPrice.ServicePriceId,
            NewServicePriceId = newPrice.ServicePriceId,
            newPrice.ShopServiceId,
            newPrice.Price,
            newPrice.MinPrice,
            newPrice.MaxPrice,
            newPrice.PriceUnit,
            newPrice.EffectiveFrom,
            newPrice.IsActive
        };
    }

    #endregion

    #region Quản lý kỹ thuật viên

    public async Task<object?> GetTechniciansAsync(
        int managerId, int shopId)
    {
        // managerId is the authenticated UserId supplied by the controller.
        await EnsureShopAccessAsync(managerId, shopId);
        bool ownsShop = await _db.Shop.AnyAsync(s =>
            s.ShopId == shopId);

        if (!ownsShop)
            return null;

        return await _db.TechnicianShop
            .AsNoTracking()
            .Where(ts =>
                ts.ShopId == shopId &&
                ts.LeftAt == null)
            .OrderBy(ts => ts.TechnicianId)
            .Select(ts => new
            {
                ts.TechnicianShopId,
                ts.TechnicianId,
                ts.ShopId,
                ts.Technician.UserId,
                ts.Technician.User.FullName,
                ts.Technician.ExperienceYears,
                ts.Technician.Introduction,
                ts.Technician.TechnicianStatus,
                ts.Status,
                ts.IsPrimary,
                ts.JoinedAt
            })
            .ToListAsync();
    }

    public async Task<object?> GetTechnicianAsync(
        int managerId, int shopId, int technicianId)
    {
        // managerId is the authenticated UserId supplied by the controller.
        await EnsureShopAccessAsync(managerId, shopId);
        return await _db.TechnicianShop
            .AsNoTracking()
            .Where(ts =>
                ts.ShopId == shopId &&
                ts.TechnicianId == technicianId &&
                ts.LeftAt == null &&
                ts.ShopId == shopId)
            .Select(ts => new
            {
                ts.TechnicianShopId,
                ts.TechnicianId,
                ts.ShopId,
                ts.Technician.UserId,
                ts.Technician.User.FullName,
                ts.Technician.ExperienceYears,
                ts.Technician.Introduction,
                ts.Technician.TechnicianStatus,
                ts.Status,
                ts.IsPrimary,
                ts.JoinedAt,
                ts.LeftAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<object?> AddTechnicianAsync(
        int managerId,
        int shopId,
        AddTechnicianToShopDto dto)
    {
        // managerId is the authenticated UserId supplied by the controller.
        await EnsureShopAccessAsync(managerId, shopId);
        bool ownsShop = await _db.Shop.AnyAsync(s =>
            s.ShopId == shopId);

        if (!ownsShop)
            return null;

        bool technicianExists = await _db.TechnicianProfile
            .AnyAsync(t => t.TechnicianId == dto.TechnicianId);

        if (!technicianExists)
            throw new InvalidOperationException(
                "Không tìm thấy hồ sơ kỹ thuật viên.");

        bool alreadyJoined = await _db.TechnicianShop
            .AnyAsync(ts =>
                ts.ShopId == shopId &&
                ts.TechnicianId == dto.TechnicianId &&
                ts.LeftAt == null);

        if (alreadyJoined)
            throw new InvalidOperationException(
                "Kỹ thuật viên đã thuộc cửa hàng này.");

        if (dto.IsPrimary)
        {
            bool hasPrimary = await _db.TechnicianShop
                .AnyAsync(ts =>
                    ts.TechnicianId == dto.TechnicianId &&
                    ts.LeftAt == null &&
                    ts.IsPrimary);

            if (hasPrimary)
                throw new InvalidOperationException(
                    "Kỹ thuật viên đã có cửa hàng chính đang hoạt động.");
        }

        var now = DateTime.UtcNow;

        var link = new TechnicianShop
        {
            ShopId = shopId,
            TechnicianId = dto.TechnicianId,
            Status = (TechnicianShopStatus)dto.Status,
            IsPrimary = dto.IsPrimary,
            JoinedAt = now,
            LeftAt = null,
            CreatedAt = now
        };

        _db.TechnicianShop.Add(link);
        await _db.SaveChangesAsync();

        return new
        {
            link.TechnicianShopId,
            link.ShopId,
            link.TechnicianId,
            link.Status,
            link.IsPrimary,
            link.JoinedAt,
            link.LeftAt
        };
    }

    public async Task<bool> UpdateTechnicianAsync(
        int managerId,
        int shopId,
        int technicianId,
        UpdateTechnicianShopDto dto)
    {
        // managerId is the authenticated UserId supplied by the controller.
        await EnsureShopAccessAsync(managerId, shopId);
        var link = await _db.TechnicianShop
            .FirstOrDefaultAsync(ts =>
                ts.ShopId == shopId &&
                ts.TechnicianId == technicianId &&
                ts.LeftAt == null &&
                ts.ShopId == shopId);

        if (link == null)
            return false;

        if (dto.IsPrimary)
        {
            bool otherPrimary = await _db.TechnicianShop
                .AnyAsync(ts =>
                    ts.TechnicianId == technicianId &&
                    ts.TechnicianShopId != link.TechnicianShopId &&
                    ts.LeftAt == null &&
                    ts.IsPrimary);

            if (otherPrimary)
                throw new InvalidOperationException(
                    "Kỹ thuật viên đang có cửa hàng chính khác.");
        }

        link.Status = (TechnicianShopStatus)dto.Status;
        link.IsPrimary = dto.IsPrimary;

        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RemoveTechnicianAsync(
        int managerId, int shopId, int technicianId)
    {
        // managerId is the authenticated UserId supplied by the controller.
        await EnsureShopAccessAsync(managerId, shopId);
        var link = await _db.TechnicianShop
            .FirstOrDefaultAsync(ts =>
                ts.ShopId == shopId &&
                ts.TechnicianId == technicianId &&
                ts.LeftAt == null &&
                ts.ShopId == shopId);

        if (link == null)
            return false;

        link.LeftAt = DateTime.UtcNow;
        link.IsPrimary = false;

        await _db.SaveChangesAsync();

        return true;
    }

    #endregion

    #region Xem danh sách và chi tiết yêu cầu sửa chữa

    public async Task<object?> GetRepairRequestsAsync(int userId, int shopId)
    {
        await EnsureShopAccessAsync(userId, shopId);
        return await _db.RepairRequest.AsNoTracking()
            .Where(r => r.ShopId == shopId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.RepairRequestId,
                r.CustomerId,
                r.ShopId,
                r.AddressId,
                r.RepairMethod,
                r.Description,
                r.PreferredDate,
                r.PreferredTimeFrom,
                r.PreferredTimeTo,
                r.Status,
                r.Priority,
                r.CreatedAt,
                r.UpdatedAt,
                Services = r.RepairRequestService.Select(sv => new
                {
                    sv.ServiceId,
                    sv.Service.ServiceName
                }).ToList()
            })
            .ToListAsync();
    }

    public async Task<object?> GetRepairRequestDetailAsync(
        int userId, int shopId, int repairRequestId)
    {
        await EnsureShopAccessAsync(userId, shopId);
        return await _db.RepairRequest.AsNoTracking()
            .Where(r => r.ShopId == shopId && r.RepairRequestId == repairRequestId)
            .Select(r => new
            {
                r.RepairRequestId,
                r.CustomerId,
                CustomerUserId = r.Customer.UserId,
                r.ShopId,
                r.AddressId,
                r.RepairMethod,
                r.Description,
                r.PreferredDate,
                r.PreferredTimeFrom,
                r.PreferredTimeTo,
                r.Status,
                r.Priority,
                r.CreatedAt,
                r.UpdatedAt,
                Services = r.RepairRequestService.Select(sv => new
                {
                    sv.ServiceId,
                    sv.Service.ServiceName
                }).ToList(),
                Images = r.RepairRequestImage.Select(img => new
                {
                    img.RepairRequestImageId,
                    img.ImageUrl
                }).ToList(),
                History = r.RepairRequestStatusHistory
                    .OrderByDescending(h => h.CreatedAt)
                    .Select(h => new
                    {
                        h.HistoryId,
                        h.OldStatus,
                        h.NewStatus,
                        h.Reason,
                        h.ChangedByUserId,
                        h.CreatedAt
                    }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    #endregion

    #region Tiếp nhận hoặc từ chối yêu cầu sửa chữa

    public Task<ManagerActionResult> AcceptRepairRequestAsync(
        int userId, int shopId, int repairRequestId, AcceptRepairRequestDto dto)
        => ChangePendingRequestAsync(userId, shopId, repairRequestId,
            RepairRequestStatus.Accepted,
            string.IsNullOrWhiteSpace(dto.Note)
                ? "Cửa hàng đã tiếp nhận yêu cầu."
                : dto.Note.Trim());

    public Task<ManagerActionResult> RejectRepairRequestAsync(
        int userId, int shopId, int repairRequestId, RejectRepairRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            return Task.FromResult(new ManagerActionResult(400,
                "Vui lòng nhập lý do từ chối."));
        return ChangePendingRequestAsync(userId, shopId, repairRequestId,
            RepairRequestStatus.Rejected, dto.Reason.Trim());
    }

    private async Task<ManagerActionResult> ChangePendingRequestAsync(
        int userId, int shopId, int repairRequestId,
        RepairRequestStatus newStatus, string reason)
    {
        await EnsureShopAccessAsync(userId, shopId);
        await using var transaction = await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);
        try
        {
            // Chặn xử lý lặp bằng điều kiện Pending ngay tại UPDATE.
            int updated = await _db.RepairRequest
                .Where(r => r.RepairRequestId == repairRequestId
                    && r.ShopId == shopId
                    && r.Status == (int)RepairRequestStatus.Pending)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(r => r.Status, (int)newStatus)
                    .SetProperty(r => r.UpdatedAt, DateTime.UtcNow));

            if (updated == 0)
            {
                var exists = await _db.RepairRequest.AsNoTracking()
                    .AnyAsync(r => r.RepairRequestId == repairRequestId
                        && r.ShopId == shopId);
                await transaction.RollbackAsync();
                return new ManagerActionResult(exists ? 409 : 404,
                    exists ? "Yêu cầu không còn Pending." : "Không tìm thấy yêu cầu.");
            }

            // SQL trigger đã chèn lịch sử. Không INSERT thêm để tránh trùng.
            var history = await _db.RepairRequestStatusHistory
                .Where(h => h.RepairRequestId == repairRequestId
                    && h.OldStatus == (int)RepairRequestStatus.Pending
                    && h.NewStatus == (int)newStatus)
                .OrderByDescending(h => h.HistoryId)
                .FirstOrDefaultAsync();
            if (history != null)
            {
                history.ChangedByUserId = userId;
                history.Reason = reason;
            }

            // Customer luôn được thông báo khi Manager tiếp nhận / từ chối.
            var info = await _db.RepairRequest.AsNoTracking()
                .Where(r => r.RepairRequestId == repairRequestId)
                .Select(r => new
                {
                    CustomerUserId = r.Customer.UserId,
                    r.Shop.ShopName
                })
                .SingleAsync();
            string code = newStatus == RepairRequestStatus.Accepted
                ? "REPAIR_REQUEST_ACCEPTED" : "REPAIR_REQUEST_REJECTED";
            await AddNotificationAsync(
                code, info.CustomerUserId, repairRequestId,
                new Dictionary<string, string>
                {
                    ["ShopName"] = info.ShopName,
                    ["RequestId"] = repairRequestId.ToString(),
                    ["Reason"] = reason
                });

            // Chỉ gửi đến KTV nếu thực sự đã có phân công còn hiệu lực.
            // Quy trình thông thường: phân công diễn ra sau khi Accept,
            // vì thế ASSIGNMENT_OFFERED sẽ thông báo đến KTV ở bước phân công.
            if (newStatus == RepairRequestStatus.Accepted)
            {
                var assignedUsers = await _db.RepairAssignment.AsNoTracking()
                    .Where(a => a.RepairRequestId == repairRequestId
                        && (a.AssignmentStatus == (int)RepairAssignmentStatus.Offered
                            || a.AssignmentStatus == (int)RepairAssignmentStatus.Accepted))
                    .Select(a => a.Technician.UserId)
                    .Distinct().ToListAsync();
                foreach (int technicianUserId in assignedUsers)
                {
                    await AddNotificationAsync("ASSIGNMENT_OFFERED",
                        technicianUserId, repairRequestId,
                        new Dictionary<string, string>
                        {
                            ["ShopName"] = info.ShopName,
                            ["RequestId"] = repairRequestId.ToString()
                        });
                }
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return new ManagerActionResult(200,
                newStatus == RepairRequestStatus.Accepted
                    ? "Tiếp nhận yêu cầu thành công."
                    : "Từ chối yêu cầu thành công.",
                new
                {
                    RepairRequestId = repairRequestId,
                    OldStatus = (int)RepairRequestStatus.Pending,
                    NewStatus = (int)newStatus,
                    Reason = reason
                });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    #endregion

    #region Phân công kỹ thuật viên
    public async Task<ManagerActionResult>
        AssignRepairTechnicianAsync(
            int managerId,
            int shopId,
            AssignTechnicianDto dto)
    {
        // managerId is the authenticated UserId supplied by the controller.
        await EnsureShopAccessAsync(managerId, shopId);
        var manager = new { UserId = managerId };

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

        var request = await _db.RepairRequest
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.RepairRequestId == dto.RepairRequestId &&
                r.ShopId == shopId);

        if (request == null)
        {
            return new ManagerActionResult(
                404,
                "Không tìm thấy yêu cầu sửa chữa.");
        }

        if (request.Status !=
            (int)RepairRequestStatus.Accepted)
        {
            return new ManagerActionResult(
                409,
                "Chỉ phân công yêu cầu đã được Shop tiếp nhận.");
        }

        bool isShopTechnician = await _db.TechnicianShop
            .AnyAsync(ts =>
                ts.ShopId == shopId &&
                ts.TechnicianId == dto.TechnicianId &&
                ts.LeftAt == null);

        if (!isShopTechnician)
        {
            return new ManagerActionResult(
                400,
                "Kỹ thuật viên không thuộc cửa hàng.");
        }

        bool hasActiveAssignment = await _db.RepairAssignment
            .AnyAsync(a =>
                a.RepairRequestId == dto.RepairRequestId &&
                (a.AssignmentStatus ==
                    (int)RepairAssignmentStatus.Offered ||
                 a.AssignmentStatus ==
                    (int)RepairAssignmentStatus.Accepted));

        if (hasActiveAssignment)
        {
            return new ManagerActionResult(
                409,
                "Yêu cầu đã có phân công đang hoạt động.");
        }

        var assignment = new RepairAssignment
        {
            RepairRequestId = dto.RepairRequestId,
            TechnicianId = dto.TechnicianId,
            AssignedByUserId = manager.UserId,
            AssignmentStatus =
                (int)RepairAssignmentStatus.Offered,
            AssignedAt = DateTime.UtcNow,
            Note = dto.Note?.Trim()
        };

        _db.RepairAssignment.Add(assignment);

        var notificationInfo = await _db.RepairRequest.AsNoTracking()
            .Where(r => r.RepairRequestId == dto.RepairRequestId)
            .Select(r => new { r.Shop.ShopName }).SingleAsync();
        var technicianUserId = await _db.TechnicianProfile.AsNoTracking()
            .Where(t => t.TechnicianId == dto.TechnicianId)
            .Select(t => t.UserId).SingleAsync();
        await AddNotificationAsync("ASSIGNMENT_OFFERED", technicianUserId,
            dto.RepairRequestId, new Dictionary<string, string>
            {
                ["ShopName"] = notificationInfo.ShopName,
                ["RequestId"] = dto.RepairRequestId.ToString()
            });

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return new ManagerActionResult(
            201,
            "Phân công thành công, chờ kỹ thuật viên nhận việc.",
            new
            {
                assignment.RepairAssignmentId,
                assignment.RepairRequestId,
                assignment.TechnicianId,
                assignment.AssignmentStatus,
                assignment.AssignedByUserId,
                assignment.AssignedAt
            });
    }

    #endregion

    #region Đổi kỹ thuật viên
    public async Task<ManagerActionResult>
        ReassignRepairTechnicianAsync(
            int managerId,
            int shopId,
            int assignmentId,
            ReassignTechnicianDto dto)
    {
        // managerId is the authenticated UserId supplied by the controller.
        await EnsureShopAccessAsync(managerId, shopId);
        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            return new ManagerActionResult(
                400,
                "Vui lòng nhập lý do đổi kỹ thuật viên.");
        }

        await EnsureShopAccessAsync(managerId, shopId);
        var manager = new { UserId = managerId };

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

        var oldAssignment = await _db.RepairAssignment
            .Include(a => a.RepairRequest)
            .FirstOrDefaultAsync(a =>
                a.RepairAssignmentId == assignmentId &&
                a.RepairRequest.ShopId == shopId);

        if (oldAssignment == null)
        {
            return new ManagerActionResult(
                404,
                "Không tìm thấy phân công.");
        }

        int oldStatus = oldAssignment.AssignmentStatus;

        if (oldStatus != (int)RepairAssignmentStatus.Offered &&
            oldStatus != (int)RepairAssignmentStatus.Accepted &&
            oldStatus != (int)RepairAssignmentStatus.Rejected)
        {
            return new ManagerActionResult(
                409,
                "Trạng thái phân công không cho phép đổi thợ.");
        }

        if (oldAssignment.TechnicianId == dto.TechnicianId)
        {
            return new ManagerActionResult(
                400,
                "Kỹ thuật viên mới phải khác kỹ thuật viên cũ.");
        }

        int requestStatus = oldAssignment.RepairRequest.Status;

        if (requestStatus != (int)RepairRequestStatus.Accepted &&
            requestStatus != (int)RepairRequestStatus.Assigned)
        {
            return new ManagerActionResult(
                409,
                "Yêu cầu hiện không cho phép đổi kỹ thuật viên.");
        }

        bool newTechnicianIsValid = await _db.TechnicianShop
            .AnyAsync(ts =>
                ts.ShopId == shopId &&
                ts.TechnicianId == dto.TechnicianId &&
                ts.LeftAt == null);

        if (!newTechnicianIsValid)
        {
            return new ManagerActionResult(
                400,
                "Kỹ thuật viên mới không thuộc cửa hàng.");
        }

        bool hasOtherActiveAssignment =
            await _db.RepairAssignment.AnyAsync(a =>
                a.RepairRequestId == oldAssignment.RepairRequestId &&
                a.RepairAssignmentId != assignmentId &&
                (a.AssignmentStatus ==
                    (int)RepairAssignmentStatus.Offered ||
                 a.AssignmentStatus ==
                    (int)RepairAssignmentStatus.Accepted));

        if (hasOtherActiveAssignment)
        {
            return new ManagerActionResult(
                409,
                "Yêu cầu đang có phân công hoạt động khác.");
        }

        bool hasCurrentSchedule = await _db.RepairSchedule
            .AnyAsync(s =>
                s.RepairRequestId == oldAssignment.RepairRequestId &&
                s.IsCurrent &&
                s.Status != (int)RepairScheduleStatus.Cancelled);

        if (hasCurrentSchedule)
        {
            return new ManagerActionResult(
                409,
                "Yêu cầu đã có lịch đang hiệu lực. Cần xử lý lịch trước khi đổi thợ.");
        }

        string reason = dto.Reason.Trim();

        if (oldStatus != (int)RepairAssignmentStatus.Rejected)
        {
            oldAssignment.AssignmentStatus =
                (int)RepairAssignmentStatus.Cancelled;

            string note = string.IsNullOrWhiteSpace(
                oldAssignment.Note)
                ? "Manager thu hồi phân công: " + reason
                : oldAssignment.Note +
                  " | Manager thu hồi phân công: " + reason;

            oldAssignment.Note = note.Length > 500
                ? note[..500]
                : note;
        }

        string newNote = "Phân công lại: " + reason;

        var newAssignment = new RepairAssignment
        {
            RepairRequestId = oldAssignment.RepairRequestId,
            TechnicianId = dto.TechnicianId,
            AssignedByUserId = manager.UserId,
            AssignmentStatus =
                (int)RepairAssignmentStatus.Offered,
            AssignedAt = DateTime.UtcNow,
            Note = newNote.Length > 500
                ? newNote[..500]
                : newNote
        };

        _db.RepairAssignment.Add(newAssignment);

        var newTechnicianUserId = await _db.TechnicianProfile.AsNoTracking()
            .Where(t => t.TechnicianId == dto.TechnicianId)
            .Select(t => t.UserId).SingleAsync();
        var shopName = await _db.Shop.AsNoTracking()
            .Where(s => s.ShopId == shopId)
            .Select(s => s.ShopName).SingleAsync();
        await AddNotificationAsync("ASSIGNMENT_OFFERED", newTechnicianUserId,
            oldAssignment.RepairRequestId, new Dictionary<string, string>
            {
                ["ShopName"] = shopName,
                ["RequestId"] = oldAssignment.RepairRequestId.ToString()
            });

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return new ManagerActionResult(
            201,
            "Đổi kỹ thuật viên thành công.",
            new
            {
                RepairRequestId = oldAssignment.RepairRequestId,
                OldAssignmentId = oldAssignment.RepairAssignmentId,
                OldTechnicianId = oldAssignment.TechnicianId,
                OldAssignmentStatus = oldAssignment.AssignmentStatus,
                NewAssignmentId = newAssignment.RepairAssignmentId,
                NewTechnicianId = newAssignment.TechnicianId,
                NewAssignmentStatus = newAssignment.AssignmentStatus,
                Reason = reason
            });
    }

    #endregion

    #region Xem va tao lich sua chua

    public async Task<object?> GetManagerRepairSchedulesAsync(
        int managerId,
        int shopId,
        bool includeHistory)
    {
        // managerId is the authenticated UserId supplied by the controller.
        await EnsureShopAccessAsync(managerId, shopId);
        bool ownsShop = await _db.Shop
            .AsNoTracking()
            .AnyAsync(s =>
                s.ShopId == shopId);

        if (!ownsShop)
            return null;

        var schedules = await _db.RepairSchedule
            .AsNoTracking()
            .Where(s =>
                s.RepairRequest.ShopId == shopId &&
                (includeHistory || s.IsCurrent))
            .OrderByDescending(s => s.StartTime)
            .Select(s => new
            {
                s.RepairScheduleId,
                s.RepairRequestId,
                s.TechnicianId,
                s.StartTime,
                s.EndTime,
                s.Status,
                s.IsCurrent,
                s.CreatedByUserId,
                s.CreatedAt,
                s.CancelledAt,
                s.CancelReason
            })
            .ToListAsync();

        return schedules;
    }

    #endregion

    public async Task<ManagerActionResult>
        CreateManagerRepairScheduleAsync(
            int managerId,
            int shopId,
            CreateRepairScheduleDto dto)
    {
        // managerId is the authenticated UserId supplied by the controller.
        await EnsureShopAccessAsync(managerId, shopId);
        var manager = new { UserId = managerId };

        if (dto.StartTime == default ||
            dto.EndTime == default ||
            dto.EndTime <= dto.StartTime)
        {
            return new ManagerActionResult(
                400,
                "Thời gian bắt đầu hoặc kết thúc không hợp lệ.");
        }

        DateTime startUtc = dto.StartTime.UtcDateTime;
        DateTime endUtc = dto.EndTime.UtcDateTime;

        if (startUtc <= DateTime.UtcNow)
        {
            return new ManagerActionResult(
                400,
                "Thời gian bắt đầu phải ở tương lai.");
        }

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

        var request = await _db.RepairRequest
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.RepairRequestId == dto.RepairRequestId &&
                r.ShopId == shopId);

        if (request == null)
        {
            return new ManagerActionResult(
                404,
                "Không tìm thấy yêu cầu sửa chữa.");
        }

        if (request.Status != (int)RepairRequestStatus.Assigned)
        {
            return new ManagerActionResult(
                409,
                "Yêu cầu phải ở trạng thái ASSIGNED để tạo lịch.");
        }

        bool belongsToShop = await _db.TechnicianShop
            .AnyAsync(ts =>
                ts.ShopId == shopId &&
                ts.TechnicianId == dto.TechnicianId &&
                ts.LeftAt == null);

        if (!belongsToShop)
        {
            return new ManagerActionResult(
                400,
                "Kỹ thuật viên không thuộc cửa hàng.");
        }

        bool acceptedAssignment = await _db.RepairAssignment
            .AnyAsync(a =>
                a.RepairRequestId == dto.RepairRequestId &&
                a.TechnicianId == dto.TechnicianId &&
                a.AssignmentStatus ==
                    (int)RepairAssignmentStatus.Accepted);

        if (!acceptedAssignment)
        {
            return new ManagerActionResult(
                409,
                "Kỹ thuật viên chưa chấp nhận phân công.");
        }

        bool hasExistingSchedule = await _db.RepairSchedule
            .AnyAsync(s =>
                s.RepairRequestId == dto.RepairRequestId);

        if (hasExistingSchedule)
        {
            return new ManagerActionResult(
                409,
                "Yêu cầu đã có lịch. API này không dùng để đổi lịch.");
        }

        bool overlaps = await _db.RepairSchedule
            .AnyAsync(s =>
                s.TechnicianId == dto.TechnicianId &&
                s.IsCurrent &&
                s.Status != (int)RepairScheduleStatus.Cancelled &&
                s.StartTime < endUtc &&
                s.EndTime > startUtc);

        if (overlaps)
        {
            return new ManagerActionResult(
                409,
                "Kỹ thuật viên đã có lịch trùng thời gian.");
        }

        var schedule = new RepairSchedule
        {
            RepairRequestId = dto.RepairRequestId,
            TechnicianId = dto.TechnicianId,
            StartTime = startUtc,
            EndTime = endUtc,
            Status = (int)RepairScheduleStatus.Scheduled,
            IsCurrent = true,
            CreatedByUserId = manager.UserId,
            CreatedAt = DateTime.UtcNow
        };

        _db.RepairSchedule.Add(schedule);

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return new ManagerActionResult(
            201,
            "Tạo lịch sửa chữa thành công.",
            new
            {
                schedule.RepairScheduleId,
                schedule.RepairRequestId,
                schedule.TechnicianId,
                schedule.StartTime,
                schedule.EndTime,
                schedule.Status,
                schedule.IsCurrent
            });
    }



    // Tạo Notification và UserNotification theo template có sẵn trong DB.
    // Phải gọi bên trong transaction nghiệp vụ; bên gọi thực hiện SaveChanges.
    private async Task AddNotificationAsync(
        string templateCode, int recipientUserId, int repairRequestId,
        IReadOnlyDictionary<string, string> values)
    {
        var template = await _db.NotificationTemplate.AsNoTracking()
            .SingleOrDefaultAsync(t => t.TemplateCode == templateCode);
        if (template == null)
            throw new InvalidOperationException(
                $"Thiếu NotificationTemplate: {templateCode}. Hãy kiểm tra DataFixNear.sql.");

        string body = template.BodyTemplate ?? string.Empty;
        foreach (var pair in values)
            body = body.Replace("{" + pair.Key + "}", pair.Value);

        var notification = new Notification
        {
            TemplateId = template.NotificationTemplateId,
            Title = template.Title,
            Body = body,
            // vì enum hiện tại mới khai báo TechnicianShopInvitation.
            RefType = (int)NotificationRefType.RepairRequest,
            RefId = repairRequestId,
            CreatedAt = DateTime.UtcNow
        };
        _db.Notification.Add(notification);
        _db.UserNotification.Add(new UserNotification
        {
            UserId = recipientUserId,
            Notification = notification,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
    }

}

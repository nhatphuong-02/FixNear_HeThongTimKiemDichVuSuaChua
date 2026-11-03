using FixNear.DTOs.Manager;
using FixNear.Models;
using FixNear.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Services;

public class ManagerService : IManagerService
{
    private readonly FixNearDbContext _db;

    public ManagerService(FixNearDbContext db)
    {
        _db = db;
    }

    #region Xem thông tin cửa hàng

    public async Task<object> GetShopsAsync(int managerId)
    {
        return await _db.Shop
            .AsNoTracking()
            .Where(x => x.ManagerId == managerId)
            .OrderBy(x => x.ShopName)
            .Select(x => new
            {
                x.ShopId,
                x.ShopName,
                x.Phone,
                x.Email,
                x.LogoUrl,
                x.Status,
                x.IsVerified
            })
            .ToListAsync();
    }

    public async Task<object?> GetShopAsync(
        int managerId, int shopId)
    {
        return await _db.Shop
            .AsNoTracking()
            .Where(x =>
                x.ManagerId == managerId &&
                x.ShopId == shopId)
            .Select(x => new
            {
                x.ShopId,
                x.ManagerId,
                x.ShopName,
                x.Description,
                x.Phone,
                x.Email,
                x.LogoUrl,
                x.TaxCode,
                x.AddressId,
                x.RatingAverage,
                x.Status,
                x.IsVerified,
                x.CreatedAt,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    #endregion

    #region Cập nhật thông tin cửa hàng

    public async Task<bool> UpdateShopAsync(
        int managerId,
        int shopId,
        UpdateShopDto dto)
    {
        var shop = await _db.Shop
            .FirstOrDefaultAsync(x =>
                x.ShopId == shopId &&
                x.ManagerId == managerId);

        if (shop == null)
            return false;

        if (string.IsNullOrWhiteSpace(dto.ShopName))
            throw new ArgumentException(
                "Tên cửa hàng không được để trống");

        shop.ShopName = dto.ShopName.Trim();
        shop.Description = dto.Description?.Trim();
        shop.Phone = dto.Phone?.Trim();
        shop.Email = dto.Email?.Trim();
        shop.LogoUrl = dto.LogoUrl?.Trim();
        shop.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return true;
    }

    #endregion


    #region Quản lý nhiều ảnh cửa hàng

    public async Task<object> GetShopImagesAsync(
        int managerId, int shopId)
    {
        return await _db.ShopImage
            .AsNoTracking()
            .Where(x =>
                x.ShopId == shopId &&
                x.Shop.ManagerId == managerId)
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
        var ownsShop = await _db.Shop.AnyAsync(x =>
            x.ShopId == shopId &&
            x.ManagerId == managerId);

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
        var image = await _db.ShopImage
            .FirstOrDefaultAsync(x =>
                x.ShopImageId == imageId &&
                x.ShopId == shopId &&
                x.Shop.ManagerId == managerId);

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
        var image = await _db.ShopImage
            .FirstOrDefaultAsync(x =>
                x.ShopImageId == imageId &&
                x.ShopId == shopId &&
                x.Shop.ManagerId == managerId);

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
        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        var images = await _db.ShopImage
            .Where(x =>
                x.ShopId == shopId &&
                x.Shop.ManagerId == managerId)
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

    #region Quan ly dich vu cua hang

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
        bool ownsShop = await _db.Shop
            .AnyAsync(s => s.ShopId == shopId &&
                           s.ManagerId == managerId);

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
        return await _db.ShopService
            .AsNoTracking()
            .Where(ss =>
                ss.ShopServiceId == shopServiceId &&
                ss.ShopId == shopId &&
                ss.Shop.ManagerId == managerId)
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
        bool ownsShop = await _db.Shop
            .AnyAsync(s => s.ShopId == shopId &&
                           s.ManagerId == managerId);

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
        var shopService = await _db.ShopService
            .FirstOrDefaultAsync(ss =>
                ss.ShopServiceId == shopServiceId &&
                ss.ShopId == shopId &&
                ss.Shop.ManagerId == managerId);

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
        var shopService = await _db.ShopService
            .FirstOrDefaultAsync(ss =>
                ss.ShopServiceId == shopServiceId &&
                ss.ShopId == shopId &&
                ss.Shop.ManagerId == managerId);

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


}

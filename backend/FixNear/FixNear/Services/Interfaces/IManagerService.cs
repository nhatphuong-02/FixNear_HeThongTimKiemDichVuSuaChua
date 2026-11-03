using FixNear.DTOs.Manager;

namespace FixNear.Services.Interfaces;

public interface IManagerService
{
    #region Xem thông tin cửa hàng

    Task<object> GetShopsAsync(int managerId);

    Task<object?> GetShopAsync(
        int managerId, int shopId);

    #endregion

    #region Cập nhật thông tin cửa hàng

    Task<bool> UpdateShopAsync(
        int managerId,
        int shopId,
        UpdateShopDto dto);

    #endregion

    #region Quản lý ảnh cửa hàng

    Task<object> GetShopImagesAsync(
        int managerId, int shopId);

    Task<object?> AddShopImageAsync(
        int managerId,
        int shopId,
        ShopImageDto dto);

    Task<bool> UpdateShopImageAsync(
        int managerId,
        int shopId,
        int imageId,
        ShopImageDto dto);

    Task<bool> DeleteShopImageAsync(
        int managerId,
        int shopId,
        int imageId);

    Task<bool> SetPrimaryImageAsync(
        int managerId,
        int shopId,
        int imageId);

    #endregion

    #region Quan ly dich vu cua hang

    Task<object> GetSystemServicesAsync();

    Task<object?> GetShopServicesAsync(
        int managerId, int shopId);

    Task<object?> GetShopServiceDetailAsync(
        int managerId, int shopId, int shopServiceId);

    Task<object?> AddShopServiceAsync(
        int managerId, int shopId, AddShopServiceDto dto);

    Task<bool> UpdateShopServiceAsync(
        int managerId, int shopId, int shopServiceId,
        UpdateShopServiceDto dto);

    Task<bool> SetShopServiceStatusAsync(
        int managerId, int shopId, int shopServiceId,
        bool isActive);

    #endregion

}
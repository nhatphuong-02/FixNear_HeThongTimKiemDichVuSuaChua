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
}
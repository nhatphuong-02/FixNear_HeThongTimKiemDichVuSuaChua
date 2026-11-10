using FixNear.DTOs.Manager;

namespace FixNear.Services.Interfaces;

public interface IManagerService
{
    #region T01 - Xem thông tin cửa hàng

    Task<object> GetShopsAsync(int managerId);

    Task<object?> GetShopAsync(
        int managerId, int shopId);

    #endregion

    #region T02 - Cập nhật thông tin cửa hàng

    Task<bool> UpdateShopAsync(
        int managerId,
        int shopId,
        UpdateShopDto dto);

    #endregion

    #region T03 - Quản lý ảnh cửa hàng

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

    #region T04 - Quản lý dịch vụ của cửa hàng

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

    #region T05 - Quản lý giá dịch vụ

    Task<object?> GetServicePricesAsync(
        int managerId, int shopId, bool includeHistory);

    Task<object?> GetServicePriceAsync(
        int managerId, int shopId, int servicePriceId);

    Task<object?> AddServicePriceAsync(
        int managerId, int shopId, ServicePriceDto dto);

    Task<object?> UpdateServicePriceAsync(
        int managerId, int shopId, int servicePriceId,
        UpdateServicePriceDto dto);

    #endregion

    #region T06 - Quản lý kỹ thuật viên

    Task<object?> GetTechniciansAsync(
        int managerId, int shopId);

    Task<object?> GetTechnicianAsync(
        int managerId, int shopId, int technicianId);

    Task<object?> AddTechnicianAsync(
        int managerId, int shopId, AddTechnicianToShopDto dto);

    Task<bool> UpdateTechnicianAsync(
        int managerId, int shopId, int technicianId,
        UpdateTechnicianShopDto dto);

    Task<bool> RemoveTechnicianAsync(
        int managerId, int shopId, int technicianId);

    #endregion

    #region T07 - Xem danh sach yeu cau sua chua

    Task<object?> GetRepairRequestsAsync(
        int managerId,
        int shopId);

    #endregion

    #region T08 - Tiep nhan yeu cau

    Task<ManagerActionResult> AcceptRepairRequestAsync(
        int managerId,
        int shopId,
        int repairRequestId,
        AcceptRepairRequestDto dto);

    #endregion

    #region T09 - Tu choi yeu cau

    Task<ManagerActionResult> RejectRepairRequestAsync(
        int managerId,
        int shopId,
        int repairRequestId,
        RejectRepairRequestDto dto);

    #endregion

    #region T10 - Phan cong ky thuat vien

    Task<ManagerActionResult> AssignRepairTechnicianAsync(
        int managerId,
        int shopId,
        AssignTechnicianDto dto);

    #endregion

    #region T11 - Doi ky thuat vien

    Task<ManagerActionResult> ReassignRepairTechnicianAsync(
        int managerId,
        int shopId,
        int assignmentId,
        ReassignTechnicianDto dto);

    #endregion

    #region T12 - Lich sua chua

    Task<object?> GetManagerRepairSchedulesAsync(
        int managerId,
        int shopId,
        bool includeHistory);

    Task<ManagerActionResult> CreateManagerRepairScheduleAsync(
        int managerId,
        int shopId,
        CreateRepairScheduleDto dto);

    #endregion


}
using FixNear.Features.Shops.DTOs;

namespace FixNear.Features.Shops.Interfaces;

public interface IShopService
{
    // Danh sách Shop mà tài khoản hiện tại quản lý
    Task<List<ShopListResponseDto>> GetMyShopsAsync(int userId);

    // Shop Profile
    Task<ShopProfileResponseDto> GetShopProfileAsync(int userId, int shopId);
    Task UpdateShopProfileAsync(int userId, int shopId, UpdateShopProfileRequestDto request);

    // Shop Members
    Task<List<ShopMemberResponseDto>> GetShopMembersAsync(int userId, int shopId);
    Task<ShopMemberResponseDto> GetShopMemberDetailAsync(int userId, int shopId, int shopMemberId);
    Task UpdateShopMemberAsync(int userId, int shopId, int shopMemberId, UpdateShopMemberRequestDto request);
    Task RemoveShopMemberAsync(int userId, int shopId, int shopMemberId);

    // Shop Technicians
    Task<List<ShopTechnicianResponseDto>> GetShopTechniciansAsync(int userId, int shopId);
    Task<ShopTechnicianDetailResponseDto> GetShopTechnicianDetailAsync(int userId, int shopId, int technicianShopId);
    Task RemoveTechnicianAsync(int userId, int shopId, int technicianShopId);

    // Technician Invitations
    Task InviteTechnicianAsync(int userId, int shopId, InviteTechnicianRequestDto request);
    Task<List<TechnicianInvitationResponseDto>> GetTechnicianInvitationsAsync(int userId, int shopId);
    Task CancelTechnicianInvitationAsync(int userId, int shopId, int invitationId);

    //Nearby shop
    Task<List<NearbyShopResponseDto>> GetNearbyShopsByServiceAsync(int serviceId, NearbyShopRequestDto request);

    // Tìm tất cả cửa hàng đang hoạt động gần vị trí GPS
    Task<List<NearbyShopResponseDto>> GetAllNearbyShopsAsync(NearbyShopRequestDto request);


}
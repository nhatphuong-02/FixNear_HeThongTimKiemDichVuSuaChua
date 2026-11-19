
using FixNear.Domain.Enums;
using FixNear.Domain.Models.Notifications;
using FixNear.Domain.Models.ServiceCatalog;
using FixNear.Domain.Models.Technicians;
using FixNear.Enums;
using FixNear.Features.Shops.DTOs;
using FixNear.Features.Shops.Interfaces;
using FixNear.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace FixNear.Features.Shops.Services;

public class ShopService : IShopService
{
    private readonly FixNearDbContext _context;

    public ShopService(FixNearDbContext context)
    {
        _context = context;
    }


    // Kiểm tra quyền quản lý Shop
    private async Task<ShopRole> GetAuthorizedShopMemberAsync(
        int userId,
        int shopId,
        params ShopRole[] allowedRoles)
    {
        var member = await _context.ShopMember
            .AsNoTracking()
            .FirstOrDefaultAsync(sm =>
                sm.ShopId == shopId &&
                sm.Manager.UserId == userId &&
                sm.Status == ShopMemberStatus.Active &&
                sm.LeftAt == null &&
                allowedRoles.Contains(sm.ShopRole));

        if (member == null)
            throw new UnauthorizedAccessException(
                "Bạn không có quyền thực hiện thao tác trên cửa hàng này.");

        return member.ShopRole;
    }

    // Đồng bộ thành viên Manager của các hội thoại thuộc Shop.
    // Phải gọi trong cùng transaction với thay đổi ShopMember.
    private async Task SyncConversationManagersAsync(int shopId, DateTime now)
    {
        var validUserIds = await _context.ShopMember
            .AsNoTracking()
            .Where(m => m.ShopId == shopId &&
                m.Status == ShopMemberStatus.Active && m.LeftAt == null &&
                (m.ShopRole == ShopRole.Owner || m.ShopRole == ShopRole.Manager))
            .Select(m => m.Manager.UserId)
            .Distinct()
            .ToListAsync();

        var conversations = await _context.Conversation
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.Status == 1 &&
                c.RepairRequest != null && c.RepairRequest.ShopId == shopId)
            .Select(c => c.ConversationId)
            .ToListAsync();

        if (conversations.Count == 0) return;

        var members = await _context.ConversationMember
            .Where(m => conversations.Contains(m.ConversationId))
            .ToListAsync();

        // Chỉ thay đổi tư cách thành viên của tài khoản ManagerProfile;
        // KHÔNG đụng Customer và Technician trong cùng hội thoại.
        var managerUserIds = await _context.ManagerProfile
            .AsNoTracking().Select(x => x.UserId).ToListAsync();
        var managerSet = managerUserIds.ToHashSet();
        var validSet = validUserIds.ToHashSet();
        var existing = members.Select(m => (m.ConversationId, m.UserId)).ToHashSet();

        foreach (var member in members.Where(m => managerSet.Contains(m.UserId)))
        {
            if (validSet.Contains(member.UserId))
            {
                if (member.LeftAt != null)
                {
                    member.LeftAt = null;
                    member.JoinedAt = now;
                    member.LastReadAt = now;
                }
            }
            else if (member.LeftAt == null) member.LeftAt = now;
        }

        foreach (int conversationId in conversations)
            foreach (int id in validUserIds)
                if (!existing.Contains((conversationId, id)))
                    _context.ConversationMember.Add(new FixNear.Domain.Models.Messaging.ConversationMember
                    {
                        ConversationId = conversationId,
                        UserId = id,
                        JoinedAt = now,
                        CreatedAt = now,
                        LastReadAt = now
                    });
    }

    // Chuyển DateTime sang UTC.
    // API cần gửi thời gian có hậu tố Z.
    private static DateTime EnsureUtc(DateTime value)
    {
        if (value == default || value.Kind != DateTimeKind.Utc)
            throw new ArgumentException(
                "Thời gian phải được gửi theo UTC (ISO 8601 có hậu tố Z).");

        return value;
    }


    // Tính khoảng cách giữa hai tọa độ
    private static double CalculateDistanceKm(
        double lat1,
        double lon1,
        double lat2,
        double lon2)
    {
        const double earthRadiusKm = 6371;

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a =
            Math.Pow(Math.Sin(dLat / 2), 2) +
            Math.Cos(ToRadians(lat1)) *
            Math.Cos(ToRadians(lat2)) *
            Math.Pow(Math.Sin(dLon / 2), 2);

        var c = 2 * Math.Atan2(
            Math.Sqrt(a),
            Math.Sqrt(1 - a));

        return earthRadiusKm * c;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }

    // =========================================================
    // SHOP MANAGEMENT
    // =========================================================

    // Lấy danh sách Shop mà tài khoản hiện tại quản lý
    public async Task<List<ShopListResponseDto>> GetMyShopsAsync(
        int userId)
    {
        return await _context.ShopMember
            .AsNoTracking()
            .Where(sm =>
                sm.Manager.UserId == userId &&
                sm.Status == ShopMemberStatus.Active &&
                sm.LeftAt == null &&
                (sm.ShopRole == ShopRole.Owner ||
                 sm.ShopRole == ShopRole.Manager))
            .OrderBy(sm => sm.JoinedAt)
            .Select(sm => new ShopListResponseDto
            {
                ShopId = sm.Shop.ShopId,
                ShopName = sm.Shop.ShopName,
                Description = sm.Shop.Description,
                LogoUrl = sm.Shop.LogoUrl,
                Phone = sm.Shop.Phone,
                Email = sm.Shop.Email,
                IsVerified = sm.Shop.IsVerified,
                CurrentUserRole = sm.ShopRole,
                JoinedAt = sm.JoinedAt
            })
            .ToListAsync();
    }

    // Xem hồ sơ Shop
    public async Task<ShopProfileResponseDto> GetShopProfileAsync(
        int userId,
        int shopId)
    {
        var currentRole = await GetAuthorizedShopMemberAsync(
            userId,
            shopId,
            ShopRole.Owner,
            ShopRole.Manager);

        var shop = await _context.Shop
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ShopId == shopId);

        if (shop == null)
            throw new KeyNotFoundException("Không tìm thấy cửa hàng.");

        return new ShopProfileResponseDto
        {
            ShopId = shop.ShopId,
            ManagerId = shop.ManagerId,
            AddressId = shop.AddressId,
            ShopName = shop.ShopName,
            Description = shop.Description,
            Phone = shop.Phone,
            Email = shop.Email,
            LogoUrl = shop.LogoUrl,
            TaxCode = shop.TaxCode,
            RatingAverage = shop.RatingAverage,
            Status = shop.Status,
            IsVerified = shop.IsVerified,
            CreatedAt = shop.CreatedAt,
            UpdatedAt = shop.UpdatedAt,
            CurrentUserRole = currentRole
        };
    }

    // Cập nhật hồ sơ Shop
    public async Task UpdateShopProfileAsync(
        int userId,
        int shopId,
        UpdateShopProfileRequestDto request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.ShopName))
            throw new ArgumentException(
                "Tên cửa hàng không được để trống.");

        await GetAuthorizedShopMemberAsync(
            userId,
            shopId,
            ShopRole.Owner,
            ShopRole.Manager);

        var shop = await _context.Shop
            .FirstOrDefaultAsync(s => s.ShopId == shopId);

        if (shop == null)
            throw new KeyNotFoundException("Không tìm thấy cửa hàng.");

        shop.ShopName = request.ShopName.Trim();
        shop.Description = request.Description?.Trim();
        shop.LogoUrl = request.LogoUrl?.Trim();
        shop.TaxCode = request.TaxCode?.Trim();
        shop.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    // =========================================================
    // SHOP MEMBER MANAGEMENT
    // =========================================================

    // Xem danh sách thành viên Shop
    public async Task<List<ShopMemberResponseDto>> GetShopMembersAsync(
        int userId,
        int shopId)
    {
        await GetAuthorizedShopMemberAsync(
            userId,
            shopId,
            ShopRole.Owner,
            ShopRole.Manager);

        return await _context.ShopMember
            .AsNoTracking()
            .Where(sm =>
                sm.ShopId == shopId &&
                sm.Status == ShopMemberStatus.Active &&
                sm.LeftAt == null)
            .OrderBy(sm => sm.ShopRole)
            .ThenBy(sm => sm.JoinedAt)
            .Select(sm => new ShopMemberResponseDto
            {
                ShopMemberId = sm.ShopMemberId,
                ManagerId = sm.ManagerId,
                UserId = sm.Manager.UserId,
                FullName = sm.Manager.User.FullName,
                AvatarUrl = sm.Manager.User.AvatarUrl,
                Email = sm.Manager.User.Email,
                ShopRole = sm.ShopRole,
                Status = sm.Status,
                JoinedAt = sm.JoinedAt
            })
            .ToListAsync();
    }

    // Xem chi tiết thành viên Shop
    public async Task<ShopMemberResponseDto> GetShopMemberDetailAsync(
        int userId,
        int shopId,
        int shopMemberId)
    {
        await GetAuthorizedShopMemberAsync(
            userId,
            shopId,
            ShopRole.Owner,
            ShopRole.Manager);

        var member = await _context.ShopMember
            .AsNoTracking()
            .Where(sm =>
                sm.ShopMemberId == shopMemberId &&
                sm.ShopId == shopId)
            .Select(sm => new ShopMemberResponseDto
            {
                ShopMemberId = sm.ShopMemberId,
                ManagerId = sm.ManagerId,
                UserId = sm.Manager.UserId,
                FullName = sm.Manager.User.FullName,
                AvatarUrl = sm.Manager.User.AvatarUrl,
                Email = sm.Manager.User.Email,
                ShopRole = sm.ShopRole,
                Status = sm.Status,
                JoinedAt = sm.JoinedAt
            })
            .FirstOrDefaultAsync();

        if (member == null)
            throw new KeyNotFoundException(
                "Không tìm thấy thành viên trong Shop.");

        return member;
    }

    // Cập nhật vai trò thành viên - chỉ Owner
    public async Task UpdateShopMemberAsync(
        int userId,
        int shopId,
        int shopMemberId,
        UpdateShopMemberRequestDto request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        await GetAuthorizedShopMemberAsync(
            userId,
            shopId,
            ShopRole.Owner);

        if (request.ShopRole != ShopRole.Manager &&
            request.ShopRole != ShopRole.Staff)
        {
            throw new ArgumentException("Vai trò mới không hợp lệ.");
        }

        var targetMember = await _context.ShopMember
            .FirstOrDefaultAsync(sm =>
                sm.ShopMemberId == shopMemberId &&
                sm.ShopId == shopId &&
                sm.Status == ShopMemberStatus.Active &&
                sm.LeftAt == null);

        if (targetMember == null)
            throw new KeyNotFoundException(
                "Không tìm thấy thành viên đang hoạt động trong Shop.");

        if (targetMember.ShopRole == ShopRole.Owner)
            throw new InvalidOperationException(
                "Không thể thay đổi vai trò Owner bằng chức năng này.");

        await using var tx = await _context.Database.BeginTransactionAsync();
        targetMember.ShopRole = request.ShopRole;
        await _context.SaveChangesAsync();
        await SyncConversationManagersAsync(shopId, DateTime.UtcNow);
        await _context.SaveChangesAsync();
        await tx.CommitAsync();
    }

    // Loại bỏ thành viên Shop
    public async Task RemoveShopMemberAsync(
        int userId,
        int shopId,
        int shopMemberId)
    {
        var currentRole = await GetAuthorizedShopMemberAsync(
            userId,
            shopId,
            ShopRole.Owner,
            ShopRole.Manager);

        var targetMember = await _context.ShopMember
            .FirstOrDefaultAsync(sm =>
                sm.ShopMemberId == shopMemberId &&
                sm.ShopId == shopId &&
                sm.Status == ShopMemberStatus.Active &&
                sm.LeftAt == null);

        if (targetMember == null)
            throw new KeyNotFoundException(
                "Không tìm thấy thành viên đang hoạt động trong Shop.");

        if (targetMember.ShopRole == ShopRole.Owner)
            throw new InvalidOperationException(
                "Không thể loại bỏ Owner bằng chức năng này.");

        // Manager chỉ được loại bỏ Staff
        if (currentRole == ShopRole.Manager &&
            targetMember.ShopRole != ShopRole.Staff)
        {
            throw new UnauthorizedAccessException(
                "Manager chỉ có thể loại bỏ thành viên Staff.");
        }

        await using var tx = await _context.Database.BeginTransactionAsync();
        var now = DateTime.UtcNow;
        targetMember.Status = ShopMemberStatus.Inactive;
        targetMember.LeftAt = now;
        await _context.SaveChangesAsync();
        await SyncConversationManagersAsync(shopId, now);
        await _context.SaveChangesAsync();
        await tx.CommitAsync();
    }

    // =========================================================
    // TECHNICIAN MANAGEMENT
    // =========================================================

    // Xem danh sách Technician đang hợp tác với Shop
    public async Task<List<ShopTechnicianResponseDto>> GetShopTechniciansAsync(
        int userId,
        int shopId)
    {
        await GetAuthorizedShopMemberAsync(
            userId,
            shopId,
            ShopRole.Owner,
            ShopRole.Manager);

        return await _context.TechnicianShop
            .AsNoTracking()
            .Where(ts =>
                ts.ShopId == shopId &&
                ts.Status == TechnicianShopStatus.Active &&
                ts.LeftAt == null)
            .OrderByDescending(ts => ts.IsPrimary)
            .ThenBy(ts => ts.JoinedAt)
            .Select(ts => new ShopTechnicianResponseDto
            {
                TechnicianShopId = ts.TechnicianShopId,
                TechnicianId = ts.TechnicianId,
                UserId = ts.Technician.UserId,
                FullName = ts.Technician.User.FullName,
                AvatarUrl = ts.Technician.User.AvatarUrl,
                Email = ts.Technician.User.Email,
                ExperienceYears = ts.Technician.ExperienceYears,
                Introduction = ts.Technician.Introduction,
                RatingAverage = ts.Technician.RatingAverage,
                IsPrimary = ts.IsPrimary,
                JoinedAt = ts.JoinedAt
            })
            .ToListAsync();
    }

    // Xem chi tiết Technician thuộc Shop
    public async Task<ShopTechnicianDetailResponseDto>
        GetShopTechnicianDetailAsync(
            int userId,
            int shopId,
            int technicianShopId)
    {
        await GetAuthorizedShopMemberAsync(
            userId,
            shopId,
            ShopRole.Owner,
            ShopRole.Manager);

        var technician = await _context.TechnicianShop
            .AsNoTracking()
            .Where(ts =>
                ts.TechnicianShopId == technicianShopId &&
                ts.ShopId == shopId)
            .Select(ts => new ShopTechnicianDetailResponseDto
            {
                TechnicianShopId = ts.TechnicianShopId,
                TechnicianId = ts.TechnicianId,
                UserId = ts.Technician.UserId,
                FullName = ts.Technician.User.FullName,
                AvatarUrl = ts.Technician.User.AvatarUrl,
                Email = ts.Technician.User.Email,
                ExperienceYears = ts.Technician.ExperienceYears,
                Introduction = ts.Technician.Introduction,
                TechnicianStatus = ts.Technician.TechnicianStatus,
                RatingAverage = ts.Technician.RatingAverage,
                IsPrimary = ts.IsPrimary,
                JoinedAt = ts.JoinedAt,
                LeftAt = ts.LeftAt
            })
            .FirstOrDefaultAsync();

        if (technician == null)
            throw new KeyNotFoundException(
                "Không tìm thấy kỹ thuật viên trong Shop.");

        return technician;
    }

    // Shop loại Technician khỏi danh sách hợp tác
    public async Task RemoveTechnicianAsync(
        int userId,
        int shopId,
        int technicianShopId)
    {
        await GetAuthorizedShopMemberAsync(
            userId,
            shopId,
            ShopRole.Owner,
            ShopRole.Manager);

        var technicianShop = await _context.TechnicianShop
            .FirstOrDefaultAsync(ts =>
                ts.TechnicianShopId == technicianShopId &&
                ts.ShopId == shopId &&
                ts.Status == TechnicianShopStatus.Active &&
                ts.LeftAt == null);

        if (technicianShop == null)
            throw new KeyNotFoundException(
                "Không tìm thấy kỹ thuật viên đang hợp tác với Shop.");

        var now = DateTime.UtcNow;

        technicianShop.Status = TechnicianShopStatus.Inactive;
        technicianShop.LeftAt = now;
        technicianShop.IsPrimary = false;

        await _context.SaveChangesAsync();
    }

    // =========================================================
    // TECHNICIAN INVITATION
    // =========================================================

    // Mời Technician đã có tài khoản bằng thông báo trong ứng dụng
    public async Task InviteTechnicianAsync(
        int userId,
        int shopId,
        InviteTechnicianRequestDto request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email không được để trống.");

        var email = request.Email.Trim().ToLowerInvariant();

        var currentRole = await GetAuthorizedShopMemberAsync(
            userId,
            shopId,
            ShopRole.Owner,
            ShopRole.Manager);

        var currentMember = await _context.ShopMember
            .FirstAsync(sm =>
                sm.ShopId == shopId &&
                sm.Manager.UserId == userId &&
                sm.Status == ShopMemberStatus.Active &&
                sm.LeftAt == null &&
                sm.ShopRole == currentRole);

        // Chỉ mời tài khoản đã tồn tại
        var existingUser = await _context.User
            .FirstOrDefaultAsync(u =>
                u.Email != null &&
                u.Email.ToLower() == email);

        if (existingUser == null)
            throw new KeyNotFoundException(
                "Không tìm thấy tài khoản với email này. Technician cần đăng ký tài khoản trước.");

        if (existingUser.RoleType != (int)RoleType.Technician)
            throw new InvalidOperationException(
                "Email này đã được sử dụng bởi tài khoản không phải kỹ thuật viên.");

        var technician = await _context.TechnicianProfile
            .FirstOrDefaultAsync(t => t.UserId == existingUser.UserId);

        if (technician == null)
            throw new InvalidOperationException(
                "Không tìm thấy hồ sơ kỹ thuật viên.");

        // Kiểm tra Technician đã hợp tác với Shop nào chưa
        var existingRelationship = await _context.TechnicianShop
            .AnyAsync(ts =>
                ts.TechnicianId == technician.TechnicianId &&
                ts.Status == TechnicianShopStatus.Active &&
                ts.LeftAt == null);

        if (existingRelationship)
            throw new InvalidOperationException(
                "Kỹ thuật viên hiện đang hợp tác với một cửa hàng khác hoặc cửa hàng này.");

        var now = DateTime.UtcNow;

        // Tự động chuyển lời mời cũ hết hạn sang Expired
        var expiredInvitations = await _context.TechnicianShopInvitation
            .Where(i =>
                i.ShopId == shopId &&
                i.Email == email &&
                i.Status == TechnicianShopInvitationStatus.Pending &&
                i.ExpiresAt <= now)
            .ToListAsync();

        foreach (var expired in expiredInvitations)
        {
            expired.Status = TechnicianShopInvitationStatus.Expired;
            expired.RespondedAt = now;
        }

        if (expiredInvitations.Count > 0)
            await _context.SaveChangesAsync();

        // Không cho phép gửi lời mời trùng khi còn Pending
        var pendingInvitation = await _context.TechnicianShopInvitation
            .AnyAsync(i =>
                i.ShopId == shopId &&
                i.Email == email &&
                i.Status == TechnicianShopInvitationStatus.Pending &&
                i.ExpiresAt > now);

        if (pendingInvitation)
            throw new InvalidOperationException(
                "Lời mời đang chờ phản hồi.");

        var shopName = await _context.Shop
            .Where(s => s.ShopId == shopId)
            .Select(s => s.ShopName)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(shopName))
            throw new KeyNotFoundException(
                "Không tìm thấy thông tin Shop.");

        var template = await _context.NotificationTemplate
            .FirstOrDefaultAsync(t =>
                t.TemplateCode == "TECHNICIAN_SHOP_INVITATION");

        if (template == null)
            throw new InvalidOperationException(
                "Không tìm thấy mẫu thông báo lời mời Technician.");

        var expiresAt = now.AddDays(7);

        var invitation = new TechnicianShopInvitation
        {
            ShopId = shopId,
            TechnicianId = technician.TechnicianId,
            Email = email,
            InvitedBy = currentMember.ManagerId,
            TokenHash = null,
            Status = TechnicianShopInvitationStatus.Pending,
            ExpiresAt = expiresAt,
            CreatedAt = now
        };

        var body = template.BodyTemplate?
            .Replace("{ShopName}", shopName);

        var notification = new Notification
        {
            TemplateId = template.NotificationTemplateId,
            Title = template.Title,
            Body = body,
            RefType = (int)NotificationRefType.TechnicianShopInvitation,
            CreatedAt = now
        };

        var userNotification = new UserNotification
        {
            UserId = existingUser.UserId,
            IsRead = false,
            CreatedAt = now,
            Notification = notification
        };

        // Invitation và Notification phải được lưu cùng nhau
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            _context.TechnicianShopInvitation.Add(invitation);
            await _context.SaveChangesAsync();

            notification.RefId = invitation.InvitationId;

            _context.Notification.Add(notification);
            _context.UserNotification.Add(userNotification);

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // Lấy danh sách lời mời đã gửi
    public async Task<List<TechnicianInvitationResponseDto>>
        GetTechnicianInvitationsAsync(
            int userId,
            int shopId)
    {
        await GetAuthorizedShopMemberAsync(
            userId,
            shopId,
            ShopRole.Owner,
            ShopRole.Manager);

        return await _context.TechnicianShopInvitation
            .AsNoTracking()
            .Where(i => i.ShopId == shopId)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new TechnicianInvitationResponseDto
            {
                InvitationId = i.InvitationId,
                ShopId = i.ShopId,
                TechnicianId = i.TechnicianId,
                Email = i.Email,
                TechnicianName = i.Technician != null
                    ? i.Technician.User.FullName
                    : null,
                InvitedByName = i.InvitedByNavigation.User.FullName,
                Status = (int)i.Status,
                ExpiresAt = i.ExpiresAt,
                RespondedAt = i.RespondedAt,
                CreatedAt = i.CreatedAt
            })
            .ToListAsync();
    }

    // Hủy lời mời Technician
    public async Task CancelTechnicianInvitationAsync(
        int userId,
        int shopId,
        int invitationId)
    {
        await GetAuthorizedShopMemberAsync(
            userId,
            shopId,
            ShopRole.Owner,
            ShopRole.Manager);

        var invitation = await _context.TechnicianShopInvitation
            .FirstOrDefaultAsync(i =>
                i.InvitationId == invitationId &&
                i.ShopId == shopId);

        if (invitation == null)
            throw new KeyNotFoundException("Không tìm thấy lời mời.");

        if (invitation.Status != TechnicianShopInvitationStatus.Pending)
            throw new InvalidOperationException(
                "Chỉ có thể hủy lời mời đang chờ phản hồi.");

        var now = DateTime.UtcNow;

        if (invitation.ExpiresAt <= now)
        {
            invitation.Status = TechnicianShopInvitationStatus.Expired;
            invitation.RespondedAt = now;

            await _context.SaveChangesAsync();

            throw new InvalidOperationException(
                "Lời mời đã hết hạn.");
        }

        invitation.Status = TechnicianShopInvitationStatus.Cancelled;
        invitation.RespondedAt = now;

        await _context.SaveChangesAsync();
    }

    // =========================================================
    // SHOP DISCOVERY
    // =========================================================

    // Tìm kiếm cửa hàng ở gần theo dịch vụ
    public async Task<List<NearbyShopResponseDto>>
        GetNearbyShopsByServiceAsync(
            int serviceId,
            NearbyShopRequestDto request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var serviceExists = await _context.Service
            .AnyAsync(s =>
                s.ServiceId == serviceId &&
                s.IsActive);

        if (!serviceExists)
            throw new KeyNotFoundException(
                "Dịch vụ không tồn tại hoặc không hoạt động.");

        var shops = await _context.ShopService
            .AsNoTracking()
            .Where(ss =>
                ss.ServiceId == serviceId &&
                ss.IsActive &&
                ss.Shop.Status == (int)ShopStatus.Active &&
                ss.Shop.Address.Location != null)
            .Select(ss => new
            {
                ss.Shop.ShopId,
                ss.Shop.ShopName,
                ss.Shop.Description,
                ss.Shop.Phone,
                ss.Shop.LogoUrl,
                ss.Shop.RatingAverage,

                Address = ss.Shop.Address.AddressLine
                    + ", " + ss.Shop.Address.Ward.WardName
                    + ", " + ss.Shop.Address.Province.ProvinceName,

                Latitude = (double)ss.Shop.Address.Location!.Latitude,
                Longitude = (double)ss.Shop.Address.Location!.Longitude
            })
            .ToListAsync();

        return shops
            .Select(shop => new NearbyShopResponseDto
            {
                ShopId = shop.ShopId,
                ShopName = shop.ShopName,
                Description = shop.Description,
                Phone = shop.Phone,
                LogoUrl = shop.LogoUrl,
                RatingAverage = shop.RatingAverage,
                Address = shop.Address,
                DistanceKm = CalculateDistanceKm(
                    request.Latitude,
                    request.Longitude,
                    shop.Latitude,
                    shop.Longitude)
            })
            .Where(shop => shop.DistanceKm <= request.RadiusKm)
            .OrderBy(shop => shop.DistanceKm)
            .ToList();
    }


    // Tìm tất cả cửa hàng đang hoạt động trong bán kính tối đa 10 km
    public async Task<List<NearbyShopResponseDto>>
        GetAllNearbyShopsAsync(NearbyShopRequestDto request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (!double.IsFinite(request.Latitude) ||
            !double.IsFinite(request.Longitude) ||
            request.Latitude < -90 || request.Latitude > 90 ||
            request.Longitude < -180 || request.Longitude > 180)
            throw new ArgumentException(
                "Tọa độ GPS không hợp lệ.");

        if (!double.IsFinite(request.RadiusKm) ||
            request.RadiusKm <= 0 ||
            request.RadiusKm > 10)
            throw new ArgumentException(
                "Bán kính tìm kiếm phải lớn hơn 0 và không quá 10 km.");

        var shops = await _context.Shop
            .AsNoTracking()
            .Where(s =>
                s.Status == (int)ShopStatus.Active &&
                s.Address.Location != null)
            .Select(s => new
            {
                s.ShopId,
                s.ShopName,
                s.Description,
                s.Phone,
                s.LogoUrl,
                s.RatingAverage,

                Address = s.Address.AddressLine
                    + ", " + s.Address.Ward.WardName
                    + ", " + s.Address.Province.ProvinceName,

                Latitude =
                    (double)s.Address.Location!.Latitude,
                Longitude =
                    (double)s.Address.Location!.Longitude
            })
            .ToListAsync();

        return shops
            .Select(s => new NearbyShopResponseDto
            {
                ShopId = s.ShopId,
                ShopName = s.ShopName,
                Description = s.Description,
                Phone = s.Phone,
                LogoUrl = s.LogoUrl,
                RatingAverage = s.RatingAverage,
                Address = s.Address,
                DistanceKm = CalculateDistanceKm(
                    request.Latitude,
                    request.Longitude,
                    s.Latitude,
                    s.Longitude)
            })
            .Where(s => s.DistanceKm <= request.RadiusKm)
            .OrderBy(s => s.DistanceKm)
            .ThenBy(s => s.ShopId)
            .ToList();
    }

}


using FixNear.Domain.Enums;
using FixNear.Domain.Models.Notifications;
using FixNear.Domain.Models.Repairs;
using FixNear.Domain.Models.Technicians;
using FixNear.Enums;
using FixNear.Features.Technicians.DTOs;
using FixNear.Features.Technicians.Interfaces;
using FixNear.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace FixNear.Features.Technicians.Services
{
    public class TechnicianService : ITechnicianService
    {
        private readonly FixNearDbContext _context;

        public TechnicianService(FixNearDbContext context)
        {
            _context = context;
        }

        //Lấy thông tin cá nhân
        public async Task<TechnicianProfileResponseDto> GetMyProfileAsync(int userId)
        {
            var technician = await _context.TechnicianProfile
                .AsNoTracking()
                .Include(t => t.User)
                .Include(t => t.TechnicianSpecialization)
                    .ThenInclude(ts => ts.Service)
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (technician == null)
                throw new KeyNotFoundException("Không tìm thấy hồ sơ kỹ thuật viên.");

            return new TechnicianProfileResponseDto
            {
                TechnicianId = technician.TechnicianId,
                FullName = technician.User.FullName,
                Email = technician.User.Email,
                Phone = technician.User.Phone,
                AvatarUrl = technician.User.AvatarUrl,
                DateOfBirth = technician.User.DateOfBirth,
                Gender = technician.User.Gender,

                ExperienceYears = technician.ExperienceYears,
                Introduction = technician.Introduction,
                RatingAverage = technician.RatingAverage,

                Specializations = technician.TechnicianSpecialization
                    .Select(ts => new TechnicianSpecializationResponseDto
                    {
                        ServiceId = ts.ServiceId,
                        ServiceName = ts.Service.ServiceName,
                        Note = ts.Note
                    })
                    .ToList()
            };
        }

        //Cập nhật hồ sơ cá nhân
        public async Task UpdateMyProfileAsync( int userId, UpdateTechnicianProfileRequestDto request)
        {
            // 1. Tìm hồ sơ Technician
            var technician = await _context.TechnicianProfile
                .Include(t => t.TechnicianSpecialization)
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (technician == null)
                throw new KeyNotFoundException("Không tìm thấy hồ sơ kỹ thuật viên.");

            // 2. Kiểm tra danh sách ServiceIds
            if (request.ServiceIds == null)
                throw new ArgumentException("Danh sách dịch vụ không hợp lệ.");

            if (request.ServiceIds.Any(id => id <= 0))
                throw new ArgumentException("ServiceId không hợp lệ.");

            if (request.ServiceIds.Count != request.ServiceIds.Distinct().Count())
                throw new ArgumentException("Danh sách dịch vụ không được trùng lặp.");

            // 3. Kiểm tra các dịch vụ tồn tại và đang hoạt động
            var serviceIds = request.ServiceIds;

            var validServiceIds = await _context.Service
                .Where(s => serviceIds.Contains(s.ServiceId) && s.IsActive)
                .Select(s => s.ServiceId)
                .ToListAsync();

            if (validServiceIds.Count != serviceIds.Count)
                throw new ArgumentException("Một hoặc nhiều dịch vụ không tồn tại hoặc đã ngừng hoạt động.");

            // 4. Bắt đầu transaction
            await using var transaction = await _context.Database.BeginTransactionAsync();

            // 5. Cập nhật thông tin hồ sơ
            technician.ExperienceYears = request.ExperienceYears;
            technician.Introduction = request.Introduction?.Trim();

            // 6. Xác định chuyên môn cần xóa
            var specializationsToRemove = technician.TechnicianSpecialization
                .Where(ts => !serviceIds.Contains(ts.ServiceId))
                .ToList();

            _context.TechnicianSpecialization.RemoveRange(specializationsToRemove);

            // 7. Xác định chuyên môn cần thêm
            var existingServiceIds = technician.TechnicianSpecialization
                .Select(ts => ts.ServiceId)
                .ToHashSet();

            var specializationsToAdd = serviceIds
                .Where(id => !existingServiceIds.Contains(id))
                .Select(id => new TechnicianSpecialization
                {
                    TechnicianId = technician.TechnicianId,
                    ServiceId = id,
                    CreatedAt = DateTime.UtcNow
                })
                .ToList();

            await _context.TechnicianSpecialization.AddRangeAsync(specializationsToAdd);

            // 8. Cập nhật thời gian
            technician.UpdatedAt = DateTime.UtcNow;

            // 9. Lưu và hoàn tất transaction
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        // Technician chấp nhận lời mời
        public async Task AcceptTechnicianInvitationAsync(int userId, int invitationId)
        {
            var now = DateTime.UtcNow;

            var user = await _context.User
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null ||
                user.RoleType != (int)RoleType.Technician)
            {
                throw new UnauthorizedAccessException("Chỉ kỹ thuật viên mới có thể chấp nhận lời mời.");
            }

            var technician = await _context.TechnicianProfile
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (technician == null)
                throw new KeyNotFoundException("Không tìm thấy hồ sơ kỹ thuật viên.");

            var invitation = await _context.TechnicianShopInvitation
                .FirstOrDefaultAsync(i => i.InvitationId == invitationId);

            if (invitation == null)
                throw new KeyNotFoundException("Không tìm thấy lời mời.");

            if (!string.Equals(
                invitation.Email.Trim(),
                user.Email?.Trim(),
                StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException(
                    "Lời mời này không dành cho tài khoản của bạn.");
            }

            if (invitation.Status != TechnicianShopInvitationStatus.Pending)
                throw new InvalidOperationException(
                    "Lời mời không còn ở trạng thái chờ phản hồi.");

            if (invitation.ExpiresAt <= now)
            {
                invitation.Status = TechnicianShopInvitationStatus.Expired;
                invitation.RespondedAt = now;

                await _context.SaveChangesAsync();

                throw new InvalidOperationException(
                    "Lời mời đã hết hạn.");
            }

            var existingRelation = await _context.TechnicianShop
                .AnyAsync(ts =>
                    ts.TechnicianId == technician.TechnicianId &&
                    ts.ShopId == invitation.ShopId &&
                    ts.LeftAt == null &&
                    ts.Status == TechnicianShopStatus.Active);

            if (existingRelation)
                throw new InvalidOperationException(
                    "Bạn đã có quan hệ hoạt động với cửa hàng này.");

            var hasPrimaryShop = await _context.TechnicianShop
                .AnyAsync(ts =>
                    ts.TechnicianId == technician.TechnicianId &&
                    ts.LeftAt == null &&
                    ts.Status == TechnicianShopStatus.Active &&
                    ts.IsPrimary);

            var recipients = await _context.ShopMember
                .Where(sm =>
                    sm.ShopId == invitation.ShopId &&
                    sm.Status == ShopMemberStatus.Active &&
                    sm.LeftAt == null &&
                    (sm.ShopRole == ShopRole.Owner ||
                     sm.ShopRole == ShopRole.Manager))
                .Select(sm => sm.Manager.UserId)
                .Distinct()
                .ToListAsync();

            var shopName = await _context.Shop
                .Where(s => s.ShopId == invitation.ShopId)
                .Select(s => s.ShopName)
                .FirstOrDefaultAsync();

            if (shopName == null)
                throw new InvalidOperationException("Không tìm thấy thông tin Shop.");

            var template = await _context.NotificationTemplate
                .FirstOrDefaultAsync(t =>
                    t.TemplateCode == "TECHNICIAN_INVITATION_ACCEPTED");

            if (template == null)
                throw new InvalidOperationException("Không tìm thấy mẫu thông báo Technician chấp nhận lời mời.");

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var technicianShop = new TechnicianShop
                {
                    TechnicianId = technician.TechnicianId,
                    ShopId = invitation.ShopId,
                    JoinedAt = now,
                    LeftAt = null,
                    Status = TechnicianShopStatus.Active,
                    IsPrimary = !hasPrimaryShop,
                    CreatedAt = now
                };

                _context.TechnicianShop.Add(technicianShop);

                invitation.TechnicianId = technician.TechnicianId;
                invitation.Status = TechnicianShopInvitationStatus.Accepted;
                invitation.RespondedAt = now;

                var notification = new Notification
                {
                    TemplateId = template.NotificationTemplateId,
                    Title = template.Title,
                    Body = template.BodyTemplate?
                        .Replace("{TechnicianName}", user.FullName)
                        .Replace("{ShopName}", shopName),
                    RefType = (int)NotificationRefType.TechnicianShopInvitation,
                    RefId = invitation.InvitationId,
                    CreatedAt = now
                };

                _context.Notification.Add(notification);

                foreach (var recipientId in recipients)
                {
                    _context.UserNotification.Add(new UserNotification
                    {
                        Notification = notification,
                        UserId = recipientId,
                        IsRead = false,
                        CreatedAt = now
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Technician từ chối lời mời
        public async Task RejectTechnicianInvitationAsync(int userId, int invitationId)
        {
            var now = DateTime.UtcNow;

            var user = await _context.User
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null ||
                user.RoleType != (int)RoleType.Technician)
            {
                throw new UnauthorizedAccessException("Chỉ kỹ thuật viên mới có thể từ chối lời mời.");
            }

            var technician = await _context.TechnicianProfile
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (technician == null)
                throw new KeyNotFoundException("Không tìm thấy hồ sơ kỹ thuật viên.");

            var invitation = await _context.TechnicianShopInvitation
                .FirstOrDefaultAsync(i => i.InvitationId == invitationId);

            if (invitation == null)
                throw new KeyNotFoundException("Không tìm thấy lời mời.");

            if (!string.Equals(
                invitation.Email.Trim(),
                user.Email?.Trim(),
                StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Lời mời này không dành cho tài khoản của bạn.");

            if (invitation.Status != TechnicianShopInvitationStatus.Pending)
                throw new InvalidOperationException("Lời mời không còn ở trạng thái chờ phản hồi.");

            if (invitation.ExpiresAt <= now)
            {
                invitation.Status = TechnicianShopInvitationStatus.Expired;
                invitation.RespondedAt = now;

                await _context.SaveChangesAsync();

                throw new InvalidOperationException("Lời mời đã hết hạn.");
            }

            var recipients = await _context.ShopMember
                .Where(sm =>
                    sm.ShopId == invitation.ShopId &&
                    sm.Status == ShopMemberStatus.Active &&
                    sm.LeftAt == null &&
                    (sm.ShopRole == ShopRole.Owner ||
                     sm.ShopRole == ShopRole.Manager))
                .Select(sm => sm.Manager.UserId)
                .Distinct()
                .ToListAsync();

            var shopName = await _context.Shop
                .Where(s => s.ShopId == invitation.ShopId)
                .Select(s => s.ShopName)
                .FirstOrDefaultAsync();

            if (shopName == null)
                throw new InvalidOperationException("Không tìm thấy thông tin Shop.");

            var template = await _context.NotificationTemplate
                .FirstOrDefaultAsync(t =>
                    t.TemplateCode == "TECHNICIAN_INVITATION_REJECTED");

            if (template == null)
                throw new InvalidOperationException("Không tìm thấy mẫu thông báo Technician từ chối lời mời.");

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                invitation.TechnicianId = technician.TechnicianId;
                invitation.Status = TechnicianShopInvitationStatus.Rejected;
                invitation.RespondedAt = now;

                var notification = new Notification
                {
                    TemplateId = template.NotificationTemplateId,
                    Title = template.Title,
                    Body = template.BodyTemplate?
                        .Replace("{TechnicianName}", user.FullName)
                        .Replace("{ShopName}", shopName),
                    RefType = (int)NotificationRefType.TechnicianShopInvitation,
                    RefId = invitation.InvitationId,
                    CreatedAt = now
                };

                _context.Notification.Add(notification);

                foreach (var recipientId in recipients)
                {
                    _context.UserNotification.Add(new UserNotification
                    {
                        Notification = notification,
                        UserId = recipientId,
                        IsRead = false,
                        CreatedAt = now
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Lấy danh sách lời mời tham gia Shop của Technician
        public async Task<List<TechnicianInvitationResponseDto>> GetMyInvitationsAsync(int userId)
        {
            var user = await _context.User.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null ||
                user.RoleType != (int)RoleType.Technician)
            {
                throw new UnauthorizedAccessException("Chỉ kỹ thuật viên mới có thể xem lời mời.");
            }

            if (string.IsNullOrWhiteSpace(user.Email))
                throw new InvalidOperationException("Tài khoản chưa có email.");

            var technicianExists = await _context.TechnicianProfile
                .AnyAsync(t => t.UserId == userId);

            if (!technicianExists)
                throw new KeyNotFoundException("Không tìm thấy hồ sơ kỹ thuật viên.");

            var now = DateTime.UtcNow;

            return await _context.TechnicianShopInvitation
                .AsNoTracking()
                .Where(i => i.Email.ToLower() == user.Email.ToLower())
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new TechnicianInvitationResponseDto
                {
                    InvitationId = i.InvitationId,
                    ShopId = i.ShopId,
                    ShopName = i.Shop.ShopName,
                    Email = i.Email,
                    InvitedByName = i.InvitedByNavigation.User.FullName,

                    Status = i.Status ==
                        TechnicianShopInvitationStatus.Pending &&
                        i.ExpiresAt <= now
                            ? (int)TechnicianShopInvitationStatus.Expired
                            : (int)i.Status,

                    ExpiresAt = i.ExpiresAt,
                    RespondedAt = i.RespondedAt,
                    CreatedAt = i.CreatedAt
                })
                .ToListAsync();
        }


        // ============================================
        // X10 - GHI NHAN LINH KIEN SU DUNG
        // ============================================
        public async Task<RecordPartUsageResult> RecordPartUsageAsync(
            CreateRepairPartUsageDto dto,
            int userId)
        {
            // 1. Kiem tra so luong
            if (dto.RepairRequestId <= 0 ||
                dto.RepairPartId <= 0 ||
                dto.Quantity <= 0 ||
                decimal.Round(dto.Quantity, 2) != dto.Quantity)
            {
                return RecordPartUsageResult.InvalidQuantity;
            }

            // 2. Tim Technician dang dang nhap
            var technicianId = await _context.TechnicianProfile
                .Where(t => t.UserId == userId)
                .Select(t => (int?)t.TechnicianId)
                .FirstOrDefaultAsync();

            if (!technicianId.HasValue)
            {
                return RecordPartUsageResult.TechnicianNotFound;
            }

            // 3. Kiem tra Technician da nhan cong viec
            bool hasAcceptedAssignment =
                await _context.RepairAssignment.AnyAsync(a =>
                    a.RepairRequestId == dto.RepairRequestId &&
                    a.TechnicianId == technicianId.Value &&
                    a.AssignmentStatus == 2);

            if (!hasAcceptedAssignment)
            {
                return RecordPartUsageResult.AssignmentNotAccepted;
            }

            // 4. Kiem tra linh kien con hoat dong
            bool partExists = await _context.RepairPart
                .AnyAsync(p =>
                    p.RepairPartId == dto.RepairPartId &&
                    p.IsActive);

            if (!partExists)
            {
                return RecordPartUsageResult.RepairPartNotFound;
            }

            // Transaction giup kiem tra va ghi nhan nhat quan
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            // 5. Tim bao gia da duoc Customer chap nhan
            var quoteId = await _context.Quote
                .Where(q =>
                    q.RepairRequestId == dto.RepairRequestId &&
                    q.TechnicianId == technicianId.Value &&
                    q.Status == 3 &&
                    q.CustomerResponse == 1)
                .OrderByDescending(q => q.QuoteId)
                .Select(q => (int?)q.QuoteId)
                .FirstOrDefaultAsync();

            if (!quoteId.HasValue)
            {
                return RecordPartUsageResult.QuoteNotAccepted;
            }

            // 6. Tim linh kien trong chi tiet bao gia
            var quotedParts = await _context.QuoteDetail
                .Where(d =>
                    d.QuoteId == quoteId.Value &&
                    d.ItemType == 3 &&
                    d.RepairPartRefId == dto.RepairPartId)
                .Select(d => new
                {
                    d.Quantity,
                    d.UnitPrice
                })
                .ToListAsync();

            if (quotedParts.Count == 0)
            {
                return RecordPartUsageResult.PartNotInQuote;
            }

            // Khong tu chon gia neu cung linh kien co nhieu don gia
            var prices = quotedParts
                .Select(d => d.UnitPrice)
                .Distinct()
                .ToList();

            if (prices.Count != 1 || prices[0] < 0)
            {
                return RecordPartUsageResult.InvalidQuotePrice;
            }

            decimal unitPrice = prices[0];

            // 7. Tinh so luong da duoc khach chap thuan
            decimal quotedQuantity = quotedParts.Sum(d => d.Quantity);

            // 8. Kiem tra so luong da su dung truoc do
            decimal usedQuantity =
                await _context.RepairPartUsage
                    .Where(u =>
                        u.RepairRequestId == dto.RepairRequestId &&
                        u.RepairPartId == dto.RepairPartId)
                    .Select(u => (decimal?)u.Quantity)
                    .SumAsync() ?? 0m;

            // Khong cho vuot so luong da bao gia
            if (usedQuantity + dto.Quantity > quotedQuantity)
            {
                return RecordPartUsageResult.QuantityExceeded;
            }

            // 9. Tao ban ghi su dung linh kien
            var usage = new RepairPartUsage
            {
                RepairRequestId = dto.RepairRequestId,
                RepairPartId = dto.RepairPartId,
                Quantity = dto.Quantity,
                UnitPriceAtUsage = unitPrice,
                Note = dto.Note?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            // Amount la cot tinh toan:
            // SQL Server tu tinh Quantity * UnitPriceAtUsage
            _context.RepairPartUsage.Add(usage);

            // 10. Luu du lieu
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return RecordPartUsageResult.Success;
        }


        // ============================================
        // X11 - HOAN THANH SUA CHUA
        // ============================================
        public async Task<CompleteRepairResult> CompleteRepairAsync(
            int repairReportId,
            int userId,
            CompleteRepairDto dto)
        {
            // Ma tam theo quy uoc de xuat voi nhom.
            // Can doi chieu voi Thinh truoc khi test tich hop.
            const int DangSuaChua = 10;
            const int ChoNghiemThu = 11;

            if (repairReportId <= 0 ||
                string.IsNullOrWhiteSpace(dto.Result))
            {
                return CompleteRepairResult.ReportNotFound;
            }

            // 1. Tim bao cao sua chua
            var reportInfo = await _context.RepairReport
                .AsNoTracking()
                .Where(r => r.RepairReportId == repairReportId)
                .Select(r => new
                {
                    r.RepairRequestId,
                    r.TechnicianId
                })
                .FirstOrDefaultAsync();

            if (reportInfo == null)
                return CompleteRepairResult.ReportNotFound;

            // 2. Xac minh KTV la chu bao cao
            bool isOwner = await _context.TechnicianProfile
                .AnyAsync(t =>
                    t.TechnicianId == reportInfo.TechnicianId &&
                    t.UserId == userId);

            // 3. KTV phai da nhan cong viec
            bool hasAcceptedAssignment =
                await _context.RepairAssignment.AnyAsync(a =>
                    a.RepairRequestId == reportInfo.RepairRequestId &&
                    a.TechnicianId == reportInfo.TechnicianId &&
                    a.AssignmentStatus == 2);

            if (!isOwner || !hasAcceptedAssignment)
                return CompleteRepairResult.NotAssigned;

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            // 4. Lay yeu cau sua chua
            var request = await _context.RepairRequest
                .FirstOrDefaultAsync(r =>
                    r.RepairRequestId == reportInfo.RepairRequestId);

            if (request == null)
                return CompleteRepairResult.ReportNotFound;

            if (request.Status == ChoNghiemThu)
                return CompleteRepairResult.AlreadyCompleted;

            if (request.Status != DangSuaChua)
                return CompleteRepairResult.InvalidStatus;

            // 5. Kiem tra khach da chap nhan bao gia
            bool hasAcceptedQuote = await _context.Quote
                .AnyAsync(q =>
                    q.RepairRequestId == request.RepairRequestId &&
                    q.Status == 3 &&
                    q.CustomerResponse == 1);

            if (!hasAcceptedQuote)
                return CompleteRepairResult.QuoteNotAccepted;

            // 6. Cap nhat bao cao sua chua
            var report = await _context.RepairReport
                .FirstOrDefaultAsync(r =>
                    r.RepairReportId == repairReportId);

            if (report == null)
                return CompleteRepairResult.ReportNotFound;

            var now = DateTime.UtcNow;
            int oldStatus = request.Status;

            report.Result = dto.Result.Trim();

            if (!string.IsNullOrWhiteSpace(dto.WorkDescription))
                report.WorkDescription = dto.WorkDescription.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Recommendation))
                report.Recommendation = dto.Recommendation.Trim();

            report.UpdatedAt = now;

            // 7. Chuyen yeu cau sang cho nghiem thu
            request.Status = ChoNghiemThu;
            request.UpdatedAt = now;

            // Chua cap nhat CompletedAt vi khach
            // van can nghiem thu va thanh toan.

            // 8. Ghi lich su trang thai
            _context.RepairRequestStatusHistory.Add(
                new RepairRequestStatusHistory
                {
                    RepairRequestId = request.RepairRequestId,
                    OldStatus = oldStatus,
                    NewStatus = ChoNghiemThu,
                    ChangedByUserId = userId,
                    Reason = "Kỹ thuật viên hoàn thành sửa chữa, chờ nghiệm thu",
                    CreatedAt = now
                });

            // 9. Luu thay doi
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return CompleteRepairResult.Success;
        }

        // ============================================
        // X01 - KY THUAT VIEN NHAN CONG VIEC
        // ============================================
        public async Task<AcceptAssignmentResult> AcceptAsync(
            int assignmentId,
            int userId)
        {
            // Tim cong viec duoc phan cho KTV
            var assignment = await _context.RepairAssignment
                .FirstOrDefaultAsync(a =>
                    a.RepairAssignmentId == assignmentId &&
                    a.Technician.UserId == userId);

            if (assignment == null)
                return AcceptAssignmentResult.NotFound;

            // Chi duoc nhan cong viec dang cho phan hoi
            if (assignment.AssignmentStatus != 1)
                return AcceptAssignmentResult.InvalidStatus;

            var respondedAt = DateTime.UtcNow;

            // Cap nhat OFFERED -> ACCEPTED
            var affectedRows = await _context.RepairAssignment
                .Where(a =>
                    a.RepairAssignmentId == assignmentId &&
                    a.AssignmentStatus == 1 &&
                    a.Technician.UserId == userId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(a => a.AssignmentStatus, 2)
                    .SetProperty(a => a.RespondedAt, respondedAt));

            return affectedRows == 1
                ? AcceptAssignmentResult.Success
                : AcceptAssignmentResult.InvalidStatus;
        }

        // ============================================
        // X02 - KY THUAT VIEN TU CHOI CONG VIEC
        // ============================================
        public async Task<RejectAssignmentResult> RejectAsync(
            int assignmentId,
            int userId,
            string reason)
        {
            // Tim cong viec thuoc KTV dang dang nhap
            var assignment = await _context.RepairAssignment
                .FirstOrDefaultAsync(a =>
                    a.RepairAssignmentId == assignmentId &&
                    a.Technician.UserId == userId);

            if (assignment == null)
                return RejectAssignmentResult.NotFound;

            // Chi duoc tu choi khi dang cho phan hoi
            if (assignment.AssignmentStatus != 1)
                return RejectAssignmentResult.InvalidStatus;

            // Kiem tra ly do tu choi
            if (string.IsNullOrWhiteSpace(reason) ||
                reason.Trim().Length > 500)
            {
                return RejectAssignmentResult.InvalidReason;
            }

            var respondedAt = DateTime.UtcNow;
            var rejectionReason = reason.Trim();

            // Cap nhat OFFERED -> REJECTED
            var affectedRows = await _context.RepairAssignment
                .Where(a =>
                    a.RepairAssignmentId == assignmentId &&
                    a.AssignmentStatus == 1 &&
                    a.Technician.UserId == userId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(a => a.AssignmentStatus, 3)
                    .SetProperty(a => a.RejectionReason, rejectionReason)
                    .SetProperty(a => a.RespondedAt, respondedAt));

            return affectedRows == 1
                ? RejectAssignmentResult.Success
                : RejectAssignmentResult.InvalidStatus;
        }

        // ============================================
        // X03 - GHI NHAN KET QUA KHAO SAT
        // POST /api/technician/surveys
        // ============================================

        public async Task<SurveyResult> CreateSurveyAsync(
            int userId,
            int repairRequestId,
            string diagnosis,
            string? recommendation)
        {
            // 1. Kiem tra du lieu dau vao
            if (repairRequestId <= 0 ||
                string.IsNullOrWhiteSpace(diagnosis))
            {
                return SurveyResult.InvalidData;
            }

            // 2. Tim cong viec cua KTV dang dang nhap
            var assignment = await _context.RepairAssignment
                .Where(a =>
                    a.RepairRequestId == repairRequestId &&
                    a.Technician.UserId == userId)
                .OrderByDescending(a => a.RepairAssignmentId)
                .FirstOrDefaultAsync();

            if (assignment == null)
                return SurveyResult.NotFound;

            // 3. Kiem tra KTV da nhan viec chua
            if (assignment.AssignmentStatus !=
                (int)RepairAssignmentStatus.Accepted)
            {
                return SurveyResult.AssignmentNotAccepted;
            }

            // 4. Kiem tra bao cao khao sat da ton tai
            bool exists = await _context.RepairReport
                .AnyAsync(r =>
                    r.RepairRequestId == repairRequestId &&
                    r.TechnicianId == assignment.TechnicianId);

            if (exists)
                return SurveyResult.AlreadyExists;

            // 5. Tao bao cao khao sat
            var report = new RepairReport
            {
                RepairRequestId = repairRequestId,
                TechnicianId = assignment.TechnicianId,
                Diagnosis = diagnosis.Trim(),
                Recommendation = recommendation?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            // 6. Luu vao database
            _context.RepairReport.Add(report);
            await _context.SaveChangesAsync();

            return SurveyResult.Success;
        }



    }
}

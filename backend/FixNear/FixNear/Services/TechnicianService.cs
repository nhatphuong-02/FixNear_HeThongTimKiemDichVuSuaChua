
using FixNear.DTOs.Technician;
using FixNear.Enums;
using FixNear.Models;
using FixNear.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace FixNear.Services
{
    public class TechnicianService : ITechnicianService
    {
        private readonly FixNearDbContext _context;

        public TechnicianService(FixNearDbContext context)
        {
            _context = context;
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

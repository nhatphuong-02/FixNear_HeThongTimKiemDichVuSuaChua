
using FixNear.Models;
using FixNear.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using FixNear.DTOs.Technician;

namespace FixNear.Services
{
    public class TechnicianAssignmentService
        : ITechnicianAssignmentService
    {
        private readonly FixNearDbContext _context;

        public TechnicianAssignmentService(
            FixNearDbContext context)
        {
            _context = context;
        }

        // ============================================
        // X01 - KY THUAT VIEN NHAN CONG VIEC
        // ============================================
        public async Task<AcceptAssignmentResult> AcceptAsync(
            int assignmentId,
            int userId)
        {
            // 1. Tim cong viec duoc phan cho KTV dang dang nhap
            var assignment = await _context.RepairAssignment
                .FirstOrDefaultAsync(a =>
                    a.RepairAssignmentId == assignmentId &&
                    a.Technician.UserId == userId);

            if (assignment == null)
                return AcceptAssignmentResult.NotFound;

            // 2. Chi duoc nhan viec dang cho phan hoi
            if (assignment.AssignmentStatus != 1)
                return AcceptAssignmentResult.InvalidStatus;

            // 3. Cap nhat trang thai OFFERED -> ACCEPTED
            var respondedAt = DateTime.UtcNow;

            var affectedRows = await _context.RepairAssignment
                .Where(a =>
                    a.RepairAssignmentId == assignmentId &&
                    a.AssignmentStatus == 1 &&
                    a.Technician.UserId == userId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(a => a.AssignmentStatus, 2)
                    .SetProperty(a => a.RespondedAt, respondedAt));

            // 4. Tra ket qua
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
            // 1. Tim cong viec thuoc KTV dang dang nhap
            var assignment = await _context.RepairAssignment
                .FirstOrDefaultAsync(a =>
                    a.RepairAssignmentId == assignmentId &&
                    a.Technician.UserId == userId);

            if (assignment == null)
                return RejectAssignmentResult.NotFound;

            // 2. Chi duoc tu choi khi dang cho phan hoi
            if (assignment.AssignmentStatus != 1)
                return RejectAssignmentResult.InvalidStatus;

            // 3. Kiem tra ly do tu choi
            if (string.IsNullOrWhiteSpace(reason) ||
                reason.Trim().Length > 500)
            {
                return RejectAssignmentResult.InvalidReason;
            }

            // 4. Cap nhat trang thai OFFERED -> REJECTED
            var respondedAt = DateTime.UtcNow;
            var rejectionReason = reason.Trim();

            var affectedRows = await _context.RepairAssignment
                .Where(a =>
                    a.RepairAssignmentId == assignmentId &&
                    a.AssignmentStatus == 1 &&
                    a.Technician.UserId == userId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(a => a.AssignmentStatus, 3)
                    .SetProperty(a => a.RejectionReason, rejectionReason)
                    .SetProperty(a => a.RespondedAt, respondedAt));

            // 5. Tra ket qua
            return affectedRows == 1
                ? RejectAssignmentResult.Success
                : RejectAssignmentResult.InvalidStatus;
        }
    }
}

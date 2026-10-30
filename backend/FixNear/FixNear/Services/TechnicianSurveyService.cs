
using FixNear.Enums;
using FixNear.Models;
using FixNear.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Services;

public class TechnicianSurveyService : ITechnicianSurveyService
{
    private readonly FixNearDbContext _context;

    public TechnicianSurveyService(FixNearDbContext context)
    {
        _context = context;
    }

    // X03 - Ghi nhan ket qua khao sat
    public async Task<SurveyResult> CreateSurveyAsync(
        int userId,
        int repairRequestId,
        string diagnosis,
        string? recommendation)
    {
        if (repairRequestId <= 0 ||
            string.IsNullOrWhiteSpace(diagnosis))
        {
            return SurveyResult.InvalidData;
        }

        // Tim cong viec cua KTV dang dang nhap
        var assignment = await _context.RepairAssignment
            .Where(a =>
                a.RepairRequestId == repairRequestId &&
                a.Technician.UserId == userId)
            .OrderByDescending(a => a.RepairAssignmentId)
            .FirstOrDefaultAsync();

        if (assignment == null)
            return SurveyResult.NotFound;

        // Chi KTV da nhan viec moi duoc ghi khao sat
        if (assignment.AssignmentStatus !=
            (int)RepairAssignmentStatus.Accepted)
        {
            return SurveyResult.AssignmentNotAccepted;
        }

        // Kiem tra bao cao trung
        bool exists = await _context.RepairReport
            .AnyAsync(r =>
                r.RepairRequestId == repairRequestId &&
                r.TechnicianId == assignment.TechnicianId);

        if (exists)
            return SurveyResult.AlreadyExists;

        // Tao bao cao khao sat
        var report = new RepairReport
        {
            RepairRequestId = repairRequestId,
            TechnicianId = assignment.TechnicianId,
            Diagnosis = diagnosis.Trim(),
            Recommendation = recommendation?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.RepairReport.Add(report);
        await _context.SaveChangesAsync();

        return SurveyResult.Success;
    }
}

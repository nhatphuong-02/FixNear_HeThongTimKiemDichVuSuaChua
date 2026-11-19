using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using FixNear.Domain.Enums;
using FixNear.Domain.Models.Billing;
using FixNear.Domain.Models.Notifications;
using FixNear.Enums;
using FixNear.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Features.Billing.Controllers;

public sealed class ConfirmBankTransferDto
{
    [Required, MinLength(5), MaxLength(100)]
    public string BankTransactionCode { get; set; } = "";
}

[ApiController]
[Authorize(Roles = "2")]
[Route("api/payments")]
public sealed class PaymentConfirmationController : ControllerBase
{
    private readonly FixNearDbContext _db;
    public PaymentConfirmationController(FixNearDbContext db) => _db = db;

    // Chỉ xác nhận SAU KHI Manager đã đối soát giao dịch trên ngân hàng.
    // Customer không có quyền xác nhận trạng thái Paid.
    [HttpPatch("{paymentId:int}/confirm-bank-transfer")]
    public async Task<IActionResult> Confirm(int paymentId, [FromBody] ConfirmBankTransferDto dto)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();
        if (paymentId <= 0 || string.IsNullOrWhiteSpace(dto?.BankTransactionCode))
            return BadRequest(new { message = "Mã giao dịch ngân hàng không hợp lệ." });

        string bankTransactionCode = dto.BankTransactionCode.Trim();
        if (bankTransactionCode.Length < 5 || bankTransactionCode.Length > 100)
            return BadRequest(new { message = "Mã giao dịch phải dài 5-100 ký tự." });

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var payment = await _db.Payment
            .Include(p => p.Invoice)
                .ThenInclude(i => i.RepairRequest)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

        if (payment == null) return NotFound();

        bool manager = await _db.ShopMember.AsNoTracking().AnyAsync(sm =>
            sm.ShopId == payment.Invoice.ShopId &&
            sm.Manager.UserId == userId &&
            sm.Status == ShopMemberStatus.Active && sm.LeftAt == null &&
            (sm.ShopRole == ShopRole.Owner || sm.ShopRole == ShopRole.Manager));
        if (!manager) return Forbid();

        if (payment.PaymentMethod != 3 || payment.Status != 1 ||
            payment.Invoice.PaidAt != null ||
            payment.Amount != payment.Invoice.TotalAmount ||
            payment.Invoice.RepairRequest.Status != (int)RepairRequestStatus.Completed ||
            !payment.Invoice.RepairRequest.ManagerAcceptedAt.HasValue)
            return Conflict(new { message = "Thanh toán không ở trạng thái chờ đối soát hợp lệ." });

        bool reused = await _db.PaymentTransaction.AsNoTracking().AnyAsync(t =>
            t.TransactionCode == bankTransactionCode && t.Status == 2);
        if (reused)
            return Conflict(new { message = "Mã giao dịch ngân hàng đã được dùng." });

        var now = DateTime.UtcNow;
        payment.Status = 2; // SUCCESS (MVP: xác nhận thủ công sau đối soát)
        payment.PaidAt = now;
        payment.UpdatedAt = now;
        payment.Invoice.Status = 2; // PAID
        payment.Invoice.PaidAt = now;
        payment.Invoice.RepairRequest.Status = (int)RepairRequestStatus.Paid;
        payment.Invoice.RepairRequest.UpdatedAt = now;

        _db.PaymentTransaction.Add(new PaymentTransaction
        {
            PaymentId = payment.PaymentId,
            TransactionCode = bankTransactionCode,
            Status = 2,
            Message = $"Owner/Manager đã đối soát giao dịch và xác nhận: UserId={userId}",
            CreatedAt = now
        });

        var template = await _db.NotificationTemplate.AsNoTracking()
            .FirstAsync(t => t.TemplateCode == "PAYMENT_CONFIRMED");
        var customerId = await _db.CustomerProfile
            .Where(c => c.CustomerId == payment.Invoice.CustomerId)
            .Select(c => c.UserId).FirstAsync();

        var notification = new Notification
        {
            TemplateId = template.NotificationTemplateId,
            Title = template.Title.Replace("{InvoiceNumber}", payment.Invoice.InvoiceNumber),
            Body = (template.BodyTemplate ?? "")
                .Replace("{InvoiceNumber}", payment.Invoice.InvoiceNumber),
            RefType = (int)NotificationRefType.Payment,
            RefId = payment.PaymentId,
            CreatedAt = now
        };
        notification.UserNotification.Add(new UserNotification
        {
            UserId = customerId, IsRead = false, CreatedAt = now
        });
        _db.Notification.Add(notification);

        // SQL trigger tự ghi lịch sử trạng thái Repair.
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC sys.sp_set_session_context @key=N'UserId', @value={userId}");
        try
        {
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        finally
        {
            await _db.Database.ExecuteSqlRawAsync(
                "EXEC sys.sp_set_session_context @key=N'UserId', @value=NULL");
        }

        return Ok(new { paymentId, status = 2, paidAt = now, bankTransactionCode });
    }
}

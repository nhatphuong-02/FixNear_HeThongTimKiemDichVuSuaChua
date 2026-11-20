using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using FixNear.Domain.Enums;
using FixNear.Enums;
using FixNear.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Features.Billing.Controllers;

public sealed class ShopBankAccountDto
{
    [Required, RegularExpression(@"^\d{6}$")]
    public string BankBin { get; set; } = "";
    [Required, RegularExpression(@"^\d{6,19}$")]
    public string BankAccountNo { get; set; } = "";
    [Required, MaxLength(100)]
    public string BankAccountName { get; set; } = "";
}

[ApiController]
[Authorize]
[Route("api")]
public sealed class BillingWorkflowController : ControllerBase
{
    private readonly FixNearDbContext _db;
    public BillingWorkflowController(FixNearDbContext db) => _db = db;

    private int? UserId => int.TryParse(
        User.FindFirstValue(ClaimTypes.NameIdentifier), out int id) ? id : null;

    private Task<bool> CanManageShop(int shopId, int userId) =>
        _db.ShopMember.AsNoTracking().AnyAsync(sm =>
            sm.ShopId == shopId && sm.Manager.UserId == userId &&
            sm.Status == ShopMemberStatus.Active && sm.LeftAt == null &&
            (sm.ShopRole == ShopRole.Owner || sm.ShopRole == ShopRole.Manager));

    // Shop đặt tài khoản thụ hưởng. Không để Customer tự truyền tài khoản khi trả tiền.
    [HttpPut("shops/{shopId:int}/payment-account")]
    [Authorize(Roles = "2")]
    public async Task<IActionResult> UpdateBank(int shopId, [FromBody] ShopBankAccountDto dto)
    {
        if (UserId is not int userId) return Unauthorized();
        if (!await CanManageShop(shopId, userId)) return Forbid();
        if (dto.BankAccountName.Trim().Length == 0) return BadRequest();
        var shop = await _db.Shop.FirstOrDefaultAsync(s => s.ShopId == shopId);
        if (shop == null) return NotFound();
        shop.BankBin = dto.BankBin;
        shop.BankAccountNo = dto.BankAccountNo;
        shop.BankAccountName = dto.BankAccountName.Trim();
        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã cập nhật tài khoản nhận thanh toán." });
    }

    // Manager nghiệm thu sau khi KTV báo hoàn thành.
    [HttpPatch("shops/{shopId:int}/repairs/{repairRequestId:int}/accept-completion")]
    [Authorize(Roles = "2")]
    public async Task<IActionResult> AcceptCompletion(int shopId, int repairRequestId)
    {
        if (UserId is not int userId) return Unauthorized();
        if (!await CanManageShop(shopId, userId)) return Forbid();

        await using var tx = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var repair = await _db.RepairRequest.FirstOrDefaultAsync(r =>
            r.ShopId == shopId && r.RepairRequestId == repairRequestId);
        if (repair == null) return NotFound();
        if (repair.Status != (int)RepairRequestStatus.Completed ||
            repair.ManagerAcceptedAt.HasValue)
            return Conflict(new { message = "Yêu cầu chưa hoàn thành hoặc đã nghiệm thu." });

        bool pending = await _db.Quote.AnyAsync(q =>
            q.RepairRequestId == repairRequestId && q.IsSupplemental &&
            (q.Status == (int)QuoteStatus.Draft || q.Status == (int)QuoteStatus.Sent));
        if (pending) return Conflict(new { message = "Còn báo giá bổ sung chưa xử lý." });

        repair.ManagerAcceptedAt = DateTime.UtcNow;
        repair.ManagerAcceptedByUserId = userId;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok(new { message = "Manager đã nghiệm thu. Có thể lập hóa đơn.", repairRequestId });
    }

    // QR VietQR từ tài khoản và số tiền ĐÃ snapshot trong Invoice.
    [HttpGet("invoices/{invoiceId:int}/bank-transfer")]
    [Authorize(Roles = "1,2")]
    public async Task<IActionResult> GetTransferInfo(int invoiceId)
    {
        if (UserId is not int userId) return Unauthorized();
        var invoice = await _db.Invoice.AsNoTracking()
            .Where(i => i.InvoiceId == invoiceId &&
                (i.Customer.UserId == userId ||
                 _db.ShopMember.Any(sm => sm.ShopId == i.ShopId &&
                     sm.Manager.UserId == userId &&
                     sm.Status == ShopMemberStatus.Active && sm.LeftAt == null &&
                     (sm.ShopRole == ShopRole.Owner || sm.ShopRole == ShopRole.Manager))))
            .FirstOrDefaultAsync();
        if (invoice == null) return NotFound();
        if (string.IsNullOrWhiteSpace(invoice.BankBin) ||
            string.IsNullOrWhiteSpace(invoice.BankAccountNo) ||
            string.IsNullOrWhiteSpace(invoice.BankAccountName) ||
            string.IsNullOrWhiteSpace(invoice.PaymentReference) ||
            invoice.TotalAmount <= 0)
            return Conflict(new { message = "Hóa đơn chưa có đủ thông tin chuyển khoản." });

        if (invoice.TotalAmount != decimal.Truncate(invoice.TotalAmount))
            return Conflict(new { message = "Số tiền hóa đơn phải là số VND nguyên." });

        string memo = Uri.EscapeDataString(invoice.PaymentReference);
        string name = Uri.EscapeDataString(invoice.BankAccountName);
        string amount = invoice.TotalAmount.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
        string qrUrl = $"https://img.vietqr.io/image/{invoice.BankBin}-{invoice.BankAccountNo}-compact2.png?amount={amount}&addInfo={memo}&accountName={name}";
        return Ok(new
        {
            invoice.InvoiceId, invoice.InvoiceNumber,
            invoice.TotalAmount, invoice.BankBin,
            invoice.BankAccountNo, invoice.BankAccountName,
            invoice.PaymentReference, qrUrl,
            status = invoice.Status
        });
    }
}

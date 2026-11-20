
using System.Data;
using FixNear.Domain.Models.Billing;
using FixNear.Domain.Models.Notifications;
using FixNear.Domain.Enums;
using FixNear.Enums;
using FixNear.Features.Invoices.DTOs;
using FixNear.Features.Invoices.Interfaces;
using FixNear.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Features.Invoices.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly FixNearDbContext _context;

        public InvoiceService(FixNearDbContext context)
        {
            _context = context;
        }

        // ============================================
        // X12 - TAO HOA DON
        // ============================================
        public async Task<CreateInvoiceResult> CreateAsync(
            CreateInvoiceDto dto,
            int userId)
        {
            // 1. Kiem tra du lieu dau vao
            if (dto.RepairRequestId <= 0 || dto.QuoteId <= 0)
            {
                return new CreateInvoiceResult
                {
                    Status = CreateInvoiceStatus.InvalidInput
                };
            }

            // Transaction tranh tao trung hoa don
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            // 2. Tim yeu cau cua Shop do Manager quan ly
            var request = await _context.RepairRequest
                .AsNoTracking()
                .Where(r =>
                    r.RepairRequestId == dto.RepairRequestId &&
                    _context.ShopMember.Any(sm =>
                        sm.ShopId == r.ShopId &&
                        sm.Manager.UserId == userId &&
                        sm.Status == ShopMemberStatus.Active &&
                        sm.LeftAt == null &&
                        (sm.ShopRole == ShopRole.Owner ||
                         sm.ShopRole == ShopRole.Manager)))
                .Select(r => new
                {
                    r.RepairRequestId,
                    r.CustomerId,
                    r.ShopId,
                    r.Status,
                    r.ManagerAcceptedAt
                })
                .FirstOrDefaultAsync();

            if (request == null)
            {
                return new CreateInvoiceResult
                {
                    Status = CreateInvoiceStatus.NotFound
                };
            }

            // Chỉ xuất hóa đơn sau khi KTV đã hoàn thành
            // và Owner/Manager đã nghiệm thu.
            if (request.Status != (int)RepairRequestStatus.Completed ||
                request.ManagerAcceptedAt == null)
            {
                return new CreateInvoiceResult
                {
                    Status = CreateInvoiceStatus.InvalidRequestStatus
                };
            }

            // 4. Khong tao trung hoa don cho cung yeu cau
            bool invoiceExists = await _context.Invoice
                .AnyAsync(i =>
                    i.RepairRequestId == request.RepairRequestId);

            if (invoiceExists)
            {
                return new CreateInvoiceResult
                {
                    Status = CreateInvoiceStatus.AlreadyExists
                };
            }

            // 5. Gộp báo giá gốc và TẤT CẢ báo giá bổ sung đã duyệt.
            var quotes = await _context.Quote
                .AsNoTracking()
                .Include(q => q.QuoteDetail)
                .Where(q => q.RepairRequestId == request.RepairRequestId &&
                    q.Status == (int)QuoteStatus.Accepted &&
                    q.CustomerResponse == (int)QuoteCustomerResponse.Accepted)
                .OrderBy(q => q.QuoteId)
                .ToListAsync();

            // QuoteId từ DTO là báo giá gốc để tương thích Mobile cũ.
            if (!quotes.Any(q => q.QuoteId == dto.QuoteId && !q.IsSupplemental))
                return new CreateInvoiceResult { Status = CreateInvoiceStatus.QuoteNotAccepted };

            // Không phát hành khi còn phát sinh chưa được xử lý.
            bool pending = await _context.Quote.AnyAsync(q =>
                q.RepairRequestId == request.RepairRequestId &&
                q.IsSupplemental &&
                (q.Status == (int)QuoteStatus.Draft || q.Status == (int)QuoteStatus.Sent));
            if (pending)
                return new CreateInvoiceResult { Status = CreateInvoiceStatus.InvalidRequestStatus };

            if (quotes.Any(q => q.QuoteDetail.Count == 0))
                return new CreateInvoiceResult { Status = CreateInvoiceStatus.EmptyQuote };

            if (quotes.SelectMany(q => q.QuoteDetail)
                .Any(d => d.Quantity <= 0 || d.UnitPrice < 0))
                return new CreateInvoiceResult { Status = CreateInvoiceStatus.InvalidQuoteDetail };

            // Snapshot tài khoản thụ hưởng tại thời điểm phát hành.
            var bank = await _context.Shop.AsNoTracking()
                .Where(sh => sh.ShopId == request.ShopId)
                .Select(sh => new { sh.BankBin, sh.BankAccountNo, sh.BankAccountName })
                .FirstAsync();
            if (string.IsNullOrWhiteSpace(bank.BankBin) ||
                string.IsNullOrWhiteSpace(bank.BankAccountNo) ||
                string.IsNullOrWhiteSpace(bank.BankAccountName))
                return new CreateInvoiceResult { Status = CreateInvoiceStatus.InvalidInput };

            // 7. Tao so hoa don duy nhat
            var now = DateTime.UtcNow;

            string invoiceNumber =
                $"INV-{now:yyyyMMdd}-{Guid.NewGuid():N}";

            // 8. Tao hoa don
            var invoice = new Invoice
            {
                RepairRequestId = request.RepairRequestId,
                CustomerId = request.CustomerId,
                ShopId = request.ShopId,

                InvoiceNumber = invoiceNumber,

                // Gia tri ban dau.
                // Trigger se dong bo sau khi them chi tiet.
                TotalAmount = 0m,

                Status = 1,
                IssuedAt = now,
                BankBin = bank.BankBin,
                BankAccountNo = bank.BankAccountNo,
                BankAccountName = bank.BankAccountName
            };

            // 9. Snapshot chi tiết mọi báo giá đã được khách chấp nhận.
            foreach (var quote in quotes)
            {
                foreach (var detail in quote.QuoteDetail)
                {
                    invoice.InvoiceDetail.Add(new InvoiceDetail
                    {
                        Description = $"{quote.QuoteNumber} - " +
                            (string.IsNullOrWhiteSpace(detail.Description)
                                ? $"Hạng mục {detail.QuoteDetailId}"
                                : detail.Description.Trim()),
                        Quantity = detail.Quantity,
                        UnitPrice = detail.UnitPrice,
                        CreatedAt = now
                    });
                }
            }

            // 10. Luu hoa don va chi tiet
            _context.Invoice.Add(invoice);

            await _context.SaveChangesAsync();

            invoice.PaymentReference = $"FN{invoice.InvoiceId}";
            await _context.SaveChangesAsync();

            // 11. Doc lai tong tien do trigger cap nhat
            await _context.Entry(invoice).ReloadAsync();

            // Không phát hành hóa đơn với số tiền không hợp lệ.
            var expected = quotes.Sum(q => q.QuoteDetail.Sum(d => d.Amount));
            if (invoice.TotalAmount <= 0 || invoice.TotalAmount != expected)
                throw new InvalidOperationException("Tổng hóa đơn và báo giá không khớp.");

            var customerUserId = await _context.CustomerProfile
                .Where(c => c.CustomerId == request.CustomerId)
                .Select(c => c.UserId)
                .FirstAsync();

            var template = await _context.NotificationTemplate
                .FirstAsync(t => t.TemplateCode == "INVOICE_ISSUED");
            var notice = new Notification
            {
                TemplateId = template.NotificationTemplateId,
                Title = template.Title.Replace("{InvoiceNumber}", invoice.InvoiceNumber),
                Body = (template.BodyTemplate ?? "")
                    .Replace("{InvoiceNumber}", invoice.InvoiceNumber)
                    .Replace("{Amount}", invoice.TotalAmount.ToString("N0") + " đ"),
                RefType = (int)NotificationRefType.Invoice,
                RefId = invoice.InvoiceId,
                CreatedAt = DateTime.UtcNow
            };
            notice.UserNotification.Add(new UserNotification
            {
                UserId = customerUserId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            _context.Notification.Add(notice);
            await _context.SaveChangesAsync();

            // 12. Hoan tat transaction
            await transaction.CommitAsync();

            return new CreateInvoiceResult
            {
                Status = CreateInvoiceStatus.Success,
                InvoiceId = invoice.InvoiceId,
                InvoiceNumber = invoice.InvoiceNumber,
                TotalAmount = invoice.TotalAmount
            };
        }

        // ============================================
        // X13 - XEM CHI TIET HOA DON
        // ============================================
        public async Task<GetInvoiceResult> GetByIdAsync(
            int invoiceId,
            int userId,
            int roleType)
        {
            // 1. Kiem tra ID hoa don
            if (invoiceId <= 0 || userId <= 0)
            {
                return new GetInvoiceResult
                {
                    Status = GetInvoiceStatus.NotFound
                };
            }

            // 2. Chi cho Customer va Manager xem hoa don
            if (roleType != 1 && roleType != 2)
            {
                return new GetInvoiceResult
                {
                    Status = GetInvoiceStatus.NotFound
                };
            }

            // 3. Tim hoa don va kiem tra quyen truy cap
            var invoice = await _context.Invoice
                .AsNoTracking()
                .Include(i => i.InvoiceDetail)
                .Where(i =>
                    i.InvoiceId == invoiceId &&
                    (
                        // Customer: chi xem hoa don cua minh
                        (roleType == 1 &&
                         i.Customer.UserId == userId)

                        ||

                        // Manager: chi xem hoa don Shop minh
                        (roleType == 2 &&
                         _context.ShopMember.Any(sm =>
                             sm.ShopId == i.ShopId &&
                             sm.Manager.UserId == userId &&
                             sm.Status == ShopMemberStatus.Active &&
                             sm.LeftAt == null &&
                             (sm.ShopRole == ShopRole.Owner ||
                              sm.ShopRole == ShopRole.Manager)))
                    ))
                .FirstOrDefaultAsync();

            if (invoice == null)
            {
                return new GetInvoiceResult
                {
                    Status = GetInvoiceStatus.NotFound
                };
            }

            // 4. Chuyen Entity sang DTO
            var response = new InvoiceResponseDto
            {
                InvoiceId = invoice.InvoiceId,
                RepairRequestId = invoice.RepairRequestId,
                InvoiceNumber = invoice.InvoiceNumber,
                CustomerId = invoice.CustomerId,
                ShopId = invoice.ShopId,
                TotalAmount = invoice.TotalAmount,
                Status = invoice.Status,
                IssuedAt = invoice.IssuedAt,
                PaidAt = invoice.PaidAt,

                Details = invoice.InvoiceDetail
                    .OrderBy(d => d.InvoiceDetailId)
                    .Select(d => new InvoiceDetailResponseDto
                    {
                        InvoiceDetailId = d.InvoiceDetailId,
                        Description = d.Description,
                        Quantity = d.Quantity,
                        UnitPrice = d.UnitPrice,
                        Amount = d.Amount
                    })
                    .ToList()
            };

            // 5. Tra ket qua
            return new GetInvoiceResult
            {
                Status = GetInvoiceStatus.Success,
                Data = response
            };
        }

    }
}

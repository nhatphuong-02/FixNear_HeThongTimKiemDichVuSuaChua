
using System.Data;
using FixNear.DTOs.Invoice;
using FixNear.Models;
using FixNear.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Services.Service
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
                    r.Shop.Manager.UserId == userId)
                .Select(r => new
                {
                    r.RepairRequestId,
                    r.CustomerId,
                    r.ShopId,
                    r.Status
                })
                .FirstOrDefaultAsync();

            if (request == null)
            {
                return new CreateInvoiceResult
                {
                    Status = CreateInvoiceStatus.NotFound
                };
            }

            // 3. Kiem tra tien do sua chua
            // Ma 11 dang la quy uoc tam cua X11.
            // Can dong bo voi Thinh truoc khi test.
            const int ChoNghiemThu = 11;

            if (request.Status != ChoNghiemThu)
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

            // 5. Lay bao gia da duoc Customer chap nhan
            var quote = await _context.Quote
                .AsNoTracking()
                .Include(q => q.QuoteDetail)
                .FirstOrDefaultAsync(q =>
                    q.QuoteId == dto.QuoteId &&
                    q.RepairRequestId == request.RepairRequestId &&
                    q.Status == 3 &&
                    q.CustomerResponse == 1);

            if (quote == null)
            {
                return new CreateInvoiceResult
                {
                    Status = CreateInvoiceStatus.QuoteNotAccepted
                };
            }

            if (quote.QuoteDetail.Count == 0)
            {
                return new CreateInvoiceResult
                {
                    Status = CreateInvoiceStatus.EmptyQuote
                };
            }

            // 6. Kiem tra chi tiet bao gia
            if (quote.QuoteDetail.Any(d =>
                d.Quantity <= 0 ||
                d.UnitPrice < 0))
            {
                return new CreateInvoiceResult
                {
                    Status = CreateInvoiceStatus.InvalidQuoteDetail
                };
            }

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
                IssuedAt = now
            };

            // 9. Sao chep chi tiet tu bao gia
            foreach (var detail in quote.QuoteDetail)
            {
                invoice.InvoiceDetail.Add(new InvoiceDetail
                {
                    Description =
                        string.IsNullOrWhiteSpace(detail.Description)
                            ? $"Hang muc bao gia {detail.QuoteDetailId}"
                            : detail.Description.Trim(),

                    Quantity = detail.Quantity,
                    UnitPrice = detail.UnitPrice,
                    CreatedAt = now

                    // Khong gan Amount.
                    // SQL Server tu tinh.
                });
            }

            // 10. Luu hoa don va chi tiet
            _context.Invoice.Add(invoice);

            await _context.SaveChangesAsync();

            // 11. Doc lai tong tien do trigger cap nhat
            await _context.Entry(invoice).ReloadAsync();

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
                         i.Shop.Manager.UserId == userId)
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

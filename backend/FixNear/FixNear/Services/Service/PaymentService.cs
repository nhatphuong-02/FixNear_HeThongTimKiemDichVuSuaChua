
using System.Data;
using FixNear.DTOs.Payment;
using FixNear.Models;
using FixNear.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Services.Service
{
    public class PaymentService : IPaymentService
    {
        private readonly FixNearDbContext _context;

        // Ma tam thoi, can thong nhat voi nhom
        private const int CashMethod = 1;
        private const int QrMethod = 2;
        private const int PendingStatus = 1;

        public PaymentService(FixNearDbContext context)
        {
            _context = context;
        }

        // ============================================
        // X14 - TAO YEU CAU THANH TOAN
        // ============================================
        public async Task<CreatePaymentResult> CreateAsync(
            CreatePaymentDto dto,
            int userId)
        {
            // 1. Kiem tra du lieu dau vao
            if (dto.InvoiceId <= 0 ||
                userId <= 0 ||
                (dto.PaymentMethod != CashMethod &&
                 dto.PaymentMethod != QrMethod))
            {
                return new CreatePaymentResult
                {
                    Status = CreatePaymentStatus.InvalidInput
                };
            }

            // 2. Transaction han che tao thanh toan trung
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            // 3. Tim hoa don thuoc Customer dang dang nhap
            var invoice = await _context.Invoice
                .AsNoTracking()
                .FirstOrDefaultAsync(i =>
                    i.InvoiceId == dto.InvoiceId &&
                    i.Customer.UserId == userId);

            if (invoice == null)
            {
                return new CreatePaymentResult
                {
                    Status = CreatePaymentStatus.InvoiceNotFound
                };
            }

            // 4. Kiem tra hoa don da thanh toan hay chua
            if (invoice.PaidAt != null)
            {
                return new CreatePaymentResult
                {
                    Status = CreatePaymentStatus.AlreadyPaid
                };
            }

            // 5. Lay so tien tu database, khong lay tu client
            if (invoice.TotalAmount <= 0)
            {
                return new CreatePaymentResult
                {
                    Status = CreatePaymentStatus.InvalidAmount
                };
            }

            // 6. Kiem tra yeu cau thanh toan da ton tai
            bool exists = await _context.Payment
                .AnyAsync(p => p.InvoiceId == invoice.InvoiceId);

            if (exists)
            {
                return new CreatePaymentResult
                {
                    Status = CreatePaymentStatus.PaymentAlreadyExists
                };
            }

            // 7. Tao ban ghi Payment
            var now = DateTime.UtcNow;

            var payment = new Payment
            {
                InvoiceId = invoice.InvoiceId,
                PaymentMethod = dto.PaymentMethod,
                Amount = invoice.TotalAmount,
                Status = PendingStatus,
                CreatedAt = now
            };

            // 8. Tao ban ghi giao dich dang cho xac nhan
            payment.PaymentTransaction.Add(
                new PaymentTransaction
                {
                    TransactionCode = null,
                    Status = PendingStatus,
                    Message = "Cho xac nhan thanh toan",
                    CreatedAt = now
                });

            // 9. Luu Payment va PaymentTransaction
            _context.Payment.Add(payment);

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            // 10. Tra ket qua
            return new CreatePaymentResult
            {
                Status = CreatePaymentStatus.Success,
                PaymentId = payment.PaymentId,
                InvoiceId = payment.InvoiceId,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                PaymentStatus = payment.Status
            };
        }

        // ============================================
        // X15 - XEM THONG TIN THANH TOAN
        // ============================================
        public async Task<GetPaymentResult> GetByIdAsync(
            int paymentId,
            int userId,
            int roleType)
        {
            // 1. Kiem tra du lieu dau vao
            if (paymentId <= 0 || userId <= 0)
            {
                return new GetPaymentResult
                {
                    Status = GetPaymentStatus.NotFound
                };
            }

            // 2. Chi Customer va Manager duoc xem
            if (roleType != 1 && roleType != 2)
            {
                return new GetPaymentResult
                {
                    Status = GetPaymentStatus.NotFound
                };
            }

            // 3. Tim Payment va kiem tra quyen so huu
            var payment = await _context.Payment
                .AsNoTracking()
                .Include(p => p.Invoice)
                .Include(p => p.PaymentTransaction)
                .Where(p =>
                    p.PaymentId == paymentId &&
                    (
                        // Customer: thanh toan thuoc hoa don cua minh
                        (roleType == 1 &&
                         p.Invoice.Customer.UserId == userId)

                        ||

                        // Manager: thanh toan cua Shop minh quan ly
                        (roleType == 2 &&
                         p.Invoice.Shop.Manager.UserId == userId)
                    ))
                .FirstOrDefaultAsync();

            // 4. Khong tim thay hoac khong co quyen
            if (payment == null)
            {
                return new GetPaymentResult
                {
                    Status = GetPaymentStatus.NotFound
                };
            }

            // 5. Chuyen Payment sang Response DTO
            var response = new PaymentResponseDto
            {
                PaymentId = payment.PaymentId,
                InvoiceId = payment.InvoiceId,
                InvoiceNumber = payment.Invoice.InvoiceNumber,

                PaymentMethod = payment.PaymentMethod,
                Amount = payment.Amount,
                Status = payment.Status,

                PaidAt = payment.PaidAt,
                CreatedAt = payment.CreatedAt,
                UpdatedAt = payment.UpdatedAt,

                Transactions = payment.PaymentTransaction
                    .OrderBy(t => t.CreatedAt)
                    .ThenBy(t => t.PaymentTransactionId)
                    .Select(t => new PaymentTransactionResponseDto
                    {
                        PaymentTransactionId =
                            t.PaymentTransactionId,

                        TransactionCode = t.TransactionCode,

                        Status = t.Status,

                        Message = t.Message,

                        CreatedAt = t.CreatedAt
                    })
                    .ToList()
            };

            // 6. Tra ket qua
            return new GetPaymentResult
            {
                Status = GetPaymentStatus.Success,
                Data = response
            };
        }

    }
}


using System.Data;
using FixNear.DTOs.Review;
using FixNear.Models;
using FixNear.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Services.Service
{
    public class ReviewService : IReviewService
    {
        private readonly FixNearDbContext _context;

        // Trang thai KTV da chap nhan phan cong
        private const int AcceptedAssignment = 2;

        // Trang thai mac dinh cua danh gia
        private const int NewReviewStatus = 1;

        public ReviewService(FixNearDbContext context)
        {
            _context = context;
        }

        // ============================================
        // X16 - TAO DANH GIA
        // ============================================
        public async Task<CreateReviewResult> CreateAsync(
            CreateReviewDto dto,
            int userId)
        {
            // 1. Kiem tra du lieu dau vao
            if (userId <= 0 ||
                dto.RepairRequestId <= 0 ||
                dto.Rating < 1 ||
                dto.Rating > 5 ||
                (dto.TechnicianId.HasValue &&
                 dto.TechnicianId.Value <= 0) ||
                (dto.Comment?.Length ?? 0) > 2000)
            {
                return new CreateReviewResult
                {
                    Status = CreateReviewStatus.InvalidInput
                };
            }

            // 2. Kiem tra danh sach URL anh
            var imageUrls = new List<string>();

            if (dto.ImageUrls != null)
            {
                // Gioi han tam thoi toi da 10 anh
                if (dto.ImageUrls.Count > 10)
                {
                    return new CreateReviewResult
                    {
                        Status = CreateReviewStatus.InvalidImages
                    };
                }

                foreach (var rawUrl in dto.ImageUrls)
                {
                    var url = rawUrl?.Trim();

                    if (string.IsNullOrWhiteSpace(url) ||
                        url.Length > 500)
                    {
                        return new CreateReviewResult
                        {
                            Status = CreateReviewStatus.InvalidImages
                        };
                    }

                    // Cho phep URL HTTP(S) hoac duong dan noi bo
                    bool isHttpUrl =
                        Uri.TryCreate(
                            url,
                            UriKind.Absolute,
                            out var uri) &&
                        (uri.Scheme == Uri.UriSchemeHttp ||
                         uri.Scheme == Uri.UriSchemeHttps);

                    bool isLocalPath =
                        url.StartsWith("/") &&
                        !url.StartsWith("//");

                    if (!isHttpUrl && !isLocalPath)
                    {
                        return new CreateReviewResult
                        {
                            Status = CreateReviewStatus.InvalidImages
                        };
                    }

                    imageUrls.Add(url);
                }
            }

            // 3. Transaction tranh tao danh gia trung
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            // 4. Tim yeu cau sua chua cua Customer
            var request = await _context.RepairRequest
                .AsNoTracking()
                .Where(r =>
                    r.RepairRequestId == dto.RepairRequestId &&
                    r.Customer.UserId == userId)
                .Select(r => new
                {
                    r.RepairRequestId,
                    r.CustomerId,
                    r.ShopId,
                    r.CompletedAt
                })
                .FirstOrDefaultAsync();

            if (request == null)
            {
                return new CreateReviewResult
                {
                    Status = CreateReviewStatus.RequestNotFound
                };
            }

            // 5. Yeu cau phai da hoan tat
            if (!request.CompletedAt.HasValue)
            {
                return new CreateReviewResult
                {
                    Status = CreateReviewStatus.NotCompleted
                };
            }

            // 6. Hoa don phai da duoc xac nhan thanh toan
            bool isPaid = await _context.Invoice
                .AsNoTracking()
                .AnyAsync(i =>
                    i.RepairRequestId == request.RepairRequestId &&
                    i.PaidAt != null);

            if (!isPaid)
            {
                return new CreateReviewResult
                {
                    Status = CreateReviewStatus.NotPaid
                };
            }

            // 7. Moi yeu cau chi duoc danh gia mot lan
            bool alreadyReviewed = await _context.Review
                .AnyAsync(r =>
                    r.RepairRequestId == request.RepairRequestId);

            if (alreadyReviewed)
            {
                return new CreateReviewResult
                {
                    Status = CreateReviewStatus.AlreadyReviewed
                };
            }

            // 8. Kiem tra KTV neu Customer co chon
            if (dto.TechnicianId.HasValue)
            {
                bool technicianAssigned =
                    await _context.RepairAssignment
                        .AnyAsync(a =>
                            a.RepairRequestId ==
                                request.RepairRequestId &&
                            a.TechnicianId ==
                                dto.TechnicianId.Value &&
                            a.AssignmentStatus ==
                                AcceptedAssignment);

                if (!technicianAssigned)
                {
                    return new CreateReviewResult
                    {
                        Status =
                            CreateReviewStatus.TechnicianNotAssigned
                    };
                }
            }

            // 9. Tao ban ghi Review
            var now = DateTime.UtcNow;

            var review = new Review
            {
                RepairRequestId = request.RepairRequestId,
                CustomerId = request.CustomerId,
                ShopId = request.ShopId,
                TechnicianId = dto.TechnicianId,
                Rating = dto.Rating,
                Comment = string.IsNullOrWhiteSpace(dto.Comment)
                    ? null
                    : dto.Comment.Trim(),
                Status = NewReviewStatus,
                CreatedAt = now
            };

            // 10. Them anh danh gia neu co
            for (int i = 0; i < imageUrls.Count; i++)
            {
                review.ReviewImage.Add(
                    new ReviewImage
                    {
                        ImageUrl = imageUrls[i],
                        DisplayOrder = i + 1,
                        CreatedAt = now
                    });
            }

            // 11. Luu Review va ReviewImage
            _context.Review.Add(review);

            await _context.SaveChangesAsync();

            // 12. Hoan tat transaction
            await transaction.CommitAsync();

            // 13. Tra ket qua
            return new CreateReviewResult
            {
                Status = CreateReviewStatus.Success,
                ReviewId = review.ReviewId,
                RepairRequestId = review.RepairRequestId,
                Rating = review.Rating,
                ImageCount = imageUrls.Count
            };
        }

        // ============================================
        // X17 - XEM DANH GIA CUA SHOP
        // ============================================
        public async Task<GetShopReviewsResult> GetByShopAsync(
            int shopId,
            int page,
            int pageSize)
        {
            // 1. Kiem tra du lieu dau vao
            if (shopId <= 0 ||
                page <= 0 ||
                pageSize <= 0 ||
                pageSize > 50 ||
                (long)(page - 1) * pageSize > int.MaxValue)
            {
                return new GetShopReviewsResult
                {
                    Status = GetShopReviewsStatus.InvalidInput
                };
            }

            // 2. Kiem tra Shop co ton tai khong
            bool shopExists = await _context.Shop
                .AsNoTracking()
                .AnyAsync(s => s.ShopId == shopId);

            if (!shopExists)
            {
                return new GetShopReviewsResult
                {
                    Status = GetShopReviewsStatus.ShopNotFound
                };
            }

            // 3. Lay danh sach danh gia cua Shop
            // Status = 1 hien la ma mac dinh.
            // Can xac nhan voi nhom day la trang thai
            // danh gia duoc phep hien thi cong khai.
            const int VisibleReviewStatus = 1;

            var query = _context.Review
                .AsNoTracking()
                .Where(r =>
                    r.ShopId == shopId &&
                    r.Status == VisibleReviewStatus);

            // 4. Dem tong so danh gia
            int totalCount = await query.CountAsync();

            // 5. Sap xep, phan trang va tra ve DTO
            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .ThenByDescending(r => r.ReviewId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ReviewResponseDto
                {
                    ReviewId = r.ReviewId,
                    ShopId = r.ShopId,
                    TechnicianId = r.TechnicianId,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,

                    Images = r.ReviewImage
                        .OrderBy(img => img.DisplayOrder)
                        .Select(img => new ReviewImageResponseDto
                        {
                            ReviewImageId = img.ReviewImageId,
                            ImageUrl = img.ImageUrl,
                            DisplayOrder = img.DisplayOrder
                        })
                        .ToList()
                })
                .ToListAsync();

            // 6. Dong goi ket qua
            var response = new ShopReviewsResponseDto
            {
                ShopId = shopId,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Reviews = reviews
            };

            return new GetShopReviewsResult
            {
                Status = GetShopReviewsStatus.Success,
                Data = response
            };
        }

        // ============================================
        // X18 - MANAGER PHAN HOI DANH GIA
        // ============================================
        public async Task<CreateReviewReplyResult> ReplyAsync(
            int reviewId,
            int managerUserId,
            CreateReviewReplyDto dto)
        {
            // 1. Kiem tra du lieu dau vao
            if (reviewId <= 0 ||
                managerUserId <= 0 ||
                dto == null ||
                string.IsNullOrWhiteSpace(dto.ReplyContent))
            {
                return new CreateReviewReplyResult
                {
                    Status = CreateReviewReplyStatus.InvalidInput
                };
            }

            string content = dto.ReplyContent.Trim();

            if (content.Length > 2000)
            {
                return new CreateReviewReplyResult
                {
                    Status = CreateReviewReplyStatus.InvalidInput
                };
            }

            // 2. Transaction han che phan hoi trung
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            // 3. Tim danh gia cua Shop do Manager quan ly
            var review = await _context.Review
                .AsNoTracking()
                .Where(r =>
                    r.ReviewId == reviewId &&
                    r.Shop.Manager.UserId == managerUserId)
                .Select(r => new
                {
                    r.ReviewId
                })
                .FirstOrDefaultAsync();

            if (review == null)
            {
                return new CreateReviewReplyResult
                {
                    Status = CreateReviewReplyStatus.ReviewNotFound
                };
            }

            // 4. Kiem tra danh gia da co phan hoi chua
            bool alreadyReplied = await _context.ReviewReply
                .AnyAsync(r => r.ReviewId == reviewId);

            if (alreadyReplied)
            {
                return new CreateReviewReplyResult
                {
                    Status = CreateReviewReplyStatus.AlreadyReplied
                };
            }

            // 5. Tao phan hoi moi
            var reply = new ReviewReply
            {
                ReviewId = reviewId,
                RepliedByUserId = managerUserId,
                ReplyContent = content,
                CreatedAt = DateTime.UtcNow
            };

            // 6. Luu vao database
            _context.ReviewReply.Add(reply);

            await _context.SaveChangesAsync();

            // 7. Hoan tat transaction
            await transaction.CommitAsync();

            // 8. Tra ket qua
            return new CreateReviewReplyResult
            {
                Status = CreateReviewReplyStatus.Success,
                ReplyId = reply.ReviewReplyId,
                ReviewId = reply.ReviewId,
                ReplyContent = reply.ReplyContent
            };
        }

    }
}

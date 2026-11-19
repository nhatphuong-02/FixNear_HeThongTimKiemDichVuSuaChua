using System.Security.Claims;
using FixNear.Domain.Models.Messaging;
using FixNear.Features.Messaging.Hubs;
using FixNear.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Features.Messaging.Controllers;

[ApiController]
[Authorize(Roles = "1,2,3")]
[Route("api/conversations/{conversationId:int}/attachments")]
public sealed class MessageAttachmentController : ControllerBase
{
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private readonly FixNearDbContext _db;
    private readonly IWebHostEnvironment _environment;
    private readonly IHubContext<ChatHub> _hub;
    private readonly ILogger<MessageAttachmentController> _logger;

    public MessageAttachmentController(
        FixNearDbContext db, IWebHostEnvironment environment,
        IHubContext<ChatHub> hub, ILogger<MessageAttachmentController> logger)
    {
        _db = db;
        _environment = environment;
        _hub = hub;
        _logger = logger;
    }

    private int? CurrentUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id : null;

    // Kiểm tra cả quyền thành viên lẫn quyền nghiệp vụ hiện hành.
    private Task<bool> CanAccessAsync(int conversationId, int userId) =>
        _db.Conversation.AsNoTracking().AnyAsync(c =>
            c.ConversationId == conversationId && !c.IsDeleted && c.Status == 1 &&
            c.RepairRequestId != null &&
            c.ConversationMember.Any(m => m.UserId == userId && m.LeftAt == null) &&
            (c.RepairRequest!.Customer.UserId == userId ||
             c.RepairRequest.Shop.ShopMembers.Any(sm =>
                 sm.Manager.UserId == userId && sm.LeftAt == null &&
                 sm.Status == FixNear.Enums.ShopMemberStatus.Active &&
                 (sm.ShopRole == FixNear.Enums.ShopRole.Owner ||
                  sm.ShopRole == FixNear.Enums.ShopRole.Manager)) ||
             c.RepairRequest.RepairAssignment.Any(a =>
                 a.Technician.UserId == userId && a.AssignmentStatus == 2)));

    // Ảnh được lưu ngoài wwwroot, chỉ tải được qua API xác thực.
    private string PrivateFolder =>
        Path.Combine(_environment.ContentRootPath, "App_Data", "message-images");

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxImageBytes + 1024 * 1024)]
    public async Task<IActionResult> Upload(
        int conversationId, [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue) return Unauthorized();
        if (!await CanAccessAsync(conversationId, userId.Value)) return NotFound();
        if (file is null || file.Length == 0 || file.Length > MaxImageBytes)
            return BadRequest(new { message = "Ảnh phải lớn hơn 0 và tối đa 5 MB." });

        // Chỉ cho JPEG và PNG, kiểm tra magic bytes thay vì chỉ tin MIME/extension.
        byte[] signature = new byte[8];
        await using (var input = file.OpenReadStream())
        {
            var read = 0;
            while (read < signature.Length)
            {
                int n = await input.ReadAsync(
                    signature.AsMemory(read, signature.Length - read), cancellationToken);
                if (n == 0) break;
                read += n;
            }
        }
        var jpeg = signature[0] == 0xFF && signature[1] == 0xD8 &&
                   signature[2] == 0xFF;
        var png = signature.SequenceEqual(new byte[]
            { 137, 80, 78, 71, 13, 10, 26, 10 });
        if (!jpeg && !png)
            return BadRequest(new { message = "Chỉ hỗ trợ ảnh JPEG hoặc PNG." });

        var mediaType = png ? "image/png" : "image/jpeg";
        var fileName = $"{Guid.NewGuid():N}" + (png ? ".png" : ".jpg");
        Directory.CreateDirectory(PrivateFolder);
        var path = Path.Combine(PrivateFolder, fileName);

        // Tệp chỉ lưu dưới tên sinh ngẫu nhiên; tên do client gửi không được sử dụng.
        var saved = false;
        try
        {
            await using (var output = new FileStream(path, FileMode.CreateNew,
                FileAccess.Write, FileShare.None, 81920, useAsync: true))
                await file.CopyToAsync(output, cancellationToken);

            var now = DateTime.UtcNow;
            var message = new Message
            {
                ConversationId = conversationId,
                SenderUserId = userId.Value,
                Content = null,
                CreatedAt = now,
                IsDeleted = false,
                MessageAttachment = new List<MessageAttachment>
                {
                    new() { FileUrl = fileName, FileType = mediaType, CreatedAt = now }
                }
            };
            _db.Message.Add(message);
            await _db.SaveChangesAsync(cancellationToken);
            saved = true;

            var attachmentId = message.MessageAttachment.Single().MessageAttachmentId;
            var payload = new
            {
                message.MessageId,
                message.ConversationId,
                message.SenderUserId,
                message.Content,
                message.CreatedAt,
                attachments = new[] { new { messageAttachmentId = attachmentId, fileType = mediaType } }
            };

            // Phát tới user còn quyền ngay lúc gửi, không phát theo group cũ.
            try
            {
                var recipients = await _db.ConversationMember.AsNoTracking()
                    .Where(m => m.ConversationId == conversationId &&
                        m.LeftAt == null && m.UserId != userId.Value &&
                        (m.Conversation.RepairRequest!.Customer.UserId == m.UserId ||
                         m.Conversation.RepairRequest.Shop.ShopMembers.Any(sm =>
                             sm.Manager.UserId == m.UserId &&
                             sm.Status == FixNear.Enums.ShopMemberStatus.Active &&
                             sm.LeftAt == null &&
                             (sm.ShopRole == FixNear.Enums.ShopRole.Owner ||
                              sm.ShopRole == FixNear.Enums.ShopRole.Manager)) ||
                         m.Conversation.RepairRequest.RepairAssignment.Any(a =>
                             a.Technician.UserId == m.UserId && a.AssignmentStatus == 2)))
                    .Select(m => m.UserId.ToString())
                    .ToListAsync(cancellationToken);
                await _hub.Clients.Users(recipients).SendAsync(
                    "ReceiveMessage", payload, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Message image saved, realtime delivery failed");
            }

            return StatusCode(201, payload);
        }
        catch
        {
            // Nếu DB lưu thất bại, dọn file rác; nếu DB đã lưu thì không xóa.
            if (!saved && System.IO.File.Exists(path)) System.IO.File.Delete(path);
            throw;
        }
    }

    [HttpGet("{attachmentId:int}")]
    public async Task<IActionResult> Download(
        int conversationId, int attachmentId, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue) return Unauthorized();
        if (!await CanAccessAsync(conversationId, userId.Value)) return NotFound();

        var attachment = await _db.MessageAttachment.AsNoTracking()
            .Where(a => a.MessageAttachmentId == attachmentId &&
                a.Message.ConversationId == conversationId && !a.Message.IsDeleted)
            .Select(a => new { a.FileUrl, a.FileType })
            .FirstOrDefaultAsync(cancellationToken);
        if (attachment == null) return NotFound();

        // Bảo vệ path traversal; FileUrl chỉ chứa tên ngẫu nhiên do server tạo.
        if (Path.GetFileName(attachment.FileUrl) != attachment.FileUrl ||
            attachment.FileUrl.Contains("..", StringComparison.Ordinal)) return NotFound();
        var path = Path.Combine(PrivateFolder, attachment.FileUrl);
        if (!System.IO.File.Exists(path)) return NotFound();

        return PhysicalFile(path, attachment.FileType ?? "application/octet-stream",
            enableRangeProcessing: true);
    }
}

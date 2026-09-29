using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class MessageAttachment
{
    public int MessageAttachmentId { get; set; }

    public int MessageId { get; set; }

    public string FileUrl { get; set; } = null!;

    public string? FileType { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Message Message { get; set; } = null!;
}

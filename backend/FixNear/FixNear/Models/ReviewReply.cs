using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class ReviewReply
{
    public int ReviewReplyId { get; set; }

    public int ReviewId { get; set; }

    public int RepliedByUserId { get; set; }

    public string ReplyContent { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual User RepliedByUser { get; set; } = null!;

    public virtual Review Review { get; set; } = null!;
}

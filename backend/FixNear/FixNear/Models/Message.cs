using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class Message
{
    public int MessageId { get; set; }

    public int ConversationId { get; set; }

    public int SenderUserId { get; set; }

    public string? Content { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Conversation Conversation { get; set; } = null!;

    public virtual ConversationMember ConversationMember { get; set; } = null!;

    public virtual ICollection<MessageAttachment> MessageAttachment { get; set; } = new List<MessageAttachment>();

    public virtual User SenderUser { get; set; } = null!;
}

using FixNear.Domain.Models.Identity;
using System;
using System.Collections.Generic;

namespace FixNear.Domain.Models.Messaging;

public partial class ConversationMember
{
    public int ConversationMemberId { get; set; }

    public int ConversationId { get; set; }

    public int UserId { get; set; }

    public DateTime JoinedAt { get; set; }

    public DateTime? LeftAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastReadAt { get; set; }

    public virtual Conversation Conversation { get; set; } = null!;

    public virtual ICollection<Message> Message { get; set; } = new List<Message>();

    public virtual User User { get; set; } = null!;
}

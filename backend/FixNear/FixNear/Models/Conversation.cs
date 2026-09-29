using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class Conversation
{
    public int ConversationId { get; set; }

    public int? RepairRequestId { get; set; }

    public int Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<ConversationMember> ConversationMember { get; set; } = new List<ConversationMember>();

    public virtual ICollection<Message> Message { get; set; } = new List<Message>();

    public virtual RepairRequest? RepairRequest { get; set; }
}

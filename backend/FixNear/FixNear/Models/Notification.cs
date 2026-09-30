using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public int? TemplateId { get; set; }

    public string Title { get; set; } = null!;

    public string? Body { get; set; }

    public int? RefType { get; set; }

    public int? RefId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual NotificationTemplate? Template { get; set; }

    public virtual ICollection<UserNotification> UserNotification { get; set; } = new List<UserNotification>();
}

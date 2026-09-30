using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class NotificationTemplate
{
    public int NotificationTemplateId { get; set; }

    public string TemplateCode { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? BodyTemplate { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Notification> Notification { get; set; } = new List<Notification>();
}

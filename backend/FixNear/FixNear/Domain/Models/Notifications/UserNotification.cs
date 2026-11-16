using FixNear.Domain.Models.Identity;
using System;
using System.Collections.Generic;

namespace FixNear.Domain.Models.Notifications;

public partial class UserNotification
{
    public int UserNotificationId { get; set; }

    public int NotificationId { get; set; }

    public int UserId { get; set; }

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Notification Notification { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}

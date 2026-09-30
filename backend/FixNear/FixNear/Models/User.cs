using System;
using System.Collections.Generic;

namespace FixNear.Models;

public partial class User
{
    public int UserId { get; set; }

    public int RoleType { get; set; }

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string PasswordHash { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public bool? Gender { get; set; }

    public int Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public virtual ICollection<ConversationMember> ConversationMember { get; set; } = new List<ConversationMember>();

    public virtual CustomerProfile? CustomerProfile { get; set; }

    public virtual ManagerProfile? ManagerProfile { get; set; }

    public virtual ICollection<Message> Message { get; set; } = new List<Message>();

    public virtual ICollection<QuoteHistory> QuoteHistory { get; set; } = new List<QuoteHistory>();

    public virtual ICollection<RepairAssignment> RepairAssignment { get; set; } = new List<RepairAssignment>();

    public virtual ICollection<RepairRequestStatusHistory> RepairRequestStatusHistory { get; set; } = new List<RepairRequestStatusHistory>();

    public virtual ICollection<RepairSchedule> RepairSchedule { get; set; } = new List<RepairSchedule>();

    public virtual ICollection<ReviewReply> ReviewReply { get; set; } = new List<ReviewReply>();

    public virtual TechnicianProfile? TechnicianProfile { get; set; }

    public virtual ICollection<UserNotification> UserNotification { get; set; } = new List<UserNotification>();

    public virtual ICollection<WarrantyHistory> WarrantyHistory { get; set; } = new List<WarrantyHistory>();
}

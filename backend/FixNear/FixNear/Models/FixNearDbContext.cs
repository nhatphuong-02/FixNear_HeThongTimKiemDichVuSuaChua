using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Models;

public partial class FixNearDbContext : DbContext
{
    internal readonly object ServiceCategories;

    public FixNearDbContext()
    {
    }

    public FixNearDbContext(DbContextOptions<FixNearDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Address> Address { get; set; }

    public virtual DbSet<Conversation> Conversation { get; set; }

    public virtual DbSet<ConversationMember> ConversationMember { get; set; }

    public virtual DbSet<CustomerProfile> CustomerProfile { get; set; }

    public virtual DbSet<Invoice> Invoice { get; set; }

    public virtual DbSet<InvoiceDetail> InvoiceDetail { get; set; }

    public virtual DbSet<Location> Location { get; set; }

    public virtual DbSet<ManagerProfile> ManagerProfile { get; set; }

    public virtual DbSet<Message> Message { get; set; }

    public virtual DbSet<MessageAttachment> MessageAttachment { get; set; }

    public virtual DbSet<Notification> Notification { get; set; }

    public virtual DbSet<NotificationTemplate> NotificationTemplate { get; set; }

    public virtual DbSet<Payment> Payment { get; set; }

    public virtual DbSet<PaymentTransaction> PaymentTransaction { get; set; }

    public virtual DbSet<Province> Province { get; set; }

    public virtual DbSet<Quote> Quote { get; set; }

    public virtual DbSet<QuoteDetail> QuoteDetail { get; set; }

    public virtual DbSet<QuoteHistory> QuoteHistory { get; set; }

    public virtual DbSet<RepairAssignment> RepairAssignment { get; set; }

    public virtual DbSet<RepairItem> RepairItem { get; set; }

    public virtual DbSet<RepairPart> RepairPart { get; set; }

    public virtual DbSet<RepairPartUsage> RepairPartUsage { get; set; }

    public virtual DbSet<RepairReport> RepairReport { get; set; }

    public virtual DbSet<RepairRequest> RepairRequest { get; set; }

    public virtual DbSet<RepairRequestImage> RepairRequestImage { get; set; }

    public virtual DbSet<RepairRequestService> RepairRequestService { get; set; }

    public virtual DbSet<RepairRequestStatusHistory> RepairRequestStatusHistory { get; set; }

    public virtual DbSet<RepairSchedule> RepairSchedule { get; set; }

    public virtual DbSet<Review> Review { get; set; }

    public virtual DbSet<ReviewImage> ReviewImage { get; set; }

    public virtual DbSet<ReviewReply> ReviewReply { get; set; }

    public virtual DbSet<Service> Service { get; set; }

    public virtual DbSet<ServiceCategory> ServiceCategory { get; set; }

    public virtual DbSet<ServicePrice> ServicePrice { get; set; }

    public virtual DbSet<Shop> Shop { get; set; }

    public virtual DbSet<ShopImage> ShopImage { get; set; }

    public virtual DbSet<ShopService> ShopService { get; set; }

    public virtual DbSet<TechnicianProfile> TechnicianProfile { get; set; }

    public virtual DbSet<TechnicianShop> TechnicianShop { get; set; }

    public virtual DbSet<TechnicianSpecialization> TechnicianSpecialization { get; set; }

    public virtual DbSet<User> User { get; set; }

    public virtual DbSet<UserNotification> UserNotification { get; set; }

    public virtual DbSet<Ward> Ward { get; set; }

    public virtual DbSet<Warranty> Warranty { get; set; }

    public virtual DbSet<WarrantyHistory> WarrantyHistory { get; set; }

    public virtual DbSet<WarrantyRequest> WarrantyRequest { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Vietnamese_CI_AS");

        // ============================================================
        // ADDRESS
        // ============================================================

        modelBuilder.Entity<Address>(entity =>
        {
            entity.ToTable(tb =>
                tb.HasTrigger("trg_Address_PreventContentUpdate"));

            entity.HasIndex(e => e.CustomerId, "IX_Address_CustomerId");

            entity.HasIndex(e => e.ProvinceId, "IX_Address_ProvinceId");

            entity.HasIndex(e => e.SupersededByAddressId,
                "IX_Address_SupersededByAddressId");

            entity.HasIndex(e => e.WardId, "IX_Address_WardId");

            entity.HasIndex(
                e => new { e.AddressId, e.CustomerId },
                "UQ_Address_AddressCustomer")
                .IsUnique();

            entity.Property(e => e.AddressLine)
                .HasMaxLength(500);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_Address_CreatedAt");

            // CustomerId có thể NULL vì Address có thể thuộc Shop
            entity.Property(e => e.CustomerId);

            entity.Property(e => e.RecipientName)
                .HasMaxLength(200);

            entity.Property(e => e.RecipientPhone)
                .HasMaxLength(20);

            entity.HasOne(d => d.Customer)
                .WithMany(p => p.Address)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_Address_Customer");

            entity.HasOne(d => d.Province)
                .WithMany(p => p.Address)
                .HasForeignKey(d => d.ProvinceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Address_Province");

            entity.HasOne(d => d.SupersededByAddress)
                .WithMany(p => p.InverseSupersededByAddress)
                .HasForeignKey(d => d.SupersededByAddressId)
                .HasConstraintName("FK_Address_Superseded");

            entity.HasOne(d => d.Ward)
                .WithMany(p => p.AddressWard)
                .HasForeignKey(d => d.WardId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Address_Ward");

            entity.HasOne(d => d.WardNavigation)
                .WithMany(p => p.AddressWardNavigation)
                .HasPrincipalKey(p =>
                    new { p.WardId, p.ProvinceId })
                .HasForeignKey(d =>
                    new { d.WardId, d.ProvinceId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Address_WardProvince");
        });

        // ============================================================
        // CONVERSATION
        // ============================================================

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.ToTable(tb =>
                tb.HasTrigger("trg_Conversation_SetUpdatedAt"));

            entity.HasIndex(
                e => e.RepairRequestId,
                "IX_Conversation_RepairRequestId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_Conversation_CreatedAt");

            entity.Property(e => e.Status)
                .HasDefaultValue(1, "DF_Conversation_Status");

            entity.HasOne(d => d.RepairRequest)
                .WithMany(p => p.Conversation)
                .HasForeignKey(d => d.RepairRequestId)
                .HasConstraintName("FK_Conversation_Request");
        });

        // ============================================================
        // CONVERSATION MEMBER
        // ============================================================

        modelBuilder.Entity<ConversationMember>(entity =>
        {
            entity.HasIndex(
                e => e.UserId,
                "IX_ConversationMember_UserId");

            entity.HasIndex(
                e => new { e.ConversationId, e.UserId },
                "UQ_ConversationMember")
                .IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_ConversationMember_CreatedAt");

            entity.Property(e => e.JoinedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_ConversationMember_JoinedAt");

            entity.HasOne(d => d.Conversation)
                .WithMany(p => p.ConversationMember)
                .HasForeignKey(d => d.ConversationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ConversationMember_Conversation");

            entity.HasOne(d => d.User)
                .WithMany(p => p.ConversationMember)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ConversationMember_User");
        });

        // ============================================================
        // CUSTOMER PROFILE
        // ============================================================

        modelBuilder.Entity<CustomerProfile>(entity =>
        {
            entity.HasKey(e => e.CustomerId);

            entity.ToTable(tb =>
                tb.HasTrigger("trg_CustomerProfile_SetUpdatedAt"));

            entity.HasIndex(
                e => e.DefaultAddressId,
                "IX_CustomerProfile_DefaultAddressId");

            entity.HasIndex(
                e => e.UserId,
                "UX_CustomerProfile_User")
                .IsUnique();

            entity.Property(e => e.CustomerId)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_CustomerProfile_CreatedAt");

            entity.HasOne(d => d.User)
                .WithOne(p => p.CustomerProfile)
                .HasForeignKey<CustomerProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CustomerProfile_User");

            // EF chỉ map DefaultAddressId -> AddressId.
            // Database vẫn giữ composite FK để kiểm tra
            // DefaultAddressId + CustomerId.
            entity.HasOne(d => d.AddressNavigation)
                .WithMany()
                .HasForeignKey(d => d.DefaultAddressId)
                .HasPrincipalKey(p => p.AddressId)
                .HasConstraintName("FK_CustomerProfile_DefaultAddress");
        });

        // ============================================================
        // INVOICE
        // ============================================================

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasIndex(
                e => e.CustomerId,
                "IX_Invoice_CustomerId");

            entity.HasIndex(
                e => e.RepairRequestId,
                "IX_Invoice_RepairRequestId");

            entity.HasIndex(
                e => e.ShopId,
                "IX_Invoice_ShopId");

            entity.HasIndex(
                e => e.InvoiceNumber,
                "UX_Invoice_Number")
                .IsUnique();

            entity.Property(e => e.InvoiceNumber)
                .HasMaxLength(50);

            entity.Property(e => e.IssuedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_Invoice_IssuedAt");

            entity.Property(e => e.Status)
                .HasDefaultValue(1, "DF_Invoice_Status");

            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Customer)
                .WithMany(p => p.Invoice)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoice_Customer");

            entity.HasOne(d => d.RepairRequest)
                .WithMany(p => p.InvoiceRepairRequest)
                .HasForeignKey(d => d.RepairRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoice_Request");

            entity.HasOne(d => d.Shop)
                .WithMany(p => p.Invoice)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoice_Shop");

            entity.HasOne(d => d.RepairRequestNavigation)
                .WithMany(p => p.InvoiceRepairRequestNavigation)
                .HasPrincipalKey(p =>
                    new
                    {
                        p.RepairRequestId,
                        p.CustomerId,
                        p.ShopId
                    })
                .HasForeignKey(d =>
                    new
                    {
                        d.RepairRequestId,
                        d.CustomerId,
                        d.ShopId
                    })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoice_RequestRef");
        });

        // ============================================================
        // INVOICE DETAIL
        // ============================================================

        modelBuilder.Entity<InvoiceDetail>(entity =>
        {
            entity.ToTable(tb =>
                tb.HasTrigger("trg_InvoiceDetail_SyncTotal"));

            entity.HasIndex(
                e => e.InvoiceId,
                "IX_InvoiceDetail_InvoiceId");

            entity.Property(e => e.Amount)
                .HasComputedColumnSql(
                    "(CONVERT([decimal](18,2),[Quantity]*[UnitPrice]))",
                    true)
                .HasColumnType("decimal(18, 2)");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_InvoiceDetail_CreatedAt");

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.Quantity)
                .HasColumnType("decimal(18, 2)");

            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Invoice)
                .WithMany(p => p.InvoiceDetail)
                .HasForeignKey(d => d.InvoiceId)
                .HasConstraintName("FK_InvoiceDetail_Invoice");
        });

        // ============================================================
        // LOCATION
        // ============================================================

        modelBuilder.Entity<Location>(entity =>
        {
            entity.ToTable(tb =>
                tb.HasTrigger("trg_Location_SetUpdatedAt"));

            entity.HasIndex(
                e => new { e.Latitude, e.Longitude },
                "IX_Location_LatLng");

            entity.HasIndex(
                e => e.AddressId,
                "UX_Location_Address")
                .IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_Location_CreatedAt");

            entity.Property(e => e.Latitude)
                .HasColumnType("decimal(9, 6)");

            entity.Property(e => e.Longitude)
                .HasColumnType("decimal(9, 6)");

            entity.HasOne(d => d.Address)
                .WithOne(p => p.Location)
                .HasForeignKey<Location>(d => d.AddressId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Location_Address");
        });

        // ============================================================
        // MANAGER PROFILE
        // ============================================================

        modelBuilder.Entity<ManagerProfile>(entity =>
        {
            entity.HasKey(e => e.ManagerId);

            entity.ToTable(tb =>
                tb.HasTrigger("trg_ManagerProfile_SetUpdatedAt"));

            entity.HasIndex(
                e => e.UserId,
                "UX_ManagerProfile_User")
                .IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_ManagerProfile_CreatedAt");

            entity.HasOne(d => d.User)
                .WithOne(p => p.ManagerProfile)
                .HasForeignKey<ManagerProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ManagerProfile_User");
        });

        // ============================================================
        // MESSAGE
        // ============================================================

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasIndex(
                e => new { e.ConversationId, e.CreatedAt },
                "IX_Message_Conversation");

            entity.HasIndex(
                e => e.SenderUserId,
                "IX_Message_SenderUserId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_Message_CreatedAt");

            entity.HasOne(d => d.Conversation)
                .WithMany(p => p.Message)
                .HasForeignKey(d => d.ConversationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Message_Conversation");

            entity.HasOne(d => d.SenderUser)
                .WithMany(p => p.Message)
                .HasForeignKey(d => d.SenderUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Message_Sender");

            entity.HasOne(d => d.ConversationMember)
                .WithMany(p => p.Message)
                .HasPrincipalKey(p =>
                    new
                    {
                        p.ConversationId,
                        p.UserId
                    })
                .HasForeignKey(d =>
                    new
                    {
                        d.ConversationId,
                        d.SenderUserId
                    })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Message_Member");
        });

        // ============================================================
        // MESSAGE ATTACHMENT
        // ============================================================

        modelBuilder.Entity<MessageAttachment>(entity =>
        {
            entity.HasIndex(
                e => e.MessageId,
                "IX_MessageAttachment_MessageId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_MessageAttachment_CreatedAt");

            entity.Property(e => e.FileType)
                .HasMaxLength(50);

            entity.Property(e => e.FileUrl)
                .HasMaxLength(500);

            entity.HasOne(d => d.Message)
                .WithMany(p => p.MessageAttachment)
                .HasForeignKey(d => d.MessageId)
                .HasConstraintName("FK_MessageAttachment_Message");
        });

        // ============================================================
        // NOTIFICATION
        // ============================================================

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasIndex(
                e => e.TemplateId,
                "IX_Notification_TemplateId");

            entity.Property(e => e.Body)
                .HasMaxLength(2000);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_Notification_CreatedAt");

            entity.Property(e => e.Title)
                .HasMaxLength(200);

            entity.HasOne(d => d.Template)
                .WithMany(p => p.Notification)
                .HasForeignKey(d => d.TemplateId)
                .HasConstraintName("FK_Notification_Template");
        });

        // ============================================================
        // NOTIFICATION TEMPLATE
        // ============================================================

        modelBuilder.Entity<NotificationTemplate>(entity =>
        {
            entity.HasIndex(
                e => e.TemplateCode,
                "UX_NotificationTemplate_Code")
                .IsUnique();

            entity.Property(e => e.BodyTemplate)
                .HasMaxLength(2000);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_NotificationTemplate_CreatedAt");

            entity.Property(e => e.TemplateCode)
                .HasMaxLength(50);

            entity.Property(e => e.Title)
                .HasMaxLength(200);
        });

        // ============================================================
        // PAYMENT
        // ============================================================

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable(tb =>
                tb.HasTrigger("trg_Payment_SetUpdatedAt"));

            entity.HasIndex(
                e => e.InvoiceId,
                "IX_Payment_InvoiceId");

            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_Payment_CreatedAt");

            entity.Property(e => e.Status)
                .HasDefaultValue(1, "DF_Payment_Status");

            entity.HasOne(d => d.Invoice)
                .WithMany(p => p.Payment)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payment_Invoice");
        });

        // ============================================================
        // PAYMENT TRANSACTION
        // ============================================================

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasIndex(
                e => e.PaymentId,
                "IX_PaymentTransaction_PaymentId");

            entity.HasIndex(
                e => e.TransactionCode,
                "UX_PaymentTransaction_Code")
                .IsUnique()
                .HasFilter("([TransactionCode] IS NOT NULL)");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_PaymentTransaction_CreatedAt");

            entity.Property(e => e.Message)
                .HasMaxLength(500);

            entity.Property(e => e.TransactionCode)
                .HasMaxLength(100);

            entity.HasOne(d => d.Payment)
                .WithMany(p => p.PaymentTransaction)
                .HasForeignKey(d => d.PaymentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PaymentTransaction_Payment");
        });

        // ============================================================
        // PROVINCE
        // ============================================================

        modelBuilder.Entity<Province>(entity =>
        {
            entity.HasIndex(
                e => e.ProvinceCode,
                "UX_Province_Code")
                .IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_Province_CreatedAt");

            entity.Property(e => e.ProvinceCode)
                .HasMaxLength(20);

            entity.Property(e => e.ProvinceName)
                .HasMaxLength(200);
        });

        // ============================================================
        // QUOTE
        // ============================================================

        modelBuilder.Entity<Quote>(entity =>
        {
            entity.ToTable(tb =>
                tb.HasTrigger("trg_Quote_SetUpdatedAt"));

            entity.HasIndex(
                e => e.RepairRequestId,
                "IX_Quote_RepairRequestId");

            entity.HasIndex(
                e => e.TechnicianId,
                "IX_Quote_TechnicianId");

            entity.HasIndex(
                e => e.QuoteNumber,
                "UX_Quote_Number")
                .IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_Quote_CreatedAt");

            entity.Property(e => e.QuoteNumber)
                .HasMaxLength(50);

            entity.Property(e => e.Status)
                .HasDefaultValue(1, "DF_Quote_Status");

            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.RepairRequest)
                .WithMany(p => p.Quote)
                .HasForeignKey(d => d.RepairRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Quote_Request");

            entity.HasOne(d => d.Technician)
                .WithMany(p => p.Quote)
                .HasForeignKey(d => d.TechnicianId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Quote_Technician");
        });

        // ============================================================
        // QUOTE DETAIL
        // ============================================================

        modelBuilder.Entity<QuoteDetail>(entity =>
        {
            entity.ToTable(tb =>
                tb.HasTrigger("trg_QuoteDetail_SyncTotal"));

            entity.HasIndex(
                e => e.QuoteId,
                "IX_QuoteDetail_QuoteId");

            entity.HasIndex(
                e => e.RepairItemRefId,
                "IX_QuoteDetail_RepairItemRefId");

            entity.HasIndex(
                e => e.RepairPartRefId,
                "IX_QuoteDetail_RepairPartRefId");

            entity.HasIndex(
                e => e.ServiceRefId,
                "IX_QuoteDetail_ServiceRefId");

            entity.Property(e => e.Amount)
                .HasComputedColumnSql(
                    "(CONVERT([decimal](18,2),[Quantity]*[UnitPrice]))",
                    true)
                .HasColumnType("decimal(18, 2)");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_QuoteDetail_CreatedAt");

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.Quantity)
                .HasColumnType("decimal(18, 2)");

            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Quote)
                .WithMany(p => p.QuoteDetail)
                .HasForeignKey(d => d.QuoteId)
                .HasConstraintName("FK_QuoteDetail_Quote");

            entity.HasOne(d => d.RepairItemRef)
                .WithMany(p => p.QuoteDetail)
                .HasForeignKey(d => d.RepairItemRefId)
                .HasConstraintName("FK_QuoteDetail_RepairItem");

            entity.HasOne(d => d.RepairPartRef)
                .WithMany(p => p.QuoteDetail)
                .HasForeignKey(d => d.RepairPartRefId)
                .HasConstraintName("FK_QuoteDetail_RepairPart");

            entity.HasOne(d => d.ServiceRef)
                .WithMany(p => p.QuoteDetail)
                .HasForeignKey(d => d.ServiceRefId)
                .HasConstraintName("FK_QuoteDetail_Service");
        });

        // ============================================================
        // QUOTE HISTORY
        // ============================================================

        modelBuilder.Entity<QuoteHistory>(entity =>
        {
            entity.HasIndex(
                e => e.ChangedByUserId,
                "IX_QuoteHistory_ChangedByUserId");

            entity.HasIndex(
                e => e.QuoteId,
                "IX_QuoteHistory_QuoteId");

            entity.Property(e => e.ChangeNote)
                .HasMaxLength(500);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_QuoteHistory_CreatedAt");

            entity.Property(e => e.SnapshotTotalAmount)
                .HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.ChangedByUser)
                .WithMany(p => p.QuoteHistory)
                .HasForeignKey(d => d.ChangedByUserId)
                .HasConstraintName("FK_QuoteHistory_User");

            entity.HasOne(d => d.Quote)
                .WithMany(p => p.QuoteHistory)
                .HasForeignKey(d => d.QuoteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QuoteHistory_Quote");
        });

        // ============================================================
        // REPAIR ASSIGNMENT
        // ============================================================

        modelBuilder.Entity<RepairAssignment>(entity =>
        {
            entity.HasIndex(
                e => e.AssignedByUserId,
                "IX_RepairAssignment_AssignedByUserId");

            entity.HasIndex(
                e => e.RepairRequestId,
                "IX_RepairAssignment_RepairRequestId");

            entity.HasIndex(
                e => e.TechnicianId,
                "IX_RepairAssignment_TechnicianId");

            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_RepairAssignment_AssignedAt");

            entity.Property(e => e.AssignmentStatus)
                .HasDefaultValue(
                    1,
                    "DF_RepairAssignment_Status");

            entity.Property(e => e.Note)
                .HasMaxLength(500);

            entity.Property(e => e.RejectionReason)
                .HasMaxLength(500);

            entity.HasOne(d => d.AssignedByUser)
                .WithMany(p => p.RepairAssignment)
                .HasForeignKey(d => d.AssignedByUserId)
                .HasConstraintName("FK_RepairAssignment_AssignedBy");

            entity.HasOne(d => d.RepairRequest)
                .WithMany(p => p.RepairAssignment)
                .HasForeignKey(d => d.RepairRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RepairAssignment_Request");

            entity.HasOne(d => d.Technician)
                .WithMany(p => p.RepairAssignment)
                .HasForeignKey(d => d.TechnicianId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RepairAssignment_Technician");
        });

        // ============================================================
        // REPAIR ITEM
        // ============================================================

        modelBuilder.Entity<RepairItem>(entity =>
        {
            entity.ToTable(tb =>
                tb.HasTrigger("trg_RepairItem_SetUpdatedAt"));

            entity.HasIndex(
                e => e.ItemCode,
                "UX_RepairItem_Code")
                .IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_RepairItem_CreatedAt");

            entity.Property(e => e.DefaultPrice)
                .HasColumnType("decimal(18, 2)");

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true, "DF_RepairItem_IsActive");

            entity.Property(e => e.ItemCode)
                .HasMaxLength(50);

            entity.Property(e => e.ItemName)
                .HasMaxLength(200);
        });

        // ============================================================
        // REPAIR PART
        // ============================================================

        modelBuilder.Entity<RepairPart>(entity =>
        {
            entity.ToTable(tb =>
                tb.HasTrigger("trg_RepairPart_SetUpdatedAt"));

            entity.HasIndex(
                e => e.PartCode,
                "UX_RepairPart_Code")
                .IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_RepairPart_CreatedAt");

            entity.Property(e => e.CurrentPrice)
                .HasColumnType("decimal(18, 2)");

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true, "DF_RepairPart_IsActive");

            entity.Property(e => e.PartCode)
                .HasMaxLength(50);

            entity.Property(e => e.PartName)
                .HasMaxLength(200);

            entity.Property(e => e.Unit)
                .HasMaxLength(50);
        });

        // ============================================================
        // REPAIR PART USAGE
        // ============================================================

        modelBuilder.Entity<RepairPartUsage>(entity =>
        {
            entity.HasIndex(
                e => e.RepairPartId,
                "IX_RepairPartUsage_RepairPartId");

            entity.HasIndex(
                e => e.RepairRequestId,
                "IX_RepairPartUsage_RepairRequestId");

            entity.Property(e => e.Amount)
                .HasComputedColumnSql(
                    "(CONVERT([decimal](18,2),[Quantity]*[UnitPriceAtUsage]))",
                    true)
                .HasColumnType("decimal(18, 2)");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_RepairPartUsage_CreatedAt");

            entity.Property(e => e.Note)
                .HasMaxLength(500);

            entity.Property(e => e.Quantity)
                .HasColumnType("decimal(18, 2)");

            entity.Property(e => e.UnitPriceAtUsage)
                .HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.RepairPart)
                .WithMany(p => p.RepairPartUsage)
                .HasForeignKey(d => d.RepairPartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RepairPartUsage_Part");

            entity.HasOne(d => d.RepairRequest)
                .WithMany(p => p.RepairPartUsage)
                .HasForeignKey(d => d.RepairRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RepairPartUsage_Request");
        });

        // ============================================================
        // REPAIR REPORT
        // ============================================================

        modelBuilder.Entity<RepairReport>(entity =>
        {
            entity.ToTable(tb =>
                tb.HasTrigger("trg_RepairReport_SetUpdatedAt"));

            entity.HasIndex(
                e => e.RepairRequestId,
                "IX_RepairReport_RepairRequestId");

            entity.HasIndex(
                e => e.TechnicianId,
                "IX_RepairReport_TechnicianId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_RepairReport_CreatedAt");

            entity.Property(e => e.Diagnosis)
                .HasMaxLength(2000);

            entity.Property(e => e.Recommendation)
                .HasMaxLength(1000);

            entity.Property(e => e.Result)
                .HasMaxLength(1000);

            entity.Property(e => e.WorkDescription)
                .HasMaxLength(2000);

            entity.HasOne(d => d.RepairRequest)
                .WithMany(p => p.RepairReport)
                .HasForeignKey(d => d.RepairRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RepairReport_Request");

            entity.HasOne(d => d.Technician)
                .WithMany(p => p.RepairReport)
                .HasForeignKey(d => d.TechnicianId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RepairReport_Technician");
        });

        // ============================================================
        // REPAIR REQUEST
        // ============================================================

        modelBuilder.Entity<RepairRequest>(entity =>
        {
            entity.ToTable(tb =>
            {
                tb.HasTrigger("trg_RepairRequest_SetUpdatedAt");
                tb.HasTrigger("trg_RepairRequest_StatusHistory");
            });

            entity.HasIndex(
                e => e.AddressId,
                "IX_RepairRequest_AddressId");

            entity.HasIndex(
                e => new { e.CustomerId, e.CreatedAt },
                "IX_RepairRequest_Customer")
                .IsDescending(false, true);

            entity.HasIndex(
                e => new { e.ShopId, e.Status, e.CreatedAt },
                "IX_RepairRequest_Shop_Status")
                .IsDescending(false, false, true);

            entity.HasIndex(
                e => new
                {
                    e.RepairRequestId,
                    e.CustomerId,
                    e.ShopId
                },
                "UQ_RepairRequest_Ref")
                .IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_RepairRequest_CreatedAt");

            entity.Property(e => e.Description)
                .HasMaxLength(2000);

            entity.Property(e => e.Priority)
                .HasDefaultValue(2, "DF_RepairRequest_Priority");

            entity.Property(e => e.RepairMethod)
                .HasDefaultValue(1, "DF_RepairRequest_Method");

            entity.Property(e => e.Status)
                .HasDefaultValue(1, "DF_RepairRequest_Status");

            entity.HasOne(d => d.Customer)
                .WithMany(p => p.RepairRequest)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RepairRequest_Customer");

            entity.HasOne(d => d.Shop)
                .WithMany(p => p.RepairRequest)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RepairRequest_Shop");

            // EF chỉ map AddressId -> AddressId.
            // Database vẫn giữ composite FK để đảm bảo
            // Address thuộc đúng Customer.
            entity.HasOne(d => d.Address)
                .WithMany(p => p.RepairRequest)
                .HasForeignKey(d => d.AddressId)
                .HasPrincipalKey(p => p.AddressId)
                .HasConstraintName("FK_RepairRequest_AddressCustomer");
        });

        // ============================================================
        // REPAIR REQUEST IMAGE
        // ============================================================

        modelBuilder.Entity<RepairRequestImage>(entity =>
        {
            entity.HasIndex(
                e => e.RepairRequestId,
                "IX_RepairRequestImage_RepairRequestId");

            entity.Property(e => e.Caption)
                .HasMaxLength(200);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_RepairRequestImage_CreatedAt");

            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500);

            entity.HasOne(d => d.RepairRequest)
                .WithMany(p => p.RepairRequestImage)
                .HasForeignKey(d => d.RepairRequestId)
                .HasConstraintName("FK_RepairRequestImage_Request");
        });

        // ============================================================
        // REPAIR REQUEST SERVICE
        // ============================================================

        modelBuilder.Entity<RepairRequestService>(entity =>
        {
            entity.HasIndex(
                e => e.ServiceId,
                "IX_RepairRequestService_ServiceId");

            entity.HasIndex(
                e => new
                {
                    e.RepairRequestId,
                    e.ServiceId
                },
                "UX_RepairRequestService")
                .IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_RepairRequestService_CreatedAt");

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.Note)
                .HasMaxLength(500);

            entity.HasOne(d => d.RepairRequest)
                .WithMany(p => p.RepairRequestService)
                .HasForeignKey(d => d.RepairRequestId)
                .HasConstraintName("FK_RepairRequestService_Request");

            entity.HasOne(d => d.Service)
                .WithMany(p => p.RepairRequestService)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RepairRequestService_Service");
        });

        // ============================================================
        // REPAIR REQUEST STATUS HISTORY
        // ============================================================

        modelBuilder.Entity<RepairRequestStatusHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId);

            entity.HasIndex(
                e => e.ChangedByUserId,
                "IX_RepairRequestStatusHistory_ChangedByUserId");

            entity.HasIndex(
                e => e.RepairRequestId,
                "IX_RepairRequestStatusHistory_RepairRequestId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_RRStatusHistory_CreatedAt");

            entity.Property(e => e.Reason)
                .HasMaxLength(500);

            entity.HasOne(d => d.ChangedByUser)
                .WithMany(p => p.RepairRequestStatusHistory)
                .HasForeignKey(d => d.ChangedByUserId)
                .HasConstraintName("FK_RRStatusHistory_User");

            entity.HasOne(d => d.RepairRequest)
                .WithMany(p => p.RepairRequestStatusHistory)
                .HasForeignKey(d => d.RepairRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RRStatusHistory_Request");
        });

        // ============================================================
        // REPAIR SCHEDULE
        // ============================================================

        modelBuilder.Entity<RepairSchedule>(entity =>
        {
            entity.ToTable(tb =>
                tb.HasTrigger("trg_RepairSchedule_NoOverlap"));

            entity.HasIndex(
                e => e.CreatedByUserId,
                "IX_RepairSchedule_CreatedByUserId");

            entity.HasIndex(
                e => new { e.TechnicianId, e.StartTime },
                "IX_RepairSchedule_Tech_Time");

            entity.HasIndex(
                e => e.RepairRequestId,
                "UX_RepairSchedule_Current")
                .IsUnique()
                .HasFilter("([IsCurrent]=(1))");

            entity.Property(e => e.CancelReason)
                .HasMaxLength(500);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_RepairSchedule_CreatedAt");

            entity.Property(e => e.IsCurrent)
                .HasDefaultValue(
                    true,
                    "DF_RepairSchedule_IsCurrent");

            entity.Property(e => e.Status)
                .HasDefaultValue(
                    1,
                    "DF_RepairSchedule_Status");

            entity.HasOne(d => d.CreatedByUser)
                .WithMany(p => p.RepairSchedule)
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK_RepairSchedule_CreatedBy");

            entity.HasOne(d => d.RepairRequest)
                .WithOne(p => p.RepairSchedule)
                .HasForeignKey<RepairSchedule>(
                    d => d.RepairRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RepairSchedule_Request");

            entity.HasOne(d => d.Technician)
                .WithMany(p => p.RepairSchedule)
                .HasForeignKey(d => d.TechnicianId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RepairSchedule_Technician");
        });

        // ============================================================
        // REVIEW
        // ============================================================

        modelBuilder.Entity<Review>(entity =>
        {
            entity.ToTable(tb =>
            {
                tb.HasTrigger("trg_Review_SetUpdatedAt");
                tb.HasTrigger("trg_Review_UpdateRatings");
            });

            entity.HasIndex(
                e => e.CustomerId,
                "IX_Review_CustomerId");

            entity.HasIndex(
                e => new { e.ShopId, e.Status },
                "IX_Review_Shop_Status");

            entity.HasIndex(
                e => new { e.TechnicianId, e.Status },
                "IX_Review_Technician_Status")
                .HasFilter("([TechnicianId] IS NOT NULL)");

            entity.HasIndex(
                e => e.RepairRequestId,
                "UX_Review_Request")
                .IsUnique();

            entity.Property(e => e.Comment)
                .HasMaxLength(2000);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_Review_CreatedAt");

            entity.Property(e => e.Status)
                .HasDefaultValue(1, "DF_Review_Status");

            entity.HasOne(d => d.Customer)
                .WithMany(p => p.Review)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Review_Customer");

            entity.HasOne(d => d.RepairRequest)
                .WithOne(p => p.ReviewRepairRequest)
                .HasForeignKey<Review>(d => d.RepairRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Review_Request");

            entity.HasOne(d => d.Shop)
                .WithMany(p => p.Review)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Review_Shop");

            entity.HasOne(d => d.Technician)
                .WithMany(p => p.Review)
                .HasForeignKey(d => d.TechnicianId)
                .HasConstraintName("FK_Review_Technician");

            entity.HasOne(d => d.RepairRequestNavigation)
                .WithMany(p => p.ReviewRepairRequestNavigation)
                .HasPrincipalKey(p =>
                    new
                    {
                        p.RepairRequestId,
                        p.CustomerId,
                        p.ShopId
                    })
                .HasForeignKey(d =>
                    new
                    {
                        d.RepairRequestId,
                        d.CustomerId,
                        d.ShopId
                    })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Review_RequestRef");
        });

        // ============================================================
        // REVIEW IMAGE
        // ============================================================

        modelBuilder.Entity<ReviewImage>(entity =>
        {
            entity.HasIndex(
                e => e.ReviewId,
                "IX_ReviewImage_ReviewId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_ReviewImage_CreatedAt");

            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500);

            entity.HasOne(d => d.Review)
                .WithMany(p => p.ReviewImage)
                .HasForeignKey(d => d.ReviewId)
                .HasConstraintName("FK_ReviewImage_Review");
        });

        // ============================================================
        // REVIEW REPLY
        // ============================================================

        modelBuilder.Entity<ReviewReply>(entity =>
        {
            entity.HasIndex(
                e => e.RepliedByUserId,
                "IX_ReviewReply_RepliedByUserId");

            entity.HasIndex(
                e => e.ReviewId,
                "IX_ReviewReply_ReviewId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_ReviewReply_CreatedAt");

            entity.Property(e => e.ReplyContent)
                .HasMaxLength(2000);

            entity.HasOne(d => d.RepliedByUser)
                .WithMany(p => p.ReviewReply)
                .HasForeignKey(d => d.RepliedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewReply_User");

            entity.HasOne(d => d.Review)
                .WithMany(p => p.ReviewReply)
                .HasForeignKey(d => d.ReviewId)
                .HasConstraintName("FK_ReviewReply_Review");
        });

        // ============================================================
        // SERVICE
        // ============================================================

        modelBuilder.Entity<Service>(entity =>
        {
            entity.ToTable(tb =>
                tb.HasTrigger("trg_Service_SetUpdatedAt"));

            entity.HasIndex(
                e => e.CategoryId,
                "IX_Service_CategoryId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_Service_CreatedAt");

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(
                    true,
                    "DF_Service_IsActive");

            entity.Property(e => e.ServiceName)
                .HasMaxLength(200);

            entity.HasOne(d => d.Category)
                .WithMany(p => p.Service)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Service_Category");
        });

        // ============================================================
        // SERVICE CATEGORY
        // ============================================================

        modelBuilder.Entity<ServiceCategory>(entity =>
        {
            entity.Property(e => e.CategoryName)
                .HasMaxLength(200);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_ServiceCategory_CreatedAt");

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(
                    true,
                    "DF_ServiceCategory_IsActive");
        });

        // ============================================================
        // SERVICE PRICE
        // ============================================================

        modelBuilder.Entity<ServicePrice>(entity =>
        {
            entity.HasIndex(
                e => e.ShopServiceId,
                "UX_ServicePrice_Active")
                .IsUnique()
                .HasFilter("([IsActive]=(1))");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_ServicePrice_CreatedAt");

            entity.Property(e => e.EffectiveFrom)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_ServicePrice_EffectiveFrom");

            entity.Property(e => e.IsActive)
                .HasDefaultValue(
                    true,
                    "DF_ServicePrice_IsActive");

            entity.Property(e => e.MaxPrice)
                .HasColumnType("decimal(18, 2)");

            entity.Property(e => e.MinPrice)
                .HasColumnType("decimal(18, 2)");

            entity.Property(e => e.Price)
                .HasColumnType("decimal(18, 2)");

            entity.Property(e => e.PriceUnit)
                .HasMaxLength(50);

            entity.HasOne(d => d.ShopService)
                .WithOne(p => p.ServicePrice)
                .HasForeignKey<ServicePrice>(
                    d => d.ShopServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServicePrice_ShopService");
        });

        // ============================================================
        // SHOP
        // ============================================================

        modelBuilder.Entity<Shop>(entity =>
        {
            entity.ToTable(tb =>
                tb.HasTrigger("trg_Shop_SetUpdatedAt"));

            entity.HasIndex(
                e => e.AddressId,
                "IX_Shop_AddressId");

            entity.HasIndex(
                e => e.ManagerId,
                "IX_Shop_ManagerId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_Shop_CreatedAt");

            entity.Property(e => e.Description)
                .HasMaxLength(2000);

            entity.Property(e => e.Email)
                .HasMaxLength(200);

            entity.Property(e => e.LogoUrl)
                .HasMaxLength(500);

            entity.Property(e => e.Phone)
                .HasMaxLength(20);

            entity.Property(e => e.RatingAverage)
                .HasColumnType("decimal(3, 2)");

            entity.Property(e => e.ShopName)
                .HasMaxLength(200);

            entity.Property(e => e.Status)
                .HasDefaultValue(1, "DF_Shop_Status");

            entity.Property(e => e.TaxCode)
                .HasMaxLength(50);

            entity.HasOne(d => d.Address)
                .WithMany(p => p.Shop)
                .HasForeignKey(d => d.AddressId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Shop_Address");

            entity.HasOne(d => d.Manager)
                .WithMany(p => p.Shop)
                .HasForeignKey(d => d.ManagerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Shop_Manager");
        });

        // ============================================================
        // SHOP IMAGE
        // ============================================================

        modelBuilder.Entity<ShopImage>(entity =>
        {
            entity.HasIndex(
                e => e.ShopId,
                "UX_ShopImage_Primary")
                .IsUnique()
                .HasFilter("([IsPrimary]=(1))");

            entity.Property(e => e.Caption)
                .HasMaxLength(200);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_ShopImage_CreatedAt");

            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500);

            entity.HasOne(d => d.Shop)
                .WithOne(p => p.ShopImage)
                .HasForeignKey<ShopImage>(d => d.ShopId)
                .HasConstraintName("FK_ShopImage_Shop");
        });

        // ============================================================
        // SHOP SERVICE
        // ============================================================

        modelBuilder.Entity<ShopService>(entity =>
        {
            entity.HasIndex(
                e => e.ServiceId,
                "IX_ShopService_ServiceId");

            entity.HasIndex(
                e => new { e.ShopId, e.ServiceId },
                "UX_ShopService")
                .IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_ShopService_CreatedAt");

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(
                    true,
                    "DF_ShopService_IsActive");

            entity.HasOne(d => d.Service)
                .WithMany(p => p.ShopService)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShopService_Service");

            entity.HasOne(d => d.Shop)
                .WithMany(p => p.ShopService)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShopService_Shop");
        });

        // ============================================================
        // TECHNICIAN PROFILE
        // ============================================================

        modelBuilder.Entity<TechnicianProfile>(entity =>
        {
            entity.HasKey(e => e.TechnicianId);

            entity.ToTable(tb =>
                tb.HasTrigger("trg_TechnicianProfile_SetUpdatedAt"));

            entity.HasIndex(
                e => e.UserId,
                "UX_TechnicianProfile_User")
                .IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_TechnicianProfile_CreatedAt");

            entity.Property(e => e.Introduction)
                .HasMaxLength(1000);

            entity.Property(e => e.RatingAverage)
                .HasColumnType("decimal(3, 2)");

            entity.Property(e => e.TechnicianStatus)
                .HasDefaultValue(
                    1,
                    "DF_TechnicianProfile_Status");

            entity.HasOne(d => d.User)
                .WithOne(p => p.TechnicianProfile)
                .HasForeignKey<TechnicianProfile>(
                    d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TechnicianProfile_User");
        });

        // ============================================================
        // TECHNICIAN SHOP
        // ============================================================

        modelBuilder.Entity<TechnicianShop>(entity =>
        {
            entity.HasIndex(
                e => e.ShopId,
                "IX_TechnicianShop_ShopId");

            entity.HasIndex(
                e => new { e.TechnicianId, e.ShopId },
                "UX_TechnicianShop_Active")
                .IsUnique()
                .HasFilter("([LeftAt] IS NULL)");

            entity.HasIndex(
                e => e.TechnicianId,
                "UX_TechnicianShop_Primary")
                .IsUnique()
                .HasFilter(
                    "([IsPrimary]=(1) AND [LeftAt] IS NULL)");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_TechnicianShop_CreatedAt");

            entity.Property(e => e.JoinedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_TechnicianShop_JoinedAt");

            entity.Property(e => e.Status)
                .HasDefaultValue(
                    1,
                    "DF_TechnicianShop_Status");

            entity.HasOne(d => d.Shop)
                .WithMany(p => p.TechnicianShop)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TechnicianShop_Shop");

            entity.HasOne(d => d.Technician)
                .WithOne(p => p.TechnicianShop)
                .HasForeignKey<TechnicianShop>(
                    d => d.TechnicianId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TechnicianShop_Technician");
        });

        // ============================================================
        // TECHNICIAN SPECIALIZATION
        // ============================================================

        modelBuilder.Entity<TechnicianSpecialization>(entity =>
        {
            entity.HasIndex(
                e => e.ServiceId,
                "IX_TechnicianSpecialization_ServiceId");

            entity.HasIndex(
                e => new { e.TechnicianId, e.ServiceId },
                "UX_TechSpec")
                .IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_TechSpec_CreatedAt");

            entity.Property(e => e.Note)
                .HasMaxLength(500);

            entity.HasOne(d => d.Service)
                .WithMany(p => p.TechnicianSpecialization)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TechSpec_Service");

            entity.HasOne(d => d.Technician)
                .WithMany(p => p.TechnicianSpecialization)
                .HasForeignKey(d => d.TechnicianId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TechSpec_Technician");
        });

        // ============================================================
        // USER
        // ============================================================

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable(tb =>
                tb.HasTrigger("trg_User_SetUpdatedAt"));

            entity.HasIndex(
                e => e.Email,
                "UX_User_Email")
                .IsUnique()
                .HasFilter("([Email] IS NOT NULL)");

            entity.HasIndex(
                e => e.Phone,
                "UX_User_Phone")
                .IsUnique()
                .HasFilter("([Phone] IS NOT NULL)");

            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_User_CreatedAt");

            entity.Property(e => e.Email)
                .HasMaxLength(200);

            entity.Property(e => e.FullName)
                .HasMaxLength(200);

            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255);

            entity.Property(e => e.Phone)
                .HasMaxLength(20);

            entity.Property(e => e.Status)
                .HasDefaultValue(1, "DF_User_Status");
        });

        // ============================================================
        // USER NOTIFICATION
        // ============================================================

        modelBuilder.Entity<UserNotification>(entity =>
        {
            entity.HasIndex(
                e => new
                {
                    e.UserId,
                    e.IsRead,
                    e.CreatedAt
                },
                "IX_UserNotification_User")
                .IsDescending(false, false, true);

            entity.HasIndex(
                e => new
                {
                    e.NotificationId,
                    e.UserId
                },
                "UX_UserNotification")
                .IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_UserNotification_CreatedAt");

            entity.HasOne(d => d.Notification)
                .WithMany(p => p.UserNotification)
                .HasForeignKey(d => d.NotificationId)
                .HasConstraintName("FK_UserNotification_Notification");

            entity.HasOne(d => d.User)
                .WithMany(p => p.UserNotification)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserNotification_User");
        });

        // ============================================================
        // WARD
        // ============================================================

        modelBuilder.Entity<Ward>(entity =>
        {
            entity.HasIndex(
                e => new { e.WardId, e.ProvinceId },
                "UQ_Ward_WardProvince")
                .IsUnique();

            entity.HasIndex(
                e => new { e.ProvinceId, e.WardCode },
                "UX_Ward_Province_Code")
                .IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_Ward_CreatedAt");

            entity.Property(e => e.WardCode)
                .HasMaxLength(20);

            entity.Property(e => e.WardName)
                .HasMaxLength(200);

            entity.HasOne(d => d.Province)
                .WithMany(p => p.Ward)
                .HasForeignKey(d => d.ProvinceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ward_Province");
        });

        // ============================================================
        // WARRANTY
        // ============================================================

        modelBuilder.Entity<Warranty>(entity =>
        {
            entity.HasIndex(
                e => e.InvoiceId,
                "IX_Warranty_InvoiceId");

            entity.HasIndex(
                e => e.RepairRequestId,
                "IX_Warranty_RepairRequestId");

            entity.HasIndex(
                e => e.WarrantyCode,
                "UX_Warranty_Code")
                .IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_Warranty_CreatedAt");

            entity.Property(e => e.Status)
                .HasDefaultValue(
                    1,
                    "DF_Warranty_Status");

            entity.Property(e => e.Terms)
                .HasMaxLength(2000);

            entity.Property(e => e.WarrantyCode)
                .HasMaxLength(50);

            entity.HasOne(d => d.Invoice)
                .WithMany(p => p.Warranty)
                .HasForeignKey(d => d.InvoiceId)
                .HasConstraintName("FK_Warranty_Invoice");

            entity.HasOne(d => d.RepairRequest)
                .WithMany(p => p.Warranty)
                .HasForeignKey(d => d.RepairRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Warranty_Request");
        });

        // ============================================================
        // WARRANTY HISTORY
        // ============================================================

        modelBuilder.Entity<WarrantyHistory>(entity =>
        {
            entity.HasIndex(
                e => e.ChangedByUserId,
                "IX_WarrantyHistory_ChangedByUserId");

            entity.HasIndex(
                e => e.WarrantyRequestId,
                "IX_WarrantyHistory_WarrantyRequestId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_WarrantyHistory_CreatedAt");

            entity.Property(e => e.Note)
                .HasMaxLength(500);

            entity.HasOne(d => d.ChangedByUser)
                .WithMany(p => p.WarrantyHistory)
                .HasForeignKey(d => d.ChangedByUserId)
                .HasConstraintName("FK_WarrantyHistory_User");

            entity.HasOne(d => d.WarrantyRequest)
                .WithMany(p => p.WarrantyHistory)
                .HasForeignKey(d => d.WarrantyRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WarrantyHistory_WarrantyRequest");
        });

        // ============================================================
        // WARRANTY REQUEST
        // ============================================================

        modelBuilder.Entity<WarrantyRequest>(entity =>
        {
            entity.ToTable(tb =>
                tb.HasTrigger("trg_WarrantyRequest_SetUpdatedAt"));

            entity.HasIndex(
                e => e.CustomerId,
                "IX_WarrantyRequest_CustomerId");

            entity.HasIndex(
                e => e.WarrantyId,
                "IX_WarrantyRequest_WarrantyId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_WarrantyRequest_CreatedAt");

            entity.Property(e => e.IssueDescription)
                .HasMaxLength(2000);

            entity.Property(e => e.Status)
                .HasDefaultValue(
                    1,
                    "DF_WarrantyRequest_Status");

            entity.HasOne(d => d.Customer)
                .WithMany(p => p.WarrantyRequest)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WarrantyRequest_Customer");

            entity.HasOne(d => d.Warranty)
                .WithMany(p => p.WarrantyRequest)
                .HasForeignKey(d => d.WarrantyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WarrantyRequest_Warranty");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
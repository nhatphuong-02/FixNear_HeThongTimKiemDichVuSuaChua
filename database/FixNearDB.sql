CREATE DATABASE FixNearDB COLLATE Vietnamese_CI_AS;
GO

ALTER DATABASE FixNearDB SET READ_COMMITTED_SNAPSHOT ON;
GO

USE FixNearDB;
GO


-- 1. Province
CREATE TABLE dbo.Province (
    ProvinceId      INT IDENTITY(1,1) NOT NULL,
    ProvinceCode    NVARCHAR(20)    NOT NULL,
    ProvinceName    NVARCHAR(200)   NOT NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_Province_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Province PRIMARY KEY (ProvinceId)
);
GO

-- 2. Ward
CREATE TABLE dbo.Ward (
    WardId          INT IDENTITY(1,1) NOT NULL,
    ProvinceId      INT             NOT NULL,
    WardCode        NVARCHAR(20)    NOT NULL,
    WardName        NVARCHAR(200)   NOT NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_Ward_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Ward PRIMARY KEY (WardId),
    CONSTRAINT FK_Ward_Province FOREIGN KEY (ProvinceId) REFERENCES dbo.Province(ProvinceId)
);
GO

-- 3. User
CREATE TABLE dbo.[User] (
    UserId          INT IDENTITY(1,1) NOT NULL,
    RoleType        INT             NOT NULL,       -- 1: ADMIN, 2: MANAGER, 3: TECHNICIAN, 4: CUSTOMER
    FullName        NVARCHAR(200)   NOT NULL,
    Phone           NVARCHAR(20)    NULL,
    Email           NVARCHAR(200)   NULL,
    PasswordHash    NVARCHAR(255)   NOT NULL,
    AvatarUrl       NVARCHAR(500)   NULL,
    DateOfBirth     DATE            NULL,
    Gender          BIT             NULL,           -- 1: NAM, 0: NỮ
    Status          INT             NOT NULL CONSTRAINT DF_User_Status DEFAULT (1), -- 1: ACTIVE, 2: INACTIVE... (thay cho cột IsActive cũ)
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_User_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2       NULL,
    LastLoginAt     DATETIME2       NULL,
    CONSTRAINT PK_User PRIMARY KEY (UserId)
);
GO

-- 4. CustomerProfile
CREATE TABLE dbo.CustomerProfile (
    CustomerId       INT IDENTITY(1,1) NOT NULL,
    UserId           INT            NOT NULL,
    DefaultAddressId INT            NULL,
    CreatedAt        DATETIME2      NOT NULL CONSTRAINT DF_CustomerProfile_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt        DATETIME2      NULL,
    CONSTRAINT PK_CustomerProfile PRIMARY KEY (CustomerId),
    CONSTRAINT FK_CustomerProfile_User FOREIGN KEY (UserId) REFERENCES dbo.[User](UserId)
);
GO

-- 5. ManagerProfile
CREATE TABLE dbo.ManagerProfile (
    ManagerId       INT IDENTITY(1,1) NOT NULL,
    UserId          INT             NOT NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_ManagerProfile_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2       NULL,
    CONSTRAINT PK_ManagerProfile PRIMARY KEY (ManagerId),
    CONSTRAINT FK_ManagerProfile_User FOREIGN KEY (UserId) REFERENCES dbo.[User](UserId)
);
GO

-- 6. TechnicianProfile
CREATE TABLE dbo.TechnicianProfile (
    TechnicianId    INT IDENTITY(1,1) NOT NULL,
    UserId          INT             NOT NULL,
    ExperienceYears INT             NULL,
    Introduction    NVARCHAR(1000)  NULL,
    TechnicianStatus INT            NOT NULL CONSTRAINT DF_TechnicianProfile_Status DEFAULT (1), -- 1: AVAILABLE
    RatingAverage   DECIMAL(3,2)    NOT NULL CONSTRAINT DF_TechnicianProfile_Rating DEFAULT (0),
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_TechnicianProfile_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2       NULL,
    CONSTRAINT PK_TechnicianProfile PRIMARY KEY (TechnicianId),
    CONSTRAINT FK_TechnicianProfile_User FOREIGN KEY (UserId) REFERENCES dbo.[User](UserId)
);
GO

-- 7. Address
CREATE TABLE dbo.Address (
    AddressId               INT IDENTITY(1,1) NOT NULL,
    OwnerType               INT            NOT NULL, -- 1: CUSTOMER, 2: SHOP
    CustomerId              INT            NULL,
    ProvinceId              INT            NOT NULL,
    WardId                  INT            NOT NULL,
    AddressLine             NVARCHAR(500)  NOT NULL,
    RecipientName           NVARCHAR(200)  NULL,
    RecipientPhone          NVARCHAR(20)   NULL,
    IsLocked                BIT            NOT NULL CONSTRAINT DF_Address_IsLocked DEFAULT (0),
    SupersededByAddressId   INT            NULL,
    CreatedAt               DATETIME2      NOT NULL CONSTRAINT DF_Address_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Address PRIMARY KEY (AddressId),
    CONSTRAINT FK_Address_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.CustomerProfile(CustomerId),
    CONSTRAINT FK_Address_Province FOREIGN KEY (ProvinceId) REFERENCES dbo.Province(ProvinceId),
    CONSTRAINT FK_Address_Ward FOREIGN KEY (WardId) REFERENCES dbo.Ward(WardId),
    CONSTRAINT FK_Address_Superseded FOREIGN KEY (SupersededByAddressId) REFERENCES dbo.Address(AddressId)
);
GO

-- 8. Location
CREATE TABLE dbo.Location (
    LocationId      INT IDENTITY(1,1) NOT NULL,
    AddressId       INT             NOT NULL,
    Latitude        DECIMAL(9,6)    NOT NULL,
    Longitude       DECIMAL(9,6)    NOT NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_Location_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2       NULL,
    CONSTRAINT PK_Location PRIMARY KEY (LocationId),
    CONSTRAINT FK_Location_Address FOREIGN KEY (AddressId) REFERENCES dbo.Address(AddressId)
);
GO

-- 9. Shop
CREATE TABLE dbo.Shop (
    ShopId          INT IDENTITY(1,1) NOT NULL,
    ManagerId       INT             NOT NULL,
    AddressId       INT             NOT NULL,
    ShopName        NVARCHAR(200)   NOT NULL,
    Description     NVARCHAR(2000)  NULL,
    Phone           NVARCHAR(20)    NULL,
    Email           NVARCHAR(200)   NULL,
    LogoUrl         NVARCHAR(500)   NULL,
    TaxCode         NVARCHAR(50)    NULL,
    RatingAverage   DECIMAL(3,2)    NOT NULL CONSTRAINT DF_Shop_Rating DEFAULT (0),
    Status          INT             NOT NULL CONSTRAINT DF_Shop_Status DEFAULT (1), -- 1: ACTIVE
    IsVerified      BIT             NOT NULL CONSTRAINT DF_Shop_IsVerified DEFAULT (0),
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_Shop_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2       NULL,
    CONSTRAINT PK_Shop PRIMARY KEY (ShopId),
    CONSTRAINT FK_Shop_Manager FOREIGN KEY (ManagerId) REFERENCES dbo.ManagerProfile(ManagerId),
    CONSTRAINT FK_Shop_Address FOREIGN KEY (AddressId) REFERENCES dbo.Address(AddressId)
);
GO

-- 10. ShopImage
CREATE TABLE dbo.ShopImage (
    ShopImageId     INT IDENTITY(1,1) NOT NULL,
    ShopId          INT             NOT NULL,
    ImageUrl        NVARCHAR(500)   NOT NULL,
    Caption         NVARCHAR(200)   NULL,
    IsPrimary       BIT             NOT NULL CONSTRAINT DF_ShopImage_IsPrimary DEFAULT (0),
    DisplayOrder    INT             NOT NULL CONSTRAINT DF_ShopImage_DisplayOrder DEFAULT (0),
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_ShopImage_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_ShopImage PRIMARY KEY (ShopImageId),
    CONSTRAINT FK_ShopImage_Shop FOREIGN KEY (ShopId) REFERENCES dbo.Shop(ShopId) ON DELETE CASCADE
);
GO

-- 11. ServiceCategory
CREATE TABLE dbo.ServiceCategory (
    ServiceCategoryId INT IDENTITY(1,1) NOT NULL,
    CategoryName    NVARCHAR(200)   NOT NULL,
    Description     NVARCHAR(500)   NULL,
    IsActive        BIT             NOT NULL CONSTRAINT DF_ServiceCategory_IsActive DEFAULT (1),
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_ServiceCategory_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_ServiceCategory PRIMARY KEY (ServiceCategoryId)
);
GO

-- 12. Service
CREATE TABLE dbo.Service (
    ServiceId       INT IDENTITY(1,1) NOT NULL,
    CategoryId      INT             NOT NULL,
    ServiceName     NVARCHAR(200)   NOT NULL,
    Description     NVARCHAR(1000)  NULL,
    IsActive        BIT             NOT NULL CONSTRAINT DF_Service_IsActive DEFAULT (1),
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_Service_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2       NULL,
    CONSTRAINT PK_Service PRIMARY KEY (ServiceId),
    CONSTRAINT FK_Service_Category FOREIGN KEY (CategoryId) REFERENCES dbo.ServiceCategory(ServiceCategoryId)
);
GO

-- 13. ShopService
CREATE TABLE dbo.ShopService (
    ShopServiceId   INT IDENTITY(1,1) NOT NULL,
    ShopId          INT             NOT NULL,
    ServiceId       INT             NOT NULL,
    Description     NVARCHAR(1000)  NULL,
    IsActive        BIT             NOT NULL CONSTRAINT DF_ShopService_IsActive DEFAULT (1),
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_ShopService_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_ShopService PRIMARY KEY (ShopServiceId),
    CONSTRAINT FK_ShopService_Shop FOREIGN KEY (ShopId) REFERENCES dbo.Shop(ShopId),
    CONSTRAINT FK_ShopService_Service FOREIGN KEY (ServiceId) REFERENCES dbo.Service(ServiceId)
);
GO

-- 14. ServicePrice
CREATE TABLE dbo.ServicePrice (
    ServicePriceId  INT IDENTITY(1,1) NOT NULL,
    ShopServiceId   INT             NOT NULL,
    Price           DECIMAL(18,2)   NOT NULL,
    MinPrice        DECIMAL(18,2)   NULL,
    MaxPrice        DECIMAL(18,2)   NULL,
    PriceUnit       NVARCHAR(50)    NULL,
    EffectiveFrom   DATETIME2       NOT NULL CONSTRAINT DF_ServicePrice_EffectiveFrom DEFAULT SYSUTCDATETIME(),
    EffectiveTo     DATETIME2       NULL,
    IsActive        BIT             NOT NULL CONSTRAINT DF_ServicePrice_IsActive DEFAULT (1),
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_ServicePrice_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_ServicePrice PRIMARY KEY (ServicePriceId),
    CONSTRAINT FK_ServicePrice_ShopService FOREIGN KEY (ShopServiceId) REFERENCES dbo.ShopService(ShopServiceId)
);
GO

-- 15. TechnicianSpecialization
CREATE TABLE dbo.TechnicianSpecialization (
    TechnicianSpecializationId INT IDENTITY(1,1) NOT NULL,
    TechnicianId    INT             NOT NULL,
    ServiceId       INT             NOT NULL,
    Note            NVARCHAR(500)   NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_TechSpec_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_TechnicianSpecialization PRIMARY KEY (TechnicianSpecializationId),
    CONSTRAINT FK_TechSpec_Technician FOREIGN KEY (TechnicianId) REFERENCES dbo.TechnicianProfile(TechnicianId),
    CONSTRAINT FK_TechSpec_Service FOREIGN KEY (ServiceId) REFERENCES dbo.Service(ServiceId)
);
GO

-- 16. TechnicianShop
CREATE TABLE dbo.TechnicianShop (
    TechnicianShopId INT IDENTITY(1,1) NOT NULL,
    TechnicianId    INT             NOT NULL,
    ShopId          INT             NOT NULL,
    JoinedAt        DATETIME2       NOT NULL CONSTRAINT DF_TechnicianShop_JoinedAt DEFAULT SYSUTCDATETIME(),
    LeftAt          DATETIME2       NULL,
    Status          INT             NOT NULL CONSTRAINT DF_TechnicianShop_Status DEFAULT (1),
    IsPrimary       BIT             NOT NULL CONSTRAINT DF_TechnicianShop_IsPrimary DEFAULT (0),
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_TechnicianShop_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_TechnicianShop PRIMARY KEY (TechnicianShopId),
    CONSTRAINT FK_TechnicianShop_Technician FOREIGN KEY (TechnicianId) REFERENCES dbo.TechnicianProfile(TechnicianId),
    CONSTRAINT FK_TechnicianShop_Shop FOREIGN KEY (ShopId) REFERENCES dbo.Shop(ShopId)
);
GO

-- 17. RepairItem
CREATE TABLE dbo.RepairItem (
    RepairItemId    INT IDENTITY(1,1) NOT NULL,
    ItemCode        NVARCHAR(50)    NOT NULL,
    ItemName        NVARCHAR(200)   NOT NULL,
    Description     NVARCHAR(1000)  NULL,
    DefaultPrice    DECIMAL(18,2)   NOT NULL CONSTRAINT DF_RepairItem_DefaultPrice DEFAULT (0),
    IsActive        BIT             NOT NULL CONSTRAINT DF_RepairItem_IsActive DEFAULT (1),
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_RepairItem_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2       NULL,
    CONSTRAINT PK_RepairItem PRIMARY KEY (RepairItemId)
);
GO

-- 18. RepairPart
CREATE TABLE dbo.RepairPart (
    RepairPartId    INT IDENTITY(1,1) NOT NULL,
    PartCode        NVARCHAR(50)    NOT NULL,
    PartName        NVARCHAR(200)   NOT NULL,
    Description     NVARCHAR(1000)  NULL,
    Unit            NVARCHAR(50)    NULL,
    CurrentPrice    DECIMAL(18,2)   NOT NULL CONSTRAINT DF_RepairPart_CurrentPrice DEFAULT (0),
    IsActive        BIT             NOT NULL CONSTRAINT DF_RepairPart_IsActive DEFAULT (1),
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_RepairPart_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2       NULL,
    CONSTRAINT PK_RepairPart PRIMARY KEY (RepairPartId)
);
GO

-- 19. RepairRequest
CREATE TABLE dbo.RepairRequest (
    RepairRequestId INT IDENTITY(1,1) NOT NULL,
    CustomerId      INT             NOT NULL,
    ShopId          INT             NOT NULL,
    AddressId       INT             NULL,           -- NULL khi RepairMethod = 2 (tại shop)
    RepairMethod    INT             NOT NULL CONSTRAINT DF_RepairRequest_Method DEFAULT (1), -- 1: AtHome, 2: AtShop
    Description     NVARCHAR(2000)  NULL,
    PreferredDate   DATE            NULL,
    PreferredTimeFrom TIME          NULL,
    PreferredTimeTo TIME            NULL,
    Status          INT             NOT NULL CONSTRAINT DF_RepairRequest_Status DEFAULT (1), -- 1: PENDING
    Priority        INT             NOT NULL CONSTRAINT DF_RepairRequest_Priority DEFAULT (2), -- 2: NORMAL
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_RepairRequest_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2       NULL,
    CompletedAt     DATETIME2       NULL,
    CancelledAt     DATETIME2       NULL,
    CONSTRAINT PK_RepairRequest PRIMARY KEY (RepairRequestId),
    CONSTRAINT FK_RepairRequest_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.CustomerProfile(CustomerId),
    CONSTRAINT FK_RepairRequest_Shop FOREIGN KEY (ShopId) REFERENCES dbo.Shop(ShopId)
);
GO

-- 20. RepairRequestImage
CREATE TABLE dbo.RepairRequestImage (
    RepairRequestImageId INT IDENTITY(1,1) NOT NULL,
    RepairRequestId INT             NOT NULL,
    ImageUrl        NVARCHAR(500)   NOT NULL,
    Caption         NVARCHAR(200)   NULL,
    DisplayOrder    INT             NOT NULL CONSTRAINT DF_RepairRequestImage_DisplayOrder DEFAULT (0),
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_RepairRequestImage_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_RepairRequestImage PRIMARY KEY (RepairRequestImageId),
    CONSTRAINT FK_RepairRequestImage_Request FOREIGN KEY (RepairRequestId) REFERENCES dbo.RepairRequest(RepairRequestId) ON DELETE CASCADE
);
GO

-- 21. RepairRequestService
CREATE TABLE dbo.RepairRequestService (
    RepairRequestServiceId INT IDENTITY(1,1) NOT NULL,
    RepairRequestId INT             NOT NULL,
    ServiceId       INT             NOT NULL,
    Description     NVARCHAR(500)   NULL,
    Note            NVARCHAR(500)   NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_RepairRequestService_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_RepairRequestService PRIMARY KEY (RepairRequestServiceId),
    CONSTRAINT FK_RepairRequestService_Request FOREIGN KEY (RepairRequestId) REFERENCES dbo.RepairRequest(RepairRequestId) ON DELETE CASCADE,
    CONSTRAINT FK_RepairRequestService_Service FOREIGN KEY (ServiceId) REFERENCES dbo.Service(ServiceId)
);
GO

-- 22. RepairRequestStatusHistory
CREATE TABLE dbo.RepairRequestStatusHistory (
    HistoryId       INT IDENTITY(1,1) NOT NULL,
    RepairRequestId INT             NOT NULL,
    OldStatus       INT             NULL,
    NewStatus       INT             NOT NULL,
    ChangedByUserId INT             NULL,
    Reason          NVARCHAR(500)   NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_RRStatusHistory_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_RepairRequestStatusHistory PRIMARY KEY (HistoryId),
    CONSTRAINT FK_RRStatusHistory_Request FOREIGN KEY (RepairRequestId) REFERENCES dbo.RepairRequest(RepairRequestId),
    CONSTRAINT FK_RRStatusHistory_User FOREIGN KEY (ChangedByUserId) REFERENCES dbo.[User](UserId)
);
GO

-- 23. RepairAssignment
CREATE TABLE dbo.RepairAssignment (
    RepairAssignmentId INT IDENTITY(1,1) NOT NULL,
    RepairRequestId INT             NOT NULL,
    TechnicianId    INT             NOT NULL,
    AssignedByUserId INT            NULL,
    AssignmentStatus INT            NOT NULL CONSTRAINT DF_RepairAssignment_Status DEFAULT (1), -- 1: OFFERED
    AssignedAt      DATETIME2       NOT NULL CONSTRAINT DF_RepairAssignment_AssignedAt DEFAULT SYSUTCDATETIME(),
    RespondedAt     DATETIME2       NULL,
    RejectionReason NVARCHAR(500)   NULL,
    Note            NVARCHAR(500)   NULL,
    CONSTRAINT PK_RepairAssignment PRIMARY KEY (RepairAssignmentId),
    CONSTRAINT FK_RepairAssignment_Request FOREIGN KEY (RepairRequestId) REFERENCES dbo.RepairRequest(RepairRequestId),
    CONSTRAINT FK_RepairAssignment_Technician FOREIGN KEY (TechnicianId) REFERENCES dbo.TechnicianProfile(TechnicianId),
    CONSTRAINT FK_RepairAssignment_AssignedBy FOREIGN KEY (AssignedByUserId) REFERENCES dbo.[User](UserId)
);
GO

-- 24. RepairSchedule
CREATE TABLE dbo.RepairSchedule (
    RepairScheduleId INT IDENTITY(1,1) NOT NULL,
    RepairRequestId INT             NOT NULL,
    TechnicianId    INT             NOT NULL,
    StartTime       DATETIME2       NOT NULL,
    EndTime         DATETIME2       NOT NULL,
    Status          INT             NOT NULL CONSTRAINT DF_RepairSchedule_Status DEFAULT (1), -- 1: CONFIRMED
    IsCurrent       BIT             NOT NULL CONSTRAINT DF_RepairSchedule_IsCurrent DEFAULT (1),
    CreatedByUserId INT             NULL,
    CancelledAt     DATETIME2       NULL,
    CancelReason    NVARCHAR(500)   NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_RepairSchedule_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_RepairSchedule PRIMARY KEY (RepairScheduleId),
    CONSTRAINT FK_RepairSchedule_Request FOREIGN KEY (RepairRequestId) REFERENCES dbo.RepairRequest(RepairRequestId),
    CONSTRAINT FK_RepairSchedule_Technician FOREIGN KEY (TechnicianId) REFERENCES dbo.TechnicianProfile(TechnicianId),
    CONSTRAINT FK_RepairSchedule_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES dbo.[User](UserId)
);
GO

-- 25. Quote
CREATE TABLE dbo.Quote (
    QuoteId         INT IDENTITY(1,1) NOT NULL,
    RepairRequestId INT             NOT NULL,
    TechnicianId    INT             NOT NULL,
    QuoteNumber     NVARCHAR(50)    NOT NULL,
    TotalAmount     DECIMAL(18,2)   NOT NULL CONSTRAINT DF_Quote_TotalAmount DEFAULT (0),
    Status          INT             NOT NULL CONSTRAINT DF_Quote_Status DEFAULT (1), -- 1: DRAFT
    CustomerResponse INT            NULL,
    CustomerResponseAt DATETIME2    NULL,
    ValidUntil      DATETIME2       NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_Quote_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2       NULL,
    CONSTRAINT PK_Quote PRIMARY KEY (QuoteId),
    CONSTRAINT FK_Quote_Request FOREIGN KEY (RepairRequestId) REFERENCES dbo.RepairRequest(RepairRequestId),
    CONSTRAINT FK_Quote_Technician FOREIGN KEY (TechnicianId) REFERENCES dbo.TechnicianProfile(TechnicianId)
);
GO

-- 26. QuoteDetail
CREATE TABLE dbo.QuoteDetail (
    QuoteDetailId   INT IDENTITY(1,1) NOT NULL,
    QuoteId         INT             NOT NULL,
    ItemType        INT             NOT NULL, -- 1: SERVICE, 2: REPAIR_ITEM, 3: REPAIR_PART
    ServiceRefId    INT             NULL,
    RepairItemRefId INT             NULL,
    RepairPartRefId INT             NULL,
    Description     NVARCHAR(500)   NULL,
    Quantity        DECIMAL(18,2)   NOT NULL,
    UnitPrice       DECIMAL(18,2)   NOT NULL,
    Amount          AS CAST(Quantity * UnitPrice AS DECIMAL(18,2)) PERSISTED NOT NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_QuoteDetail_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_QuoteDetail PRIMARY KEY (QuoteDetailId),
    CONSTRAINT FK_QuoteDetail_Quote FOREIGN KEY (QuoteId) REFERENCES dbo.Quote(QuoteId) ON DELETE CASCADE,
    CONSTRAINT FK_QuoteDetail_Service FOREIGN KEY (ServiceRefId) REFERENCES dbo.Service(ServiceId),
    CONSTRAINT FK_QuoteDetail_RepairItem FOREIGN KEY (RepairItemRefId) REFERENCES dbo.RepairItem(RepairItemId),
    CONSTRAINT FK_QuoteDetail_RepairPart FOREIGN KEY (RepairPartRefId) REFERENCES dbo.RepairPart(RepairPartId)
);
GO

-- 27. QuoteHistory
CREATE TABLE dbo.QuoteHistory (
    QuoteHistoryId  INT IDENTITY(1,1) NOT NULL,
    QuoteId         INT             NOT NULL,
    ChangedByUserId INT             NULL,
    OldStatus       INT             NULL,
    NewStatus       INT             NULL,
    ChangeNote      NVARCHAR(500)   NULL,
    SnapshotTotalAmount DECIMAL(18,2) NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_QuoteHistory_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_QuoteHistory PRIMARY KEY (QuoteHistoryId),
    CONSTRAINT FK_QuoteHistory_Quote FOREIGN KEY (QuoteId) REFERENCES dbo.Quote(QuoteId),
    CONSTRAINT FK_QuoteHistory_User FOREIGN KEY (ChangedByUserId) REFERENCES dbo.[User](UserId)
);
GO

-- 28. RepairPartUsage
CREATE TABLE dbo.RepairPartUsage (
    RepairPartUsageId INT IDENTITY(1,1) NOT NULL,
    RepairRequestId INT             NOT NULL,
    RepairPartId    INT             NOT NULL,
    Quantity        DECIMAL(18,2)   NOT NULL,
    UnitPriceAtUsage DECIMAL(18,2)  NOT NULL,
    Amount          AS CAST(Quantity * UnitPriceAtUsage AS DECIMAL(18,2)) PERSISTED NOT NULL,
    Note            NVARCHAR(500)   NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_RepairPartUsage_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_RepairPartUsage PRIMARY KEY (RepairPartUsageId),
    CONSTRAINT FK_RepairPartUsage_Request FOREIGN KEY (RepairRequestId) REFERENCES dbo.RepairRequest(RepairRequestId),
    CONSTRAINT FK_RepairPartUsage_Part FOREIGN KEY (RepairPartId) REFERENCES dbo.RepairPart(RepairPartId)
);
GO

-- 29. RepairReport
CREATE TABLE dbo.RepairReport (
    RepairReportId  INT IDENTITY(1,1) NOT NULL,
    RepairRequestId INT             NOT NULL,
    TechnicianId    INT             NOT NULL,
    Diagnosis       NVARCHAR(2000)  NULL,
    WorkDescription NVARCHAR(2000)  NULL,
    Result          NVARCHAR(1000)  NULL,
    Recommendation  NVARCHAR(1000)  NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_RepairReport_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2       NULL,
    CONSTRAINT PK_RepairReport PRIMARY KEY (RepairReportId),
    CONSTRAINT FK_RepairReport_Request FOREIGN KEY (RepairRequestId) REFERENCES dbo.RepairRequest(RepairRequestId),
    CONSTRAINT FK_RepairReport_Technician FOREIGN KEY (TechnicianId) REFERENCES dbo.TechnicianProfile(TechnicianId)
);
GO

-- 30. Invoice
CREATE TABLE dbo.Invoice (
    InvoiceId       INT IDENTITY(1,1) NOT NULL,
    RepairRequestId INT             NOT NULL,
    InvoiceNumber   NVARCHAR(50)    NOT NULL,
    CustomerId      INT             NOT NULL,
    ShopId          INT             NOT NULL,
    TotalAmount     DECIMAL(18,2)   NOT NULL CONSTRAINT DF_Invoice_TotalAmount DEFAULT (0),
    Status          INT             NOT NULL CONSTRAINT DF_Invoice_Status DEFAULT (1), -- 1: UNPAID
    IssuedAt        DATETIME2       NOT NULL CONSTRAINT DF_Invoice_IssuedAt DEFAULT SYSUTCDATETIME(),
    PaidAt          DATETIME2       NULL,
    CONSTRAINT PK_Invoice PRIMARY KEY (InvoiceId),
    CONSTRAINT FK_Invoice_Request FOREIGN KEY (RepairRequestId) REFERENCES dbo.RepairRequest(RepairRequestId),
    CONSTRAINT FK_Invoice_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.CustomerProfile(CustomerId),
    CONSTRAINT FK_Invoice_Shop FOREIGN KEY (ShopId) REFERENCES dbo.Shop(ShopId)
);
GO

-- 31. InvoiceDetail
CREATE TABLE dbo.InvoiceDetail (
    InvoiceDetailId INT IDENTITY(1,1) NOT NULL,
    InvoiceId       INT             NOT NULL,
    Description     NVARCHAR(500)   NOT NULL,
    Quantity        DECIMAL(18,2)   NOT NULL,
    UnitPrice       DECIMAL(18,2)   NOT NULL,
    Amount          AS CAST(Quantity * UnitPrice AS DECIMAL(18,2)) PERSISTED NOT NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_InvoiceDetail_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_InvoiceDetail PRIMARY KEY (InvoiceDetailId),
    CONSTRAINT FK_InvoiceDetail_Invoice FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoice(InvoiceId) ON DELETE CASCADE
);
GO

-- 32. Payment
CREATE TABLE dbo.Payment (
    PaymentId       INT IDENTITY(1,1) NOT NULL,
    InvoiceId       INT             NOT NULL,
    PaymentMethod   INT             NOT NULL, -- 1: CASH, 2: CARD, 3: BANK_TRANSFER
    Amount          DECIMAL(18,2)   NOT NULL,
    Status          INT             NOT NULL CONSTRAINT DF_Payment_Status DEFAULT (1), -- 1: PENDING
    PaidAt          DATETIME2       NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_Payment_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2       NULL,
    CONSTRAINT PK_Payment PRIMARY KEY (PaymentId),
    CONSTRAINT FK_Payment_Invoice FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoice(InvoiceId)
);
GO

-- 33. PaymentTransaction
CREATE TABLE dbo.PaymentTransaction (
    PaymentTransactionId INT IDENTITY(1,1) NOT NULL,
    PaymentId       INT             NOT NULL,
    TransactionCode NVARCHAR(100)   NULL,
    Status          INT             NOT NULL,
    Message         NVARCHAR(500)   NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_PaymentTransaction_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_PaymentTransaction PRIMARY KEY (PaymentTransactionId),
    CONSTRAINT FK_PaymentTransaction_Payment FOREIGN KEY (PaymentId) REFERENCES dbo.Payment(PaymentId)
);
GO

-- 34. Review
CREATE TABLE dbo.Review (
    ReviewId        INT IDENTITY(1,1) NOT NULL,
    RepairRequestId INT             NOT NULL,
    CustomerId      INT             NOT NULL,
    ShopId          INT             NOT NULL,
    TechnicianId    INT             NULL,           -- thợ được đánh giá (nguồn của TechnicianProfile.RatingAverage)
    Rating          INT             NOT NULL,
    Comment         NVARCHAR(2000)  NULL,
    Status          INT             NOT NULL CONSTRAINT DF_Review_Status DEFAULT (1), -- 1: PUBLISHED
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_Review_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2       NULL,
    CONSTRAINT PK_Review PRIMARY KEY (ReviewId),
    CONSTRAINT FK_Review_Request FOREIGN KEY (RepairRequestId) REFERENCES dbo.RepairRequest(RepairRequestId),
    CONSTRAINT FK_Review_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.CustomerProfile(CustomerId),
    CONSTRAINT FK_Review_Shop FOREIGN KEY (ShopId) REFERENCES dbo.Shop(ShopId),
    CONSTRAINT FK_Review_Technician FOREIGN KEY (TechnicianId) REFERENCES dbo.TechnicianProfile(TechnicianId)
);
GO

-- 35. ReviewImage
CREATE TABLE dbo.ReviewImage (
    ReviewImageId   INT IDENTITY(1,1) NOT NULL,
    ReviewId        INT             NOT NULL,
    ImageUrl        NVARCHAR(500)   NOT NULL,
    DisplayOrder    INT             NOT NULL CONSTRAINT DF_ReviewImage_DisplayOrder DEFAULT (0),
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_ReviewImage_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_ReviewImage PRIMARY KEY (ReviewImageId),
    CONSTRAINT FK_ReviewImage_Review FOREIGN KEY (ReviewId) REFERENCES dbo.Review(ReviewId) ON DELETE CASCADE
);
GO

-- 36. ReviewReply
CREATE TABLE dbo.ReviewReply (
    ReviewReplyId   INT IDENTITY(1,1) NOT NULL,
    ReviewId        INT             NOT NULL,
    RepliedByUserId INT             NOT NULL,
    ReplyContent    NVARCHAR(2000)  NOT NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_ReviewReply_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_ReviewReply PRIMARY KEY (ReviewReplyId),
    CONSTRAINT FK_ReviewReply_Review FOREIGN KEY (ReviewId) REFERENCES dbo.Review(ReviewId) ON DELETE CASCADE,
    CONSTRAINT FK_ReviewReply_User FOREIGN KEY (RepliedByUserId) REFERENCES dbo.[User](UserId)
);
GO

-- 37. NotificationTemplate
CREATE TABLE dbo.NotificationTemplate (
    NotificationTemplateId INT IDENTITY(1,1) NOT NULL,
    TemplateCode    NVARCHAR(50)    NOT NULL,
    Title           NVARCHAR(200)   NOT NULL,
    BodyTemplate    NVARCHAR(2000)  NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_NotificationTemplate_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_NotificationTemplate PRIMARY KEY (NotificationTemplateId)
);
GO

-- 38. Notification
CREATE TABLE dbo.Notification (
    NotificationId  INT IDENTITY(1,1) NOT NULL,
    TemplateId      INT             NULL,
    Title           NVARCHAR(200)   NOT NULL,
    Body            NVARCHAR(2000)  NULL,
    RefType         INT             NULL,
    RefId           INT             NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_Notification_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Notification PRIMARY KEY (NotificationId),
    CONSTRAINT FK_Notification_Template FOREIGN KEY (TemplateId) REFERENCES dbo.NotificationTemplate(NotificationTemplateId)
);
GO

-- 39. UserNotification
CREATE TABLE dbo.UserNotification (
    UserNotificationId INT IDENTITY(1,1) NOT NULL,
    NotificationId  INT             NOT NULL,
    UserId          INT             NOT NULL,
    IsRead          BIT             NOT NULL CONSTRAINT DF_UserNotification_IsRead DEFAULT (0),
    ReadAt          DATETIME2       NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_UserNotification_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_UserNotification PRIMARY KEY (UserNotificationId),
    CONSTRAINT FK_UserNotification_Notification FOREIGN KEY (NotificationId) REFERENCES dbo.Notification(NotificationId) ON DELETE CASCADE,
    CONSTRAINT FK_UserNotification_User FOREIGN KEY (UserId) REFERENCES dbo.[User](UserId)
);
GO

-- 40. Warranty
CREATE TABLE dbo.Warranty (
    WarrantyId      INT IDENTITY(1,1) NOT NULL,
    RepairRequestId INT             NOT NULL,
    InvoiceId       INT             NULL,
    WarrantyCode    NVARCHAR(50)    NOT NULL,
    StartDate       DATE            NOT NULL,
    EndDate         DATE            NOT NULL,
    Terms           NVARCHAR(2000)  NULL,
    Status          INT             NOT NULL CONSTRAINT DF_Warranty_Status DEFAULT (1), -- 1: ACTIVE
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_Warranty_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Warranty PRIMARY KEY (WarrantyId),
    CONSTRAINT FK_Warranty_Request FOREIGN KEY (RepairRequestId) REFERENCES dbo.RepairRequest(RepairRequestId),
    CONSTRAINT FK_Warranty_Invoice FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoice(InvoiceId)
);
GO

-- 41. WarrantyRequest
CREATE TABLE dbo.WarrantyRequest (
    WarrantyRequestId INT IDENTITY(1,1) NOT NULL,
    WarrantyId      INT             NOT NULL,
    CustomerId      INT             NOT NULL,
    IssueDescription NVARCHAR(2000) NOT NULL,
    Status          INT             NOT NULL CONSTRAINT DF_WarrantyRequest_Status DEFAULT (1), -- 1: OPEN
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_WarrantyRequest_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2       NULL,
    CONSTRAINT PK_WarrantyRequest PRIMARY KEY (WarrantyRequestId),
    CONSTRAINT FK_WarrantyRequest_Warranty FOREIGN KEY (WarrantyId) REFERENCES dbo.Warranty(WarrantyId),
    CONSTRAINT FK_WarrantyRequest_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.CustomerProfile(CustomerId)
);
GO

-- 42. WarrantyHistory
CREATE TABLE dbo.WarrantyHistory (
    WarrantyHistoryId INT IDENTITY(1,1) NOT NULL,
    WarrantyRequestId INT           NOT NULL,
    ChangedByUserId INT             NULL,
    OldStatus       INT             NULL,
    NewStatus       INT             NOT NULL,
    Note            NVARCHAR(500)   NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_WarrantyHistory_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_WarrantyHistory PRIMARY KEY (WarrantyHistoryId),
    CONSTRAINT FK_WarrantyHistory_WarrantyRequest FOREIGN KEY (WarrantyRequestId) REFERENCES dbo.WarrantyRequest(WarrantyRequestId),
    CONSTRAINT FK_WarrantyHistory_User FOREIGN KEY (ChangedByUserId) REFERENCES dbo.[User](UserId)
);
GO

-- 43. Conversation
CREATE TABLE dbo.Conversation (
    ConversationId  INT IDENTITY(1,1) NOT NULL,
    RepairRequestId INT             NULL,
    Status          INT             NOT NULL CONSTRAINT DF_Conversation_Status DEFAULT (1), -- 1: OPEN
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_Conversation_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2       NULL,
    IsDeleted       BIT             NOT NULL CONSTRAINT DF_Conversation_IsDeleted DEFAULT (0),
    DeletedAt       DATETIME2       NULL,
    CONSTRAINT PK_Conversation PRIMARY KEY (ConversationId),
    CONSTRAINT FK_Conversation_Request FOREIGN KEY (RepairRequestId) REFERENCES dbo.RepairRequest(RepairRequestId)
);
GO

-- 44. ConversationMember
CREATE TABLE dbo.ConversationMember (
    ConversationMemberId INT IDENTITY(1,1) NOT NULL,
    ConversationId  INT             NOT NULL,
    UserId          INT             NOT NULL,
    JoinedAt        DATETIME2       NOT NULL CONSTRAINT DF_ConversationMember_JoinedAt DEFAULT SYSUTCDATETIME(),
    LeftAt          DATETIME2       NULL,           -- soft leave: giữ dòng để lịch sử tin nhắn còn nguyên
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_ConversationMember_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_ConversationMember PRIMARY KEY (ConversationMemberId),
    CONSTRAINT UQ_ConversationMember UNIQUE (ConversationId, UserId),
    CONSTRAINT FK_ConversationMember_Conversation FOREIGN KEY (ConversationId) REFERENCES dbo.Conversation(ConversationId),
    CONSTRAINT FK_ConversationMember_User FOREIGN KEY (UserId) REFERENCES dbo.[User](UserId)
);
GO

-- 45. Message
CREATE TABLE dbo.Message (
    MessageId       INT IDENTITY(1,1) NOT NULL,
    ConversationId  INT             NOT NULL,
    SenderUserId    INT             NOT NULL,
    Content         NVARCHAR(MAX)   NULL,
    IsDeleted       BIT             NOT NULL CONSTRAINT DF_Message_IsDeleted DEFAULT (0),
    DeletedAt       DATETIME2       NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_Message_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Message PRIMARY KEY (MessageId),
    CONSTRAINT FK_Message_Conversation FOREIGN KEY (ConversationId) REFERENCES dbo.Conversation(ConversationId),
    CONSTRAINT FK_Message_Sender FOREIGN KEY (SenderUserId) REFERENCES dbo.[User](UserId)
);
GO

-- 46. MessageAttachment
CREATE TABLE dbo.MessageAttachment (
    MessageAttachmentId INT IDENTITY(1,1) NOT NULL,
    MessageId       INT             NOT NULL,
    FileUrl         NVARCHAR(500)   NOT NULL,
    FileType        NVARCHAR(50)    NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_MessageAttachment_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_MessageAttachment PRIMARY KEY (MessageAttachmentId),
    CONSTRAINT FK_MessageAttachment_Message FOREIGN KEY (MessageId) REFERENCES dbo.Message(MessageId) ON DELETE CASCADE
);
GO

/* ==================================================================
   PHASE 2 - CONSTRAINTS (FK phức hợp, UNIQUE, CHECK)
   ================================================================== */

-- 2.1 Ràng buộc toàn vẹn phức hợp (thay cho kiểm tra bằng trigger/app)

-- Ward phải thuộc đúng Province của Address
ALTER TABLE dbo.Ward ADD CONSTRAINT UQ_Ward_WardProvince UNIQUE (WardId, ProvinceId);
ALTER TABLE dbo.Address ADD CONSTRAINT FK_Address_WardProvince
    FOREIGN KEY (WardId, ProvinceId) REFERENCES dbo.Ward(WardId, ProvinceId);
GO

-- Địa chỉ mặc định / địa chỉ của yêu cầu sửa phải thuộc đúng khách hàng
-- (khi DefaultAddressId / AddressId = NULL thì FK không được kiểm tra)
ALTER TABLE dbo.Address ADD CONSTRAINT UQ_Address_AddressCustomer UNIQUE (AddressId, CustomerId);
ALTER TABLE dbo.CustomerProfile ADD CONSTRAINT FK_CustomerProfile_DefaultAddress
    FOREIGN KEY (DefaultAddressId, CustomerId) REFERENCES dbo.Address(AddressId, CustomerId);
ALTER TABLE dbo.RepairRequest ADD CONSTRAINT FK_RepairRequest_AddressCustomer
    FOREIGN KEY (AddressId, CustomerId) REFERENCES dbo.Address(AddressId, CustomerId);
GO

-- Invoice/Review lưu trùng CustomerId, ShopId với RepairRequest => bắt buộc phải khớp
ALTER TABLE dbo.RepairRequest ADD CONSTRAINT UQ_RepairRequest_Ref UNIQUE (RepairRequestId, CustomerId, ShopId);
ALTER TABLE dbo.Invoice ADD CONSTRAINT FK_Invoice_RequestRef
    FOREIGN KEY (RepairRequestId, CustomerId, ShopId) REFERENCES dbo.RepairRequest(RepairRequestId, CustomerId, ShopId);
ALTER TABLE dbo.Review ADD CONSTRAINT FK_Review_RequestRef
    FOREIGN KEY (RepairRequestId, CustomerId, ShopId) REFERENCES dbo.RepairRequest(RepairRequestId, CustomerId, ShopId);
GO

-- Người gửi tin nhắn phải là thành viên của cuộc trò chuyện
ALTER TABLE dbo.Message ADD CONSTRAINT FK_Message_Member
    FOREIGN KEY (ConversationId, SenderUserId) REFERENCES dbo.ConversationMember(ConversationId, UserId);
GO

-- 2.2 CHECK
ALTER TABLE dbo.TechnicianProfile ADD CONSTRAINT CK_TechnicianProfile_Rating CHECK (RatingAverage BETWEEN 0 AND 5);
ALTER TABLE dbo.Shop              ADD CONSTRAINT CK_Shop_Rating              CHECK (RatingAverage BETWEEN 0 AND 5);
ALTER TABLE dbo.ServicePrice      ADD CONSTRAINT CK_ServicePrice_Price       CHECK (Price >= 0);
ALTER TABLE dbo.RepairItem        ADD CONSTRAINT CK_RepairItem_DefaultPrice  CHECK (DefaultPrice >= 0);
ALTER TABLE dbo.RepairPart        ADD CONSTRAINT CK_RepairPart_CurrentPrice  CHECK (CurrentPrice >= 0);
ALTER TABLE dbo.RepairSchedule    ADD CONSTRAINT CK_RepairSchedule_TimeRange CHECK (EndTime > StartTime);
ALTER TABLE dbo.Quote             ADD CONSTRAINT CK_Quote_Amounts            CHECK (TotalAmount >= 0);
ALTER TABLE dbo.QuoteDetail       ADD CONSTRAINT CK_QuoteDetail_Quantity     CHECK (Quantity > 0);
ALTER TABLE dbo.Invoice           ADD CONSTRAINT CK_Invoice_Amounts          CHECK (TotalAmount >= 0);
ALTER TABLE dbo.Payment           ADD CONSTRAINT CK_Payment_Amount           CHECK (Amount > 0);
ALTER TABLE dbo.Review            ADD CONSTRAINT CK_Review_Rating            CHECK (Rating BETWEEN 1 AND 5);
GO

ALTER TABLE dbo.[User]   ADD CONSTRAINT CK_User_Role    CHECK (RoleType BETWEEN 1 AND 4);
ALTER TABLE dbo.[User]   ADD CONSTRAINT CK_User_Contact CHECK (Phone IS NOT NULL OR Email IS NOT NULL);
ALTER TABLE dbo.Address  ADD CONSTRAINT CK_Address_Owner CHECK (
    (OwnerType = 1 AND CustomerId IS NOT NULL) OR (OwnerType = 2 AND CustomerId IS NULL));
ALTER TABLE dbo.Location ADD CONSTRAINT CK_Location_Range CHECK (
    Latitude BETWEEN -90 AND 90 AND Longitude BETWEEN -180 AND 180);
ALTER TABLE dbo.ServicePrice ADD CONSTRAINT CK_ServicePrice_Range CHECK (
        (MinPrice IS NULL OR MinPrice >= 0)
    AND (MaxPrice IS NULL OR MaxPrice >= ISNULL(MinPrice, 0))
    AND (EffectiveTo IS NULL OR EffectiveTo > EffectiveFrom));
ALTER TABLE dbo.RepairRequest ADD CONSTRAINT CK_RepairRequest_Method CHECK (RepairMethod IN (1, 2));
-- Sửa tại nhà (1) bắt buộc có địa chỉ; sửa tại shop (2) thì không cần
ALTER TABLE dbo.RepairRequest ADD CONSTRAINT CK_RepairRequest_Address CHECK (RepairMethod = 2 OR AddressId IS NOT NULL);
ALTER TABLE dbo.RepairRequest ADD CONSTRAINT CK_RepairRequest_Time CHECK (
    PreferredTimeFrom IS NULL OR PreferredTimeTo IS NULL OR PreferredTimeTo > PreferredTimeFrom);
ALTER TABLE dbo.QuoteDetail ADD CONSTRAINT CK_QuoteDetail_Item CHECK (
    (ItemType = 1 AND ServiceRefId    IS NOT NULL AND RepairItemRefId IS NULL     AND RepairPartRefId IS NULL) OR
    (ItemType = 2 AND RepairItemRefId IS NOT NULL AND ServiceRefId    IS NULL     AND RepairPartRefId IS NULL) OR
    (ItemType = 3 AND RepairPartRefId IS NOT NULL AND ServiceRefId    IS NULL     AND RepairItemRefId IS NULL));
ALTER TABLE dbo.QuoteDetail     ADD CONSTRAINT CK_QuoteDetail_UnitPrice CHECK (UnitPrice >= 0);
ALTER TABLE dbo.InvoiceDetail   ADD CONSTRAINT CK_InvoiceDetail         CHECK (Quantity > 0 AND UnitPrice >= 0);
ALTER TABLE dbo.RepairPartUsage ADD CONSTRAINT CK_RepairPartUsage       CHECK (Quantity > 0 AND UnitPriceAtUsage >= 0);
ALTER TABLE dbo.Warranty        ADD CONSTRAINT CK_Warranty_Dates        CHECK (EndDate >= StartDate);
GO

-- 2.3 UNIQUE / filtered UNIQUE
CREATE UNIQUE INDEX UX_Province_Code             ON dbo.Province(ProvinceCode);
CREATE UNIQUE INDEX UX_Ward_Province_Code        ON dbo.Ward(ProvinceId, WardCode);
CREATE UNIQUE INDEX UX_User_Email                ON dbo.[User](Email) WHERE Email IS NOT NULL;
CREATE UNIQUE INDEX UX_User_Phone                ON dbo.[User](Phone) WHERE Phone IS NOT NULL;
CREATE UNIQUE INDEX UX_CustomerProfile_User      ON dbo.CustomerProfile(UserId);
CREATE UNIQUE INDEX UX_ManagerProfile_User       ON dbo.ManagerProfile(UserId);
CREATE UNIQUE INDEX UX_TechnicianProfile_User    ON dbo.TechnicianProfile(UserId);
CREATE UNIQUE INDEX UX_Location_Address          ON dbo.Location(AddressId);
CREATE UNIQUE INDEX UX_ShopService               ON dbo.ShopService(ShopId, ServiceId);
CREATE UNIQUE INDEX UX_ServicePrice_Active       ON dbo.ServicePrice(ShopServiceId) WHERE IsActive = 1;
CREATE UNIQUE INDEX UX_TechSpec                  ON dbo.TechnicianSpecialization(TechnicianId, ServiceId);
CREATE UNIQUE INDEX UX_TechnicianShop_Active     ON dbo.TechnicianShop(TechnicianId, ShopId) WHERE LeftAt IS NULL;
CREATE UNIQUE INDEX UX_TechnicianShop_Primary    ON dbo.TechnicianShop(TechnicianId) WHERE IsPrimary = 1 AND LeftAt IS NULL;
CREATE UNIQUE INDEX UX_ShopImage_Primary         ON dbo.ShopImage(ShopId) WHERE IsPrimary = 1;
CREATE UNIQUE INDEX UX_RepairItem_Code           ON dbo.RepairItem(ItemCode);
CREATE UNIQUE INDEX UX_RepairPart_Code           ON dbo.RepairPart(PartCode);
CREATE UNIQUE INDEX UX_RepairRequestService      ON dbo.RepairRequestService(RepairRequestId, ServiceId);
CREATE UNIQUE INDEX UX_RepairSchedule_Current    ON dbo.RepairSchedule(RepairRequestId) WHERE IsCurrent = 1;
CREATE UNIQUE INDEX UX_Quote_Number              ON dbo.Quote(QuoteNumber);
CREATE UNIQUE INDEX UX_Invoice_Number            ON dbo.Invoice(InvoiceNumber);
CREATE UNIQUE INDEX UX_Warranty_Code             ON dbo.Warranty(WarrantyCode);
CREATE UNIQUE INDEX UX_Review_Request            ON dbo.Review(RepairRequestId);
CREATE UNIQUE INDEX UX_NotificationTemplate_Code ON dbo.NotificationTemplate(TemplateCode);
CREATE UNIQUE INDEX UX_UserNotification          ON dbo.UserNotification(NotificationId, UserId);
CREATE UNIQUE INDEX UX_PaymentTransaction_Code   ON dbo.PaymentTransaction(TransactionCode) WHERE TransactionCode IS NOT NULL;
GO

-- 2.4 Index cho các đường truy vấn nóng (chạy TRƯỚC đoạn tự sinh index FK)
CREATE INDEX IX_Location_LatLng           ON dbo.Location(Latitude, Longitude) INCLUDE (AddressId);
CREATE INDEX IX_RepairRequest_Shop_Status ON dbo.RepairRequest(ShopId, Status, CreatedAt DESC);
CREATE INDEX IX_RepairRequest_Customer    ON dbo.RepairRequest(CustomerId, CreatedAt DESC);
CREATE INDEX IX_Message_Conversation      ON dbo.Message(ConversationId, CreatedAt);
CREATE INDEX IX_UserNotification_User     ON dbo.UserNotification(UserId, IsRead, CreatedAt DESC);
CREATE INDEX IX_Review_Shop_Status        ON dbo.Review(ShopId, Status) INCLUDE (Rating);
CREATE INDEX IX_Review_Technician_Status  ON dbo.Review(TechnicianId, Status) INCLUDE (Rating) WHERE TechnicianId IS NOT NULL;
-- Phục vụ trigger chống trùng lịch thợ (range lock theo thợ + thời gian)
CREATE INDEX IX_RepairSchedule_Tech_Time  ON dbo.RepairSchedule(TechnicianId, StartTime) INCLUDE (EndTime, IsCurrent, CancelledAt);
GO

-- Tự sinh index cho mọi cột FK còn chưa được index (SQL Server không tự tạo).
-- DISTINCT để một cột nằm trong nhiều FK không bị tạo trùng tên index.
DECLARE @sql NVARCHAR(MAX);
SELECT @sql = STRING_AGG(CAST(
    N'CREATE INDEX ' + QUOTENAME(N'IX_' + x.TableName + N'_' + x.ColumnName)
  + N' ON ' + QUOTENAME(x.SchemaName) + N'.' + QUOTENAME(x.TableName) + N'(' + QUOTENAME(x.ColumnName) + N');'
  AS NVARCHAR(MAX)), CHAR(10))
FROM (
    SELECT DISTINCT s.name AS SchemaName, t.name AS TableName, c.name AS ColumnName
    FROM sys.foreign_key_columns fkc
    JOIN sys.tables  t ON t.object_id = fkc.parent_object_id
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
    WHERE NOT EXISTS (
        SELECT 1 FROM sys.index_columns ic
        WHERE ic.object_id = fkc.parent_object_id
          AND ic.column_id = fkc.parent_column_id
          AND ic.key_ordinal = 1)
) x;
IF @sql IS NOT NULL EXEC sys.sp_executesql @sql;
GO

/* ==================================================================
   PHASE 3 - TRIGGERS
   ================================================================== */

-- Trigger 1: Address là immutable (so sánh giá trị thật, phân biệt hoa/thường và khoảng trắng cuối)
CREATE TRIGGER dbo.trg_Address_PreventContentUpdate
ON dbo.Address
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i JOIN deleted d ON d.AddressId = i.AddressId
        WHERE i.OwnerType <> d.OwnerType
           OR ISNULL(i.CustomerId, -1) <> ISNULL(d.CustomerId, -1)
           OR i.ProvinceId <> d.ProvinceId
           OR i.WardId <> d.WardId
           OR i.AddressLine COLLATE Latin1_General_BIN2 <> d.AddressLine COLLATE Latin1_General_BIN2
           OR DATALENGTH(i.AddressLine) <> DATALENGTH(d.AddressLine)
           OR ISNULL(i.RecipientName, N'')  COLLATE Latin1_General_BIN2 <> ISNULL(d.RecipientName, N'')  COLLATE Latin1_General_BIN2
           OR ISNULL(DATALENGTH(i.RecipientName), -1)  <> ISNULL(DATALENGTH(d.RecipientName), -1)
           OR ISNULL(i.RecipientPhone, N'') COLLATE Latin1_General_BIN2 <> ISNULL(d.RecipientPhone, N'') COLLATE Latin1_General_BIN2
           OR ISNULL(DATALENGTH(i.RecipientPhone), -1) <> ISNULL(DATALENGTH(d.RecipientPhone), -1)
           OR i.CreatedAt <> d.CreatedAt)
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50001, N'Address là immutable. Hãy INSERT địa chỉ mới và set SupersededByAddressId.', 1;
    END
END
GO

-- Trigger 2: Ghi lịch sử trạng thái RepairRequest (cả lúc tạo mới) + người thực hiện.
-- App gọi EXEC sp_set_session_context N'UserId', @id; trước khi INSERT/UPDATE.
CREATE TRIGGER dbo.trg_RepairRequest_StatusHistory
ON dbo.RepairRequest
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @UserId INT = TRY_CAST(SESSION_CONTEXT(N'UserId') AS INT);

    INSERT INTO dbo.RepairRequestStatusHistory (RepairRequestId, OldStatus, NewStatus, ChangedByUserId, Reason)
    SELECT i.RepairRequestId, d.Status, i.Status, @UserId,
           CASE WHEN d.RepairRequestId IS NULL THEN N'Tạo yêu cầu' ELSE N'Thay đổi trạng thái' END
    FROM inserted i
    LEFT JOIN deleted d ON d.RepairRequestId = i.RepairRequestId
    WHERE d.RepairRequestId IS NULL OR i.Status <> d.Status;
END
GO

-- Trigger 3: Tự cập nhật điểm đánh giá của Shop VÀ Technician.
-- Review.TechnicianId (nếu có) phải là thợ đã được phân công cho yêu cầu đó.
CREATE TRIGGER dbo.trg_Review_UpdateRatings
ON dbo.Review
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1 FROM inserted i
        WHERE i.TechnicianId IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM dbo.RepairAssignment a
                          WHERE a.RepairRequestId = i.RepairRequestId AND a.TechnicianId = i.TechnicianId))
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50011, N'Thợ được đánh giá chưa từng được phân công cho yêu cầu sửa chữa này.', 1;
    END

    UPDATE s
    SET RatingAverage = ISNULL((SELECT CAST(AVG(CAST(r.Rating AS DECIMAL(9,4))) AS DECIMAL(3,2))
                                FROM dbo.Review r WHERE r.ShopId = s.ShopId AND r.Status = 1), 0)
    FROM dbo.Shop s
    WHERE s.ShopId IN (SELECT ShopId FROM inserted UNION SELECT ShopId FROM deleted);

    UPDATE tp
    SET RatingAverage = ISNULL((SELECT CAST(AVG(CAST(r.Rating AS DECIMAL(9,4))) AS DECIMAL(3,2))
                                FROM dbo.Review r WHERE r.TechnicianId = tp.TechnicianId AND r.Status = 1), 0)
    FROM dbo.TechnicianProfile tp
    WHERE tp.TechnicianId IN (SELECT TechnicianId FROM inserted WHERE TechnicianId IS NOT NULL
                              UNION
                              SELECT TechnicianId FROM deleted  WHERE TechnicianId IS NOT NULL);
END
GO

-- Trigger 4: Quote.TotalAmount luôn = tổng QuoteDetail.Amount
CREATE TRIGGER dbo.trg_QuoteDetail_SyncTotal
ON dbo.QuoteDetail
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE q
    SET TotalAmount = ISNULL((SELECT SUM(d.Amount) FROM dbo.QuoteDetail d WHERE d.QuoteId = q.QuoteId), 0)
    FROM dbo.Quote q
    WHERE q.QuoteId IN (SELECT QuoteId FROM inserted UNION SELECT QuoteId FROM deleted);
END
GO

-- Trigger 5: Invoice.TotalAmount luôn = tổng InvoiceDetail.Amount
CREATE TRIGGER dbo.trg_InvoiceDetail_SyncTotal
ON dbo.InvoiceDetail
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE v
    SET TotalAmount = ISNULL((SELECT SUM(d.Amount) FROM dbo.InvoiceDetail d WHERE d.InvoiceId = v.InvoiceId), 0)
    FROM dbo.Invoice v
    WHERE v.InvoiceId IN (SELECT InvoiceId FROM inserted UNION SELECT InvoiceId FROM deleted);
END
GO

-- Trigger 6: Chặn trùng lịch của cùng một thợ (chỉ tính lịch hiện hành, chưa hủy).
-- UPDLOCK + HOLDLOCK khóa range để 2 giao dịch đồng thời không cùng lọt qua.
CREATE TRIGGER dbo.trg_RepairSchedule_NoOverlap
ON dbo.RepairSchedule
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT (UPDATE(TechnicianId) OR UPDATE(StartTime) OR UPDATE(EndTime)
            OR UPDATE(IsCurrent) OR UPDATE(CancelledAt))
        RETURN;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN dbo.RepairSchedule s WITH (UPDLOCK, HOLDLOCK)
          ON s.TechnicianId = i.TechnicianId
         AND s.RepairScheduleId <> i.RepairScheduleId
         AND s.IsCurrent = 1
         AND s.CancelledAt IS NULL
         AND s.StartTime < i.EndTime
         AND s.EndTime   > i.StartTime
        WHERE i.IsCurrent = 1 AND i.CancelledAt IS NULL)
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50010, N'Thợ đã có lịch khác trùng khoảng thời gian này.', 1;
    END
END
GO

-- Trigger 7 (tự sinh): tự set UpdatedAt = giờ UTC cho mọi bảng có cột UpdatedAt.
-- Nếu app chủ động set UpdatedAt trong câu UPDATE thì trigger tôn trọng giá trị đó.
DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += N'EXEC(N''CREATE OR ALTER TRIGGER ' + QUOTENAME(s.name) + N'.' + QUOTENAME(N'trg_' + t.name + N'_SetUpdatedAt')
    + N' ON ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name)
    + N' AFTER UPDATE AS BEGIN SET NOCOUNT ON; IF UPDATE(UpdatedAt) RETURN; '
    + N'UPDATE x SET x.UpdatedAt = SYSUTCDATETIME() FROM ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name)
    + N' x JOIN inserted i ON i.' + QUOTENAME(pk.name) + N' = x.' + QUOTENAME(pk.name) + N'; END'');' + CHAR(10)
FROM sys.tables t
JOIN sys.schemas s   ON s.schema_id = t.schema_id
JOIN sys.columns uc  ON uc.object_id = t.object_id AND uc.name = N'UpdatedAt'
JOIN sys.indexes pki ON pki.object_id = t.object_id AND pki.is_primary_key = 1
JOIN sys.index_columns pkic ON pkic.object_id = t.object_id AND pkic.index_id = pki.index_id AND pkic.key_ordinal = 1
JOIN sys.columns pk  ON pk.object_id = t.object_id AND pk.column_id = pkic.column_id;
EXEC sys.sp_executesql @sql;
GO

/* ==================================================================
   PHASE 4 - TYPE & STORED PROCEDURE
   ================================================================== */

CREATE TYPE dbo.IntIdList AS TABLE (Id INT NOT NULL);
GO

CREATE PROCEDURE dbo.sp_CreateRepairRequest
    @CustomerId         INT,
    @ShopId             INT,
    @AddressId          INT = NULL,          -- bắt buộc khi @RepairMethod = 1 (tại nhà)
    @RepairMethod       INT = 1,             -- 1: AtHome, 2: AtShop
    @Description        NVARCHAR(2000) = NULL,
    @PreferredDate      DATE = NULL,
    @PreferredTimeFrom  TIME = NULL,
    @PreferredTimeTo    TIME = NULL,
    @ServiceIds         dbo.IntIdList READONLY,
    @NewRepairRequestId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        IF @RepairMethod NOT IN (1, 2)
            THROW 50003, N'RepairMethod không hợp lệ (1: tại nhà, 2: tại shop).', 1;

        IF NOT EXISTS (SELECT 1 FROM @ServiceIds)
            THROW 50004, N'Phải có ít nhất 1 ServiceId.', 1;

        IF NOT EXISTS (SELECT 1 FROM dbo.CustomerProfile c
                       JOIN dbo.[User] u ON u.UserId = c.UserId
                       WHERE c.CustomerId = @CustomerId AND u.Status = 1)
            THROW 50008, N'Khách hàng không tồn tại hoặc tài khoản không hoạt động.', 1;

        IF @RepairMethod = 1
        BEGIN
            IF @AddressId IS NULL
                THROW 50009, N'Sửa tại nhà bắt buộc phải có địa chỉ.', 1;

            IF NOT EXISTS (SELECT 1 FROM dbo.Address
                           WHERE AddressId = @AddressId AND OwnerType = 1
                             AND CustomerId = @CustomerId AND SupersededByAddressId IS NULL)
                THROW 50005, N'Địa chỉ không thuộc khách hàng này hoặc đã bị thay thế.', 1;
        END
        ELSE
            SET @AddressId = NULL;           -- sửa tại shop: không lưu địa chỉ khách

        IF NOT EXISTS (SELECT 1 FROM dbo.Shop WHERE ShopId = @ShopId AND Status = 1)
            THROW 50006, N'Cửa hàng không tồn tại hoặc không hoạt động.', 1;

        IF EXISTS (SELECT 1 FROM (SELECT DISTINCT Id FROM @ServiceIds) x
                   WHERE NOT EXISTS (SELECT 1
                                     FROM dbo.ShopService ss
                                     JOIN dbo.Service sv ON sv.ServiceId = ss.ServiceId
                                     WHERE ss.ShopId = @ShopId AND ss.ServiceId = x.Id
                                       AND ss.IsActive = 1 AND sv.IsActive = 1))
            THROW 50007, N'Có dịch vụ không được cửa hàng cung cấp.', 1;

        BEGIN TRANSACTION;

        INSERT INTO dbo.RepairRequest (CustomerId, ShopId, AddressId, RepairMethod, Description,
                                       PreferredDate, PreferredTimeFrom, PreferredTimeTo, Status)
        VALUES (@CustomerId, @ShopId, @AddressId, @RepairMethod, @Description,
                @PreferredDate, @PreferredTimeFrom, @PreferredTimeTo, 1);
        SET @NewRepairRequestId = SCOPE_IDENTITY();

        INSERT INTO dbo.RepairRequestService (RepairRequestId, ServiceId)
        SELECT DISTINCT @NewRepairRequestId, Id FROM @ServiceIds;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

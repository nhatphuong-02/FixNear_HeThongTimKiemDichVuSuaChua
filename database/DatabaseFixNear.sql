CREATE DATABASE FixNearDB;
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
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_Province_CreatedAt DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Province PRIMARY KEY (ProvinceId)
);
GO

-- 2. Ward
CREATE TABLE dbo.Ward (
    WardId          INT IDENTITY(1,1) NOT NULL,
    ProvinceId      INT             NOT NULL,
    WardCode        NVARCHAR(20)    NOT NULL,
    WardName        NVARCHAR(200)   NOT NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_Ward_CreatedAt DEFAULT SYSDATETIME(),
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
    Status          INT             NOT NULL CONSTRAINT DF_User_Status DEFAULT (1), -- 1: ACTIVE, 2: INACTIVE...
    IsActive        BIT             NOT NULL CONSTRAINT DF_User_IsActive DEFAULT (1),
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_User_CreatedAt DEFAULT SYSDATETIME(),
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
    CreatedAt        DATETIME2      NOT NULL CONSTRAINT DF_CustomerProfile_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt        DATETIME2      NULL,
    CONSTRAINT PK_CustomerProfile PRIMARY KEY (CustomerId),
    CONSTRAINT FK_CustomerProfile_User FOREIGN KEY (UserId) REFERENCES dbo.[User](UserId)
);
GO

-- 5. ManagerProfile
CREATE TABLE dbo.ManagerProfile (
    ManagerId       INT IDENTITY(1,1) NOT NULL,
    UserId          INT             NOT NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_ManagerProfile_CreatedAt DEFAULT SYSDATETIME(),
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
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_TechnicianProfile_CreatedAt DEFAULT SYSDATETIME(),
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
    CreatedAt               DATETIME2      NOT NULL CONSTRAINT DF_Address_CreatedAt DEFAULT SYSDATETIME(),
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
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_Location_CreatedAt DEFAULT SYSDATETIME(),
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
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_Shop_CreatedAt DEFAULT SYSDATETIME(),
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
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_ShopImage_CreatedAt DEFAULT SYSDATETIME(),
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
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_ServiceCategory_CreatedAt DEFAULT SYSDATETIME(),
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
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_Service_CreatedAt DEFAULT SYSDATETIME(),
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
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_ShopService_CreatedAt DEFAULT SYSDATETIME(),
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
    EffectiveFrom   DATETIME2       NOT NULL CONSTRAINT DF_ServicePrice_EffectiveFrom DEFAULT SYSDATETIME(),
    EffectiveTo     DATETIME2       NULL,
    IsActive        BIT             NOT NULL CONSTRAINT DF_ServicePrice_IsActive DEFAULT (1),
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_ServicePrice_CreatedAt DEFAULT SYSDATETIME(),
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
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_TechSpec_CreatedAt DEFAULT SYSDATETIME(),
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
    JoinedAt        DATETIME2       NOT NULL CONSTRAINT DF_TechnicianShop_JoinedAt DEFAULT SYSDATETIME(),
    LeftAt          DATETIME2       NULL,
    Status          INT             NOT NULL CONSTRAINT DF_TechnicianShop_Status DEFAULT (1),
    IsPrimary       BIT             NOT NULL CONSTRAINT DF_TechnicianShop_IsPrimary DEFAULT (0),
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_TechnicianShop_CreatedAt DEFAULT SYSDATETIME(),
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
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_RepairItem_CreatedAt DEFAULT SYSDATETIME(),
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
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_RepairPart_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt       DATETIME2       NULL,
    CONSTRAINT PK_RepairPart PRIMARY KEY (RepairPartId)
);
GO

-- 19. RepairRequest
CREATE TABLE dbo.RepairRequest (
    RepairRequestId INT IDENTITY(1,1) NOT NULL,
    CustomerId      INT             NOT NULL,
    ShopId          INT             NOT NULL,
    AddressId       INT             NOT NULL,
    RepairMethod    INT             NOT NULL CONSTRAINT DF_RepairRequest_Method DEFAULT (1), -- 1: AtHome, 2: AtShop
    Description     NVARCHAR(2000)  NULL,
    PreferredDate   DATE            NULL,
    PreferredTimeFrom TIME          NULL,
    PreferredTimeTo TIME            NULL,
    Status          INT             NOT NULL CONSTRAINT DF_RepairRequest_Status DEFAULT (1), -- 1: PENDING
    Priority        INT             NOT NULL CONSTRAINT DF_RepairRequest_Priority DEFAULT (2), -- 2: NORMAL
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_RepairRequest_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt       DATETIME2       NULL,
    CompletedAt     DATETIME2       NULL,
    CancelledAt     DATETIME2       NULL,
    CONSTRAINT PK_RepairRequest PRIMARY KEY (RepairRequestId),
    CONSTRAINT FK_RepairRequest_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.CustomerProfile(CustomerId),
    CONSTRAINT FK_RepairRequest_Shop FOREIGN KEY (ShopId) REFERENCES dbo.Shop(ShopId),
    CONSTRAINT FK_RepairRequest_Address FOREIGN KEY (AddressId) REFERENCES dbo.Address(AddressId)
);
GO

-- 20. RepairRequestImage
CREATE TABLE dbo.RepairRequestImage (
    RepairRequestImageId INT IDENTITY(1,1) NOT NULL,
    RepairRequestId INT             NOT NULL,
    ImageUrl        NVARCHAR(500)   NOT NULL,
    Caption         NVARCHAR(200)   NULL,
    DisplayOrder    INT             NOT NULL CONSTRAINT DF_RepairRequestImage_DisplayOrder DEFAULT (0),
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_RepairRequestImage_CreatedAt DEFAULT SYSDATETIME(),
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
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_RepairRequestService_CreatedAt DEFAULT SYSDATETIME(),
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
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_RRStatusHistory_CreatedAt DEFAULT SYSDATETIME(),
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
    AssignedAt      DATETIME2       NOT NULL CONSTRAINT DF_RepairAssignment_AssignedAt DEFAULT SYSDATETIME(),
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
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_RepairSchedule_CreatedAt DEFAULT SYSDATETIME(),
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
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_Quote_CreatedAt DEFAULT SYSDATETIME(),
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
    Amount          DECIMAL(18,2)   NOT NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_QuoteDetail_CreatedAt DEFAULT SYSDATETIME(),
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
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_QuoteHistory_CreatedAt DEFAULT SYSDATETIME(),
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
    Amount          DECIMAL(18,2)   NOT NULL,
    Note            NVARCHAR(500)   NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_RepairPartUsage_CreatedAt DEFAULT SYSDATETIME(),
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
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_RepairReport_CreatedAt DEFAULT SYSDATETIME(),
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
    IssuedAt        DATETIME2       NOT NULL CONSTRAINT DF_Invoice_IssuedAt DEFAULT SYSDATETIME(),
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
    Amount          DECIMAL(18,2)   NOT NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_InvoiceDetail_CreatedAt DEFAULT SYSDATETIME(),
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
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_Payment_CreatedAt DEFAULT SYSDATETIME(),
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
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_PaymentTransaction_CreatedAt DEFAULT SYSDATETIME(),
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
    Rating          INT             NOT NULL,
    Comment         NVARCHAR(2000)  NULL,
    Status          INT             NOT NULL CONSTRAINT DF_Review_Status DEFAULT (1), -- 1: PUBLISHED
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_Review_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt       DATETIME2       NULL,
    CONSTRAINT PK_Review PRIMARY KEY (ReviewId),
    CONSTRAINT FK_Review_Request FOREIGN KEY (RepairRequestId) REFERENCES dbo.RepairRequest(RepairRequestId),
    CONSTRAINT FK_Review_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.CustomerProfile(CustomerId),
    CONSTRAINT FK_Review_Shop FOREIGN KEY (ShopId) REFERENCES dbo.Shop(ShopId)
);
GO

-- 35. ReviewImage
CREATE TABLE dbo.ReviewImage (
    ReviewImageId   INT IDENTITY(1,1) NOT NULL,
    ReviewId        INT             NOT NULL,
    ImageUrl        NVARCHAR(500)   NOT NULL,
    DisplayOrder    INT             NOT NULL CONSTRAINT DF_ReviewImage_DisplayOrder DEFAULT (0),
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_ReviewImage_CreatedAt DEFAULT SYSDATETIME(),
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
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_ReviewReply_CreatedAt DEFAULT SYSDATETIME(),
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
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_NotificationTemplate_CreatedAt DEFAULT SYSDATETIME(),
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
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_Notification_CreatedAt DEFAULT SYSDATETIME(),
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
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_UserNotification_CreatedAt DEFAULT SYSDATETIME(),
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
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_Warranty_CreatedAt DEFAULT SYSDATETIME(),
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
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_WarrantyRequest_CreatedAt DEFAULT SYSDATETIME(),
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
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_WarrantyHistory_CreatedAt DEFAULT SYSDATETIME(),
    CONSTRAINT PK_WarrantyHistory PRIMARY KEY (WarrantyHistoryId),
    CONSTRAINT FK_WarrantyHistory_WarrantyRequest FOREIGN KEY (WarrantyRequestId) REFERENCES dbo.WarrantyRequest(WarrantyRequestId),
    CONSTRAINT FK_WarrantyHistory_User FOREIGN KEY (ChangedByUserId) REFERENCES dbo.[User](UserId)
);
GO

-- 43. Conversation
CREATE TABLE dbo.Conversation (
    ConversationId  INT IDENTITY(1,1) NOT NULL,
    RepairRequestId INT             NULL,
    ShopId          INT             NOT NULL,
    Status          INT             NOT NULL CONSTRAINT DF_Conversation_Status DEFAULT (1), -- 1: OPEN
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_Conversation_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt       DATETIME2       NULL,
    CONSTRAINT PK_Conversation PRIMARY KEY (ConversationId),
    CONSTRAINT FK_Conversation_Request FOREIGN KEY (RepairRequestId) REFERENCES dbo.RepairRequest(RepairRequestId),
    CONSTRAINT FK_Conversation_Shop FOREIGN KEY (ShopId) REFERENCES dbo.Shop(ShopId)
);
GO

-- 44. ConversationMember
CREATE TABLE dbo.ConversationMember (
    ConversationMemberId INT IDENTITY(1,1) NOT NULL,
    ConversationId  INT             NOT NULL,
    UserId          INT             NOT NULL,
    JoinedAt        DATETIME2       NOT NULL CONSTRAINT DF_ConversationMember_JoinedAt DEFAULT SYSDATETIME(),
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_ConversationMember_CreatedAt DEFAULT SYSDATETIME(),
    CONSTRAINT PK_ConversationMember PRIMARY KEY (ConversationMemberId),
    CONSTRAINT FK_ConversationMember_Conversation FOREIGN KEY (ConversationId) REFERENCES dbo.Conversation(ConversationId) ON DELETE CASCADE,
    CONSTRAINT FK_ConversationMember_User FOREIGN KEY (UserId) REFERENCES dbo.[User](UserId)
);
GO

-- 45. Message
CREATE TABLE dbo.Message (
    MessageId       INT IDENTITY(1,1) NOT NULL,
    ConversationId  INT             NOT NULL,
    SenderUserId    INT             NOT NULL,
    Content         NVARCHAR(MAX)   NULL,
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_Message_CreatedAt DEFAULT SYSDATETIME(),
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
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_MessageAttachment_CreatedAt DEFAULT SYSDATETIME(),
    CONSTRAINT PK_MessageAttachment PRIMARY KEY (MessageAttachmentId),
    CONSTRAINT FK_MessageAttachment_Message FOREIGN KEY (MessageId) REFERENCES dbo.Message(MessageId) ON DELETE CASCADE
);
GO


ALTER TABLE dbo.CustomerProfile ADD CONSTRAINT FK_CustomerProfile_DefaultAddress FOREIGN KEY (DefaultAddressId) REFERENCES dbo.Address(AddressId);
GO
ALTER TABLE dbo.TechnicianProfile ADD CONSTRAINT CK_TechnicianProfile_Rating CHECK (RatingAverage BETWEEN 0 AND 5);
GO
ALTER TABLE dbo.Shop ADD CONSTRAINT CK_Shop_Rating CHECK (RatingAverage BETWEEN 0 AND 5);
GO
ALTER TABLE dbo.ServicePrice ADD CONSTRAINT CK_ServicePrice_Price CHECK (Price >= 0);
GO
ALTER TABLE dbo.RepairItem ADD CONSTRAINT CK_RepairItem_DefaultPrice CHECK (DefaultPrice >= 0);
GO
ALTER TABLE dbo.RepairPart ADD CONSTRAINT CK_RepairPart_CurrentPrice CHECK (CurrentPrice >= 0);
GO
ALTER TABLE dbo.RepairSchedule ADD CONSTRAINT CK_RepairSchedule_TimeRange CHECK (EndTime > StartTime);
GO
ALTER TABLE dbo.Quote ADD CONSTRAINT CK_Quote_Amounts CHECK (TotalAmount >= 0);
GO
ALTER TABLE dbo.QuoteDetail ADD CONSTRAINT CK_QuoteDetail_Quantity CHECK (Quantity > 0);
GO
ALTER TABLE dbo.QuoteDetail ADD CONSTRAINT CK_QuoteDetail_Amount CHECK (Amount >= 0);
GO
ALTER TABLE dbo.Invoice ADD CONSTRAINT CK_Invoice_Amounts CHECK (TotalAmount >= 0);
GO
ALTER TABLE dbo.Payment ADD CONSTRAINT CK_Payment_Amount CHECK (Amount > 0);
GO
ALTER TABLE dbo.Review ADD CONSTRAINT CK_Review_Rating CHECK (Rating BETWEEN 1 AND 5);
GO

/* ==================================================================
   PHASE 3 - TRIGGERS
   ================================================================== */

-- Trigger 1: Ngăn chặn update Address
CREATE TRIGGER dbo.trg_Address_PreventContentUpdate
ON dbo.Address
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF UPDATE(OwnerType) OR UPDATE(CustomerId) OR UPDATE(ProvinceId)
       OR UPDATE(WardId) OR UPDATE(AddressLine) OR UPDATE(RecipientName)
       OR UPDATE(RecipientPhone) OR UPDATE(CreatedAt)
    BEGIN
        RAISERROR(N'Address la immutable. Hay INSERT dia chi moi va set SupersededByAddressId.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END
GO

-- Trigger 2: Tự động ghi lịch sử trạng thái RepairRequest
CREATE TRIGGER dbo.trg_RepairRequest_StatusHistory
ON dbo.RepairRequest
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF UPDATE(Status)
    BEGIN
        INSERT INTO dbo.RepairRequestStatusHistory (RepairRequestId, OldStatus, NewStatus, ChangedByUserId, Reason)
        SELECT i.RepairRequestId, d.Status, i.Status, NULL, N'Hệ thống tự động ghi nhận thay đổi trạng thái'
        FROM inserted i
        JOIN deleted d ON i.RepairRequestId = d.RepairRequestId
        WHERE i.Status <> d.Status;
    END
END
GO

-- Trigger 3: Tự động cập nhật điểm đánh giá cho Shop
CREATE TRIGGER dbo.trg_Review_UpdateShopRating
ON dbo.Review
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DISTINCT ShopId INTO #AffectedShops
    FROM (SELECT ShopId FROM inserted UNION SELECT ShopId FROM deleted) AS tmp;

    UPDATE s
    SET RatingAverage = ISNULL((SELECT AVG(CAST(Rating AS DECIMAL(3,2))) FROM dbo.Review WHERE ShopId = s.ShopId AND Status = 1), 0)
    FROM dbo.Shop s JOIN #AffectedShops a ON s.ShopId = a.ShopId;
END
GO


CREATE TYPE dbo.IntIdList AS TABLE (Id INT NOT NULL);
GO

CREATE PROCEDURE dbo.sp_CreateRepairRequest
    @CustomerId     INT,
    @ShopId         INT,
    @AddressId      INT,
    @RepairMethod   INT = 1,
    @Description    NVARCHAR(2000) = NULL,
    @PreferredDate  DATE = NULL,
    @PreferredTimeFrom TIME = NULL,
    @PreferredTimeTo   TIME = NULL,
    @ServiceIds     dbo.IntIdList READONLY,
    @NewRepairRequestId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM @ServiceIds) THROW 50004, 'Phai co it nhat 1 ServiceId.', 1;

        BEGIN TRANSACTION;

        INSERT INTO dbo.RepairRequest (CustomerId, ShopId, AddressId, RepairMethod, Description, PreferredDate, PreferredTimeFrom, PreferredTimeTo, Status)
        VALUES (@CustomerId, @ShopId, @AddressId, @RepairMethod, @Description, @PreferredDate, @PreferredTimeFrom, @PreferredTimeTo, 1); 
        SET @NewRepairRequestId = SCOPE_IDENTITY();

        INSERT INTO dbo.RepairRequestService (RepairRequestId, ServiceId)
        SELECT @NewRepairRequestId, Id FROM @ServiceIds;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO


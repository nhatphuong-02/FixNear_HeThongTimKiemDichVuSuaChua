/* ==================================================================
   FixNearDB - FULL SEED DATA SCRIPT
   - Bao phủ 100% các bảng (46 bảng)
   - Mỗi bảng 2-3 records
   - Data liên kết chuỗi logic: Từ tạo User -> Đặt lịch -> Báo giá -> Thanh toán -> Đánh giá -> Bảo hành -> Chat
   ================================================================== */

USE FixNearDB;
GO

BEGIN TRANSACTION SeedFullData;
BEGIN TRY

-- Khai báo tất cả các biến ID cần thiết để link dữ liệu
DECLARE 
    @ProvDN INT, @ProvHCM INT, 
    @WardHC INT, @WardLK INT, @WardQ1 INT,
    @U_Thinh INT, @U_Tien INT, @U_Hoang INT, @U_Phuong INT, @U_Nhat INT,
    @Mgr_Tien INT, @Tech_Hoang INT, @Tech_Phuong INT, @Cus_Nhat INT,
    @AddrCus1 INT, @AddrCus2 INT, @AddrShop1 INT, @AddrShop2 INT,
    @Loc1 INT, @Loc2 INT,
    @Shop1 INT, @Shop2 INT,
    @CatDienThoai INT, @CatDien INT, @CatNuoc INT,
    @SvcManHinh INT, @SvcPin INT, @SvcChapDien INT, @SvcOngNuoc INT,
    @SS_ManHinh INT, @SS_ChapDien INT, @SS_OngNuoc INT,
    @ItemKham INT, @ItemVeSinh INT,
    @PartManHinh INT, @PartOngNuoc INT,
    @Req1 INT, @Req2 INT,
    @Sched1 INT, @Sched2 INT,
    @Quote1 INT, @Quote2 INT,
    @Inv1 INT, @Inv2 INT,
    @Pay1 INT, @Pay2 INT,
    @Rev1 INT, @Rev2 INT,
    @Tpl1 INT, @Tpl2 INT, @Notif1 INT, @Notif2 INT,
    @War1 INT, @War2 INT, @WarReq1 INT,
    @Conv1 INT, @Conv2 INT, @Msg1 INT, @Msg2 INT;

PRINT N'1. Tạo Province & Ward...';
INSERT INTO dbo.Province (ProvinceCode, ProvinceName) VALUES (N'DNG', N'Đà Nẵng'), (N'HCM', N'Hồ Chí Minh');
SET @ProvDN = (SELECT TOP 1 ProvinceId FROM dbo.Province WHERE ProvinceCode = N'DNG');
SET @ProvHCM = (SELECT TOP 1 ProvinceId FROM dbo.Province WHERE ProvinceCode = N'HCM');

INSERT INTO dbo.Ward (ProvinceId, WardCode, WardName) VALUES 
    (@ProvDN, N'HC01', N'Phường Hải Châu 1'), 
    (@ProvDN, N'LK01', N'Phường Hòa Khánh Bắc'),
    (@ProvHCM, N'Q101', N'Phường Bến Nghé');
SET @WardHC = (SELECT TOP 1 WardId FROM dbo.Ward WHERE WardCode = N'HC01');
SET @WardLK = (SELECT TOP 1 WardId FROM dbo.Ward WHERE WardCode = N'LK01');
SET @WardQ1 = (SELECT TOP 1 WardId FROM dbo.Ward WHERE WardCode = N'Q101');

PRINT N'2. Tạo Users...';
-- RoleType: 1=ADMIN, 2=MANAGER, 3=TECHNICIAN, 4=CUSTOMER | Gender: 1=Nam, 0=Nữ | Status: 1=Active
INSERT INTO dbo.[User] (RoleType, FullName, Phone, Email, PasswordHash, Gender, Status) 
VALUES (1, N'Lê Văn Thịnh', N'0901111111', N'thinh@fixnear.vn', N'HASH_ADMIN', 1, 1); SET @U_Thinh = SCOPE_IDENTITY();
INSERT INTO dbo.[User] (RoleType, FullName, Phone, Email, PasswordHash, Gender, Status) 
VALUES (2, N'Phạm Thanh Tiến', N'0902222222', N'tien@fixnear.vn', N'HASH_MGR', 1, 1); SET @U_Tien = SCOPE_IDENTITY();
INSERT INTO dbo.[User] (RoleType, FullName, Phone, Email, PasswordHash, Gender, Status) 
VALUES (3, N'Hồ Văn Hoàng', N'0903333333', N'hoang@fixnear.vn', N'HASH_TECH1', 1, 1); SET @U_Hoang = SCOPE_IDENTITY();
INSERT INTO dbo.[User] (RoleType, FullName, Phone, Email, PasswordHash, Gender, Status) 
VALUES (3, N'Quách Nhật Phương', N'0904444444', N'phuong@fixnear.vn', N'HASH_TECH2', 0, 1); SET @U_Phuong = SCOPE_IDENTITY();
INSERT INTO dbo.[User] (RoleType, FullName, Phone, Email, PasswordHash, Gender, Status) 
VALUES (4, N'Hoàng Văn Quốc Nhật', N'0905555555', N'nhat@fixnear.vn', N'HASH_CUS', 1, 1); SET @U_Nhat = SCOPE_IDENTITY();

PRINT N'3. Tạo Profiles...';
INSERT INTO dbo.ManagerProfile (UserId) VALUES (@U_Tien); SET @Mgr_Tien = SCOPE_IDENTITY();
INSERT INTO dbo.TechnicianProfile (UserId, ExperienceYears, Introduction, TechnicianStatus) VALUES 
    (@U_Hoang, 5, N'Chuyên điện thoại, thay pin/màn hình', 1),
    (@U_Phuong, 8, N'Kỹ sư điện nước dân dụng', 1);
SET @Tech_Hoang = (SELECT TOP 1 TechnicianId FROM dbo.TechnicianProfile WHERE UserId = @U_Hoang);
SET @Tech_Phuong = (SELECT TOP 1 TechnicianId FROM dbo.TechnicianProfile WHERE UserId = @U_Phuong);
INSERT INTO dbo.CustomerProfile (UserId) VALUES (@U_Nhat); SET @Cus_Nhat = SCOPE_IDENTITY();

PRINT N'4. Tạo Address & Location...';
-- OwnerType: 1=Customer, 2=Shop
INSERT INTO dbo.Address (OwnerType, CustomerId, ProvinceId, WardId, AddressLine, RecipientName, RecipientPhone) VALUES 
    (1, @Cus_Nhat, @ProvDN, @WardLK, N'Ký túc xá DMC', N'Quốc Nhật', N'0905555555'),
    (1, @Cus_Nhat, @ProvDN, @WardHC, N'Công ty ABC, Bạch Đằng', N'Quốc Nhật', N'0905555555');
SET @AddrCus1 = (SELECT TOP 1 AddressId FROM dbo.Address WHERE AddressLine LIKE N'%Ký túc xá%');
SET @AddrCus2 = (SELECT TOP 1 AddressId FROM dbo.Address WHERE AddressLine LIKE N'%Công ty ABC%');
UPDATE dbo.CustomerProfile SET DefaultAddressId = @AddrCus1 WHERE CustomerId = @Cus_Nhat;

INSERT INTO dbo.Address (OwnerType, CustomerId, ProvinceId, WardId, AddressLine, RecipientName, RecipientPhone) VALUES 
    (2, NULL, @ProvDN, @WardHC, N'123 Lê Duẩn', N'Shop FixNear Center', N'19001001'),
    (2, NULL, @ProvDN, @WardLK, N'45 Tôn Đức Thắng', N'Shop Điện Nước Liên Chiểu', N'19001002');
SET @AddrShop1 = (SELECT TOP 1 AddressId FROM dbo.Address WHERE AddressLine LIKE N'%Lê Duẩn%');
SET @AddrShop2 = (SELECT TOP 1 AddressId FROM dbo.Address WHERE AddressLine LIKE N'%Tôn Đức Thắng%');

INSERT INTO dbo.Location (AddressId, Latitude, Longitude) VALUES 
    (@AddrShop1, 16.0666, 108.2111),
    (@AddrShop2, 16.0777, 108.1555);

PRINT N'5. Tạo Shop, Service & Pricing...';
INSERT INTO dbo.Shop (ManagerId, AddressId, ShopName, Status, IsVerified) VALUES 
    (@Mgr_Tien, @AddrShop1, N'FixNear Store Điện Thoại', 1, 1),
    (@Mgr_Tien, @AddrShop2, N'FixNear Điện Nước Pro', 1, 1);
SET @Shop1 = (SELECT TOP 1 ShopId FROM dbo.Shop WHERE ShopName LIKE N'%Điện Thoại%');
SET @Shop2 = (SELECT TOP 1 ShopId FROM dbo.Shop WHERE ShopName LIKE N'%Điện Nước%');

INSERT INTO dbo.ShopImage (ShopId, ImageUrl, IsPrimary) VALUES 
    (@Shop1, N'img_shop1.jpg', 1), (@Shop1, N'img_shop1_in.jpg', 0),
    (@Shop2, N'img_shop2.jpg', 1);

INSERT INTO dbo.ServiceCategory (CategoryName) VALUES (N'Điện thoại'), (N'Điện'), (N'Nước');
SET @CatDienThoai = (SELECT TOP 1 ServiceCategoryId FROM dbo.ServiceCategory WHERE CategoryName=N'Điện thoại');
SET @CatDien = (SELECT TOP 1 ServiceCategoryId FROM dbo.ServiceCategory WHERE CategoryName=N'Điện');
SET @CatNuoc = (SELECT TOP 1 ServiceCategoryId FROM dbo.ServiceCategory WHERE CategoryName=N'Nước');

INSERT INTO dbo.Service (CategoryId, ServiceName) VALUES 
    (@CatDienThoai, N'Thay màn hình iPhone'), (@CatDienThoai, N'Thay Pin Điện Thoại'),
    (@CatDien, N'Xử lý chập điện'), (@CatNuoc, N'Thay ống nước');
SET @SvcManHinh = (SELECT TOP 1 ServiceId FROM dbo.Service WHERE ServiceName LIKE N'%màn hình%');
SET @SvcPin = (SELECT TOP 1 ServiceId FROM dbo.Service WHERE ServiceName LIKE N'%Pin%');
SET @SvcChapDien = (SELECT TOP 1 ServiceId FROM dbo.Service WHERE ServiceName LIKE N'%chập điện%');
SET @SvcOngNuoc = (SELECT TOP 1 ServiceId FROM dbo.Service WHERE ServiceName LIKE N'%ống nước%');

INSERT INTO dbo.ShopService (ShopId, ServiceId) VALUES 
    (@Shop1, @SvcManHinh), (@Shop1, @SvcPin),
    (@Shop2, @SvcChapDien), (@Shop2, @SvcOngNuoc);
SET @SS_ManHinh = (SELECT TOP 1 ShopServiceId FROM dbo.ShopService WHERE ServiceId=@SvcManHinh);
SET @SS_ChapDien = (SELECT TOP 1 ShopServiceId FROM dbo.ShopService WHERE ServiceId=@SvcChapDien);

INSERT INTO dbo.ServicePrice (ShopServiceId, Price, PriceUnit) VALUES 
    (@SS_ManHinh, 100000, N'lần'), (@SS_ChapDien, 150000, N'lần');

INSERT INTO dbo.TechnicianSpecialization (TechnicianId, ServiceId) VALUES 
    (@Tech_Hoang, @SvcManHinh), (@Tech_Hoang, @SvcPin),
    (@Tech_Phuong, @SvcChapDien), (@Tech_Phuong, @SvcOngNuoc);

INSERT INTO dbo.TechnicianShop (TechnicianId, ShopId, Status, IsPrimary) VALUES 
    (@Tech_Hoang, @Shop1, 1, 1), (@Tech_Phuong, @Shop2, 1, 1);

PRINT N'6. Tạo RepairItem & Part...';
INSERT INTO dbo.RepairItem (ItemCode, ItemName, DefaultPrice) VALUES 
    (N'ITM-01', N'Phí khám lỗi tại nhà', 50000), (N'ITM-02', N'Vệ sinh máy', 100000);
SET @ItemKham = (SELECT TOP 1 RepairItemId FROM dbo.RepairItem WHERE ItemCode=N'ITM-01');
INSERT INTO dbo.RepairPart (PartCode, PartName, CurrentPrice) VALUES 
    (N'PART-IP15', N'Màn hình iPhone 15 Pro Max', 6000000), (N'PART-PVC', N'Ống nhựa Bình Minh 27', 35000);
SET @PartManHinh = (SELECT TOP 1 RepairPartId FROM dbo.RepairPart WHERE PartCode=N'PART-IP15');
SET @PartOngNuoc = (SELECT TOP 1 RepairPartId FROM dbo.RepairPart WHERE PartCode=N'PART-PVC');

PRINT N'7. Repair Requests (Nhật đặt lịch 2 đơn)...';
-- Đơn 1: Đem máy ra tiệm (Method=2) - Shop 1 (ĐT)
INSERT INTO dbo.RepairRequest (CustomerId, ShopId, AddressId, RepairMethod, Description, Status, Priority)
VALUES (@Cus_Nhat, @Shop1, @AddrCus1, 2, N'Máy rơi vỡ nát màn hình, mai mình mang ra tiệm', 4, 2); -- 4: In_Progress
SET @Req1 = SCOPE_IDENTITY();
INSERT INTO dbo.RepairRequestImage (RepairRequestId, ImageUrl) VALUES (@Req1, N'iphone_vo.jpg'), (@Req1, N'iphone_vo2.jpg');
INSERT INTO dbo.RepairRequestService (RepairRequestId, ServiceId) VALUES (@Req1, @SvcManHinh);

-- Đơn 2: Sửa tại nhà (Method=1) - Shop 2 (Nước)
INSERT INTO dbo.RepairRequest (CustomerId, ShopId, AddressId, RepairMethod, Description, Status, Priority)
VALUES (@Cus_Nhat, @Shop2, @AddrCus1, 1, N'Ký túc xá bị vỡ ống nước, thợ tới gấp', 5, 4); -- 5: Completed, 4: Urgent
SET @Req2 = SCOPE_IDENTITY();
INSERT INTO dbo.RepairRequestService (RepairRequestId, ServiceId) VALUES (@Req2, @SvcOngNuoc);

-- Lịch sử
INSERT INTO dbo.RepairRequestStatusHistory (RepairRequestId, OldStatus, NewStatus, Reason) VALUES 
    (@Req1, 1, 2, N'Shop tiếp nhận'), (@Req2, 4, 5, N'Hoàn tất sửa nước');

PRINT N'8. Assignment & Schedule...';
INSERT INTO dbo.RepairAssignment (RepairRequestId, TechnicianId, AssignedByUserId, AssignmentStatus) VALUES 
    (@Req1, @Tech_Hoang, @U_Tien, 2), -- 2: ACCEPTED
    (@Req2, @Tech_Phuong, @U_Tien, 2);
INSERT INTO dbo.RepairSchedule (RepairRequestId, TechnicianId, StartTime, EndTime, Status) VALUES 
    (@Req1, @Tech_Hoang, DATEADD(DAY, 1, SYSDATETIME()), DATEADD(DAY, 1, DATEADD(HOUR, 2, SYSDATETIME())), 1),
    (@Req2, @Tech_Phuong, DATEADD(DAY, -1, SYSDATETIME()), DATEADD(DAY, -1, DATEADD(HOUR, 1, SYSDATETIME())), 2); -- 2: Completed

PRINT N'9. Quotes...';
INSERT INTO dbo.Quote (RepairRequestId, TechnicianId, QuoteNumber, TotalAmount, Status, CustomerResponse) VALUES 
    (@Req1, @Tech_Hoang, N'QT-001', 6100000, 3, 1), -- 3: Accepted
    (@Req2, @Tech_Phuong, N'QT-002', 150000, 3, 1);
SET @Quote1 = (SELECT TOP 1 QuoteId FROM dbo.Quote WHERE QuoteNumber=N'QT-001');
SET @Quote2 = (SELECT TOP 1 QuoteId FROM dbo.Quote WHERE QuoteNumber=N'QT-002');

-- ItemType: 1=SVC, 2=ITEM, 3=PART
INSERT INTO dbo.QuoteDetail (QuoteId, ItemType, ServiceRefId, RepairItemRefId, RepairPartRefId, Quantity, UnitPrice, Amount) VALUES 
    (@Quote1, 1, @SvcManHinh, NULL, NULL, 1, 100000, 100000),
    (@Quote1, 3, NULL, NULL, @PartManHinh, 1, 6000000, 6000000),
    (@Quote2, 2, NULL, @ItemKham, NULL, 1, 50000, 50000),
    (@Quote2, 1, @SvcOngNuoc, NULL, NULL, 1, 100000, 100000);
INSERT INTO dbo.QuoteHistory (QuoteId, NewStatus, ChangeNote) VALUES (@Quote1, 3, N'Khách chốt giá màn hình'), (@Quote2, 3, N'Khách ok sửa nước');

PRINT N'10. Báo cáo & Hóa đơn...';
INSERT INTO dbo.RepairPartUsage (RepairRequestId, RepairPartId, Quantity, UnitPriceAtUsage, Amount) VALUES (@Req1, @PartManHinh, 1, 6000000, 6000000);
INSERT INTO dbo.RepairReport (RepairRequestId, TechnicianId, Diagnosis, WorkDescription) VALUES (@Req1, @Tech_Hoang, N'Hỏng màn', N'Đã thay'), (@Req2, @Tech_Phuong, N'Vỡ ống', N'Đã nối');

INSERT INTO dbo.Invoice (RepairRequestId, InvoiceNumber, CustomerId, ShopId, TotalAmount, Status) VALUES 
    (@Req1, N'INV-001', @Cus_Nhat, @Shop1, 6100000, 1), -- 1: UNPAID
    (@Req2, N'INV-002', @Cus_Nhat, @Shop2, 150000, 3);  -- 3: PAID
SET @Inv1 = (SELECT TOP 1 InvoiceId FROM dbo.Invoice WHERE InvoiceNumber=N'INV-001');
SET @Inv2 = (SELECT TOP 1 InvoiceId FROM dbo.Invoice WHERE InvoiceNumber=N'INV-002');

INSERT INTO dbo.InvoiceDetail (InvoiceId, Description, Quantity, UnitPrice, Amount) VALUES 
    (@Inv1, N'Thay màn hình + Part', 1, 6100000, 6100000),
    (@Inv2, N'Khám và nối ống', 1, 150000, 150000);

PRINT N'11. Thanh toán...';
-- Status: 2=SUCCESS | PaymentMethod: 1=CASH, 3=BANK_TRANSFER
INSERT INTO dbo.Payment (InvoiceId, PaymentMethod, Amount, Status) VALUES (@Inv2, 3, 150000, 2);
SET @Pay2 = SCOPE_IDENTITY();
INSERT INTO dbo.PaymentTransaction (PaymentId, TransactionCode, Status, Message) VALUES (@Pay2, N'MBBANK-123', 2, N'CK OK');

PRINT N'12. Đánh giá (Review)...';
INSERT INTO dbo.Review (RepairRequestId, CustomerId, ShopId, Rating, Comment, Status) VALUES 
    (@Req2, @Cus_Nhat, @Shop2, 5, N'Phương làm nhanh gọn, nhiệt tình lắm', 1),
    (@Req1, @Cus_Nhat, @Shop1, 4, N'Shop phục vụ tốt, giá hơi cao', 1);
SET @Rev2 = (SELECT TOP 1 ReviewId FROM dbo.Review WHERE ShopId=@Shop2);
INSERT INTO dbo.ReviewImage (ReviewId, ImageUrl) VALUES (@Rev2, N'ong_nuoc_xong.jpg');
INSERT INTO dbo.ReviewReply (ReviewId, RepliedByUserId, ReplyContent) VALUES (@Rev2, @U_Tien, N'Cảm ơn bạn Nhật đã ủng hộ ạ!');

PRINT N'13. Bảo hành...';
INSERT INTO dbo.Warranty (RepairRequestId, InvoiceId, WarrantyCode, StartDate, EndDate, Status) VALUES 
    (@Req2, @Inv2, N'WAR-002', CAST(SYSDATETIME() AS DATE), DATEADD(MONTH, 3, CAST(SYSDATETIME() AS DATE)), 1);
SET @War2 = SCOPE_IDENTITY();
INSERT INTO dbo.WarrantyRequest (WarrantyId, CustomerId, IssueDescription, Status) VALUES (@War2, @Cus_Nhat, N'Chỗ nối rỉ một ít nước', 1);
SET @WarReq1 = SCOPE_IDENTITY();
INSERT INTO dbo.WarrantyHistory (WarrantyRequestId, NewStatus, Note) VALUES (@WarReq1, 1, N'Tiếp nhận BH');

PRINT N'14. Chat & Notification...';
INSERT INTO dbo.NotificationTemplate (TemplateCode, Title, BodyTemplate) VALUES (N'TPL_QUOTE', N'Báo giá mới', N'Có báo giá mới từ shop'), (N'TPL_ASSIGN', N'Phân công', N'Thợ đang đến');
SET @Tpl1 = (SELECT TOP 1 NotificationTemplateId FROM dbo.NotificationTemplate WHERE TemplateCode=N'TPL_QUOTE');
INSERT INTO dbo.Notification (TemplateId, Title, Body, RefType, RefId) VALUES (@Tpl1, N'Báo giá sửa nước', N'Mời bạn xem', 1, @Req2); SET @Notif1 = SCOPE_IDENTITY();
INSERT INTO dbo.UserNotification (NotificationId, UserId) VALUES (@Notif1, @U_Nhat);

INSERT INTO dbo.Conversation (RepairRequestId, ShopId, Status) VALUES (@Req1, @Shop1, 1), (@Req2, @Shop2, 1);
SET @Conv1 = (SELECT TOP 1 ConversationId FROM dbo.Conversation WHERE RepairRequestId=@Req1);
INSERT INTO dbo.ConversationMember (ConversationId, UserId) VALUES (@Conv1, @U_Nhat), (@Conv1, @U_Tien);
INSERT INTO dbo.Message (ConversationId, SenderUserId, Content) VALUES (@Conv1, @U_Nhat, N'Shop check màn hình IP15 cho mình nhé');
SET @Msg1 = SCOPE_IDENTITY();
INSERT INTO dbo.MessageAttachment (MessageId, FileUrl) VALUES (@Msg1, N'man_hinh.png');
INSERT INTO dbo.Message (ConversationId, SenderUserId, Content) VALUES (@Conv1, @U_Tien, N'Dạ bên em còn hàng Zin ạ, giá 6 củ nhé bạn.');

COMMIT TRANSACTION SeedFullData;
PRINT N'==> TẠO DỮ LIỆU SEED (46 BẢNG) THÀNH CÔNG!';

END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION SeedFullData;
    PRINT N'==> LỖI SEED DATA: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
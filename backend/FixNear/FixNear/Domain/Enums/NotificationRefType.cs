namespace FixNear.Domain.Enums
{ 
    public enum NotificationRefType
    {
        // Lời mời hợp tác giữa Shop và Technician
        TechnicianShopInvitation = 1,

        // Yêu cầu sửa chữa
        RepairRequest = 2,

        // Phân công kỹ thuật viên
        RepairAssignment = 3,

        // Lịch sửa chữa
        RepairSchedule = 4,

        // Báo giá
        Quote = 5,

        // Hóa đơn
        Invoice = 6,

        // Giao dịch thanh toán
        Payment = 7,

        // Đánh giá dịch vụ
        Review = 8,

        // Phiếu bảo hành
        Warranty = 9,

        // Yêu cầu bảo hành
        WarrantyRequest = 10,

        // Cửa hàng
        Shop = 11,

        // Tin nhắn
        Message = 12
    }
}

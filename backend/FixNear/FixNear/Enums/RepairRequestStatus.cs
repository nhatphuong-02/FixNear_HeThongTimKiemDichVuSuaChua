namespace FixNear.Enums;

public enum RepairRequestStatus
{
    Pending = 1,        // Customer tạo, chờ Shop phản hồi
    Accepted = 2,       // Shop đã tiếp nhận
    Rejected = 3,       // Shop từ chối
    Assigned = 4,       // Tech đã được phân công và nhận việc
    Inspecting = 5,     // Tech đang khảo sát
    Quoted = 6,         // Tech đã gửi báo giá
    QuoteApproved = 7,  // Customer duyệt báo giá
    Repairing = 8,      // Đang sửa chữa
    Completed = 9,      // Sửa xong, chờ nghiệm thu/thanh toán
    Paid = 10,          // Đã thanh toán
    Cancelled = 11      // Yêu cầu bị hủy
}

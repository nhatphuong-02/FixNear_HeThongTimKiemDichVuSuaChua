namespace FixNear.Enums;

public enum RepairScheduleStatus
{
    Scheduled = 1,    // Lịch mới tạo
    Rescheduled = 2,  // Lịch đã được đổi giờ
    Completed = 3,    // Buổi hẹn đã diễn ra
    Cancelled = 4     // Lịch đã bị hủy
}

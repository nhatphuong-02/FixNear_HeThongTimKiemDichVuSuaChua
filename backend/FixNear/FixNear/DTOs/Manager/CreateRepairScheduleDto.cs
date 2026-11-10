using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.Manager;

public class CreateRepairScheduleDto : IValidatableObject
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Mã yêu cầu phải lớn hơn 0.")]
    public int RepairRequestId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Mã kỹ thuật viên phải lớn hơn 0.")]
    public int TechnicianId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập thời gian bắt đầu.")]
    public DateTimeOffset StartTime { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập thời gian kết thúc.")]
    public DateTimeOffset EndTime { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (StartTime == default)
        {
            yield return new ValidationResult(
                "Vui lòng nhập thời gian bắt đầu.",
                new[] { nameof(StartTime) });
        }

        if (EndTime == default)
        {
            yield return new ValidationResult(
                "Vui lòng nhập thời gian kết thúc.",
                new[] { nameof(EndTime) });
        }

        if (StartTime != default &&
            EndTime != default &&
            EndTime <= StartTime)
        {
            yield return new ValidationResult(
                "Thời gian kết thúc phải sau thời gian bắt đầu.",
                new[] { nameof(StartTime), nameof(EndTime) });
        }
    }
}

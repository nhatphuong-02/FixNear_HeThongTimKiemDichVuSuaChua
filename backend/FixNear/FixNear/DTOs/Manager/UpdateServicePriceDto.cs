using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.Manager;

public class UpdateServicePriceDto : IValidatableObject
{
    [Range(typeof(decimal), "0.01", "9999999999999999")]
    public decimal Price { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    [StringLength(50)]
    public string? PriceUnit { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext context)
    {
        if (MinPrice is < 0 || MaxPrice is < 0)
            yield return new ValidationResult(
                "Giá không được âm.");

        if (MinPrice.HasValue && MaxPrice.HasValue &&
            MinPrice > MaxPrice)
            yield return new ValidationResult(
                "MinPrice không được lớn hơn MaxPrice.");

        if ((MinPrice.HasValue && Price < MinPrice) ||
            (MaxPrice.HasValue && Price > MaxPrice))
            yield return new ValidationResult(
                "Price nằm ngoài khoảng giá hợp lệ.");
    }
}

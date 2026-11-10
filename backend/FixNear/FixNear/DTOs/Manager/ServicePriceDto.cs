using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.Manager;

public class ServicePriceDto : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int ShopServiceId { get; set; }

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
                "Giá tối thiểu và tối đa không được âm.");

        if (MinPrice.HasValue && MaxPrice.HasValue &&
            MinPrice > MaxPrice)
            yield return new ValidationResult(
                "MinPrice không được lớn hơn MaxPrice.");

        if ((MinPrice.HasValue && Price < MinPrice) ||
            (MaxPrice.HasValue && Price > MaxPrice))
            yield return new ValidationResult(
                "Price phải nằm trong khoảng giá tối thiểu và tối đa.");
    }
}

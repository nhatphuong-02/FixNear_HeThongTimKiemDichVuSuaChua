using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.Customer
{
    public class AddressRequestDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn tỉnh/thành phố")]
        public int ProvinceId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn quận/huyện")]
        public int WardId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        public string AddressLine { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập tên người liên lạc")]
        public string RecipientName { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        public string RecipientPhone { get; set; } = null!;
    }
}

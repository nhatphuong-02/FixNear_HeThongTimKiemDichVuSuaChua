using FixNear.Enums;
using System.ComponentModel.DataAnnotations;

namespace FixNear.DTOs.Customer
{
    public class CustomerProfileResponseDto
    {
        public int CustomerId { get; set; }
        public int UserId { get; set; }
        public string? AvatarUrl { get; set; }
        public string FullName { get; set; }
        public string? Phone {  get; set; }
        public string? Email { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public Gender? Gender { get; set; }
        public RoleType RoleType { get; set; }
        public int? DefaultAddressId {  get; set; }
        public int? ProvinceId { get; set; }
        public int? WardId { get; set; }
        public string? AddressLine { get; set; }
        public string? RecipientName { get; set; }
        public string? RecipientPhone { get; set; }
    }
}

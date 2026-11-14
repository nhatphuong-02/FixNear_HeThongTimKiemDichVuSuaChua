namespace FixNear.Features.Auth.DTOs
{
    public class CurrentUserResponseDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public int RoleType { get; set; }
    }
}

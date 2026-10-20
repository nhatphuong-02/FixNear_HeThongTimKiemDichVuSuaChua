namespace FixNear.Services.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(int userId, string fullname, int roleType);
    }
}

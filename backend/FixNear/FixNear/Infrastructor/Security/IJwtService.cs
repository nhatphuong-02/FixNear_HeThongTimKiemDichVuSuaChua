namespace FixNear.Infrastructor.Security
{
    public interface IJwtService
    {
        string GenerateToken(int userId, string fullname, int roleType);
    }
}

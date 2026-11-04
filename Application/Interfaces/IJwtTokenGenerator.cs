namespace Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(int userId, string email, string fullName, string role);
}

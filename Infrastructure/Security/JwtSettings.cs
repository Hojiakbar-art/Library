namespace Infrastructure.Security;

public class JwtSettings
{
    public string Key { get; set; } = "super-secret-key-for-library-api-1234567890";
    public string Issuer { get; set; } = "LibraryApi";
    public string Audience { get; set; } = "LibraryApiUsers";
    public int ExpiresMinutes { get; set; } = 60;
}

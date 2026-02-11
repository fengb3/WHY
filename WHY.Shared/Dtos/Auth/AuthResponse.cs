namespace WHY.Shared.Dtos.Auth;

public class AuthResponse
{
    public string? Token { get; set; }
    public long ExpiresInMilliseconds { get; set; }
}

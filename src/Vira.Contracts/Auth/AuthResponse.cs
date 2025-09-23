namespace Vira.Contracts.Auth;
public sealed record AuthResponse(string AccessToken, string RefreshToken, string TokenType = "Bearer", int ExpiresIn = 1800);

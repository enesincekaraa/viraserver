namespace Vira.Contracts.Auth;
public sealed record RegisterRequest(string Email, string Password, string FullName);

namespace Vira.Contracts.Auth;
public sealed record MeResponse(Guid Id, string Email, string FullName, string Role);

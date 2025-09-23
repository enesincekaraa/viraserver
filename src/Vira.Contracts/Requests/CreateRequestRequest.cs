namespace Vira.Contracts.Requests;
public sealed record CreateRequestRequest(
    string Title, string? Description,
    Guid CategoryId, double Latitude, double Longitude);

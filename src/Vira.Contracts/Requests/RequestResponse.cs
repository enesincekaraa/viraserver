namespace Vira.Contracts.Requests;
public sealed record RequestResponse(
    Guid Id, string Title, string? Description,
    Guid? CategoryId, int Status, Guid CreatedByUserId, Guid? AssignedToUserId,
    double Latitude, double Longitude);

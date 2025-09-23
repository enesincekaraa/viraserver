public sealed record NearbyRequestResponse(
    Guid Id, string Title, string? Description, Guid CategoryId, int Status,
    Guid CreatedByUserId, Guid? AssignedToUserId, double Latitude, double Longitude,
    double DistanceMeters);


namespace Vira.Contracts.Requests;
public sealed record UpdateRequestDto(
    string Title,
    string? Description,
    Guid CategoryId
    );

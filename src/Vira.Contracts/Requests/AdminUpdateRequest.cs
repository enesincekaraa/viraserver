namespace Vira.Contracts.Requests;
public sealed record AdminUpdateRequest(int? Status, Guid? AssignedToUserId);

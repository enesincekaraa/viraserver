using MediatR;
using Vira.Shared;

namespace Vira.Application.Features.Requests.AdminList;
public sealed record AdminListQuery(
    int Page = 1,
    int PageSize = 10,
    int? Status = null,
    Guid? CategoryId = null,
    Guid? CreatedByUserId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    string? Search = null
) : IRequest<PagedResult<RequestListItemDto>>;

public sealed record RequestListItemDto(
    Guid Id, string Title, string? Description, Guid? CategoryId,
    int Status, Guid CreatedByUserId, Guid? AssignedToUserId,
    double Latitude, double Longitude, DateTime CreatedAt
);


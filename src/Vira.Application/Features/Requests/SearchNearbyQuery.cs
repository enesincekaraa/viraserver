using MediatR;
using NetTopologySuite.Geometries;
using Vira.Application.Abstractions.Repositories;
using Vira.Domain.Entities;
using Vira.Shared;

namespace Vira.Application.Features.Requests;
public sealed record SearchNearbyQuery(double Latitude, double Longitude, double RadiusKm,
    Guid? CategoryId = null, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<NearbyRequestResponse>>>;


public sealed class SearchNearbyHandler(IReadRepository<Request> _req, GeometryFactory _gf)
    : IRequestHandler<SearchNearbyQuery, Result<PagedResult<NearbyRequestResponse>>>
{
    public async Task<Result<PagedResult<NearbyRequestResponse>>> Handle(SearchNearbyQuery q, CancellationToken ct)
    {
        var center = _gf.CreatePoint(new Coordinate(q.Longitude, q.Latitude));
        if (q.Latitude is < -90 or > 90) return Result<PagedResult<NearbyRequestResponse>>.Failure("Bad.Lat", "Lat aralığı -90..90");
        if (q.Longitude is < -180 or > 180) return Result<PagedResult<NearbyRequestResponse>>.Failure("Bad.Lng", "Lng aralığı -180..180");
        if (q.RadiusKm is <= 0 or > 20) q = q with { RadiusKm = Math.Clamp(q.RadiusKm, 0.1, 20) };

        var meters = q.RadiusKm * 1000.0;

        var (entities, total) = await _req.ListPagedAsync(
            q.Page, q.PageSize,
            predicate: r => r.Location != null
                         && r.Location.Distance(center) <= meters
                         && (q.CategoryId == null || r.CategoryId == q.CategoryId),
            orderBy: s => s.OrderBy(r => r.Location!.Distance(center)),
            ct: ct);

        var items = entities.Select(e => new NearbyRequestResponse(
            e.Id, e.Title, e.Description, e.CategoryId ?? Guid.Empty, (int)e.Status,
            e.CreatedByUserId, e.AssignedToUserId, e.Latitude, e.Longitude,
            e.Location!.Distance(center)  // geography -> metre
        )).ToList();

        return Result<PagedResult<NearbyRequestResponse>>.Success(new(items, q.Page, q.PageSize, total));
    }
}

using FluentValidation;
using MediatR;
using NetTopologySuite.Geometries;
using Vira.Application.Abstractions.Repositories;
using Vira.Contracts.Requests;
using Vira.Domain.Entities;
using Vira.Shared;

namespace Vira.Application.Features.Requests;

public sealed record CreateRequestCommand(
    Guid UserId, string Title, string? Description,
    Guid CategoryId, double Latitude, double Longitude)
    : IRequest<Result<RequestResponse>>;

public sealed class CreateRequestValidator : AbstractValidator<CreateRequestCommand>
{
    public CreateRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
}

public sealed class CreateRequestHandler(IRepository<Request> _repo, IUnitOfWork _uow, GeometryFactory _gf) : IRequestHandler<CreateRequestCommand, Result<RequestResponse>>
{
    public async Task<Result<RequestResponse>> Handle(CreateRequestCommand c, CancellationToken ct)
    {
        var e = new Request(c.Title, c.UserId, c.Latitude, c.Longitude, c.Description, c.CategoryId);
        var pt = _gf.CreatePoint(new Coordinate(c.Longitude, c.Latitude));
        e.SetLocation(pt);

        await _repo.AddAsync(e, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<RequestResponse>.Success(new(
            e.Id, e.Title, e.Description, e.CategoryId, (int)e.Status,
            e.CreatedByUserId, e.AssignedToUserId, e.Latitude, e.Longitude));
    }
}

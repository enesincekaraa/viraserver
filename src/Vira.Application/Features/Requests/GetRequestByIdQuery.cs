using FluentValidation;
using MediatR;
using Vira.Application.Abstractions.Repositories;
using Vira.Contracts.Requests;
using Vira.Domain.Entities;
using Vira.Shared;

namespace Vira.Application.Features.Requests;
public sealed record GetRequestByIdQuery(Guid Id) : IRequest<Result<RequestResponse>>;

public sealed class GetRequestByIdValidator : AbstractValidator<GetRequestByIdQuery>
{ public GetRequestByIdValidator() => RuleFor(x => x.Id).NotEmpty(); }

public sealed class GetRequestByIdHandler(IReadRepository<Request> _repo) : IRequestHandler<GetRequestByIdQuery, Result<RequestResponse>>
{
    public async Task<Result<RequestResponse>> Handle(GetRequestByIdQuery request, CancellationToken cancellationToken)
    {
        var e = await _repo.GetByIdAsync(request.Id, cancellationToken);
        if (e is null) return Result<RequestResponse>.Failure("Request not found", "Talep bulunamadı");
        return Result<RequestResponse>.Success(new(
            e.Id, e.Title, e.Description, e.CategoryId, (int)e.Status,
            e.CreatedByUserId, e.AssignedToUserId, e.Latitude, e.Longitude));
    }
}
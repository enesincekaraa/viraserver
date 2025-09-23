using MediatR;
using Vira.Application.Abstractions.Repositories;
using Vira.Contracts.Auth;
using Vira.Domain.Entities;
using Vira.Shared;

namespace Vira.Application.Features.Auth;

public sealed record MeQuery(Guid UserId) : IRequest<Result<MeResponse>>;

public sealed class MeHandler : IRequestHandler<MeQuery, Result<MeResponse>>
{
    private readonly IReadRepository<User> _users;
    public MeHandler(IReadRepository<User> users) => _users = users;

    public async Task<Result<MeResponse>> Handle(MeQuery req, CancellationToken ct)
    {
        var u = await _users.GetByIdAsync(req.UserId, ct);
        if (u is null) return Result<MeResponse>.Failure("Auth.UserMissing", "Kullanıcı bulunamadı.");
        return Result<MeResponse>.Success(new(u.Id, u.Email, u.FullName, u.Role));
    }
}

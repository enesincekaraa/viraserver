using FluentValidation;
using MediatR;
using Vira.Application.Abstractions.Auth;
using Vira.Application.Abstractions.Repositories;
using Vira.Contracts.Auth;
using Vira.Domain.Entities;
using Vira.Shared;

namespace Vira.Application.Features.Auth;

public sealed record RefreshCommand(string RefreshToken) : IRequest<Result<AuthResponse>>;

public sealed class RefreshValidator : AbstractValidator<RefreshCommand>
{
    public RefreshValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}

public sealed class RefreshHandler : IRequestHandler<RefreshCommand, Result<AuthResponse>>
{
    private readonly IRepository<RefreshToken> _tokens;
    private readonly IReadRepository<User> _users;
    private readonly IUnitOfWork _uow;
    private readonly IJwtTokenService _jwt;

    public RefreshHandler(IRepository<RefreshToken> tokens, IReadRepository<User> users, IUnitOfWork uow, IJwtTokenService jwt)
    { _tokens = tokens; _users = users; _uow = uow; _jwt = jwt; }

    public async Task<Result<AuthResponse>> Handle(RefreshCommand req, CancellationToken ct)
    {
        var token = (await _tokens.ListAsync(t => t.Token == req.RefreshToken, ct)).FirstOrDefault();
        if (token is null || token.Revoked || token.ExpiresAtUtc <= DateTime.UtcNow)
            return Result<AuthResponse>.Failure("Auth.RefreshInvalid", "Refresh token geçersiz.");

        var user = await _users.GetByIdAsync(token.UserId, ct);
        if (user is null) return Result<AuthResponse>.Failure("Auth.UserMissing", "Kullanıcı bulunamadı.");

        // eski token'ı tek kullanımlık düşün: revoke edelim
        token.Revoke();

        var (access, exp) = _jwt.CreateAccessToken(user);
        var newRefresh = _jwt.GenerateRefreshToken();
        var rt = new RefreshToken(user.Id, newRefresh, DateTime.UtcNow.AddDays(_jwt.GetRefreshDays()));
        await _tokens.AddAsync(rt, ct);

        await _uow.SaveChangesAsync(ct);

        return Result<AuthResponse>.Success(new(access, newRefresh, "Bearer", (int)(exp - DateTime.UtcNow).TotalSeconds));
    }
}

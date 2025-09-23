using FluentValidation;
using MediatR;
using Vira.Application.Abstractions.Auth;
using Vira.Application.Abstractions.Repositories;
using Vira.Contracts.Auth;
using Vira.Domain.Entities;
using Vira.Infrastructure.Auth;
using Vira.Shared;

namespace Vira.Application.Features.Auth;
public sealed record LoginCommand(string Email, string Password) : IRequest<Result<AuthResponse>>;

public sealed class LoginValidator : FluentValidation.AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Password)
        .MinimumLength(8)
        .Matches("[A-Z]").WithMessage("En az 1 büyük harf")
        .Matches("[a-z]").WithMessage("En az 1 küçük harf")
        .Matches("[0-9]").WithMessage("En az 1 rakam")
        .Matches("[^a-zA-Z0-9]").WithMessage("En az 1 özel karakter");
    }
}

public sealed class LoginHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IReadRepository<User> _users;
    private readonly IRepository<RefreshToken> _tokens;
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;

    public LoginHandler(IReadRepository<User> users, IRepository<RefreshToken> tokens, IUnitOfWork uow, IPasswordHasher hasher, IJwtTokenService jwt)
    { _users = users; _tokens = tokens; _uow = uow; _hasher = hasher; _jwt = jwt; }
    public async Task<Result<AuthResponse>> Handle(LoginCommand req, CancellationToken ct)
    {
        var user = (await _users.ListAsync(u => u.Email == req.Email, ct)).FirstOrDefault();
        if (user is null) return Result<AuthResponse>.Failure("Auth.InvalidCredentials", "E-posta veya şifre hatalı.");

        if (!_hasher.Verify(req.Password, user.PasswordHash))
            return Result<AuthResponse>.Failure("Auth.InvalidCredentials", "E-posta veya şifre hatalı.");

        var (access, exp) = _jwt.CreateAccessToken(user);
        var refresh = _jwt.GenerateRefreshToken();
        var rt = new RefreshToken(user.Id, refresh, DateTime.UtcNow.AddDays(_jwt.GetRefreshDays()));
        await _tokens.AddAsync(rt, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<AuthResponse>.Success(new(access, refresh, "Bearer", (int)(exp - DateTime.UtcNow).TotalSeconds));
    }
}

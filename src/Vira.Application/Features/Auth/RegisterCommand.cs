using FluentValidation;
using MediatR;
using Vira.Application.Abstractions.Auth;
using Vira.Application.Abstractions.Repositories;
using Vira.Contracts.Auth;
using Vira.Domain.Entities;
using Vira.Infrastructure.Auth;
using Vira.Shared;
namespace Vira.Application.Features.Auth;

public sealed record RegisterCommand(string Email, string Password, string FullName) : IRequest<Result<AuthResponse>>;

public sealed class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Password)
                .MinimumLength(8)
                .Matches("[A-Z]").WithMessage("En az 1 büyük harf")
                .Matches("[a-z]").WithMessage("En az 1 küçük harf")
                .Matches("[0-9]").WithMessage("En az 1 rakam")
                .Matches("[^a-zA-Z0-9]").WithMessage("En az 1 özel karakter");

        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
    }
}

public sealed class RegisterHandler : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    private readonly IRepository<User> _users;
    private readonly IRepository<RefreshToken> _tokens;
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;

    public RegisterHandler(IRepository<User> users, IRepository<RefreshToken> tokens, IUnitOfWork uow, IPasswordHasher hasher, IJwtTokenService jwt)
    { _users = users; _tokens = tokens; _uow = uow; _hasher = hasher; _jwt = jwt; }

    public async Task<Result<AuthResponse>> Handle(RegisterCommand req, CancellationToken ct)
    {
        // basit email uniq kontrol
        var existing = (await _users.ListAsync(u => u.Email == req.Email, ct)).FirstOrDefault();
        if (existing is not null) return Result<AuthResponse>.Failure("Auth.EmailExists", "E-posta zaten kayıtlı.");

        var user = new User(req.Email, _hasher.Hash(req.Password), req.FullName, role: "User");
        await _users.AddAsync(user, ct);

        var (access, exp) = _jwt.CreateAccessToken(user);
        var refresh = _jwt.GenerateRefreshToken();
        var rt = new RefreshToken(user.Id, refresh, DateTime.UtcNow.AddDays(_jwt.GetRefreshDays()));
        await _tokens.AddAsync(rt, ct);

        await _uow.SaveChangesAsync(ct);

        return Result<AuthResponse>.Success(new(access, refresh, "Bearer", (int)(exp - DateTime.UtcNow).TotalSeconds));
    }
}

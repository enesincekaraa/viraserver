using FluentValidation;
using MediatR;
using Vira.Application.Abstractions.Auth;
using Vira.Application.Abstractions.Repositories;
using Vira.Contracts.Assists;
using Vira.Domain.Entities;

namespace Vira.Application.Features.Assists;
public sealed class CreateAssist
{
    public sealed record CreateAssistTicketCommand(
    int Type, string ElderFullName, string? ElderPhone,
    string Address, double Latitude, double Longitude,
    DateTime? ScheduledAtUtc, string? Notes)
    : IRequest<AssistsDtos.AssistResponse>;

    public sealed class CreateAssistValidator : AbstractValidator<CreateAssistTicketCommand>
    {
        public CreateAssistValidator()
        {
            RuleFor(x => x.ElderFullName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Address).NotEmpty().MaximumLength(400);
            RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
            RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
            // Hata aldığın kısım => int için Enum.IsDefined kullandık
            RuleFor(x => x.Type)
                .Must(v => Enum.IsDefined(typeof(AssistType), v))
                .WithMessage("Type geçersiz.");
        }
    }

    public sealed class CreateAssistTicketHandler
        : IRequestHandler<CreateAssistTicketCommand, AssistsDtos.AssistResponse>
    {
        private readonly IRepository<AssistTicket> _repo;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUser _me;

        public CreateAssistTicketHandler(IRepository<AssistTicket> repo, IUnitOfWork uow, ICurrentUser me)
        { _repo = repo; _uow = uow; _me = me; }

        public async Task<AssistsDtos.AssistResponse> Handle(CreateAssistTicketCommand c, CancellationToken ct)
        {
            var userId = _me.UserId ?? throw new InvalidOperationException("User not authenticated");

            var entity = new AssistTicket(
                userId, (AssistType)c.Type, c.ElderFullName, c.ElderPhone,
                c.Address, c.Latitude, c.Longitude, c.ScheduledAtUtc, c.Notes);

            await _repo.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            return new AssistsDtos.AssistResponse(
                entity.Id, (int)entity.Type, (int)entity.Status, entity.CreatedByUserId,
                entity.ElderFullName, entity.ElderPhone, entity.Address,
                entity.Latitude, entity.Longitude, entity.AssignedToUserId,
                entity.ScheduledAtUtc, entity.Notes, entity.CreatedAt);
        }
    }
}

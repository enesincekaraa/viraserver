using Vira.Domain.Entities;

namespace Vira.Application.Abstractions.Persistence;

public interface IReadDb
{
    // Sadece ihtiyaç duyduğunu ekle. Şimdilik Requests yeterli.
    IQueryable<Request> Requests { get; }
    IQueryable<RequestAttachment> RequestAttachments { get; }
    IQueryable<RequestComment> RequestComments { get; }

    IQueryable<AssistTicket> AssistTickets { get; }
}

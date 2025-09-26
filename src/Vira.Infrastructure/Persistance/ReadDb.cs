using Microsoft.EntityFrameworkCore;
using Vira.Application.Abstractions.Persistence;
using Vira.Domain.Entities;

namespace Vira.Infrastructure.Persistence.Read;

public sealed class ReadDb : IReadDb
{
    private readonly AppDbContext _db;
    public ReadDb(AppDbContext db) => _db = db;

    public IQueryable<Request> Requests => _db.Requests.AsNoTracking();
    public IQueryable<RequestAttachment> RequestAttachments => _db.RequestAttachments.AsNoTracking();
    public IQueryable<RequestComment> RequestComments => _db.Set<RequestComment>().AsNoTracking();
    public IQueryable<AssistTicket> AssistTickets => _db.AssistTickets.AsNoTracking();
}

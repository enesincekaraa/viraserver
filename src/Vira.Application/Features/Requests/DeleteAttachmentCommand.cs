using MediatR;
using Vira.Application.Abstractions.Files;
using Vira.Application.Abstractions.Repositories;
using Vira.Domain.Entities;
using Vira.Shared;

namespace Vira.Application.Features.Requests;

public sealed record DeleteAttachmentCommand(Guid RequestId, Guid AttachmentId) : IRequest<Result>;

public sealed class DeleteAttachmentHandler : IRequestHandler<DeleteAttachmentCommand, Result>
{
    private readonly IRepository<RequestAttachment> _attRepo;
    private readonly IReadRepository<Request> _reqRead;
    private readonly IUnitOfWork _uow;
    private readonly IFileStorage _storage;

    public DeleteAttachmentHandler(IRepository<RequestAttachment> attRepo,
                                   IReadRepository<Request> reqRead,
                                   IUnitOfWork uow,
                                   IFileStorage storage)
    { _attRepo = attRepo; _reqRead = reqRead; _uow = uow; _storage = storage; }

    public async Task<Result> Handle(DeleteAttachmentCommand c, CancellationToken ct)
    {
        var req = await _reqRead.GetByIdAsync(c.RequestId, ct);
        if (req is null) return Result.Failure("Request.NotFound", "Talep bulunamadı.");

        var att = await _attRepo.GetByIdAsync(c.AttachmentId, ct);
        if (att is null || att.RequestId != c.RequestId)
            return Result.Failure("Attachment.NotFound", "Ek bulunamadı.");

        // Fiziksel dosyayı sil
        var sub = $"requests/{c.RequestId}";
        await _storage.DeleteAsync(sub, att.FileName);

        // DB’den sil
        await _attRepo.DeleteAsync(att, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Success();
    }
}

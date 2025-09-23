using FluentValidation;
using MediatR;
using Vira.Application.Abstractions.Files;
using Vira.Application.Abstractions.Repositories;
using Vira.Contracts.Requests;
using Vira.Domain.Entities;
using Vira.Shared;

namespace Vira.Application.Features.Requests;

public sealed record AddAttachmentCommand(Guid RequestId, string OriginalName, string ContentType, Stream Content)
    : IRequest<Result<AttachmentResponse>>;

public sealed class AddAttachmentValidator : AbstractValidator<AddAttachmentCommand>
{
    public AddAttachmentValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.OriginalName).NotEmpty().MaximumLength(260);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(100);
    }
}

public sealed class AddAttachmentHandler : IRequestHandler<AddAttachmentCommand, Result<AttachmentResponse>>
{
    private readonly IRepository<Request> _reqRepo;
    private readonly IRepository<RequestAttachment> _attRepo;
    private readonly IFileStorage _storage;
    private readonly IUnitOfWork _uow;

    public AddAttachmentHandler(IRepository<Request> reqRepo, IRepository<RequestAttachment> attRepo,
                                IFileStorage storage, IUnitOfWork uow)
    { _reqRepo = reqRepo; _attRepo = attRepo; _storage = storage; _uow = uow; }

    public async Task<Result<AttachmentResponse>> Handle(AddAttachmentCommand c, CancellationToken ct)
    {
        var req = await _reqRepo.GetByIdAsync(c.RequestId, ct);
        if (req is null) return Result<AttachmentResponse>.Failure("Request.NotFound", "Talep bulunamadı.");

        var ext = Path.GetExtension(c.OriginalName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".heic" };
        if (!allowed.Contains(ext)) return Result<AttachmentResponse>.Failure("Attachment.InvalidType", "Sadece JPG/PNG/HEIC");

        var saved = await _storage.SaveAsync(c.Content, c.OriginalName, c.ContentType, $"requests/{req.Id}", ct);

        var att = new RequestAttachment(req.Id, saved.StoredFileName, c.OriginalName, saved.ContentType, saved.SizeBytes, saved.Url);
        await _attRepo.AddAsync(att, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<AttachmentResponse>.Success(
            new AttachmentResponse(att.Id, att.OriginalName, att.Url, att.ContentType, att.SizeBytes, att.CreatedAt));
    }
}

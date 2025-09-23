using FluentValidation;
using MediatR;
using Vira.Application.Abstractions.Repositories;
using Vira.Contracts.Requests.Comments;
using Vira.Domain.Entities;
using Vira.Shared;

namespace Vira.Application.Features.Comments;
public sealed record CreateCommentCommand(Guid RequestId, Guid AuthorUserId, string Text) : IRequest<Result<CommentResponse>>;

public sealed class CreateCommentCommandValidator : AbstractValidator<CreateCommentCommand>
{
    public CreateCommentCommandValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.AuthorUserId).NotEmpty();
        RuleFor(x => x.Text).NotEmpty().MaximumLength(1000);
    }
}

public sealed class CreateCommentCommandHandler(IRepository<Request> _requests, IRepository<RequestComment> _comments, IUnitOfWork _uow) :
    IRequestHandler<CreateCommentCommand, Result<CommentResponse>>
{
    public async Task<Result<CommentResponse>> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
    {
        var req = await _requests.GetByIdAsync(request.RequestId, cancellationToken);
        if (req is null)
            return Result<CommentResponse>.Failure("NotFound", "Request not found");

        var entity = new RequestComment(request.RequestId, request.AuthorUserId, request.Text);
        await _comments.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var response = new CommentResponse(
            entity.Id,
            entity.RequestId,
            entity.AuthorUserId,
            (int)entity.Type,
            entity.Text,
            entity.CreatedAt
        );
        return Result<CommentResponse>.Success(response);
    }
}

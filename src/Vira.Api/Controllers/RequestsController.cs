using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using Vira.Application.Features.Comments;
using Vira.Application.Features.Requests;
using Vira.Contracts.Requests;
using Vira.Contracts.Requests.Comments;
using Vira.Shared;

namespace Vira.Api.Controllers;

[ApiController]
[Route("api/requests")]
[Produces("application/json")]
[Authorize]
public sealed class RequestsController : ControllerBase
{
    private readonly ISender _sender;
    public RequestsController(ISender sender) => _sender = sender;

    [EnableRateLimiting("Writes")]
    [HttpPost]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<RequestResponse>> Create([FromBody] CreateRequestRequest body, CancellationToken ct)
    {
        var uid = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var r = await _sender.Send(new CreateRequestCommand(uid, body.Title, body.Description, body.CategoryId, body.Latitude, body.Longitude), ct);
        if (!r.IsSuccess) return BadRequest(r.Error);
        return CreatedAtAction(nameof(GetById), new { id = r.Value!.Id }, r.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RequestResponse>> GetById(Guid id, CancellationToken ct)
    {
        var r = await _sender.Send(new GetRequestByIdQuery(id), ct);
        return r.IsSuccess ? Ok(r.Value) : NotFound(r.Error);
    }

    [HttpGet("mine")]
    public async Task<ActionResult> Mine([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] int? status = null, CancellationToken ct = default)
    {
        var uid = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var r = await _sender.Send(new ListRequestsQuery(page, pageSize, status, null, uid, null), ct);
        return r.IsSuccess ? Ok(r.Value) : BadRequest(r.Error);
    }

    [Authorize(Roles = "Admin,Operator")]
    [HttpGet]
    public async Task<ActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] int? status = null, [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? createdBy = null, [FromQuery] Guid? assignedTo = null,
        CancellationToken ct = default)
    {
        var r = await _sender.Send(new ListRequestsQuery(page, pageSize, status, categoryId, createdBy, assignedTo), ct);
        return r.IsSuccess ? Ok(r.Value) : BadRequest(r.Error);
    }

    [Authorize(Roles = "Admin,Operator")]
    [EnableRateLimiting("Writes")]
    [HttpPost("{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, [FromQuery] Guid? toUserId, CancellationToken ct)
    {
        var uid = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var r = await _sender.Send(new AssignRequestCommand(id, toUserId, uid), ct);
        return r.IsSuccess ? NoContent() : NotFound(r.Error);
    }

    [Authorize(Roles = "Admin,Operator")]
    [EnableRateLimiting("Writes")]
    [HttpPost("{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id, CancellationToken ct)
    {
        var uid = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var r = await _sender.Send(new ResolveRequestCommand(id, uid), ct);
        return r.IsSuccess ? NoContent() : NotFound(r.Error);
    }

    [Authorize(Roles = "Admin,Operator")]
    [EnableRateLimiting("Writes")]
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, CancellationToken ct)
    {
        var uid = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var r = await _sender.Send(new RejectRequestCommand(id, uid), ct);
        return r.IsSuccess ? NoContent() : NotFound(r.Error);
    }

    // ---- Attachments ----
    [HttpPost("{id:guid}/attachments")]
    [EnableRateLimiting("Uploads")]
    [RequestSizeLimit(25_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<AttachmentResponse>> Upload(Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("Dosya boş.");
        using var s = file.OpenReadStream();
        var r = await _sender.Send(new AddAttachmentCommand(id, file.FileName, file.ContentType ?? "application/octet-stream", s), ct);
        return r.IsSuccess ? Ok(r.Value) : BadRequest(r.Error);
    }

    // --- Attachments ---

    // Listele (kullanıcı kendi talebini, admin/operator tüm talepleri görebilir dersen burada yetki kontrolü yapabilirsin;
    // basit tutuyoruz: [Authorize] olan herkes, var olan talebin eklerini listeleyebilir.)
    [HttpGet("{id:guid}/attachments")]
    [ProducesResponseType(typeof(List<AttachmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AttachmentResponse>>> ListAttachments(Guid id, CancellationToken ct)
    {
        var r = await _sender.Send(new ListAttachmentQuery(id), ct);
        return r.IsSuccess ? Ok(r.Value) : NotFound(r.Error);
    }

    // Sil (sadece Admin/Operator; istersen talebi oluşturan kullanıcıya da izin verilebilir)
    [Authorize(Roles = "Admin,Operator")]
    [HttpDelete("{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DeleteAttachment(Guid id, Guid attachmentId, CancellationToken ct)
    {
        var r = await _sender.Send(new DeleteAttachmentCommand(id, attachmentId), ct);
        return r.IsSuccess ? NoContent() : NotFound(r.Error);
    }

    // --- Comments ---
    // Ekle
    [EnableRateLimiting("Comments")]
    [Authorize]
    [HttpPost("{id:guid}/comments")]
    [ProducesResponseType(typeof(CommentResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CommentResponse>> AddComment(Guid id, [FromBody] CreateCommentRequest body, CancellationToken ct)
    {
        var uid = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var r = await _sender.Send(new CreateCommentCommand(id, uid, body.Text), ct);
        if (!r.IsSuccess) return BadRequest(r.Error);
        return CreatedAtAction(nameof(GetComments), new { id }, r.Value);
    }

    // Listele(sayfalı)
    [HttpGet("{id:guid}/comments")]
    [ProducesResponseType(typeof(PagedResult<CommentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CommentResponse>>> GetComments(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var r = await _sender.Send(new ListCommentQuery(id, page, pageSize), ct);
        return r.IsSuccess ? Ok(r.Value) : NotFound(r.Error);
    }

    [HttpDelete("{id:guid}/comments/{commentId:guid}")]
    public async Task<IActionResult> DeleteComment(Guid id, Guid commentId, CancellationToken ct)
    {
        var uid = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var IsAdminOrOperator = User.IsInRole("Admin") || User.IsInRole("Operator");
        var r = await _sender.Send(new DeleteCommentCommand(id, commentId, uid, IsAdminOrOperator), ct);
        return r.IsSuccess ? NoContent() : NotFound(r.Error);
    }
    [EnableRateLimiting("Search")]
    [HttpGet("nearby")]
    public async Task<ActionResult<PagedResult<RequestResponse>>> Nearby(
    [FromQuery] double lat,
    [FromQuery] double lng,
    [FromQuery] double radiusKm = 1,
    [FromQuery] Guid? categoryId = null,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken ct = default)
    {
        var r = await _sender.Send(new SearchNearbyQuery(lat, lng, radiusKm, categoryId, page, pageSize), ct);
        return r.IsSuccess ? Ok(r.Value) : BadRequest(r.Error);
    }


    // UPDATE
    [EnableRateLimiting("Writes")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRequestDto dto, CancellationToken ct)
    {
        var cmd = new UpdateRequestCommand(id, dto.Title, dto.Description, dto.CategoryId);
        var r = await _sender.Send(cmd, ct);
        return r.IsSuccess ? NoContent() : BadRequest(r.Error);
    }

    // DELETE
    [EnableRateLimiting("Writes")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var r = await _sender.Send(new DeleteRequestCommand(id), ct);
        return r.IsSuccess ? NoContent() : BadRequest(r.Error);
    }







}

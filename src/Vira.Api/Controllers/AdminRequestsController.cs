using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text;
using Vira.Application.Features.Requests;
using Vira.Application.Features.Requests.AdminList;
using Vira.Application.Features.Requests.Stats;
using Vira.Contracts.Requests;
using Vira.Shared;

[ApiController]
[Route("api/admin/requests")]
[Authorize(Roles = "Admin")]
public class AdminRequestsController : ControllerBase
{
    private readonly ISender _sender;
    public AdminRequestsController(ISender sender) => _sender = sender;

    // 1) İstatistik (hazır)
    [HttpGet("stats/overview")]
    public async Task<IActionResult> Stats(CancellationToken ct)
        => Ok(await _sender.Send(new RequestsStatsQuery(), ct));

    // 2) Detay
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminRequestDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => Ok(await _sender.Send(new AdminGetByIdQuery(id), ct));

    // 3) Liste (arama rate-limit)
    [HttpGet("adminlist")]
    [EnableRateLimiting("Search")] // Program.cs’de policy varsa bırak, yoksa kaldır veya "api" yap
    [ProducesResponseType(typeof(PagedResult<RequestListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AdminList([FromQuery] AdminListQuery query, CancellationToken ct)
        => Ok(await _sender.Send(query, ct));

    // 4) Patch (durum/atama)
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] AdminUpdateRequest body, CancellationToken ct)
    {
        var ok = await _sender.Send(new AdminUpdateCommand(id, body.Status, body.AssignedToUserId), ct);
        return ok ? NoContent() : NotFound();
    }

    // 5) Soft delete
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken ct)
    {
        var ok = await _sender.Send(new AdminSoftDeleteCommand(id), ct);
        return ok ? NoContent() : NotFound();
    }

    // 6) Restore
    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct)
    {
        var ok = await _sender.Send(new AdminRestoreCommand(id), ct);
        return ok ? NoContent() : NotFound();
    }

    // 7) CSV export
    [EnableRateLimiting("Search")]
    [HttpGet("export")]
    [Authorize(Roles = "Admin")]                // istersen Admin,Operator
    public async Task<IActionResult> Export(
    [FromQuery] int? status, [FromQuery] Guid? categoryId,
    [FromQuery] Guid? createdByUserId, [FromQuery] DateTime? fromUtc,
    [FromQuery] DateTime? toUtc, [FromQuery] string? search,
    CancellationToken ct)
    {
        var (name, csv, contentType) = await _sender.Send(
            new AdminExportCsvQuery(status, categoryId, createdByUserId, fromUtc, toUtc, search), ct);

        return File(Encoding.UTF8.GetBytes(csv), contentType, name);
    }

}

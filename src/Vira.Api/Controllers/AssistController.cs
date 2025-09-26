using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vira.Application.Features.Assists;
using Vira.Application.Features.Requests;
using Vira.Contracts.Assists;
using Vira.Shared;
using static Vira.Application.Features.Assists.AdminAssistsStats;
using static Vira.Application.Features.Assists.AdminList;
using static Vira.Application.Features.Assists.CreateAssist;
using static Vira.Application.Features.Assists.MyAssists;

namespace Vira.Api.Controllers;

[ApiController]
[Route("api/assist")]
public sealed class AssistController : ControllerBase
{
    private readonly ISender _sender;
    public AssistController(ISender sender) => _sender = sender;

    // Citizen
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<AssistsDtos.AssistResponse>> Create(
        AssistsDtos.CreateAssistRequest body, CancellationToken ct)
        => Ok(await _sender.Send(new CreateAssistTicketCommand(
            body.Type, body.ElderFullName, body.ElderPhone,
            body.Address, body.Latitude, body.Longitude,
            body.ScheduledAtUtc, body.Notes), ct));

    [Authorize]
    [HttpGet("mine")]
    public async Task<ActionResult<PagedResult<AssistsDtos.AssistListItem>>> Mine(
        [FromQuery] MyAssistsListQuery q, CancellationToken ct)
        => Ok(await _sender.Send(q, ct));


    // Admin
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<PagedResult<AssistsDtos.AssistListItem>>> AdminList(
        [FromQuery] AdminListQuery q, CancellationToken ct)
        => Ok(await _sender.Send(q, ct));

    [Authorize(Roles = "Admin")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AssistsDtos.AssistResponse>> AdminGetById(Guid id, CancellationToken ct)
        => Ok(await _sender.Send(new AdminGetByIdQuery(id), ct));

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, AssistsDtos.AssignAssistRequest body, CancellationToken ct)
        => (await _sender.Send(new AdminAssing.AdminAssignCommand(id, body.AssignedToUserId), ct))
            ? NoContent() : NotFound();

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, AssistsDtos.ChangeStatusRequest body, CancellationToken ct)
        => (await _sender.Send(new AdminChangeStatus.AdminChangeStatusCommand(id, body.Status, body.Reason), ct))
            ? NoContent() : NotFound();

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken ct)
        => (await _sender.Send(new AdminSoftDeleteCommand(id), ct)) ? NoContent() : NotFound();

    [Authorize(Roles = "Admin")]
    [HttpGet("export.csv")]
    public async Task<IActionResult> Export([FromQuery] int? status, [FromQuery] int? type, [FromQuery] string? search, CancellationToken ct)
    {
        var (content, name, contentType) = await _sender.Send(new AdminExportCsv.AdminExportCsvQuery(status, type, search), ct);
        return File(content, contentType, name);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("stats/overview")]
    public async Task<ActionResult<AssistStatsDto>> Stats(CancellationToken ct)
        => Ok(await _sender.Send(new AdminAssistStatsQuery(), ct));
}

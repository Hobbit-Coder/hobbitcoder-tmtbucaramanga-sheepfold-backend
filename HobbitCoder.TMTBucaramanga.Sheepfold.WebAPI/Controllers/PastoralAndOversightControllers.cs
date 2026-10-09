using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Notifications;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Reports;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Retired;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.Controllers;

[Route("api/follow-ups")]
public class FollowUpsController : SheepfoldControllerBase
{
    public FollowUpsController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public Task<PagedResult<FollowUpListItem>> GetAsync([FromQuery] GetFollowUpsQuery query) => Send<GetFollowUpsQuery, PagedResult<FollowUpListItem>>(query);

    [HttpGet("subjects")]
    public Task<List<FollowUpSubject>> GetSubjectsAsync([FromQuery] GetFollowUpSubjectsQuery query) => Send<GetFollowUpSubjectsQuery, List<FollowUpSubject>>(query);

    [HttpGet("{id:guid}")]
    public Task<FollowUpDetailResponse> GetByIdAsync(Guid id) => Send<GetFollowUpQuery, FollowUpDetailResponse>(new GetFollowUpQuery { Id = id });

    [HttpPost]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateAsync([FromBody] CreateFollowUpCommand command)
    {
        var id = await Send<CreateFollowUpCommand, Guid>(command);
        return Created($"/api/follow-ups/{id}", id);
    }

    [HttpPut("{id:guid}/schedule")]
    public Task<IActionResult> ScheduleAsync(Guid id, [FromBody] ScheduleFollowUpCommand command)
    {
        command.Id = id;
        return NoContentAfter(command);
    }

    [HttpPost("{id:guid}/complete")]
    public Task<IActionResult> CompleteAsync(Guid id, [FromBody] FollowUpContent content) =>
        NoContentAfter(new CompleteFollowUpCommand { Id = id, Content = content });

    [HttpPut("{id:guid}/content")]
    public Task<IActionResult> UpdateContentAsync(Guid id, [FromBody] FollowUpContent content) =>
        NoContentAfter(new UpdateFollowUpContentCommand { Id = id, Content = content });

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> RetireAsync(Guid id) => NoContentAfter(new RetireFollowUpCommand { Id = id });
}

[Route("api/notifications")]
public class NotificationsController : SheepfoldControllerBase
{
    public NotificationsController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public Task<PagedResult<NotificationResponse>> GetAsync([FromQuery] GetMyNotificationsQuery query) =>
        Send<GetMyNotificationsQuery, PagedResult<NotificationResponse>>(query);
}

[Route("api/reports")]
public class ReportsController : SheepfoldControllerBase
{
    public ReportsController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet("{family}")]
    public Task<ReportResponse> GetAsync(ReportFamily family, [FromQuery] GetReportQuery query)
    {
        query.Family = family;
        return Send<GetReportQuery, ReportResponse>(query);
    }
}

[Route("api/audit")]
public class AuditController : SheepfoldControllerBase
{
    public AuditController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public Task<PagedResult<AuditEntryResponse>> GetAsync([FromQuery] GetAuditEntriesQuery query) => Send<GetAuditEntriesQuery, PagedResult<AuditEntryResponse>>(query);

    [HttpGet("{id:guid}")]
    public Task<AuditEntryResponse> GetByIdAsync(Guid id) => Send<GetAuditEntryQuery, AuditEntryResponse>(new GetAuditEntryQuery { Id = id });
}

[Route("api/retired")]
public class RetiredController : SheepfoldControllerBase
{
    public RetiredController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public Task<List<RetiredRecordResponse>> GetAsync([FromQuery] GetRetiredRecordsQuery query) => Send<GetRetiredRecordsQuery, List<RetiredRecordResponse>>(query);

    [HttpGet("{type}/{id:guid}/impact")]
    public Task<RetiredImpactResponse> GetImpactAsync(RetiredType type, Guid id) =>
        Send<GetRetiredImpactQuery, RetiredImpactResponse>(new GetRetiredImpactQuery { Type = type, Id = id });

    [HttpPost("{type}/{id:guid}/restore")]
    public Task<IActionResult> RestoreAsync(RetiredType type, Guid id, [FromBody] RestoreRetiredRecordCommand? command)
    {
        command ??= new RestoreRetiredRecordCommand();
        command.Type = type;
        command.Id = id;
        return NoContentAfter(command);
    }

    [HttpDelete("{type}/{id:guid}")]
    public Task<IActionResult> DeleteAsync(RetiredType type, Guid id) => NoContentAfter(new DeleteRetiredRecordCommand { Type = type, Id = id });
}

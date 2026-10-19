using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendance.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendance.Queries;

/// <summary>Registro de asistencia del evento para el usuario.</summary>
public class GetAttendanceSheetQuery : IRequest<AttendanceSheetResponse>
{
    public Guid EventId { get; set; }
    public Guid? GroupId { get; set; }
    public string? Search { get; set; }
}

public class GetAttendanceSheetQueryHandler : IRequestHandler<GetAttendanceSheetQuery, AttendanceSheetResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly AttendanceSheetService _sheet;

    public GetAttendanceSheetQueryHandler(IApplicationDbContext dbContext, MinistryAccess access, AttendanceSheetService sheet)
    {
        _dbContext = dbContext;
        _access = access;
        _sheet = sheet;
    }

    public async Task<AttendanceSheetResponse> Handle(GetAttendanceSheetQuery request, CancellationToken cancellationToken = default)
    {
        var @event = await AttendanceHelpers.FindOpenEventAsync(_dbContext, _access, request.EventId, cancellationToken);
        var people = await _sheet.MarkablePeopleAsync(@event, cancellationToken);

        var groups = people.Where(person => person.GroupId is not null && person.Context is not null)
            .DistinctBy(person => person.GroupId)
            .Select(person => new AttendanceGroupOption { Id = person.GroupId!.Value, Name = person.Context! })
            .OrderBy(group => group.Name)
            .ToList();

        IEnumerable<AttendancePerson> filtered = people;

        if (request.GroupId is not null)
        {
            filtered = filtered.Where(person => person.GroupId == request.GroupId);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            filtered = filtered.Where(person => person.FullName.Contains(request.Search.Trim(), StringComparison.CurrentCultureIgnoreCase));
        }

        var list = filtered.ToList();

        return new AttendanceSheetResponse
        {
            EventId = @event.Id,
            Status = @event.Status,
            Groups = groups,
            People = list,
            Summary = new AttendanceSummary
            {
                Attended = list.Count(person => person.Mark == AttendanceMark.Attended),
                Late = list.Count(person => person.Mark == AttendanceMark.Late),
                Unmarked = list.Count(person => person.Mark is null)
            }
        };
    }
}

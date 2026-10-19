using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendance.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Calendar.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Notifications.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Contexts;
using HobbitCoder.TMTBucaramanga.Sheepfold.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.IntegrationTests.Events;

/// <summary>Eventos, público, encargados, calendario y asistencia.</summary>
public class EventTests : IClassFixture<SheepfoldApiFixture>
{
    private readonly SheepfoldApiFixture _fixture;
    private readonly MinistryScenario _scenario;

    public EventTests(SheepfoldApiFixture fixture)
    {
        _fixture = fixture;
        _scenario = new MinistryScenario(fixture);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    /// <summary>Dos grupos con su líder y su aprendiz, y un asistente en cada uno.</summary>
    private async Task<(Guid GroupA, Guid GroupB, TestUser LeaderA, TestUser LeaderB, TestUser ApprenticeA, Guid AttendeeA, Guid AttendeeB)> TwoGroupsAsync()
    {
        var groupA = await _scenario.CreateGroupAsync();
        var groupB = await _scenario.CreateGroupAsync();
        var leaderA = _fixture.AddUser(UserRole.Leader);
        var leaderB = _fixture.AddUser(UserRole.Leader);
        var apprenticeA = _fixture.AddUser(UserRole.Apprentice);
        await _scenario.SetTeamAsync(groupA, leaders: [leaderA.Id], apprentices: [apprenticeA.Id]);
        await _scenario.SetTeamAsync(groupB, leaders: [leaderB.Id]);

        return (groupA, groupB, leaderA, leaderB, apprenticeA,
            await _scenario.CreateAttendeeAsync(groupA), await _scenario.CreateAttendeeAsync(groupB));
    }

    [Fact]
    public async Task Leader_CreatesForTheirGroup_ButNotForAnother_AndApprenticeCannotCreate()
    {
        var (groupA, groupB, leaderA, _, apprenticeA, _, _) = await TwoGroupsAsync();

        var created = await _scenario.CreateEventAsync(leaderA.Token, EventAudience.Groups, [groupA], [leaderA.Id]);
        var detail = await _scenario.GetAsync<EventDetailResponse>($"/api/events/{created}", leaderA.Token);
        Assert.Equal(leaderA.Id, detail.Creator.Id);

        using var otherGroup = await _scenario.SendAsync(HttpMethod.Post, "/api/events", leaderA.Token, EventBody(EventAudience.Groups, [groupB], leaderA.Id));
        using var allMinistry = await _scenario.SendAsync(HttpMethod.Post, "/api/events", leaderA.Token, EventBody(EventAudience.AllMinistry, [], leaderA.Id));
        using var apprentice = await _scenario.SendAsync(HttpMethod.Post, "/api/events", apprenticeA.Token, EventBody(EventAudience.Groups, [groupA], apprenticeA.Id));

        Assert.Equal(HttpStatusCode.BadRequest, otherGroup.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, allMinistry.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, apprentice.StatusCode);
    }

    [Fact]
    public async Task GroupEvent_IsHiddenFromOtherGroups_AndDraftsOnlyFromTheirAdministrators()
    {
        var (groupA, _, leaderA, leaderB, apprenticeA, _, _) = await TwoGroupsAsync();
        var published = await _scenario.CreateEventAsync(_scenario.Director.Token, EventAudience.Groups, [groupA]);
        var draft = await _scenario.CreateEventAsync(_scenario.Director.Token, EventAudience.Groups, [groupA], publish: false);

        using var otherLeader = await _scenario.SendAsync(HttpMethod.Get, $"/api/events/{published}", leaderB.Token);
        using var draftForLeader = await _scenario.SendAsync(HttpMethod.Get, $"/api/events/{draft}", leaderA.Token);
        var calendar = await _scenario.GetAsync<CalendarResponse>($"/api/calendar?{MinistryScenario.ReferenceMonth}", leaderB.Token);

        Assert.Equal(HttpStatusCode.NotFound, otherLeader.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, draftForLeader.StatusCode);
        Assert.DoesNotContain(calendar.Events, item => item.Id == published);
        await _scenario.GetAsync<EventDetailResponse>($"/api/events/{published}", apprenticeA.Token);
    }

    [Fact]
    public async Task Manager_AdministersOnlyTheirEvent_AndOthersCannotChangeItsStatus()
    {
        var (groupA, _, leaderA, _, apprenticeA, _, _) = await TwoGroupsAsync();
        var managed = await _scenario.CreateEventAsync(_scenario.Director.Token, EventAudience.Groups, [groupA], [apprenticeA.Id]);
        var other = await _scenario.CreateEventAsync(_scenario.Director.Token, EventAudience.Groups, [groupA], [_scenario.Director.Id]);

        using var cancelled = await _scenario.SendAsync(HttpMethod.Post, $"/api/events/{managed}/status", apprenticeA.Token, new { status = EventStatus.Cancelled });
        using var notManager = await _scenario.SendAsync(HttpMethod.Post, $"/api/events/{other}/status", apprenticeA.Token, new { status = EventStatus.Cancelled });
        using var leaderNotManager = await _scenario.SendAsync(HttpMethod.Post, $"/api/events/{other}/status", leaderA.Token, new { status = EventStatus.Finished });

        Assert.Equal(HttpStatusCode.NoContent, cancelled.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, notManager.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, leaderNotManager.StatusCode);
    }

    [Fact]
    public async Task Cancellation_NotifiesTheEventAudience_ButNotOtherGroups()
    {
        var (groupA, _, leaderA, leaderB, _, _, _) = await TwoGroupsAsync();
        var eventId = await _scenario.CreateEventAsync(_scenario.Director.Token, EventAudience.Groups, [groupA]);

        using var cancel = await _scenario.SendAsync(HttpMethod.Post, $"/api/events/{eventId}/status", _scenario.Director.Token, new { status = EventStatus.Cancelled });
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);

        var forA = await _scenario.GetAsync<Page<NotificationResponse>>("/api/notifications", leaderA.Token);
        var forB = await _scenario.GetAsync<Page<NotificationResponse>>("/api/notifications", leaderB.Token);
        Assert.Contains(forA.Items, item => item.Kind == NotificationKind.EventCancelled && item.EventId == eventId);
        Assert.DoesNotContain(forB.Items, item => item.EventId == eventId);
    }

    [Fact]
    public async Task Attendance_HasTwoExclusiveMarks_CanBeCleared_AndIsAudited()
    {
        var (groupA, _, _, _, apprenticeA, attendeeA, attendeeB) = await TwoGroupsAsync();
        var eventId = await _scenario.CreateEventAsync(_scenario.Director.Token, EventAudience.Groups, [groupA]);

        var sheet = await _scenario.GetAsync<AttendanceSheetResponse>($"/api/events/{eventId}/attendance", apprenticeA.Token);
        Assert.Contains(sheet.People, person => person.PersonId == attendeeA && person.Mark is null);
        Assert.DoesNotContain(sheet.People, person => person.PersonId == attendeeB);

        await MarkAsync(eventId, apprenticeA.Token, attendeeA, AttendanceMark.Attended, HttpStatusCode.OK);
        await MarkAsync(eventId, apprenticeA.Token, attendeeA, AttendanceMark.Late, HttpStatusCode.OK);

        sheet = await _scenario.GetAsync<AttendanceSheetResponse>($"/api/events/{eventId}/attendance", apprenticeA.Token);
        Assert.Equal(AttendanceMark.Late, sheet.People.Single(person => person.PersonId == attendeeA).Mark);
        Assert.Equal(1, sheet.Summary.Late);

        await MarkAsync(eventId, apprenticeA.Token, attendeeA, null, HttpStatusCode.OK);
        sheet = await _scenario.GetAsync<AttendanceSheetResponse>($"/api/events/{eventId}/attendance", apprenticeA.Token);
        Assert.Null(sheet.People.Single(person => person.PersonId == attendeeA).Mark);

        using var scope = _fixture.Services.CreateScope();
        var actions = await scope.ServiceProvider.GetRequiredService<SheepfoldContext>().AuditEntries
            .Where(entry => entry.Module == AuditModule.Attendance && entry.EntityId == eventId)
            .Select(entry => entry.Action).ToListAsync(CancellationToken);
        Assert.Equal(3, actions.Count);
    }

    [Fact]
    public async Task Attendance_OutsideTheUserScope_IsRejected()
    {
        var (groupA, groupB, _, leaderB, _, attendeeA, _) = await TwoGroupsAsync();
        var eventId = await _scenario.CreateEventAsync(_scenario.Director.Token, EventAudience.AllMinistry);

        await MarkAsync(eventId, leaderB.Token, attendeeA, AttendanceMark.Attended, HttpStatusCode.Forbidden);

        Assert.NotEqual(groupA, groupB);
    }

    [Fact]
    public async Task Attendance_StaysOpenAfterFinishing()
    {
        var (groupA, _, leaderA, _, _, attendeeA, _) = await TwoGroupsAsync();
        var eventId = await _scenario.CreateEventAsync(_scenario.Director.Token, EventAudience.Groups, [groupA]);
        using var finish = await _scenario.SendAsync(HttpMethod.Post, $"/api/events/{eventId}/status", _scenario.Director.Token, new { status = EventStatus.Finished });
        Assert.Equal(HttpStatusCode.NoContent, finish.StatusCode);

        await MarkAsync(eventId, leaderA.Token, attendeeA, AttendanceMark.Attended, HttpStatusCode.OK);
    }

    [Fact]
    public async Task EventWithoutAttendance_HasNoAttendanceRecord()
    {
        var (groupA, _, leaderA, _, _, _, _) = await TwoGroupsAsync();
        var eventId = await _scenario.CreateEventAsync(_scenario.Director.Token, EventAudience.Groups, [groupA], requiresAttendance: false);

        using var response = await _scenario.SendAsync(HttpMethod.Get, $"/api/events/{eventId}/attendance", leaderA.Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ServersAudience_IncludesOnlyServerAttendees()
    {
        var groupId = await _scenario.CreateGroupAsync();
        var server = await _scenario.CreateAttendeeAsync(groupId, isServer: true);
        var notServer = await _scenario.CreateAttendeeAsync(groupId);
        var eventId = await _scenario.CreateEventAsync(_scenario.Director.Token, EventAudience.PrimaryGroupAndServers);

        var sheet = await _scenario.GetAsync<AttendanceSheetResponse>($"/api/events/{eventId}/attendance", _scenario.Director.Token);

        Assert.Contains(sheet.People, person => person.PersonId == server);
        Assert.DoesNotContain(sheet.People, person => person.PersonId == notServer);
    }

    /// <summary>La población de un evento pasado usa el grupo de esa fecha; un traslado posterior no la cambia.</summary>
    /// <remarks>
    ///     El asistente ya pertenecía al grupo A antes del evento.
    /// </remarks>
    [Fact]
    public async Task PastEventPopulation_UsesTheGroupAtTheEventDate()
    {
        var (groupA, groupB, leaderA, leaderB, _, attendeeA, _) = await TwoGroupsAsync();

        using (var scope = _fixture.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SheepfoldContext>();
            var membership = await dbContext.GroupMemberships.SingleAsync(item => item.AttendeeId == attendeeA, CancellationToken);
            membership.StartedAt = DateTime.UtcNow.AddDays(-7);
            await dbContext.SaveChangesAsync(CancellationToken);
        }

        var past = await _scenario.CreateEventAsync(_scenario.Director.Token, EventAudience.Groups, [groupA], startsAt: DateTimeOffset.UtcNow.AddDays(-1));

        using var transfer = await _scenario.SendAsync(HttpMethod.Post, $"/api/attendees/{attendeeA}/transfer", _scenario.Director.Token, new { groupId = groupB });
        Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);

        var sheet = await _scenario.GetAsync<AttendanceSheetResponse>($"/api/events/{past}/attendance", leaderA.Token);
        Assert.Contains(sheet.People, person => person.PersonId == attendeeA);

        using var otherLeader = await _scenario.SendAsync(HttpMethod.Get, $"/api/events/{past}/attendance", leaderB.Token);
        Assert.Equal(HttpStatusCode.NotFound, otherLeader.StatusCode);
    }

    [Fact]
    public async Task EventList_FiltersWithinTheVisibleScope()
    {
        var (groupA, groupB, leaderA, _, _, _, _) = await TwoGroupsAsync();
        var mine = await _scenario.CreateEventAsync(_scenario.Director.Token, EventAudience.Groups, [groupA]);
        var others = await _scenario.CreateEventAsync(_scenario.Director.Token, EventAudience.Groups, [groupB]);

        var filtered = await _scenario.GetAsync<Page<EventSummaryResponse>>($"/api/events?groupId={groupB}", leaderA.Token);
        var all = await _scenario.GetAsync<Page<EventSummaryResponse>>("/api/events?pageSize=100", leaderA.Token);

        Assert.Empty(filtered.Items);
        Assert.Contains(all.Items, item => item.Id == mine);
        Assert.DoesNotContain(all.Items, item => item.Id == others);
    }

    private async Task MarkAsync(Guid eventId, string token, Guid attendeeId, AttendanceMark? mark, HttpStatusCode expected)
    {
        using var response = await _scenario.SendAsync(HttpMethod.Put, $"/api/events/{eventId}/attendance", token,
            new { personType = PersonType.Attendee, personId = attendeeId, mark });
        Assert.Equal(expected, response.StatusCode);
    }

    private static object EventBody(EventAudience audience, Guid[] groupIds, Guid manager) => new
    {
        title = "Evento no permitido",
        eventTypeId = MinistryScenario.EventTypeId,
        startsAt = MinistryScenario.ReferenceEventStart.AddDays(2).AddHours(-2),
        endsAt = MinistryScenario.ReferenceEventStart.AddDays(2),
        location = "Salón",
        description = "Prueba",
        audience,
        groupIds,
        generationIds = Array.Empty<Guid>(),
        requiresAttendance = true,
        managerIds = new[] { manager },
        publish = true
    };
}

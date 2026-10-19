using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendance.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Calendar.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Reports.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Contexts;
using HobbitCoder.TMTBucaramanga.Sheepfold.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.IntegrationTests.FollowUps;

/// <summary>Seguimiento pastoral y confidencialidad.</summary>
public class FollowUpTests : IClassFixture<SheepfoldApiFixture>
{
    private const string Secret = "Contenido pastoral sensible";

    private readonly SheepfoldApiFixture _fixture;
    private readonly MinistryScenario _scenario;
    private Guid _topicId;

    public FollowUpTests(SheepfoldApiFixture fixture)
    {
        _fixture = fixture;
        _scenario = new MinistryScenario(fixture);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    /// <summary>Un grupo con coordinador, líder y aprendiz, un asistente y un tema de seguimiento.</summary>
    private async Task<(Guid Group, TestUser Coordinator, TestUser Leader, TestUser Apprentice, Guid Attendee)> SetUpAsync()
    {
        var group = await _scenario.CreateGroupAsync();
        var coordinator = _fixture.AddUser(UserRole.Coordinator);
        var leader = _fixture.AddUser(UserRole.Leader);
        var apprentice = _fixture.AddUser(UserRole.Apprentice);
        await _scenario.SetTeamAsync(group, coordinators: [coordinator.Id], leaders: [leader.Id], apprentices: [apprentice.Id]);

        var admin = _fixture.AddUser(UserRole.Admin);
        using var topic = await _scenario.SendAsync(HttpMethod.Post, "/api/catalogs/FollowUpTopics", admin.Token, new { name = $"Proceso personal {Guid.NewGuid().ToString()[..6]}" });
        _topicId = await MinistryScenario.ReadAsync<Guid>(topic);

        return (group, coordinator, leader, apprentice, await _scenario.CreateAttendeeAsync(group));
    }

    private object Content(bool confidential) => new
    {
        meetingDate = new DateOnly(2026, 10, 1),
        location = "Sala de conversación",
        topicId = _topicId,
        description = Secret,
        commitments = "Mantener el hábito de lectura.",
        observations = "Seguimos orando.",
        isConfidential = confidential
    };

    [Fact]
    public async Task Pending_WithoutDate_IsNotShownAsToday_AndCompletingUpdatesTheSameRecord()
    {
        var (_, _, leader, _, attendee) = await SetUpAsync();

        using var created = await _scenario.SendAsync(HttpMethod.Post, "/api/follow-ups", leader.Token, new { subjectType = PersonType.Attendee, subjectId = attendee });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = await MinistryScenario.ReadAsync<Guid>(created);

        var calendar = await _scenario.GetAsync<CalendarResponse>("/api/calendar?from=2026-10-01&to=2026-10-31", leader.Token);
        Assert.DoesNotContain(calendar.FollowUps, item => item.Id == id);
        Assert.True(calendar.PendingWithoutDate >= 1);

        using var complete = await _scenario.SendAsync(HttpMethod.Post, $"/api/follow-ups/{id}/complete", leader.Token, Content(confidential: false));
        Assert.Equal(HttpStatusCode.NoContent, complete.StatusCode);

        using var scope = _fixture.Services.CreateScope();
        var followUps = await scope.ServiceProvider.GetRequiredService<SheepfoldContext>().FollowUps
            .Where(item => item.SubjectAttendeeId == attendee).ToListAsync(CancellationToken);
        Assert.Equal(FollowUpStatus.Done, Assert.Single(followUps).Status);
    }

    [Fact]
    public async Task Completing_WithoutTopic_IsRejected_AndStaysPending()
    {
        var (_, _, leader, _, attendee) = await SetUpAsync();
        using var created = await _scenario.SendAsync(HttpMethod.Post, "/api/follow-ups", leader.Token, new { subjectType = PersonType.Attendee, subjectId = attendee });
        var id = await MinistryScenario.ReadAsync<Guid>(created);

        using var complete = await _scenario.SendAsync(HttpMethod.Post, $"/api/follow-ups/{id}/complete", leader.Token,
            new { meetingDate = new DateOnly(2026, 10, 1), location = "Sala", description = "x", commitments = "y" });

        Assert.Equal(HttpStatusCode.BadRequest, complete.StatusCode);
        Assert.Equal(FollowUpStatus.Pending, (await _scenario.GetAsync<FollowUpDetailResponse>($"/api/follow-ups/{id}", leader.Token)).FollowUp.Status);
    }

    [Fact]
    public async Task Apprentice_CannotRegisterOrSeeFollowUps()
    {
        var (_, _, _, apprentice, attendee) = await SetUpAsync();

        using var create = await _scenario.SendAsync(HttpMethod.Post, "/api/follow-ups", apprentice.Token, new { subjectType = PersonType.Attendee, subjectId = attendee });
        using var list = await _scenario.SendAsync(HttpMethod.Get, "/api/follow-ups", apprentice.Token);

        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
    }

    [Fact]
    public async Task Leader_CannotFollowUpAPersonOutsideTheirGroup()
    {
        var (_, _, leader, _, _) = await SetUpAsync();
        var otherGroup = await _scenario.CreateGroupAsync();
        var outsider = await _scenario.CreateAttendeeAsync(otherGroup);

        using var create = await _scenario.SendAsync(HttpMethod.Post, "/api/follow-ups", leader.Token, new { subjectType = PersonType.Attendee, subjectId = outsider });

        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
    }

    /// <summary>
    ///     Otro responsable con alcance ve los datos generales, nunca el contenido (ni en el JSON).
    /// </summary>
    [Fact]
    public async Task ConfidentialContent_IsOnlyForTheAuthorAndTheDirector_AndItsReadIsAudited()
    {
        var (_, coordinator, leader, _, attendee) = await SetUpAsync();
        using var created = await _scenario.SendAsync(HttpMethod.Post, "/api/follow-ups", leader.Token,
            new { subjectType = PersonType.Attendee, subjectId = attendee, completion = Content(confidential: true) });
        var id = await MinistryScenario.ReadAsync<Guid>(created);

        using var forCoordinator = await _scenario.SendAsync(HttpMethod.Get, $"/api/follow-ups/{id}", coordinator.Token);
        var raw = await forCoordinator.Content.ReadAsStringAsync(CancellationToken);
        var protectedView = await MinistryScenario.ReadAsync<FollowUpDetailResponse>(forCoordinator);
        Assert.True(protectedView.ContentProtected);
        Assert.Null(protectedView.Content);
        Assert.DoesNotContain(Secret, raw, StringComparison.Ordinal);
        Assert.DoesNotContain("Sala de conversación", raw, StringComparison.Ordinal);

        Assert.Equal(Secret, (await _scenario.GetAsync<FollowUpDetailResponse>($"/api/follow-ups/{id}", leader.Token)).Content!.Description);
        Assert.Equal(Secret, (await _scenario.GetAsync<FollowUpDetailResponse>($"/api/follow-ups/{id}", _scenario.Director.Token)).Content!.Description);

        using var scope = _fixture.Services.CreateScope();
        var reads = await scope.ServiceProvider.GetRequiredService<SheepfoldContext>().AuditEntries
            .CountAsync(entry => entry.EntityId == id && entry.Action == "SensitiveRead", CancellationToken);
        Assert.Equal(2, reads);
    }

    [Fact]
    public async Task RemovingConfidentiality_GivesTheContentToTheOrdinaryHierarchy()
    {
        var (_, coordinator, leader, _, attendee) = await SetUpAsync();
        using var created = await _scenario.SendAsync(HttpMethod.Post, "/api/follow-ups", leader.Token,
            new { subjectType = PersonType.Attendee, subjectId = attendee, completion = Content(confidential: true) });
        var id = await MinistryScenario.ReadAsync<Guid>(created);

        using var update = await _scenario.SendAsync(HttpMethod.Put, $"/api/follow-ups/{id}/content", leader.Token, Content(confidential: false));
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        Assert.Equal(Secret, (await _scenario.GetAsync<FollowUpDetailResponse>($"/api/follow-ups/{id}", coordinator.Token)).Content!.Description);

        using var otherEdit = await _scenario.SendAsync(HttpMethod.Put, $"/api/follow-ups/{id}/content", coordinator.Token, Content(confidential: true));
        Assert.Equal(HttpStatusCode.Forbidden, otherEdit.StatusCode);
    }

    [Fact]
    public async Task TheSubject_NeverSeesTheirOwnFollowUp()
    {
        var (_, coordinator, leader, _, _) = await SetUpAsync();
        using var created = await _scenario.SendAsync(HttpMethod.Post, "/api/follow-ups", coordinator.Token,
            new { subjectType = PersonType.User, subjectId = leader.Id, completion = Content(confidential: false) });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = await MinistryScenario.ReadAsync<Guid>(created);

        using var detail = await _scenario.SendAsync(HttpMethod.Get, $"/api/follow-ups/{id}", leader.Token);
        var done = await _scenario.GetAsync<Page<FollowUpListItem>>("/api/follow-ups?status=Done&pageSize=100", leader.Token);
        var calendar = await _scenario.GetAsync<CalendarResponse>("/api/calendar?from=2026-10-01&to=2026-10-31", leader.Token);

        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
        Assert.DoesNotContain(done.Items, item => item.Id == id);
        Assert.DoesNotContain(calendar.FollowUps, item => item.Id == id);
    }

    [Fact]
    public async Task DoneFollowUp_KeepsTheGroupOfTheMeeting_AfterATransfer()
    {
        var (group, coordinator, leader, _, attendee) = await SetUpAsync();
        using var created = await _scenario.SendAsync(HttpMethod.Post, "/api/follow-ups", leader.Token,
            new { subjectType = PersonType.Attendee, subjectId = attendee, completion = Content(confidential: false) });
        var id = await MinistryScenario.ReadAsync<Guid>(created);

        var other = await _scenario.CreateGroupAsync();
        using var transfer = await _scenario.SendAsync(HttpMethod.Post, $"/api/attendees/{attendee}/transfer", _scenario.Director.Token, new { groupId = other });
        Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);

        const string period = "from=2026-10-01&to=2026-10-31";
        var previousGroup = await _scenario.GetAsync<ReportResponse>($"/api/reports/FollowUps?{period}&groupId={group}", _scenario.Director.Token);
        var currentGroup = await _scenario.GetAsync<ReportResponse>($"/api/reports/FollowUps?{period}&groupId={other}", _scenario.Director.Token);
        var forPreviousCoordinator = await _scenario.GetAsync<Page<FollowUpListItem>>($"/api/follow-ups?status=Done&groupId={group}&pageSize=100", coordinator.Token);

        Assert.Equal(1, previousGroup.Metrics.Single(metric => metric.Key == "done").Value);
        Assert.Equal(0, currentGroup.Metrics.Single(metric => metric.Key == "done").Value);
        Assert.Contains(forPreviousCoordinator.Items, item => item.Id == id);
    }
}

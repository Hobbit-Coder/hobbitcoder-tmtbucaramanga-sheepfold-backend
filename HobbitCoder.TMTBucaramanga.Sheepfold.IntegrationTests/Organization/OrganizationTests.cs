using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Context.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Contexts;
using HobbitCoder.TMTBucaramanga.Sheepfold.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.IntegrationTests.Organization;

/// <summary>Grupos, equipo, asistentes, contactos, cursos y traslados.</summary>
public class OrganizationTests : IClassFixture<SheepfoldApiFixture>
{
    private readonly SheepfoldApiFixture _fixture;
    private readonly MinistryScenario _scenario;

    public OrganizationTests(SheepfoldApiFixture fixture)
    {
        _fixture = fixture;
        _scenario = new MinistryScenario(fixture);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Group_ShowsLeadersGender_WithoutBlocking_AndCoordinationIsNotMembership()
    {
        var groupId = await _scenario.CreateGroupAsync();
        var coordinator = _fixture.AddUser(UserRole.Coordinator, Gender.Male);
        var leader = _fixture.AddUser(UserRole.Leader, Gender.Male);
        await _scenario.SetTeamAsync(groupId, coordinators: [coordinator.Id], leaders: [leader.Id]);

        var detail = await _scenario.GetAsync<GroupDetailResponse>($"/api/groups/{groupId}", _scenario.Director.Token);

        Assert.False(detail.ExpectedLeadersComposition);
        Assert.Contains(detail.Group.Team, member => member.UserId == leader.Id && member.Gender == Gender.Male);
        Assert.Empty(detail.Members);
    }

    [Fact]
    public async Task Team_RequiresTheMatchingRole()
    {
        var groupId = await _scenario.CreateGroupAsync();
        var apprentice = _fixture.AddUser(UserRole.Apprentice);

        using var response = await _scenario.SendAsync(HttpMethod.Put, $"/api/groups/{groupId}/team", _scenario.Director.Token,
            new { coordinatorIds = Array.Empty<Guid>(), leaderIds = new[] { apprentice.Id }, apprenticeIds = Array.Empty<Guid>() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CoordinatorThatAlsoLeads_KeepsTheCoordinatorRole_AndActsAsLeader()
    {
        var coordinated = await _scenario.CreateGroupAsync();
        var led = await _scenario.CreateGroupAsync();
        var coordinator = _fixture.AddUser(UserRole.Coordinator);
        await _scenario.SetTeamAsync(coordinated, coordinators: [coordinator.Id]);
        await _scenario.SetTeamAsync(led, leaders: [coordinator.Id]);
        var attendee = await _scenario.CreateAttendeeAsync(led);

        var profile = await _scenario.GetAsync<AttendeeProfileResponse>($"/api/attendees/{attendee}", coordinator.Token);

        Assert.True(profile.Capabilities.FollowUp);
    }

    [Fact]
    public async Task Coordinator_CannotSeeAttendeesOfAGroupTheyDoNotCoordinate()
    {
        var mine = await _scenario.CreateGroupAsync();
        var other = await _scenario.CreateGroupAsync();
        var coordinator = _fixture.AddUser(UserRole.Coordinator);
        await _scenario.SetTeamAsync(mine, coordinators: [coordinator.Id]);
        var outsider = await _scenario.CreateAttendeeAsync(other);

        using var profile = await _scenario.SendAsync(HttpMethod.Get, $"/api/attendees/{outsider}", coordinator.Token);
        var list = await _scenario.GetAsync<Page<AttendeeListItem>>($"/api/attendees?groupId={other}", coordinator.Token);

        Assert.Equal(HttpStatusCode.NotFound, profile.StatusCode);
        Assert.Empty(list.Items);
    }

    /// <summary>
    ///     Registró en un grupo que no administra: no por eso puede consultarlo ni editarlo.
    /// </summary>
    [Fact]
    public async Task Registration_RequiresTheDesignation_WhichDoesNotWidenTheEditingScope()
    {
        var groupId = await _scenario.CreateGroupAsync();
        var leader = _fixture.AddUser(UserRole.Leader);
        var registrar = _fixture.AddUser(UserRole.Leader, canRegisterAttendees: true);

        using var denied = await _scenario.SendAsync(HttpMethod.Post, "/api/attendees", leader.Token, new
        {
            firstName = "Mateo", lastName = "Díaz", groupId,
            contacts = new[] { new { kind = ContactKind.EmergencyContact, fullName = "Luz Díaz", relationship = "Madre", phoneNumber = "3000000000" } }
        });
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        var created = await _scenario.CreateAttendeeAsync(groupId, token: registrar.Token);

        using var afterwards = await _scenario.SendAsync(HttpMethod.Get, $"/api/attendees/{created}", registrar.Token);
        Assert.Equal(HttpStatusCode.NotFound, afterwards.StatusCode);
    }

    [Fact]
    public async Task Registrar_TransfersToAnyActiveGroup_AndOnlyThemSeeEveryGroupAsDestination()
    {
        var led = await _scenario.CreateGroupAsync();
        var other = await _scenario.CreateGroupAsync();
        var leader = _fixture.AddUser(UserRole.Leader);
        var registrar = _fixture.AddUser(UserRole.Leader, canRegisterAttendees: true);
        await _scenario.SetTeamAsync(led, leaders: [leader.Id, registrar.Id]);
        var attendee = await _scenario.CreateAttendeeAsync(led);

        var leaderDestinations = await _scenario.GetAsync<List<DestinationGroupResponse>>("/api/groups/destinations", leader.Token);
        var registrarDestinations = await _scenario.GetAsync<List<DestinationGroupResponse>>("/api/groups/destinations", registrar.Token);
        Assert.Equal([led], leaderDestinations.Select(group => group.Id));
        Assert.Contains(registrarDestinations, group => group.Id == other);

        var leaderProfile = await _scenario.GetAsync<AttendeeProfileResponse>($"/api/attendees/{attendee}", leader.Token);
        var registrarProfile = await _scenario.GetAsync<AttendeeProfileResponse>($"/api/attendees/{attendee}", registrar.Token);
        Assert.False(leaderProfile.Capabilities.Transfer);
        Assert.True(registrarProfile.Capabilities.Transfer);

        using var denied = await _scenario.SendAsync(HttpMethod.Post, $"/api/attendees/{attendee}/transfer", leader.Token, new { groupId = other });
        using var transferred = await _scenario.SendAsync(HttpMethod.Post, $"/api/attendees/{attendee}/transfer", registrar.Token, new { groupId = other });

        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, transferred.StatusCode);
    }

    [Fact]
    public async Task Sniper_ManagesEveryCurrentAttendee_WithoutFollowUpsNorRetirement()
    {
        var groupId = await _scenario.CreateGroupAsync();
        var sniper = _fixture.AddUser(UserRole.Sniper);
        var attendee = await _scenario.CreateAttendeeAsync(groupId);

        var list = await _scenario.GetAsync<Page<AttendeeListItem>>($"/api/attendees?groupId={groupId}", sniper.Token);
        var profile = await _scenario.GetAsync<AttendeeProfileResponse>($"/api/attendees/{attendee}", sniper.Token);
        using var updated = await _scenario.SendAsync(HttpMethod.Put, $"/api/attendees/{attendee}", sniper.Token,
            new { firstName = "Ana", lastName = "Sniper", isServer = false, isActive = true });

        Assert.Contains(list.Items, item => item.Id == attendee);
        Assert.False(profile.Capabilities.FollowUp);
        Assert.False(profile.Capabilities.Retire);
        Assert.False(profile.Capabilities.Transfer);
        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
    }

    [Fact]
    public async Task Sniper_CanBeAnApprentice_ButStaysOutOfTheHierarchy()
    {
        var groupId = await _scenario.CreateGroupAsync();
        var leader = _fixture.AddUser(UserRole.Leader);
        var sniper = _fixture.AddUser(UserRole.Sniper);
        await _scenario.SetTeamAsync(groupId, leaders: [leader.Id], apprentices: [sniper.Id]);

        var subjects = await _scenario.GetAsync<List<FollowUpSubject>>("/api/follow-ups/subjects", leader.Token);
        var context = await _scenario.GetAsync<MyContextResponse>("/api/me/context", sniper.Token);

        Assert.DoesNotContain(subjects, subject => subject.Id == sniper.Id);
        Assert.Contains(context.Groups, group => group.GroupId == groupId && group.Responsibility == GroupResponsibility.Apprentice);
        Assert.False(context.Capabilities.FollowUps);
        Assert.False(context.Capabilities.CreateEvents);
    }

    [Fact]
    public async Task Sniper_DoesNotSeeReports()
    {
        var sniper = _fixture.AddUser(UserRole.Sniper);

        var context = await _scenario.GetAsync<MyContextResponse>("/api/me/context", sniper.Token);
        using var report = await _scenario.SendAsync(HttpMethod.Get, "/api/reports/Attendance?from=2026-10-01&to=2026-10-31", sniper.Token);

        Assert.False(context.Capabilities.Reports);
        Assert.Equal(HttpStatusCode.Forbidden, report.StatusCode);
    }

    [Fact]
    public async Task Pulso_RequiresALegalGuardian()
    {
        var groupId = await _scenario.CreateGroupAsync(MinistryScenario.Pulso);

        using var response = await _scenario.SendAsync(HttpMethod.Post, "/api/attendees", _scenario.Director.Token, new
        {
            firstName = "Mateo", lastName = "Díaz", groupId,
            contacts = new[] { new { kind = ContactKind.EmergencyContact, fullName = "Luz Díaz", relationship = "Madre", phoneNumber = "3000000000" } }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Contacts", (await MinistryScenario.ReadAsync<ValidationProblemDetails>(response)).Errors.Keys);

        await _scenario.CreateAttendeeAsync(groupId, contactKind: ContactKind.LegalGuardian);
    }

    [Fact]
    public async Task Attendee_WithoutGroup_IsRejected()
    {
        using var response = await _scenario.SendAsync(HttpMethod.Post, "/api/attendees", _scenario.Director.Token,
            new { firstName = "Mateo", lastName = "Díaz", contacts = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Leader_EditsTheirAttendee_AndTheChangeIsAudited_ButNotOthers()
    {
        var mine = await _scenario.CreateGroupAsync();
        var other = await _scenario.CreateGroupAsync();
        var leader = _fixture.AddUser(UserRole.Leader);
        await _scenario.SetTeamAsync(mine, leaders: [leader.Id]);
        var attendee = await _scenario.CreateAttendeeAsync(mine);
        var outsider = await _scenario.CreateAttendeeAsync(other);
        var body = new { firstName = "Ana", lastName = "Actualizada", isServer = true, isActive = true };

        using var updated = await _scenario.SendAsync(HttpMethod.Put, $"/api/attendees/{attendee}", leader.Token, body);
        using var denied = await _scenario.SendAsync(HttpMethod.Put, $"/api/attendees/{outsider}", leader.Token, body);

        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);

        using var scope = _fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SheepfoldContext>();
        Assert.True(await dbContext.AuditEntries.AnyAsync(entry => entry.EntityId == attendee && entry.ActorUserId == leader.Id.ToString(), CancellationToken));
        Assert.NotEqual("Actualizada", (await dbContext.Attendees.SingleAsync(item => item.Id == outsider, CancellationToken)).LastName);
    }

    [Fact]
    public async Task Transfer_KeepsExactlyOneActiveGroup_AndTheHistory()
    {
        var from = await _scenario.CreateGroupAsync();
        var to = await _scenario.CreateGroupAsync();
        var attendee = await _scenario.CreateAttendeeAsync(from);

        using var transfer = await _scenario.SendAsync(HttpMethod.Post, $"/api/attendees/{attendee}/transfer", _scenario.Director.Token, new { groupId = to });
        Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);

        var profile = await _scenario.GetAsync<AttendeeProfileResponse>($"/api/attendees/{attendee}", _scenario.Director.Token);
        var history = await _scenario.GetAsync<List<MembershipHistoryItem>>($"/api/attendees/{attendee}/history", _scenario.Director.Token);

        Assert.Equal(to, profile.Group!.Id);
        Assert.Equal(2, history.Count);
        Assert.Single(history, item => item.EndedAt is null);
        Assert.Contains(history, item => item.Group.Id == from && item.EndedAt is not null);
    }

    [Fact]
    public async Task Transfer_ToAnInactiveGroup_IsRejected_AndTheCurrentGroupIsKept()
    {
        var from = await _scenario.CreateGroupAsync();
        var inactive = await _scenario.CreateGroupAsync();
        using var deactivate = await _scenario.SendAsync(HttpMethod.Put, $"/api/groups/{inactive}", _scenario.Director.Token, new { name = "Inactivo " + Guid.NewGuid().ToString()[..6], isActive = false });
        Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);
        var attendee = await _scenario.CreateAttendeeAsync(from);

        using var transfer = await _scenario.SendAsync(HttpMethod.Post, $"/api/attendees/{attendee}/transfer", _scenario.Director.Token, new { groupId = inactive });

        Assert.Equal(HttpStatusCode.BadRequest, transfer.StatusCode);
        Assert.Equal(from, (await _scenario.GetAsync<AttendeeProfileResponse>($"/api/attendees/{attendee}", _scenario.Director.Token)).Group!.Id);
    }

    [Fact]
    public async Task Transfer_FromPulsoToEco_RequiresAnEmergencyContact_AndKeepsTheGuardian()
    {
        var pulso = await _scenario.CreateGroupAsync(MinistryScenario.Pulso);
        var eco = await _scenario.CreateGroupAsync(MinistryScenario.Eco);
        var attendee = await _scenario.CreateAttendeeAsync(pulso, contactKind: ContactKind.LegalGuardian);

        using var missing = await _scenario.SendAsync(HttpMethod.Post, $"/api/attendees/{attendee}/transfer", _scenario.Director.Token, new { groupId = eco });
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);

        using var transfer = await _scenario.SendAsync(HttpMethod.Post, $"/api/attendees/{attendee}/transfer", _scenario.Director.Token, new
        {
            groupId = eco,
            contact = new { kind = ContactKind.EmergencyContact, fullName = "Luis Pérez", relationship = "Tío", phoneNumber = "+573000000103" }
        });
        Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);

        var profile = await _scenario.GetAsync<AttendeeProfileResponse>($"/api/attendees/{attendee}", _scenario.Director.Token);
        Assert.Equal(2, profile.Contacts.Count);
        Assert.False(profile.MissingRequiredContact);
    }

    [Fact]
    public async Task LastRequiredContact_CannotBeRemoved()
    {
        var groupId = await _scenario.CreateGroupAsync();
        var attendee = await _scenario.CreateAttendeeAsync(groupId);
        var contact = (await _scenario.GetAsync<AttendeeProfileResponse>($"/api/attendees/{attendee}", _scenario.Director.Token)).Contacts.Single();

        using var response = await _scenario.SendAsync(HttpMethod.Delete, $"/api/attendees/{attendee}/contacts/{contact.Id}", _scenario.Director.Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Courses_AllowFinishedWithoutDate_RejectDatesForCoursesInProgress_AndDuplicates()
    {
        var groupId = await _scenario.CreateGroupAsync();
        var attendee = await _scenario.CreateAttendeeAsync(groupId);

        using var finished = await _scenario.SendAsync(HttpMethod.Post, $"/api/attendees/{attendee}/courses", _scenario.Director.Token,
            new { courseId = MinistryScenario.CourseId, status = CourseStatus.Finished });
        using var inProgressWithDate = await _scenario.SendAsync(HttpMethod.Post, $"/api/attendees/{attendee}/courses", _scenario.Director.Token,
            new { courseId = MinistryScenario.CourseId, status = CourseStatus.InProgress, finishedOn = new DateOnly(2026, 9, 10) });
        using var invalidStatus = await _scenario.SendAsync(HttpMethod.Post, $"/api/attendees/{attendee}/courses", _scenario.Director.Token,
            new { courseId = MinistryScenario.CourseId, status = 7 });
        using var duplicate = await _scenario.SendAsync(HttpMethod.Post, $"/api/attendees/{attendee}/courses", _scenario.Director.Token,
            new { courseId = MinistryScenario.CourseId, status = CourseStatus.InProgress });

        Assert.Equal(HttpStatusCode.OK, finished.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, inProgressWithDate.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidStatus.StatusCode);
    }

    [Fact]
    public async Task EducationalStage_ThatDoesNotMatchTheGeneration_WarnsWithoutBlocking()
    {
        var groupId = await _scenario.CreateGroupAsync(MinistryScenario.Eco);
        var attendee = await _scenario.CreateAttendeeAsync(groupId);

        using var update = await _scenario.SendAsync(HttpMethod.Put, $"/api/attendees/{attendee}", _scenario.Director.Token,
            new { firstName = "Ana", lastName = "Pérez", educationalStage = EducationalStage.Semester3, isActive = true });

        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
        Assert.True((await _scenario.GetAsync<AttendeeProfileResponse>($"/api/attendees/{attendee}", _scenario.Director.Token)).GenerationWarning);
    }

    [Fact]
    public async Task Group_WithCurrentAttendees_CannotBeDeactivated()
    {
        var groupId = await _scenario.CreateGroupAsync();
        await _scenario.CreateAttendeeAsync(groupId);

        using var response = await _scenario.SendAsync(HttpMethod.Put, $"/api/groups/{groupId}", _scenario.Director.Token, new { name = "Grupo " + Guid.NewGuid().ToString()[..6], isActive = false });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Renewal_MovesAttendeesToTheNewYear_AndDeactivatesTheEmptiedOldGroup()
    {
        var old = await _scenario.CreateGroupAsync(year: 2026);
        var renewed = await _scenario.CreateGroupAsync(year: 2027);
        var attendee = await _scenario.CreateAttendeeAsync(old);

        var overview = await _scenario.GetAsync<RenewalOverviewResponse>("/api/groups/renewal?year=2027", _scenario.Director.Token);
        Assert.Contains(overview.PendingAttendees, item => item.AttendeeId == attendee);

        using var save = await _scenario.SendAsync(HttpMethod.Post, "/api/groups/renewal/assignments", _scenario.Director.Token,
            new { year = 2027, assignments = new[] { new { attendeeId = attendee, groupId = renewed } } });
        Assert.Equal(HttpStatusCode.NoContent, save.StatusCode);

        var oldGroup = await _scenario.GetAsync<GroupDetailResponse>($"/api/groups/{old}", _scenario.Director.Token);
        Assert.False(oldGroup.Group.IsActive);

        var history = await _scenario.GetAsync<List<GroupHistoryEntry>>($"/api/groups/{old}/history", _scenario.Director.Token);
        Assert.Contains(history, entry => entry.AttendeeId == attendee && entry.EndedAt is not null);
    }

    [Fact]
    public async Task TeamCandidates_AreActiveTeamAccounts_OnlyForDirectorAndCoordinators()
    {
        var leader = _fixture.AddUser(UserRole.Leader);
        var coordinator = _fixture.AddUser(UserRole.Coordinator);
        var admin = _fixture.AddUser(UserRole.Admin);

        var candidates = await _scenario.GetAsync<List<TeamCandidate>>("/api/groups/team-candidates", coordinator.Token);
        Assert.Contains(candidates, candidate => candidate.UserId == leader.Id && candidate.Role == UserRole.Leader);
        Assert.DoesNotContain(candidates, candidate => candidate.UserId == admin.Id || candidate.Role == UserRole.Director);

        using var forbidden = await _scenario.SendAsync(HttpMethod.Get, "/api/groups/team-candidates", leader.Token);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }
}

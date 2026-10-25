using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendance.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Audit.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Reports.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Retired.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Settings.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Contexts;
using HobbitCoder.TMTBucaramanga.Sheepfold.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.IntegrationTests.Oversight;

/// <summary>Archivos en S3, reportes, auditoría, conservación y configuración.</summary>
public class OversightTests : IClassFixture<SheepfoldApiFixture>
{
    private readonly SheepfoldApiFixture _fixture;
    private readonly MinistryScenario _scenario;

    public OversightTests(SheepfoldApiFixture fixture)
    {
        _fixture = fixture;
        _scenario = new MinistryScenario(fixture);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task<HttpResponseMessage> UploadAsync(Guid eventId, string token, string content = "contenido del archivo")
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(content));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "file", "guia-estudio.pdf");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/events/{eventId}/attachments") { Content = form };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await _scenario.Client.SendAsync(request, CancellationToken);
    }

    /// <summary>
    ///     Solo quien administra el evento adjunta archivos al evento.
    /// </summary>
    [Fact]
    public async Task Attachment_IsStoredPrivately_AndOnlyDownloadableByEventViewers()
    {
        var mine = await _scenario.CreateGroupAsync();
        var other = await _scenario.CreateGroupAsync();
        var leader = _fixture.AddUser(UserRole.Leader);
        var outsider = _fixture.AddUser(UserRole.Leader);
        await _scenario.SetTeamAsync(mine, leaders: [leader.Id]);
        await _scenario.SetTeamAsync(other, leaders: [outsider.Id]);
        var eventId = await _scenario.CreateEventAsync(_scenario.Director.Token, EventAudience.Groups, [mine]);

        using var upload = await UploadAsync(eventId, _scenario.Director.Token);
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        var file = await MinistryScenario.ReadAsync<FileResponse>(upload);

        using var download = await _scenario.SendAsync(HttpMethod.Get, $"/api/events/{eventId}/attachments/{file.Id}", leader.Token);
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("contenido del archivo", await download.Content.ReadAsStringAsync(CancellationToken));
        Assert.Equal("attachment", download.Content.Headers.ContentDisposition?.DispositionType);

        using var denied = await _scenario.SendAsync(HttpMethod.Get, $"/api/events/{eventId}/attachments/{file.Id}", outsider.Token);
        Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);

        using var notAdmin = await UploadAsync(eventId, leader.Token);
        Assert.Equal(HttpStatusCode.Forbidden, notAdmin.StatusCode);

        using var scope = _fixture.Services.CreateScope();
        var stored = await scope.ServiceProvider.GetRequiredService<SheepfoldContext>().EventAttachments.SingleAsync(item => item.Id == file.Id, CancellationToken);
        Assert.Contains(stored.ObjectKey, _fixture.Storage.Keys);
    }

    [Fact]
    public async Task StorageFailure_LeavesNoMetadata()
    {
        var eventId = await _scenario.CreateEventAsync(_scenario.Director.Token, EventAudience.AllMinistry);
        _fixture.Storage.FailNextPut = true;

        using var upload = await UploadAsync(eventId, _scenario.Director.Token);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, upload.StatusCode);
        using var scope = _fixture.Services.CreateScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<SheepfoldContext>().EventAttachments.AnyAsync(item => item.EventId == eventId, CancellationToken));
    }

    [Fact]
    public async Task Comment_WithAttachment_IsVisibleToParticipants()
    {
        var eventId = await _scenario.CreateEventAsync(_scenario.Director.Token, EventAudience.AllMinistry);
        var leader = _fixture.AddUser(UserRole.Leader);

        using var form = new MultipartFormDataContent { { new StringContent("Agregué un borrador."), "body" } };
        form.Add(new ByteArrayContent([1, 2, 3]), "files", "borrador.pdf");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/events/{eventId}/comments") { Content = form };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", leader.Token);
        using var response = await _scenario.Client.SendAsync(request, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var comment = await MinistryScenario.ReadAsync<CommentResponse>(response);

        var comments = await _scenario.GetAsync<List<CommentResponse>>($"/api/events/{eventId}/comments", _scenario.Director.Token);

        Assert.Contains(comments, item => item.Id == comment.Id && item.Attachments.Count == 1);
    }

    [Fact]
    public async Task Reports_AreLimitedToTheUserScope()
    {
        var mine = await _scenario.CreateGroupAsync();
        var other = await _scenario.CreateGroupAsync();
        var leader = _fixture.AddUser(UserRole.Leader);
        await _scenario.SetTeamAsync(mine, leaders: [leader.Id]);
        await _scenario.CreateAttendeeAsync(mine, isServer: true);
        await _scenario.CreateAttendeeAsync(other, isServer: true);

        var forLeader = await _scenario.GetAsync<ReportResponse>("/api/reports/People?from=2026-10-01&to=2026-10-31", leader.Token);
        var forDirector = await _scenario.GetAsync<ReportResponse>($"/api/reports/People?from=2026-10-01&to=2026-10-31&groupId={other}", _scenario.Director.Token);

        Assert.Equal(1, forLeader.Metrics.Single(metric => metric.Key == "attendees").Value);
        Assert.Equal(1, forDirector.Metrics.Single(metric => metric.Key == "servers").Value);
    }

    [Fact]
    public async Task AttendanceSignals_AreNotConfiguredUntilTheAdminSetsTheParameters()
    {
        var report = await _scenario.GetAsync<ReportResponse>("/api/reports/Attendance?from=2026-10-01&to=2026-10-31", _scenario.Director.Token);

        Assert.All(report.Signals, signal => Assert.False(signal.Configured));
    }

    [Fact]
    public async Task Configuration_IsManagedByTheAdmin_AndOnlyReadByTheDirector()
    {
        var admin = _fixture.AddUser(UserRole.Admin);
        var current = await _scenario.GetAsync<OperationalSettingsDto>("/api/settings/indicators", _scenario.Director.Token);

        using var directorUpdate = await _scenario.SendAsync(HttpMethod.Put, "/api/settings/indicators", _scenario.Director.Token, current);
        using var leaderRead = await _scenario.SendAsync(HttpMethod.Get, "/api/settings/indicators", _fixture.AddUser(UserRole.Leader).Token);
        using var adminCreate = await _scenario.SendAsync(HttpMethod.Post, "/api/catalogs/EventTypes", admin.Token, new { name = $"Vigilia {Guid.NewGuid().ToString()[..6]}" });
        using var directorCreate = await _scenario.SendAsync(HttpMethod.Post, "/api/catalogs/EventTypes", _scenario.Director.Token, new { name = "No permitido" });

        Assert.Equal(HttpStatusCode.Forbidden, directorUpdate.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, leaderRead.StatusCode);
        Assert.Equal(HttpStatusCode.Created, adminCreate.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, directorCreate.StatusCode);
    }

    /// <summary>Sin avisos internos no queda registro de lo avisado y los programados se repetirían por WhatsApp.</summary>
    [Fact]
    public async Task InAppNotifications_CannotBeDisabled()
    {
        var admin = _fixture.AddUser(UserRole.Admin);
        var options = new { channels = new Dictionary<string, bool> { ["InApp"] = false, ["WhatsApp"] = true }, kinds = new Dictionary<string, bool>() };

        using var response = await _scenario.SendAsync(HttpMethod.Put, "/api/settings/notifications", admin.Token, options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Audit_IsOnlyForDirectors_AndRecordsAttendanceCorrections()
    {
        var groupId = await _scenario.CreateGroupAsync();
        var attendee = await _scenario.CreateAttendeeAsync(groupId);
        var eventId = await _scenario.CreateEventAsync(_scenario.Director.Token, EventAudience.Groups, [groupId]);
        using var mark = await _scenario.SendAsync(HttpMethod.Put, $"/api/events/{eventId}/attendance", _scenario.Director.Token,
            new { personType = PersonType.Attendee, personId = attendee, mark = AttendanceMark.Late });
        Assert.Equal(HttpStatusCode.OK, mark.StatusCode);

        var audit = await _scenario.GetAsync<Page<AuditEntryResponse>>("/api/audit?module=Attendance&pageSize=100", _scenario.Director.Token);
        Assert.Contains(audit.Items, entry => entry.EntityId == eventId && entry.After!.Contains("Late", StringComparison.Ordinal));

        using var leader = await _scenario.SendAsync(HttpMethod.Get, "/api/audit", _fixture.AddUser(UserRole.Coordinator).Token);
        Assert.Equal(HttpStatusCode.Forbidden, leader.StatusCode);
    }

    [Fact]
    public async Task RetiredAttendee_IsHidden_ThenRestored_OrPermanentlyDeleted()
    {
        var groupId = await _scenario.CreateGroupAsync();
        var attendee = await _scenario.CreateAttendeeAsync(groupId);
        var admin = _fixture.AddUser(UserRole.Admin);

        using var retire = await _scenario.SendAsync(HttpMethod.Delete, $"/api/attendees/{attendee}", _scenario.Director.Token);
        Assert.Equal(HttpStatusCode.NoContent, retire.StatusCode);
        using var hidden = await _scenario.SendAsync(HttpMethod.Get, $"/api/attendees/{attendee}", _scenario.Director.Token);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);

        var retired = await _scenario.GetAsync<List<RetiredRecordResponse>>("/api/retired?type=Attendee", _scenario.Director.Token);
        Assert.Contains(retired, item => item.Id == attendee);
        var forAdmin = await _scenario.GetAsync<List<RetiredRecordResponse>>("/api/retired", admin.Token);
        Assert.DoesNotContain(forAdmin, item => item.Type is RetiredType.Attendee or RetiredType.FollowUp);

        using var restore = await _scenario.SendAsync(HttpMethod.Post, $"/api/retired/Attendee/{attendee}/restore", _scenario.Director.Token, new { });
        Assert.Equal(HttpStatusCode.NoContent, restore.StatusCode);
        await _scenario.GetAsync<object>($"/api/attendees/{attendee}", _scenario.Director.Token);

        using var notRetired = await _scenario.SendAsync(HttpMethod.Delete, $"/api/retired/Attendee/{attendee}", _scenario.Director.Token);
        Assert.Equal(HttpStatusCode.NotFound, notRetired.StatusCode);

        using var retireAgain = await _scenario.SendAsync(HttpMethod.Delete, $"/api/attendees/{attendee}", _scenario.Director.Token);
        var impact = await _scenario.GetAsync<RetiredImpactResponse>($"/api/retired/Attendee/{attendee}/impact", _scenario.Director.Token);
        Assert.True(impact.CanDelete);
        using var delete = await _scenario.SendAsync(HttpMethod.Delete, $"/api/retired/Attendee/{attendee}", _scenario.Director.Token);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        using var scope = _fixture.Services.CreateScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<SheepfoldContext>().Attendees.IgnoreQueryFilters().AnyAsync(item => item.Id == attendee, CancellationToken));
    }
}

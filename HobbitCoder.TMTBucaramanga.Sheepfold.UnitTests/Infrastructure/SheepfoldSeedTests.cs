using HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Seeding;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.UnitTests.Infrastructure;

public class SheepfoldSeedTests
{
    /// <summary>Generaciones y cursos que ya siembran las migraciones (HasData).</summary>
    private static readonly IEnumerable<Guid> MigratedGenerationIds = new[]
        { "0199c2a0-0000-7000-8000-000000000101", "0199c2a0-0000-7000-8000-000000000102", "0199c2a0-0000-7000-8000-000000000103", "0199c2a0-0000-7000-8000-000000000104" }.Select(Guid.Parse);

    private static readonly IEnumerable<Guid> MigratedCourseIds = new[]
        { "0199c2a0-0000-7000-8000-000000000301", "0199c2a0-0000-7000-8000-000000000302", "0199c2a0-0000-7000-8000-000000000303", "0199c2a0-0000-7000-8000-000000000304" }.Select(Guid.Parse);

    private static readonly SheepfoldSeed Seed = SheepfoldSeed.Load();

    [Fact]
    public void LoadsTheEmbeddedSeed()
    {
        Assert.Single(Seed.Generations);
        Assert.Equal(12, Seed.Groups.Count);
        Assert.NotEmpty(Seed.Assignments);
        Assert.True(Seed.Attendees.Count > 250);
        Assert.Equal(48, Seed.Events.Count);
    }

    [Fact]
    public void EveryReferencePointsToARecordOfTheSeedOrOfTheMigrations()
    {
        var generationIds = Seed.Generations.Select(generation => generation.Id).Concat(MigratedGenerationIds).ToHashSet();
        var courseIds = Seed.Courses.Select(course => course.Id).Concat(MigratedCourseIds).ToHashSet();
        var groupIds = Seed.Groups.Select(group => group.Id).ToHashSet();
        var eventTypeIds = Seed.EventTypes.Select(type => type.Id).ToHashSet();

        Assert.All(Seed.Groups, group => Assert.Contains(group.GenerationId, generationIds));
        Assert.All(Seed.Assignments, assignment => Assert.Contains(assignment.GroupId, groupIds));
        Assert.All(Seed.Events, @event => Assert.Contains(@event.EventTypeId, eventTypeIds));
        Assert.All(Seed.Attendees, attendee =>
        {
            Assert.True(attendee.Memberships.Count <= 1);
            Assert.All(attendee.Memberships, membership => Assert.Equal((attendee.Id, true), (membership.AttendeeId, groupIds.Contains(membership.GroupId))));
            Assert.All(attendee.Courses, course => Assert.Equal((attendee.Id, true), (course.AttendeeId, courseIds.Contains(course.CourseId))));
            Assert.All(attendee.Contacts, contact => Assert.Equal(attendee.Id, contact.AttendeeId));
        });
    }

    [Fact]
    public void KeysAreUnique()
    {
        Assert.Equal(Seed.Attendees.Count, Seed.Attendees.Select(attendee => attendee.Id).Distinct().Count());
        Assert.Equal(Seed.Events.Count, Seed.Events.Select(@event => @event.Id).Distinct().Count());
        Assert.Equal(Seed.EventTypes.Count, Seed.EventTypes.Select(type => type.Name).Distinct().Count());
        Assert.Equal(Seed.Assignments.Count, Seed.Assignments.Select(item => (item.GroupId, item.UserId, item.Responsibility)).Distinct().Count());
        Assert.All(Seed.Attendees.SelectMany(attendee => attendee.Courses).GroupBy(course => (course.AttendeeId, course.CourseId)), group => Assert.Single(group));
    }

    [Fact]
    public void EventsAreUtcAndWellFormed()
    {
        Assert.All(Seed.Events, @event =>
        {
            Assert.Equal(DateTimeKind.Utc, @event.StartsAt.Kind);
            Assert.True(@event.EndsAt >= @event.StartsAt);
            Assert.InRange(@event.Title.Length, 1, 150);
            Assert.False(string.IsNullOrWhiteSpace(@event.Description));
        });
    }
}

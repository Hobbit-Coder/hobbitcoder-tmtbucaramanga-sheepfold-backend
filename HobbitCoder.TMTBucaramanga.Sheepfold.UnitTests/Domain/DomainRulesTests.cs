using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Utils;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.UnitTests.Domain;

public class DomainRulesTests
{
    private static Generation Generation(GenerationProfile profile) => new() { Name = profile.ToString(), Profile = profile };

    /// <summary>§9: PULSO exige tutor legal; las demás generaciones, contacto de emergencia.</summary>
    [Theory]
    [InlineData(GenerationProfile.Pulso, ContactKind.LegalGuardian)]
    [InlineData(GenerationProfile.Eco, ContactKind.EmergencyContact)]
    [InlineData(GenerationProfile.Legado1To5, ContactKind.EmergencyContact)]
    [InlineData(GenerationProfile.Legado6To10, ContactKind.EmergencyContact)]
    [InlineData(GenerationProfile.Other, ContactKind.EmergencyContact)]
    public void RequiredContact_DependsOnTheGeneration(GenerationProfile profile, ContactKind expected)
    {
        Assert.Equal(expected, Generation(profile).RequiredContactKind);
    }

    /// <summary>§8 y decisión P03: etapas que corresponden a cada generación.</summary>
    [Theory]
    [InlineData(GenerationProfile.Pulso, EducationalStage.Grade6, true)]
    [InlineData(GenerationProfile.Pulso, EducationalStage.Grade9, false)]
    [InlineData(GenerationProfile.Eco, EducationalStage.Grade12, true)]
    [InlineData(GenerationProfile.Eco, EducationalStage.Semester1, false)]
    [InlineData(GenerationProfile.Legado1To5, EducationalStage.Semester5, true)]
    [InlineData(GenerationProfile.Legado1To5, EducationalStage.Semester6, false)]
    [InlineData(GenerationProfile.Legado6To10, EducationalStage.FirstJob, true)]
    [InlineData(GenerationProfile.Legado6To10, EducationalStage.Grade11, false)]
    [InlineData(GenerationProfile.Other, EducationalStage.Grade6, true)]
    public void EducationalStage_MatchesTheGenerationProfile(GenerationProfile profile, EducationalStage stage, bool expected)
    {
        Assert.Equal(expected, Generation(profile).MatchesStage(stage));
    }

    /// <summary>Finalizar no cierra el ciclo: un finalizado o cancelado puede volver a publicarse; un borrador no se finaliza.</summary>
    [Fact]
    public void EventTransitions_FollowTheLifecycle()
    {
        Assert.Equal([EventStatus.Published, EventStatus.Cancelled], EventTransitions.From(EventStatus.Draft));
        Assert.Equal([EventStatus.Cancelled, EventStatus.Finished], EventTransitions.From(EventStatus.Published));
        Assert.Equal([EventStatus.Published], EventTransitions.From(EventStatus.Finished));
    }

    [Fact]
    public void Membership_IsActiveOnlyWithinItsPeriod()
    {
        var start = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var membership = new GroupMembership { AttendeeId = Guid.NewGuid(), GroupId = Guid.NewGuid(), StartedAt = start, EndedAt = start.AddMonths(3) };

        Assert.False(membership.WasActiveAt(start.AddDays(-1)));
        Assert.True(membership.WasActiveAt(start.AddDays(10)));
        Assert.False(membership.WasActiveAt(start.AddMonths(3)));
    }

    [Theory]
    [InlineData("2008-10-09", "2026-10-08", "2026-10-09")]
    [InlineData("2008-10-08", "2026-10-08", "2026-10-08")]
    [InlineData("2008-01-02", "2026-10-08", "2027-01-02")]
    [InlineData("2004-02-29", "2026-10-08", "2027-02-28")]
    public void NextBirthday_IsADateWithoutTime(string birthDate, string today, string expected)
    {
        var culture = System.Globalization.CultureInfo.InvariantCulture;

        Assert.Equal(DateOnly.Parse(expected, culture), LocalCalendar.NextBirthday(DateOnly.Parse(birthDate, culture), DateOnly.Parse(today, culture)));
    }

    [Fact]
    public void Age_CountsCompletedYears()
    {
        var attendee = new Attendee { FirstName = "Ana", LastName = "Pérez", BirthDate = new DateOnly(2008, 10, 9) };

        Assert.Equal(17, attendee.AgeOn(new DateOnly(2026, 10, 8)));
        Assert.Equal(18, attendee.AgeOn(new DateOnly(2026, 10, 9)));
    }
}

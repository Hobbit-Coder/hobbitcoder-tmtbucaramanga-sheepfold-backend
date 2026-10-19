using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using ValidationException = HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions.ValidationException;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;

public static class AttendeeCommandHelpers
{
    public static void Apply(Attendee attendee, IAttendeeData data)
    {
        attendee.FirstName = data.FirstName.Trim();
        attendee.LastName = data.LastName.Trim();
        attendee.Gender = data.Gender;
        attendee.PhoneNumber = string.IsNullOrWhiteSpace(data.PhoneNumber) ? null : data.PhoneNumber.Trim();
        attendee.Instagram = string.IsNullOrWhiteSpace(data.Instagram) ? null : data.Instagram.Trim();
        attendee.BirthDate = data.BirthDate;
        attendee.EducationalStage = data.EducationalStage;
        attendee.IsServer = data.IsServer;
    }

    public static AttendeeContact ToEntity(Guid attendeeId, ContactData contact) => new()
    {
        AttendeeId = attendeeId,
        Kind = contact.Kind,
        FullName = contact.FullName.Trim(),
        Relationship = contact.Relationship.Trim(),
        PhoneNumber = contact.PhoneNumber.Trim()
    };

    public static AttendeeSnapshot Snapshot(Attendee attendee) => new AttendeeSnapshot
    {
        FirstName = attendee.FirstName,
        LastName = attendee.LastName,
        Gender = attendee.Gender,
        PhoneNumber = attendee.PhoneNumber,
        Instagram = attendee.Instagram,
        BirthDate = attendee.BirthDate,
        EducationalStage = attendee.EducationalStage,
        IsServer = attendee.IsServer,
        IsActive = attendee.IsActive
    };

    /// <summary>PULSO exige al menos un tutor legal; ECO y LEGADO, al menos un contacto de emergencia.</summary>
    public static void EnsureRequiredContact(Generation generation, IEnumerable<ContactKind> kinds)
    {
        if (!kinds.Contains(generation.RequiredContactKind))
        {
            var message = generation.RequiredContactKind == ContactKind.LegalGuardian
                ? "Add at least one legal guardian."
                : "Add at least one emergency contact.";
            throw ValidationException.For("Contacts", message);
        }
    }
}

using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.Property(group => group.Name).HasMaxLength(60).IsRequired();

        // Clave candidata: año + generación + nombre.
        builder.HasIndex(group => new { group.Year, group.GenerationId, group.Name }).IsUnique();
        builder.HasIndex(group => group.IsActive);

        builder.HasOne(group => group.Generation)
            .WithMany()
            .HasForeignKey(group => group.GenerationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Tabla intermedia grupo–usuario con la responsabilidad como parte de la clave.</summary>
public class GroupAssignmentConfiguration : IEntityTypeConfiguration<GroupAssignment>
{
    public void Configure(EntityTypeBuilder<GroupAssignment> builder)
    {
        builder.HasKey(assignment => new { assignment.GroupId, assignment.UserId, assignment.Responsibility });
        builder.HasIndex(assignment => assignment.UserId);

        builder.HasOne(assignment => assignment.Group)
            .WithMany(group => group.Assignments)
            .HasForeignKey(assignment => assignment.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Historial de grupos del asistente. El índice único filtrado garantiza a lo sumo una pertenencia vigente por
/// asistente (§28.2). El filtro usa comillas dobles, válidas en PostgreSQL y en SQL Server (QUOTED_IDENTIFIER).
/// </summary>
public class GroupMembershipConfiguration : IEntityTypeConfiguration<GroupMembership>
{
    public void Configure(EntityTypeBuilder<GroupMembership> builder)
    {
        builder.HasIndex(membership => membership.AttendeeId)
            .IsUnique()
            .HasFilter("\"EndedAt\" IS NULL")
            .HasDatabaseName("IX_GroupMemberships_AttendeeId_Current");

        builder.HasIndex(membership => new { membership.AttendeeId, membership.StartedAt }).IsUnique();
        builder.HasIndex(membership => membership.GroupId);

        builder.ToTable(table => table.HasCheckConstraint(
            "CK_GroupMemberships_Period", "\"EndedAt\" IS NULL OR \"EndedAt\" >= \"StartedAt\""));

        builder.HasOne(membership => membership.Attendee)
            .WithMany(attendee => attendee.Memberships)
            .HasForeignKey(membership => membership.AttendeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(membership => membership.Group)
            .WithMany(group => group.Memberships)
            .HasForeignKey(membership => membership.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(membership => membership.IsCurrent);
    }
}

public class AttendeeConfiguration : IEntityTypeConfiguration<Attendee>
{
    public void Configure(EntityTypeBuilder<Attendee> builder)
    {
        builder.Property(attendee => attendee.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(attendee => attendee.LastName).HasMaxLength(100).IsRequired();
        builder.Property(attendee => attendee.PhoneNumber).HasMaxLength(30);
        builder.Property(attendee => attendee.Instagram).HasMaxLength(60);
        builder.HasIndex(attendee => new { attendee.LastName, attendee.FirstName });
        builder.Ignore(attendee => attendee.FullName);
    }
}

public class AttendeeContactConfiguration : IEntityTypeConfiguration<AttendeeContact>
{
    public void Configure(EntityTypeBuilder<AttendeeContact> builder)
    {
        builder.Property(contact => contact.FullName).HasMaxLength(150).IsRequired();
        builder.Property(contact => contact.Relationship).HasMaxLength(60).IsRequired();
        builder.Property(contact => contact.PhoneNumber).HasMaxLength(30).IsRequired();

        builder.HasOne(contact => contact.Attendee)
            .WithMany(attendee => attendee.Contacts)
            .HasForeignKey(contact => contact.AttendeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AttendeeCourseConfiguration : IEntityTypeConfiguration<AttendeeCourse>
{
    public void Configure(EntityTypeBuilder<AttendeeCourse> builder)
    {
        // La fecha de finalización solo corresponde a cursos finalizados (§10).
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_AttendeeCourses_FinishedOn", "\"FinishedOn\" IS NULL OR \"Status\" = 'Finished'"));

        // Clave candidata (asistente, curso): cada curso aparece una vez en la formación del asistente.
        builder.HasIndex(course => new { course.AttendeeId, course.CourseId }).IsUnique();

        builder.HasOne(course => course.Attendee)
            .WithMany(attendee => attendee.Courses)
            .HasForeignKey(course => course.AttendeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(course => course.Course)
            .WithMany()
            .HasForeignKey(course => course.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

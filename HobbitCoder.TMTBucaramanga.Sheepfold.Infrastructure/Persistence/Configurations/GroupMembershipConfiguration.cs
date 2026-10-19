using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

/// <summary>
/// Historial de grupos del asistente. El índice único filtrado garantiza a lo sumo una pertenencia vigente por
/// asistente. El filtro usa comillas dobles, válidas en PostgreSQL y en SQL Server (QUOTED_IDENTIFIER).
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

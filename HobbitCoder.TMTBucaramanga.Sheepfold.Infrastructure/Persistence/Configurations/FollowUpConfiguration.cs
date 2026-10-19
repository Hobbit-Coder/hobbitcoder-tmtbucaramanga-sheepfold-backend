using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

public class FollowUpConfiguration : IEntityTypeConfiguration<FollowUp>
{
    /// <summary>
    ///     Configura el mapeo de la entidad &lt;see cref="FollowUp"/&gt;.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Exactamente una persona atendida: un asistente o un usuario.
    /// </para>
    /// <para>
    ///     La hora programada solo existe con fecha programada.
    /// </para>
    /// <para>
    ///     Un realizado tiene fecha, lugar, tema, descripción y compromisos.
    /// </para>
    /// <para>
    ///     Un grupo con historial de asistentes no se elimina definitivamente, así que la referencia no queda huérfana.
    /// </para>
    /// </remarks>
    public void Configure(EntityTypeBuilder<FollowUp> builder)
    {
        builder.Property(followUp => followUp.Location).HasMaxLength(150);
        builder.Property(followUp => followUp.Description).HasMaxLength(4000);
        builder.Property(followUp => followUp.Commitments).HasMaxLength(4000);
        builder.Property(followUp => followUp.Observations).HasMaxLength(4000);
        builder.HasIndex(followUp => followUp.CreatedBy);
        builder.HasIndex(followUp => followUp.SubjectUserId);
        builder.Ignore(followUp => followUp.AuthorUserId);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_FollowUps_Subject",
                "(\"SubjectAttendeeId\" IS NULL AND \"SubjectUserId\" IS NOT NULL) OR (\"SubjectAttendeeId\" IS NOT NULL AND \"SubjectUserId\" IS NULL)");

            table.HasCheckConstraint("CK_FollowUps_ScheduledTime", "\"ScheduledTime\" IS NULL OR \"ScheduledDate\" IS NOT NULL");

            table.HasCheckConstraint("CK_FollowUps_Done",
                "\"Status\" <> 'Done' OR (\"MeetingDate\" IS NOT NULL AND \"Location\" IS NOT NULL AND \"TopicId\" IS NOT NULL AND \"Description\" IS NOT NULL AND \"Commitments\" IS NOT NULL)");
        });

        builder.HasOne(followUp => followUp.SubjectAttendee).WithMany()
            .HasForeignKey(followUp => followUp.SubjectAttendeeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(followUp => followUp.Topic).WithMany()
            .HasForeignKey(followUp => followUp.TopicId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(followUp => followUp.Group).WithMany()
            .HasForeignKey(followUp => followUp.GroupId).OnDelete(DeleteBehavior.Restrict);
    }
}

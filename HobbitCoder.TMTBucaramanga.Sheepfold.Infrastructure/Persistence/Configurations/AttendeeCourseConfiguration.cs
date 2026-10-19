using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

public class AttendeeCourseConfiguration : IEntityTypeConfiguration<AttendeeCourse>
{
    /// <summary>
    ///     Configura el mapeo de la entidad &lt;see cref="AttendeeCourse"/&gt;.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     La fecha de finalización solo corresponde a cursos finalizados.
    /// </para>
    /// <para>
    ///     Clave candidata (asistente, curso): cada curso aparece una vez en la formación del asistente.
    /// </para>
    /// </remarks>
    public void Configure(EntityTypeBuilder<AttendeeCourse> builder)
    {
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_AttendeeCourses_FinishedOn", "\"FinishedOn\" IS NULL OR \"Status\" = 'Finished'"));

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

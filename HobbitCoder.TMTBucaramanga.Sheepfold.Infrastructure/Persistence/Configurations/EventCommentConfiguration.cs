using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

public class EventCommentConfiguration : IEntityTypeConfiguration<EventComment>
{
    /// <summary>
    ///     Configura el mapeo de la entidad &lt;see cref="EventComment"/&gt;.
    /// </summary>
    /// <remarks>
    ///     Sin cascada en la autorreferencia (SQL Server no admite varias rutas de cascada); al borrar el evento se
    ///     borran juntos comentarios y respuestas.
    /// </remarks>
    public void Configure(EntityTypeBuilder<EventComment> builder)
    {
        builder.Property(comment => comment.Body).HasMaxLength(4000).IsRequired();
        builder.HasIndex(comment => new { comment.EventId, comment.CreatedDate });

        builder.HasOne(comment => comment.Event).WithMany(@event => @event.Comments)
            .HasForeignKey(comment => comment.EventId).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(comment => comment.ParentComment).WithMany(comment => comment.Replies)
            .HasForeignKey(comment => comment.ParentCommentId).OnDelete(DeleteBehavior.NoAction);
    }
}

using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

public class EventCommentAttachmentConfiguration : IEntityTypeConfiguration<EventCommentAttachment>
{
    public void Configure(EntityTypeBuilder<EventCommentAttachment> builder)
    {
        StoredFileConfiguration.Configure(builder);

        builder.HasOne(attachment => attachment.Comment).WithMany(comment => comment.Attachments)
            .HasForeignKey(attachment => attachment.CommentId).OnDelete(DeleteBehavior.Cascade);
    }
}

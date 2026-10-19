using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

public class EventAttachmentConfiguration : IEntityTypeConfiguration<EventAttachment>
{
    public void Configure(EntityTypeBuilder<EventAttachment> builder)
    {
        StoredFileConfiguration.Configure(builder);

        builder.HasOne(attachment => attachment.Event).WithMany(@event => @event.Attachments)
            .HasForeignKey(attachment => attachment.EventId).OnDelete(DeleteBehavior.Cascade);
    }
}

using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

public class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        builder.Property(todoItem => todoItem.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(todoItem => todoItem.Description)
            .HasMaxLength(1000)
            .IsRequired();
    }
}

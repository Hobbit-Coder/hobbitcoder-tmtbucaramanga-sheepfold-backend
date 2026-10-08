using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

public class TodoListConfiguration : IEntityTypeConfiguration<TodoList>
{
    public void Configure(EntityTypeBuilder<TodoList> builder)
    {
        builder.Property(todoList => todoList.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasMany(todoList => todoList.TodoItems)
            .WithOne(todoItem => todoItem.TodoList)
            .HasForeignKey(todoItem => todoItem.TodoListId);
    }
}

using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

public class TodoItem: SoftDeleteableEntity
{
    public required Guid TodoListId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required TodoStatus Status { get; set; } = TodoStatus.Pending;

    public virtual TodoList TodoList { get; set; } = null!;
}

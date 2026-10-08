using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.TodoLists.Common;

public class TodoItemResponse
{
    public required Guid Id { get; set; }
    public required Guid TodoListId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required TodoStatus Status { get; set; }
}

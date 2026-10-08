using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.TodoLists.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.TodoLists.Queries;

public class GetTodoItemsQuery : IRequest<List<TodoItemResponse>>
{
    public Guid? TodoListId { get; set; }
    public TodoStatus? Status { get; set; }
}

public class GetTodoItemsQueryHandler : IRequestHandler<GetTodoItemsQuery, List<TodoItemResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetTodoItemsQueryHandler(IApplicationDbContext context)
    {
        _dbContext = context;
    }

    public async Task<List<TodoItemResponse>> Handle(GetTodoItemsQuery request, CancellationToken cancellationToken = default)
    {
        var todoItems = await _dbContext.TodoItems
            .Where(todoItem => request.TodoListId == null || todoItem.TodoListId == request.TodoListId)
            .Where(todoItem => request.Status == null || todoItem.Status == request.Status)
            .Select(todoItem => new TodoItemResponse
            {
                Id = todoItem.Id,
                TodoListId = todoItem.TodoListId,
                Title = todoItem.Title,
                Description = todoItem.Description,
                Status = todoItem.Status
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return todoItems;
    }
}

public class GetTodoItemsQueryValidator : AbstractValidator<GetTodoItemsQuery>
{
    public GetTodoItemsQueryValidator()
    {
        RuleFor(query => query.TodoListId)
            .NotEmpty().When(query => query.TodoListId.HasValue).WithMessage("Invalid TodoListId");

        RuleFor(query => query.Status)
            .IsInEnum().When(query => query.Status.HasValue).WithMessage("Invalid status");
    }
}

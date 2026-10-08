using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.TodoLists.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.TodoLists.Queries;

public class GetTodoListByIdQuery : IRequest<TodoListResponse>
{
    public required Guid Id { get; set; }
}

public class GetTodoListByIdQueryHandler : IRequestHandler<GetTodoListByIdQuery, TodoListResponse>
{
    private readonly IApplicationDbContext _dbContext;

    public GetTodoListByIdQueryHandler(IApplicationDbContext context)
    {
        _dbContext = context;
    }

    public async Task<TodoListResponse> Handle(GetTodoListByIdQuery request, CancellationToken cancellationToken = default)
    {
        var todoList = await _dbContext.TodoLists
            .Where(todoList => todoList.Id == request.Id)
            .Select(todoList => new TodoListResponse
            {
                Id = todoList.Id,
                Title = todoList.Title,
                Status = todoList.Status,
                TodoItems = todoList.TodoItems
                    .Select(todoItem => new TodoItemResponse
                    {
                        Id = todoItem.Id,
                        TodoListId = todoItem.TodoListId,
                        Title = todoItem.Title,
                        Description = todoItem.Description,
                        Status = todoItem.Status
                    }).ToList()
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        return todoList ?? throw new NotFoundException(nameof(TodoList), request.Id);
    }
}

public class GetTodoListByIdQueryValidator : AbstractValidator<GetTodoListByIdQuery>
{
    public GetTodoListByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty().WithMessage("Id is required");
    }
}

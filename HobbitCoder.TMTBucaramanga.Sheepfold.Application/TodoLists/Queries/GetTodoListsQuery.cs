using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.TodoLists.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.TodoLists.Queries;

public class GetTodoListsQuery: IRequest<List<TodoListResponse>>
{
    public TodoStatus? Status { get; set; }
}

public class GetTodoListsQueryHandler : IRequestHandler<GetTodoListsQuery, List<TodoListResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetTodoListsQueryHandler(IApplicationDbContext context)
    {
        _dbContext = context;
    }

    public async Task<List<TodoListResponse>> Handle(GetTodoListsQuery request, CancellationToken cancellationToken = default)
    {
        var todoLists = await _dbContext.TodoLists
            .Where(todoList => request.Status == null || todoList.Status == request.Status)
            .Select(todoList => new TodoListResponse
            {
                Id = todoList.Id,
                Title = todoList.Title,
                Status = todoList.Status,
                TodoItems = todoList.TodoItems.Select(todoItem => new TodoItemResponse
                {
                    Id = todoItem.Id,
                    TodoListId = todoItem.TodoListId,
                    Title = todoItem.Title,
                    Description = todoItem.Description,
                    Status = todoItem.Status
                }).ToList()
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return todoLists;
    }
}

public class GetTodoListsQueryValidator : AbstractValidator<GetTodoListsQuery>
{
    public GetTodoListsQueryValidator()
    {
        RuleFor(query => query.Status)
            .IsInEnum().When(query => query.Status.HasValue).WithMessage("Invalid status");
    }
}

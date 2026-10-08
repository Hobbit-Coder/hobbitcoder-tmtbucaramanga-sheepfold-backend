using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.TodoLists.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.TodoLists.Queries;

public class GetTodoItemByIdQuery : IRequest<TodoItemResponse>
{
    public required Guid Id { get; set; }
}

public class GetTodoItemByIdQueryHandler : IRequestHandler<GetTodoItemByIdQuery, TodoItemResponse>
{
    private readonly IApplicationDbContext _dbContext;

    public GetTodoItemByIdQueryHandler(IApplicationDbContext context)
    {
        _dbContext = context;
    }

    public async Task<TodoItemResponse> Handle(GetTodoItemByIdQuery request, CancellationToken cancellationToken = default)
    {
        var todoItem = await _dbContext.TodoItems
            .Where(todoItem => todoItem.Id == request.Id)
            .Select(todoItem => new TodoItemResponse
            {
                Id = todoItem.Id,
                TodoListId = todoItem.TodoListId,
                Title = todoItem.Title,
                Description = todoItem.Description,
                Status = todoItem.Status
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        return todoItem ?? throw new NotFoundException(nameof(TodoItem), request.Id);
    }
}

public class GetTodoItemByIdQueryValidator : AbstractValidator<GetTodoItemByIdQuery>
{
    public GetTodoItemByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty().WithMessage("Id is required");
    }
}

using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.TodoLists.Commands;

public class CreateTodoListCommand : IRequest<Guid>
{
    public required string Title { get; set; }
    public required TodoStatus Status { get; set; }
    public List<CreateTodoListItem>? Items { get; set; } = [];
}

/// <summary>
/// Item que se crea junto con su lista. No lleva <c>TodoListId</c>: la lista todavía no existe.
/// </summary>
public class CreateTodoListItem
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required TodoStatus Status { get; set; }
}

public class CreateTodoListCommandHandler : IRequestHandler<CreateTodoListCommand, Guid>
{
    private readonly IApplicationDbContext _dbContext;

    public CreateTodoListCommandHandler(IApplicationDbContext context)
    {
        _dbContext = context;
    }

    public async Task<Guid> Handle(CreateTodoListCommand request, CancellationToken cancellationToken = default)
    {
        var todoList = new TodoList
        {
            Title = request.Title,
            Status = request.Status
        };

        _dbContext.TodoLists.Add(todoList);

        foreach (var item in request.Items ?? [])
        {
            var todoItem = new TodoItem
            {
                TodoListId = todoList.Id,
                Title = item.Title,
                Description = item.Description,
                Status = item.Status
            };

            _dbContext.TodoItems.Add(todoItem);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return todoList.Id;
    }
}

public class CreateTodoListCommandValidator : AbstractValidator<CreateTodoListCommand>
{
    public CreateTodoListCommandValidator()
    {
        RuleFor(todoListRequest => todoListRequest.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(todoListRequest => todoListRequest.Status)
            .IsInEnum().WithMessage("Invalid status");

        RuleForEach(todoListRequest => todoListRequest.Items)
            .SetValidator(new CreateTodoListItemValidator());
    }
}

public class CreateTodoListItemValidator : AbstractValidator<CreateTodoListItem>
{
    public CreateTodoListItemValidator()
    {
        RuleFor(todoItemRequest => todoItemRequest.Title).NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(todoItemRequest => todoItemRequest.Description).NotEmpty().WithMessage("Description is required")
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");

        RuleFor(todoItemRequest => todoItemRequest.Status).IsInEnum()
            .WithMessage("Invalid status");
    }
}

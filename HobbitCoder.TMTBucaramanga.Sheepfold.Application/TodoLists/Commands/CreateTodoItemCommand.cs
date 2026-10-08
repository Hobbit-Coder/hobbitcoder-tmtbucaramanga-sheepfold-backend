using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.TodoLists.Commands;

public class CreateTodoItemCommand: IRequest<Guid>
{
    public required Guid TodoListId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required TodoStatus Status { get; set; }
}

public class CreateTodoItemCommandHandler : IRequestHandler<CreateTodoItemCommand, Guid>
{
    private readonly IApplicationDbContext _dbContext;

    public CreateTodoItemCommandHandler(IApplicationDbContext context)
    {
        _dbContext = context;
    }

    public async Task<Guid> Handle(CreateTodoItemCommand request, CancellationToken cancellationToken = default)
    {
        if (!await _dbContext.TodoLists.AnyAsync(todoList => todoList.Id == request.TodoListId, cancellationToken))
        {
            throw new NotFoundException(nameof(TodoList), request.TodoListId);
        }

        var todoItem = new TodoItem
        {
            TodoListId = request.TodoListId,
            Title = request.Title,
            Description = request.Description,
            Status = request.Status
        };

        _dbContext.TodoItems.Add(todoItem);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return todoItem.Id;
    }
}

public class CreateTodoItemCommandValidator : AbstractValidator<CreateTodoItemCommand>
{
    public CreateTodoItemCommandValidator()
    {
        RuleFor(todoItemRequest => todoItemRequest.TodoListId).NotEmpty()
            .WithMessage("TodoListId is required");

        RuleFor(todoItemRequest => todoItemRequest.Title).NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(todoItemRequest => todoItemRequest.Description).NotEmpty().WithMessage("Description is required")
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");

        RuleFor(todoItemRequest => todoItemRequest.Status).IsInEnum()
            .WithMessage("Invalid status");
    }
}

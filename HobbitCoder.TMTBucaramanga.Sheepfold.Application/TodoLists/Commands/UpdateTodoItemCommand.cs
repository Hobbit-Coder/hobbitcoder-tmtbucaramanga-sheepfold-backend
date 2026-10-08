using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.TodoLists.Notifications;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.TodoLists.Commands;

public class UpdateTodoItemCommand : IRequest
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required TodoStatus Status { get; set; }
}

public class UpdateTodoItemCommandHandler : IRequestHandler<UpdateTodoItemCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;

    public UpdateTodoItemCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IMediator mediator)
    {
        _dbContext = context;
        _currentUserService = currentUserService;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(UpdateTodoItemCommand request, CancellationToken cancellationToken = default)
    {
        var todoItem = await _dbContext.TodoItems
            .FirstOrDefaultAsync(todoItem => todoItem.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(TodoItem), request.Id);

        var wasCompleted = todoItem.Status == TodoStatus.Completed;

        todoItem.Title = request.Title;
        todoItem.Description = request.Description;
        todoItem.Status = request.Status;

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (!wasCompleted && todoItem.Status == TodoStatus.Completed)
        {
            await _mediator.Publish(new TodoItemCompletedNotification
            {
                TodoItemId = todoItem.Id,
                TodoListId = todoItem.TodoListId,
                Title = todoItem.Title,
                CompletedBy = _currentUserService.UserId,
                CompletedAt = todoItem.UpdatedDate ?? DateTime.UtcNow
            }, cancellationToken);
        }

        return Unit.Value;
    }
}

public class UpdateTodoItemCommandValidator : AbstractValidator<UpdateTodoItemCommand>
{
    public UpdateTodoItemCommandValidator()
    {
        RuleFor(todoItemRequest => todoItemRequest.Id).NotEmpty()
            .WithMessage("Id is required");

        RuleFor(todoItemRequest => todoItemRequest.Title).NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(todoItemRequest => todoItemRequest.Description).NotEmpty().WithMessage("Description is required")
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");

        RuleFor(todoItemRequest => todoItemRequest.Status).IsInEnum()
            .WithMessage("Invalid status");
    }
}

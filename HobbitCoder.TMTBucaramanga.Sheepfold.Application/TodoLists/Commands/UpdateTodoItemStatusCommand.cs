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

public class UpdateTodoItemStatusCommand : IRequest
{
    public required Guid Id { get; set; }
    public required TodoStatus Status { get; set; }
}

public class UpdateTodoItemStatusCommandHandler : IRequestHandler<UpdateTodoItemStatusCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;

    public UpdateTodoItemStatusCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IMediator mediator)
    {
        _dbContext = context;
        _currentUserService = currentUserService;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(UpdateTodoItemStatusCommand request, CancellationToken cancellationToken = default)
    {
        var todoItem = await _dbContext.TodoItems
            .FirstOrDefaultAsync(todoItem => todoItem.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(TodoItem), request.Id);

        var wasCompleted = todoItem.Status == TodoStatus.Completed;

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

public class UpdateTodoItemStatusCommandValidator : AbstractValidator<UpdateTodoItemStatusCommand>
{
    public UpdateTodoItemStatusCommandValidator()
    {
        RuleFor(todoItemRequest => todoItemRequest.Id).NotEmpty()
            .WithMessage("Id is required");

        RuleFor(todoItemRequest => todoItemRequest.Status).IsInEnum()
            .WithMessage("Invalid status");
    }
}

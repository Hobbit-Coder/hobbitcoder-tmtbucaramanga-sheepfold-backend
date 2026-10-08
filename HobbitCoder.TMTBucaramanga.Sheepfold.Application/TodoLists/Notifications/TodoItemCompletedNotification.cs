using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.TodoLists.Notifications;

/// <summary>
/// Se publica cuando un item pasa a <see cref="TodoStatus.Completed"/>,
/// después de que el cambio quedó guardado.
/// </summary>
public class TodoItemCompletedNotification : INotification
{
    public required Guid TodoItemId { get; set; }
    public required Guid TodoListId { get; set; }
    public required string Title { get; set; }
    public required string CompletedBy { get; set; }
    public required DateTime CompletedAt { get; set; }
}

public class TodoItemCompletedNotificationHandler : INotificationHandler<TodoItemCompletedNotification>
{
    private readonly ILogger<TodoItemCompletedNotificationHandler> _logger;

    public TodoItemCompletedNotificationHandler(ILogger<TodoItemCompletedNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(TodoItemCompletedNotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        _logger.LogInformation("TodoItem completado. TodoItemId: {TodoItemId}, TodoListId: {TodoListId}, Title: {Title}, UserId: {UserId}, CompletedAt: {CompletedAt}",
            notification.TodoItemId, notification.TodoListId, notification.Title, notification.CompletedBy, notification.CompletedAt);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Marca la lista como completada cuando todos sus items activos quedan completados.
/// </summary>
public class CompleteTodoListOnTodoItemCompletedHandler : INotificationHandler<TodoItemCompletedNotification>
{
    private readonly IApplicationDbContext _dbContext;

    public CompleteTodoListOnTodoItemCompletedHandler(IApplicationDbContext context)
    {
        _dbContext = context;
    }

    public async Task Handle(TodoItemCompletedNotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var hasPendingItems = await _dbContext.TodoItems
            .AnyAsync(todoItem => todoItem.TodoListId == notification.TodoListId
                && todoItem.Status != TodoStatus.Completed, cancellationToken);

        if (hasPendingItems)
        {
            return;
        }

        var todoList = await _dbContext.TodoLists
            .FirstOrDefaultAsync(todoList => todoList.Id == notification.TodoListId
                && todoList.Status != TodoStatus.Completed, cancellationToken);

        if (todoList is null)
        {
            return;
        }

        todoList.Status = TodoStatus.Completed;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

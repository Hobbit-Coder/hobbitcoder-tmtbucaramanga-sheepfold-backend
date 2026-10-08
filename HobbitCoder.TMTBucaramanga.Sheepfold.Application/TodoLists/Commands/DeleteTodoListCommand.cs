using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.TodoLists.Commands;

public class DeleteTodoListCommand : IRequest
{
    public required Guid Id { get; set; }
}

public class DeleteTodoListCommandHandler : IRequestHandler<DeleteTodoListCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;

    public DeleteTodoListCommandHandler(IApplicationDbContext context)
    {
        _dbContext = context;
    }

    public async Task<Unit> Handle(DeleteTodoListCommand request, CancellationToken cancellationToken = default)
    {
        var todoList = await _dbContext.TodoLists
            .Include(todoList => todoList.TodoItems)
            .FirstOrDefaultAsync(todoList => todoList.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(TodoList), request.Id);

        _dbContext.TodoItems.RemoveRange(todoList.TodoItems);
        _dbContext.TodoLists.Remove(todoList);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public class DeleteTodoListCommandValidator : AbstractValidator<DeleteTodoListCommand>
{
    public DeleteTodoListCommandValidator()
    {
        RuleFor(todoListRequest => todoListRequest.Id)
            .NotEmpty().WithMessage("Id is required");
    }
}

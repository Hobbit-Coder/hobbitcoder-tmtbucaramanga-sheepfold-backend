using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.TodoLists.Commands;

public class DeleteTodoItemCommand : IRequest
{
    public required Guid Id { get; set; }
}

public class DeleteTodoItemCommandHandler : IRequestHandler<DeleteTodoItemCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;

    public DeleteTodoItemCommandHandler(IApplicationDbContext context)
    {
        _dbContext = context;
    }

    public async Task<Unit> Handle(DeleteTodoItemCommand request, CancellationToken cancellationToken = default)
    {
        var todoItem = await _dbContext.TodoItems
            .FirstOrDefaultAsync(todoItem => todoItem.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(TodoItem), request.Id);

        _dbContext.TodoItems.Remove(todoItem);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public class DeleteTodoItemCommandValidator : AbstractValidator<DeleteTodoItemCommand>
{
    public DeleteTodoItemCommandValidator()
    {
        RuleFor(todoItemRequest => todoItemRequest.Id).NotEmpty()
            .WithMessage("Id is required");
    }
}

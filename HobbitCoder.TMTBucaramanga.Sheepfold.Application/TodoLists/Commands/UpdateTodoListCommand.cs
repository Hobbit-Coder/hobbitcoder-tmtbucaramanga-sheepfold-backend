using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.TodoLists.Commands;

public class UpdateTodoListCommand : IRequest
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public required TodoStatus Status { get; set; }
}

public class UpdateTodoListCommandHandler : IRequestHandler<UpdateTodoListCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;

    public UpdateTodoListCommandHandler(IApplicationDbContext context)
    {
        _dbContext = context;
    }

    public async Task<Unit> Handle(UpdateTodoListCommand request, CancellationToken cancellationToken = default)
    {
        var todoList = await _dbContext.TodoLists
            .FirstOrDefaultAsync(todoList => todoList.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(TodoList), request.Id);

        todoList.Title = request.Title;
        todoList.Status = request.Status;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public class UpdateTodoListCommandValidator : AbstractValidator<UpdateTodoListCommand>
{
    public UpdateTodoListCommandValidator()
    {
        RuleFor(todoListRequest => todoListRequest.Id)
            .NotEmpty().WithMessage("Id is required");

        RuleFor(todoListRequest => todoListRequest.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(todoListRequest => todoListRequest.Status)
            .IsInEnum().WithMessage("Invalid status");
    }
}

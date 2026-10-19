using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps.Queries;

/// <summary>
/// Detalle de un seguimiento. El contenido se entrega solo a quien puede leerlo; el confidencial, solo al
/// autor y al director. Cada consulta de contenido queda en la auditoría por tratarse de información sensible.
/// </summary>
public class GetFollowUpQuery : IRequest<FollowUpDetailResponse>
{
    public Guid Id { get; set; }
}

public class GetFollowUpQueryHandler : IRequestHandler<GetFollowUpQuery, FollowUpDetailResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly IUserDirectory _userDirectory;
    private readonly AuditTrail _audit;

    public GetFollowUpQueryHandler(
        IApplicationDbContext dbContext,
        MinistryAccess access,
        IUserDirectory userDirectory,
        AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _userDirectory = userDirectory;
        _audit = audit;
    }

    public async Task<FollowUpDetailResponse> Handle(GetFollowUpQuery request, CancellationToken cancellationToken = default)
    {
        FollowUpQueryHelpers.EnsurePastoralAccess(_access);
        var directory = await _userDirectory.GetUsersAsync(cancellationToken);
        var followUp = await (await _access.VisibleFollowUpsAsync(_dbContext.FollowUps, directory, cancellationToken))
            .Include(item => item.SubjectAttendee)
            .Include(item => item.Topic)
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(FollowUp), request.Id);

        var item = (await FollowUpQueryHelpers.ToListItemsAsync(_dbContext, _access, [followUp], directory, cancellationToken))[0];

        if (followUp.Status != FollowUpStatus.Done)
        {
            return new FollowUpDetailResponse { FollowUp = item, Content = null, ContentProtected = false };
        }

        if (!_access.CanReadFollowUpContent(followUp))
        {
            return new FollowUpDetailResponse { FollowUp = item, Content = null, ContentProtected = true };
        }

        _audit.Record(AuditModule.FollowUps, AuditActions.SensitiveRead, followUp.Id, $"Seguimiento / {item.Subject.FullName}", null, null);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new FollowUpDetailResponse
        {
            FollowUp = item,
            Content = new FollowUpContentResponse
{
    MeetingDate = followUp.MeetingDate!.Value,
    Location = followUp.Location!,
    TopicId = followUp.TopicId!.Value,
    TopicName = followUp.Topic!.Name,
    Description = followUp.Description!,
    Commitments = followUp.Commitments!,
    Observations = followUp.Observations
},
            ContentProtected = false
        };
    }
}

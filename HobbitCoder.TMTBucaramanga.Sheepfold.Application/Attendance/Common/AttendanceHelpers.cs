using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendance.Common;

public static class AttendanceHelpers
{
    /// <summary>
    /// El evento debe ser visible, requerir asistencia y estar publicado o finalizado: la asistencia sigue
    /// abierta para correcciones después de finalizar.
    /// </summary>
    public static async Task<Event> FindOpenEventAsync(IApplicationDbContext dbContext, MinistryAccess access, Guid eventId, CancellationToken cancellationToken)
    {
        var visible = await access.VisibleEventsAsync(dbContext.Events, cancellationToken);
        var @event = await visible.Include(item => item.Managers).Include(item => item.Groups).Include(item => item.Generations)
            .FirstOrDefaultAsync(item => item.Id == eventId, cancellationToken)
            ?? throw new NotFoundException(nameof(Event), eventId);

        if (!@event.RequiresAttendance || @event.Status is not (EventStatus.Published or EventStatus.Finished))
        {
            throw new ConflictException("This event does not have an open attendance record.");
        }

        return @event;
    }
}

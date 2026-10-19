using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Settings.Common;

public static class SettingsHelpers
{
    public static void EnsureAdminOrDirector(MinistryAccess access)
    {
        if (!access.IsAdmin && !access.IsDirector)
        {
            throw new ForbiddenAccessException();
        }
    }

    public static async Task<NotificationOptionsDto> LoadOptionsAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        new NotificationOptionsDto
        {
            Channels = await dbContext.NotificationChannelSettings.ToDictionaryAsync(item => item.Channel, item => item.IsEnabled, cancellationToken),
            Kinds = await dbContext.NotificationKindSettings.ToDictionaryAsync(item => item.Kind, item => item.IsEnabled, cancellationToken)
        };

    public static async Task<OperationalSettings> LoadSettingsAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        await dbContext.OperationalSettings.FirstOrDefaultAsync(settings => settings.Id == OperationalSettings.SingletonId, cancellationToken)
        ?? throw new NotFoundException(nameof(OperationalSettings), OperationalSettings.SingletonId);

    public static OperationalSettingsDto ToDto(OperationalSettings settings) => new()
    {
        LowAttendanceThresholdPercent = settings.LowAttendanceThresholdPercent,
        LowAttendanceWindowDays = settings.LowAttendanceWindowDays,
        FrequentLatenessThreshold = settings.FrequentLatenessThreshold,
        FrequentLatenessWindowDays = settings.FrequentLatenessWindowDays,
        UpcomingBirthdaysDays = settings.UpcomingBirthdaysDays,
        UpcomingEventNoticeHours = settings.UpcomingEventNoticeHours,
        PendingFollowUpReminderDays = settings.PendingFollowUpReminderDays
    };
}

using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;

public static class EventCollaborationHelpers
{
    public static PersonRef Author(IReadOnlyDictionary<Guid, DirectoryUser> directory, string userId) =>
        Guid.TryParse(userId, out var id) && directory.TryGetValue(id, out var user)
            ? new PersonRef { Id = user.Id, FullName = user.FullName, Role = user.Role }
            : new PersonRef { Id = Guid.Empty, FullName = directory.NameOf(userId), Role = null };
}

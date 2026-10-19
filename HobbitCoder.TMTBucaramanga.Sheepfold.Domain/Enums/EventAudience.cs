namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

/// <summary>Público del evento. "Por grupos" es el público <see cref="Groups"/>; los demás son generales.</summary>
public enum EventAudience
{
    AllMinistry = 1,
    DirectorsAndCoordinators = 2,
    DirectorsCoordinatorsAndLeaders = 3,
    PrimaryGroup = 4,
    PrimaryGroupAndServers = 5,
    Generations = 6,
    Groups = 7
}

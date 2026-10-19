namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;

public sealed class WhatsAppNotification
{
    public required string PhoneNumber { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
}

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;

public sealed class FileResponse
{
    public required Guid Id { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required long SizeBytes { get; set; }
    public required DateTime UploadedAt { get; set; }
    public required string UploadedBy { get; set; }
}

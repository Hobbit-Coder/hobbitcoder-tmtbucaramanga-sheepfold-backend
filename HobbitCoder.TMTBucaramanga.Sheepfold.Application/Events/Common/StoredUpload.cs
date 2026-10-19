namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;

public sealed class StoredUpload
{
    public required string ObjectKey { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required long SizeBytes { get; set; }
}

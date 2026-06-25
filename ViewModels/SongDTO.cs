namespace EchoNet.ViewModels;

public sealed class SongDTO
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Artist { get; set; }
    public required string Album { get; set; }
    public required TimeSpan Duration { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required string FormattedDuration { get; set; }
    public required string FormattedCreatedAt { get; set; }
    public required bool HasCoverArt { get; set; }
    public required string HostUrl { get; set; }   
}
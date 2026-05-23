namespace EchoNet.Services;

public interface ILibScannerService
{
    Task<bool> RunFirstTimeSetupAsync(IReadOnlyList<string> selectedFolders, CancellationToken cancellationToken = default);
    Task<bool> NeedsFirstTimeSetupAsync(CancellationToken cancellationToken = default);
}
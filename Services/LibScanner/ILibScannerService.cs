namespace EchoNet.Services;

public interface ILibScannerService
{
    public Task<bool> ScanFoldersAsync(IReadOnlyList<string> selectedFolders, CancellationToken cancellationToken = default);
}
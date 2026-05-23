namespace EchoNet.ViewModels;

public sealed record LibrarySyncResult(
    int FoldersScanned,
    int FilesScanned,
    int Added,
    int Updated,
    int Deleted,
    int SkippedFolders
);
using EchoNet.ViewModels;

namespace EchoNet.Models;

public class AppData
{
    public string Theme { get; set; } = "Crimson Shadow";
    public SongMetadata songMetadata { get; set; } = new SongMetadata{};
    public TimeSpan Position { get; set; }
    public int Volume { get; set; }
    public string ViewType { get; set; } = "list";
    public string SortType { get; set; } = "az";
}

public enum AppDataTarget
{
    Theme,
    SongMetadata,
    Position,
    Volume,
    ViewType,
    SortType
}

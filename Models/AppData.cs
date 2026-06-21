using EchoNet.ViewModels;

namespace EchoNet.Models;

public class AppData
{
    public string Theme { get; set; } = "Crimson Shadow";
    public Song song { get; set; } = new Song{};
    public TimeSpan Position { get; set; }
    public int Volume { get; set; }
    public string ViewType { get; set; } = "list";
    public string SortType { get; set; } = "az";
    public PlayerState playerState { get; set; } = new PlayerState{changeState = ChangeState.NoLoop, queueState = QueueState.AZ};
    public int ShuffleSeed { get; set; } = Random.Shared.Next(int.MinValue, int.MaxValue);
}

public enum AppDataTarget
{
    Theme,
    Song,
    Position,
    Volume,
    ViewType,
    SortType,
    PlayerState,
    ShuffleSeed
}

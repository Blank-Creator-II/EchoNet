using EchoNet.ViewModels;

namespace EchoNet.Models
{
    public class AppData
    {
        public string Theme { get; set; } = "Crimson Shadow";
        public SongMetadata songMetadata { get; set; } = new SongMetadata{};
        public TimeSpan Position { get; set; }
        public int Volume { get; set; }

        /* Future settings
        public bool boolean { get; set; } = true;

        public string text { get; set; } = "smth";

        public List<string> List { get; set; } = new();
        */
    }
}
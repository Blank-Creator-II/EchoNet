namespace EchoNet.Models;

public class Theme
{
    public string Name { get; set; } = "default";
    public string Mode { get; set; } = "dark";

    public string Background { get; set; } = "#050505";
    public string Surface { get; set; } = "#101010";
    public string SurfaceAlt { get; set; } = "#181818";

    public string Text { get; set; } = "#f5f5f5";
    public string MutedText { get; set; } = "#a1a1a1";

    public string Accent { get; set; } = "#facc15";
    public string AccentHover { get; set; } = "#eab308";
    public string AccentActive { get; set; } = "#ca8a04";

    public string Border { get; set; } = "#2a2a2a";
    public string TextOnAccent { get; set; } = "#0a0a0a";
    public string BorderHover { get; set; } = "#facc15";
    public string BorderFocus { get; set; } = "#facc15";
    public string FocusRing { get; set; } = "#facc15";

    public string ButtonSecondaryBg { get; set; } = "#1f1f1f";
    public string ButtonSecondaryHoverBg { get; set; } = "#2a2a2a";
    public string ButtonSecondaryText { get; set; } = "#facc15";

    public string InputBg { get; set; } = "#0a0a0a";
    public string InputPlaceholder { get; set; } = "#4a4a4a";

    public string DisabledBg { get; set; } = "#202020";
    public string DisabledText { get; set; } = "#5a5a5a";
}
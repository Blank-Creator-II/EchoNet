using EchoNet.Models;

namespace EchoNet.Services;

public interface IThemeService
{
    Theme GetTheme();
    void SetTheme(string themeName);
}
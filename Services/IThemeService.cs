using EchoNet.Models;

namespace EchoNet.Services;

public interface IThemeService
{
    Theme GetTheme();
    List<Theme> GetAllThemes();
    void SetTheme(string themeName);
}
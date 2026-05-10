using EchoNet.Services;
using EchoNet.Data;
using LibVLCSharp.Shared;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;

// Load LibVLCSharp from the bundled libraries
if (OperatingSystem.IsWindows())
{
    Core.Initialize(Path.Combine(AppContext.BaseDirectory, "libvlc", "win-x64"));
}
else
{
    Core.Initialize(); // Linux: do NOT pass a directory path
}

// Build the app
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
// Add singleton
builder.Services.AddSingleton<IAudioService, VlcAudioService>();
builder.Services.AddSingleton<IThemeService, ThemeService>();
// Add database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

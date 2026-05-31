using EchoNet.Services;
using EchoNet.Data;
using EchoNet.Utils;
using LibVLCSharp.Shared;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using EchoNet.Models;

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

// Set app port 
builder.WebHost.UseUrls("http://127.0.0.1:9292");

// Add services to the container.
builder.Services.AddControllersWithViews();
// Add background hosted service
builder.Services.AddHostedService<AudioInitializationService>();
// Add singleton service
builder.Services.AddSingleton<IAudioService, VlcAudioService>();
builder.Services.AddSingleton<IThemeService, ThemeService>();
builder.Services.AddSingleton<AppDataJsonReader>();
// Add scoped service
builder.Services.AddScoped<ILibScannerService, LibScannerService>();
builder.Services.AddScoped<ISongService, SongService>();
// Add database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.Lifetime.ApplicationStarted.Register(() =>
{
    app.Logger.LogInformation("Application started.");
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    app.Logger.LogInformation("Application stopping...");
});

app.Lifetime.ApplicationStopped.Register(() =>
{
    app.Logger.LogInformation("Application stopped.");
});

// Configure the HTTP request pipeline.
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapGet("/ready", () => Results.Ok("READY"));

app.MapPost("/shutdown",
    (IHostApplicationLifetime lifetime, ILogger<Program> logger, IAudioService audio, AppDataJsonReader appDataReader, IThemeService themeService) =>
{
    try
    {
        appDataReader.UpdateInMemory(); // update memory

        appDataReader.SaveAsync().GetAwaiter().GetResult(); // save memory into disk
        
        logger.LogInformation("Shutdown save successful.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to save AppData during shutdown.");
    }

    logger.LogInformation("Shutdown requested from Electron.");

    Task.Run(async () =>
    {
        await Task.Delay(100);
        lifetime.StopApplication();
    });

    logger.LogInformation("Shutting down");
    return Results.Ok("Shutting down");
});

app.Run();

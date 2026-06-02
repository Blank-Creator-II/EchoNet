using EchoNet.Services;
using EchoNet.Data;
using EchoNet.Utils;
using EchoNet.Hubs;
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
// Add SignalR framework services
builder.Services.AddSignalR();
// Add app state container for state checking
builder.Services.AddSingleton<AppStateContainer>();
// Add background hosted service
builder.Services.AddHostedService<AppInitializationService>();
// Add singleton service
builder.Services.AddSingleton<IAudioService, VlcAudioService>();
builder.Services.AddSingleton<IThemeService, ThemeService>();
builder.Services.AddSingleton<IQueueManagerService, QueueManagerService>();
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

// Map the Hub endpoint URL
app.MapHub<AudioHub>("/audioHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Update the /ready endpoint to inspect the shared state container
app.MapGet("/ready", (AppStateContainer stateContainer) =>
{
    if (stateContainer.IsReady)
    {
        return Results.Ok("READY");
    }
    
    // Electron wrapper will see a 503 and keep polling/waiting
    return Results.StatusCode(StatusCodes.Status503ServiceUnavailable); 
});

app.MapPost("/shutdown", async
    (IHostApplicationLifetime lifetime, ILogger<Program> logger, IAudioService audio, AppDataJsonReader appDataReader, IThemeService themeService) =>
{
    try
    {
        appDataReader.UpdateInMemory(); // update memory
        await appDataReader.SaveAsync(); // save memory into disk
        
        logger.LogInformation("Shutdown save successful.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to save AppData during shutdown.");
    }

    logger.LogInformation("Shutdown requested from Electron.");

    _ = Task.Run(async () =>
    {
        await Task.Delay(100);
        lifetime.StopApplication();
    });

    logger.LogInformation("Shutting down");
    return Results.Ok("Shutting down");
});

app.Run();

using System.Globalization;
using System.Threading.Channels;
using GetIPlayer.Application;
using GetIPlayer.Core.Configuration;
using GetIPlayer.Core.Interfaces;
using GetIPlayer.Infrastructure;
using GetIPlayer.Infrastructure.Persistence;
using GetIPlayer.Web.Hubs;
using GetIPlayer.Web.Services;
using Microsoft.Extensions.Logging;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(
        formatProvider: CultureInfo.InvariantCulture,
        outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Services.AddLogging(lb =>
{
    lb.ClearProviders();
    lb.AddSerilog(Log.Logger);
});

// Load settings
var profileDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".get_iplayer");
var optionsPath = Path.Combine(profileDir, "options.json");

AppSettings settings;
try
{
    using var loggerFactory = LoggerFactory.Create(lb => lb.AddSerilog(Log.Logger));
    var optionsService = new OptionsFileService(
        new GetIPlayer.Infrastructure.FileSystem.SafeFileSystem(
            loggerFactory.CreateLogger<GetIPlayer.Infrastructure.FileSystem.SafeFileSystem>()),
        loggerFactory.CreateLogger<OptionsFileService>(),
        optionsPath);
    settings = await optionsService.LoadAsync();
}
catch
{
    settings = new AppSettings();
}

// Register infrastructure + application services
builder.Services.AddGetIPlayerServices(settings);
builder.Services.AddSingleton<IDownloadService, DownloadOrchestrator>();
builder.Services.AddSingleton<SearchOrchestrator>();
builder.Services.AddSingleton<PvrOrchestrator>();

// Web services
builder.Services.AddSingleton<DownloadTracker>();
builder.Services.AddSingleton(Channel.CreateUnbounded<DownloadRequest>(new UnboundedChannelOptions
{
    SingleReader = true
}));
builder.Services.AddHostedService<DownloadBackgroundService>();

// ASP.NET Core
builder.Services.AddRazorPages();
builder.Services.AddSignalR();
builder.Services.AddAntiforgery();

var app = builder.Build();

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Security headers (OWASP)
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' https://ichef.bbci.co.uk data:; " +
        "connect-src 'self' wss: ws:; " +
        "frame-ancestors 'none';";
    await next();
});

app.UseRouting();
app.UseAntiforgery();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
app.MapHub<DownloadHub>("/hubs/download");

app.Run();

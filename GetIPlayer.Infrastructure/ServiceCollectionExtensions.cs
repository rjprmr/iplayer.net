using GetIPlayer.Core.Configuration;
using GetIPlayer.Core.Interfaces;
using GetIPlayer.Infrastructure.Bbc;
using GetIPlayer.Infrastructure.FileSystem;
using GetIPlayer.Infrastructure.Http;
using GetIPlayer.Infrastructure.Persistence;
using GetIPlayer.Infrastructure.PostProcessing;
using GetIPlayer.Infrastructure.Process;
using GetIPlayer.Infrastructure.Streaming;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GetIPlayer.Infrastructure;

/// <summary>
/// Extension methods for registering all get_iplayer services in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register all core, infrastructure, and application services.
    /// </summary>
    public static IServiceCollection AddGetIPlayerServices(
        this IServiceCollection services,
        AppSettings? settings = null)
    {
        settings ??= new AppSettings();

        // Configuration
        services.Configure<DownloadOptions>(opt =>
        {
            opt.TvQuality = settings.Download.TvQuality;
            opt.RadioQuality = settings.Download.RadioQuality;
            opt.Subtitles = settings.Download.Subtitles;
            opt.Thumbnail = settings.Download.Thumbnail;
            opt.Tag = settings.Download.Tag;
            opt.Overwrite = settings.Download.Overwrite;
            opt.Force = settings.Download.Force;
            opt.Versions = settings.Download.Versions;
            opt.FfmpegPath = settings.Download.FfmpegPath;
            opt.AtomicParsleyPath = settings.Download.AtomicParsleyPath;
            opt.MaxConcurrentSegments = settings.Download.MaxConcurrentSegments;
        });

        services.Configure<OutputOptions>(opt =>
        {
            opt.OutputDir = settings.Output.OutputDir;
            opt.FilePrefix = settings.Output.FilePrefix;
            opt.SubDir = settings.Output.SubDir;
            opt.AddVersionToFilename = settings.Output.AddVersionToFilename;
            opt.MaxFilenameLength = settings.Output.MaxFilenameLength;
        });

        services.Configure<ProxySettings>(opt =>
        {
            opt.Url = settings.Proxy.Url;
            opt.Disabled = settings.Proxy.Disabled;
            opt.PartialProxy = settings.Proxy.PartialProxy;
        });

        // HTTP
        services.AddSingleton<UserAgentProvider>();
        services.AddHttpClient("BbcClient")
            .ConfigureHttpClient((sp, client) =>
            {
                var uaProvider = sp.GetRequiredService<UserAgentProvider>();
                client.DefaultRequestHeaders.UserAgent.ParseAdd(uaProvider.GetNext());
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var handler = new HttpClientHandler();
                var proxySettings = sp.GetRequiredService<IOptions<ProxySettings>>();
                if (!string.IsNullOrEmpty(proxySettings.Value.Url) && !proxySettings.Value.Disabled)
                {
                    ProxyHandler.Configure(handler, proxySettings.Value);
                }

                return handler;
            });

        services.AddSingleton<IHttpClientService, BbcHttpClientService>();

        // File system
        services.AddSingleton<IFileSystem, SafeFileSystem>();
        services.AddSingleton<IExternalProcessRunner, SafeProcessRunner>();

        // BBC services
        services.AddSingleton<MetadataParser>();
        services.AddSingleton<IProgrammeService, BbcProgrammeService>();
        services.AddSingleton<IStreamService, BbcStreamService>();

        // Streaming
        services.AddSingleton<SegmentDownloader>();
        services.AddSingleton<IHlsDownloader, HlsDownloader>();
        services.AddSingleton<IDashDownloader, DashDownloader>();

        // Post-processing
        services.AddSingleton<IPostProcessor>(sp =>
        {
            var processRunner = sp.GetRequiredService<IExternalProcessRunner>();
            var fileSystem = sp.GetRequiredService<IFileSystem>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<FfmpegProcessor>>();
            return new FfmpegProcessor(processRunner, fileSystem, logger, settings.Download.FfmpegPath);
        });
        services.AddSingleton<ISubtitleService, SubtitleConverter>();
        services.AddSingleton<ThumbnailDownloader>();

        // Persistence
        var profileDir = settings.ProfileDir ?? GetDefaultProfileDir();
        services.AddSingleton<ICacheService>(sp =>
        {
            var fs = sp.GetRequiredService<IFileSystem>();
            var progService = sp.GetRequiredService<IProgrammeService>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<JsonCacheService>>();
            return new JsonCacheService(fs, progService, logger, Path.Combine(profileDir, "cache"));
        });
        services.AddSingleton<IHistoryService>(sp =>
        {
            var fs = sp.GetRequiredService<IFileSystem>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<JsonHistoryService>>();
            return new JsonHistoryService(fs, logger, Path.Combine(profileDir, "history.json"));
        });
        services.AddSingleton<IPvrService>(sp =>
        {
            var fs = sp.GetRequiredService<IFileSystem>();
            var dlService = sp.GetRequiredService<IDownloadService>();
            var cacheService = sp.GetRequiredService<ICacheService>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<PvrFileService>>();
            return new PvrFileService(fs, dlService, cacheService, logger, Path.Combine(profileDir, "pvr"));
        });
        services.AddSingleton(sp =>
        {
            var fs = sp.GetRequiredService<IFileSystem>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OptionsFileService>>();
            return new OptionsFileService(fs, logger, Path.Combine(profileDir, "options.json"));
        });
        services.AddSingleton(sp =>
        {
            var fs = sp.GetRequiredService<IFileSystem>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<LockFileService>>();
            return new LockFileService(fs, logger, Path.Combine(profileDir, "locks"));
        });

        // Application orchestrators are registered by the host (CLI/Web)

        return services;
    }

    private static string GetDefaultProfileDir()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".get_iplayer");
    }
}

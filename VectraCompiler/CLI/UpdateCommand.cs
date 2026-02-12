using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;
using Spectre.Console.Cli;

namespace VectraCompiler.CLI;

public sealed class UpdateCommand : AsyncCommand<UpdateCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--check")]
        [Description("Check for updates without installing")]
        public bool CheckOnly { get; init; }
    }

    private const string GitHubRepo = "DuncanMcPherson/vectra";
    private const string GitHubApiUrl = $"https://api.github.com/repos/{GitHubRepo}/releases/latest";

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var currentVersion = GetCurrentVersion();
        AnsiConsole.MarkupLine($"Current version: [cyan]{currentVersion}[/]");
        var latestRelease = await GetLatestReleaseAsync(cancellationToken);

        if (latestRelease == null)
        {
            AnsiConsole.MarkupLine("[red]Failed to fetch latest release information[/]");
            return 1;
        }

        var latestVersion = latestRelease.TagName.TrimStart('v');
        AnsiConsole.MarkupLine($"Latest version: [cyan]{latestVersion}[/]");
        if (currentVersion == latestVersion)
        {
            AnsiConsole.MarkupLine("[green]You are already on the latest version![/]");
            return 0;
        }
        
        if (settings.CheckOnly)
        {
            AnsiConsole.MarkupLine($"[yellow]Update available:[/] {currentVersion} → {latestVersion}");
            return 0;
        }
        
        return await PerformUpdateAsync(latestRelease, cancellationToken);
    }

    private static string GetCurrentVersion()
    {
        return Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
    }

    private static async Task<GitHubRelease?> GetLatestReleaseAsync(CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "vectra-cli");

        try
        {
            var response = await client.GetAsync(GitHubApiUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<GitHubRelease>(json, options);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error fetching release info: {ex.Message}[/]");
            return null;
        }
    }
    
    private static async Task<int> PerformUpdateAsync(GitHubRelease release, CancellationToken cancellationToken)
    {
        // Determine current platform
        var platform = GetCurrentPlatform();
        var assetName = $"vectra-{platform}.zip"; // Adjust based on your artifact naming

        var asset = release.Assets.FirstOrDefault(a => a.Name == assetName);
        if (asset == null)
        {
            AnsiConsole.MarkupLine($"[red]No release asset found for platform: {platform}[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"Downloading update from: [blue]{asset.BrowserDownloadUrl}[/]");
        
        // TODO: Implement download and replacement logic
        // This is the tricky part - we need to:
        // 1. Download the new version to a temp location
        // 2. Extract it
        // 3. Replace the current executable (requires special handling since it's running)
        
        AnsiConsole.MarkupLine("[yellow]Update download not yet implemented[/]");
        return 1;
    }

    private static string GetCurrentPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "win-x64";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "linux-x64";
        throw new PlatformNotSupportedException("Unsupported platform");
    }

    private class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;
        
        [JsonPropertyName("assets")]
        public List<GitHubAsset> Assets { get; set; } = [];
    }

    private class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace UGE.Updater;

internal static class Program
{
    private sealed record ReleaseAsset(string name, string browser_download_url);
    private sealed record Release(string tag_name, bool prerelease, ReleaseAsset[] assets);

    private static async Task<int> Main(string[] args)
    {
        try
        {
            var logPath = Path.Combine(Path.GetTempPath(), "UGE.Updater.log");
            await File.AppendAllTextAsync(logPath, $"[{DateTime.Now}] Updater starting args: {string.Join(' ', args)}\n");
            var options = UpdateOptions.Parse(args);
            if (options == null)
            {
                Console.Error.WriteLine("Invalid arguments.");
                UpdateOptions.PrintUsage();
                return 2;
            }

            Console.WriteLine($"Updater starting. TargetDir: {options.AppDir}");
            await KillOldProcessAsync(options.ProcessId);

            string assetUrl = options.AssetUrl ?? await GetLatestZipAssetUrlAsync(options.Owner, options.Repo, options.GitHubToken);
            Console.WriteLine($"Latest asset: {assetUrl}");

            var tempDir = Path.Combine(Path.GetTempPath(), $"UGE_Update_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            var zipPath = Path.Combine(tempDir, "release.zip");
            await DownloadAsync(assetUrl, zipPath, options.GitHubToken);

            var extractDir = Path.Combine(tempDir, "extracted");
            ZipFile.ExtractToDirectory(zipPath, extractDir, overwriteFiles: true);

            Console.WriteLine("Copying files into target directory...");
            CopyDirectory(extractDir, options.AppDir);

            Console.WriteLine("Relaunching application...");
            if (!string.IsNullOrWhiteSpace(options.ExePath))
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = options.ExePath,
                    WorkingDirectory = Path.GetDirectoryName(options.ExePath) ?? options.AppDir,
                    UseShellExecute = true
                };
                Process.Start(startInfo);
            }

            Console.WriteLine("Update completed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Updater failed: " + ex);
            return 1;
        }
    }

    private static async Task KillOldProcessAsync(int pid)
    {
        try
        {
            var proc = Process.GetProcessById(pid);
            if (proc.HasExited) return;

            Console.WriteLine($"Requesting app (PID {pid}) to exit...");
            try
            {
                proc.CloseMainWindow();
            }
            catch { /* ignore */ }

            if (!proc.WaitForExit(5000))
            {
                Console.WriteLine("Force killing old process...");
                proc.Kill(true);
                proc.WaitForExit(5000);
            }
        }
        catch (ArgumentException)
        {
            // already exited
        }
    }

    private static async Task<string> GetLatestZipAssetUrlAsync(string owner, string repo, string? token)
    {
        using var http = CreateHttpClient(token);
        var url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
        using var resp = await http.GetAsync(url);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync();
        var release = await JsonSerializer.DeserializeAsync<Release>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                      ?? throw new InvalidOperationException("Failed to read release JSON.");

        var asset = release.assets?.FirstOrDefault(a => a.name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    ?? release.assets?.FirstOrDefault();

        if (asset == null)
            throw new InvalidOperationException("No assets found on latest release.");

        return asset.browser_download_url;
    }

    private static async Task DownloadAsync(string url, string destination, string? token)
    {
        using var http = CreateHttpClient(token);
        using var resp = await http.GetAsync(url);
        resp.EnsureSuccessStatusCode();

        await using var fs = File.Create(destination);
        await resp.Content.CopyToAsync(fs);
    }

    private static HttpClient CreateHttpClient(string? token)
    {
        var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("UGE.Updater", "1.0"));
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
        if (!string.IsNullOrWhiteSpace(token))
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return http;
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        foreach (var dir in Directory.EnumerateDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(sourceDir, dir);
            Directory.CreateDirectory(Path.Combine(targetDir, rel));
        }

        foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(sourceDir, file);
            var dest = Path.Combine(targetDir, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

            // Ensure destination file is not read-only before overwrite
            if (File.Exists(dest))
            {
                var attr = File.GetAttributes(dest);
                if (attr.HasFlag(FileAttributes.ReadOnly))
                {
                    File.SetAttributes(dest, attr & ~FileAttributes.ReadOnly);
                }
            }

            File.Copy(file, dest, overwrite: true);
        }
    }

    private sealed class UpdateOptions
    {
        public int ProcessId { get; init; }
        public string AppDir { get; init; } = "";
        public string ExePath { get; init; } = "";
        public string Owner { get; init; } = "Sopitive";
        public string Repo { get; init; } = "UniversalGametypeEditor";
        public string? AssetUrl { get; init; }
        public string? GitHubToken { get; init; }

        public static UpdateOptions? Parse(string[] args)
        {
            var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < args.Length; i++)
            {
                var a = args[i];
                if (a.StartsWith("--"))
                {
                    var key = a.TrimStart('-');
                    string? val = null;
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                    {
                        val = args[++i];
                    }
                    dict[key] = val;
                }
            }

            if (!dict.TryGetValue("pid", out var pidStr) || !int.TryParse(pidStr, out var pid)) return null;
            if (!dict.TryGetValue("appDir", out var appDir) || string.IsNullOrWhiteSpace(appDir)) return null;
            if (!dict.TryGetValue("exe", out var exePath) || string.IsNullOrWhiteSpace(exePath)) return null;

            dict.TryGetValue("owner", out var owner);
            dict.TryGetValue("repo", out var repo);
            dict.TryGetValue("assetUrl", out var assetUrl);
            dict.TryGetValue("token", out var token);
            token ??= Environment.GetEnvironmentVariable("GITHUB_TOKEN");

            return new UpdateOptions
            {
                ProcessId = pid,
                AppDir = appDir!,
                ExePath = exePath!,
                Owner = string.IsNullOrWhiteSpace(owner) ? "Sopitive" : owner!,
                Repo = string.IsNullOrWhiteSpace(repo) ? "UniversalGametypeEditor" : repo!,
                AssetUrl = assetUrl,
                GitHubToken = token
            };
        }

        public static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  UGE.Updater --pid <int> --appDir \"<path>\" --exe \"<path>\" [--owner Sopitive] [--repo UniversalGametypeEditor] [--assetUrl \"<url>\"] [--token <gh_token>]");
        }
    }
}
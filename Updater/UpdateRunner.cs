using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Updater;

public static class UpdateRunner
{
    private sealed record ReleaseAsset(string name, string browser_download_url);
    private sealed record Release(string tag_name, bool prerelease, ReleaseAsset[] assets);

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

            dict.TryGetValue("pid", out var pidStr);
            var pid = int.TryParse(pidStr, out var p) ? p : 0;

            dict.TryGetValue("appDir", out var appDir);
            dict.TryGetValue("exe", out var exePath);

            dict.TryGetValue("owner", out var owner);
            dict.TryGetValue("repo", out var repo);
            dict.TryGetValue("assetUrl", out var assetUrl);
            dict.TryGetValue("token", out var token);
            token ??= Environment.GetEnvironmentVariable("GITHUB_TOKEN");

            // Allow missing values; we’ll auto-detect later
            return new UpdateOptions
            {
                ProcessId = pid,
                AppDir = appDir ?? "",
                ExePath = exePath ?? "",
                Owner = string.IsNullOrWhiteSpace(owner) ? "Sopitive" : owner!,
                Repo = string.IsNullOrWhiteSpace(repo) ? "UniversalGametypeEditor" : repo!,
                AssetUrl = assetUrl,
                GitHubToken = token
            };
        }
    }

    public static async Task RunAsync(string[] args, TextBlock statusText, ProgressBar progressBar, TextBlock progressInfo, TextBlock logText, ScrollViewer logScroll)
    {
        var logPath = Path.Combine(Path.GetTempPath(), "UGE.Updater.log");
        try
        {
            await AppendLog(logPath, "Updater starting args: " + string.Join(' ', args), logText, logScroll);
            var options = UpdateOptions.Parse(args) ?? new UpdateOptions();
            await AppendLog(logPath, $"Parsed options: pid={options.ProcessId}, appDir='{options.AppDir}', exe='{options.ExePath}', owner='{options.Owner}', repo='{options.Repo}', assetUrl='{options.AssetUrl ?? "<auto>"}'", logText, logScroll);

            // Auto-detect appDir/exe when missing (standalone launch)
            if (string.IsNullOrWhiteSpace(options.AppDir) || string.IsNullOrWhiteSpace(options.ExePath))
            {
                var detected = DetectAppFromUpdaterLocation("UniversalGametypeEditor.exe", logPath, logText, logScroll);
                if (detected != null)
                {
                    options = new UpdateOptions
                    {
                        ProcessId = options.ProcessId,
                        AppDir = detected.Value.AppDir,
                        ExePath = detected.Value.ExePath,
                        Owner = options.Owner,
                        Repo = options.Repo,
                        AssetUrl = options.AssetUrl,
                        GitHubToken = options.GitHubToken
                    };
                    await AppendLog(logPath, $"Auto-detected AppDir: {options.AppDir}", logText, logScroll);
                    await AppendLog(logPath, $"Auto-detected ExePath: {options.ExePath}", logText, logScroll);
                }
                else
                {
                    await AppendLog(logPath, "Auto-detection did not find the app.", logText, logScroll);
                }
            }

            if (string.IsNullOrWhiteSpace(options.AppDir) || string.IsNullOrWhiteSpace(options.ExePath))
            {
                await UpdateStatus("Unable to locate the main app. Place the updater one folder above the app or pass --appDir and --exe.", statusText);
                await AppendLog(logPath, "Missing AppDir/ExePath and auto-detection failed.", logText, logScroll);
                return;
            }

            await AppendLog(logPath, $"Using AppDir: {options.AppDir}", logText, logScroll);
            await AppendLog(logPath, $"Using ExePath: {options.ExePath}", logText, logScroll);

            // FIX: correct PID handling
            if (options.ProcessId > 0)
            {
                await UpdateStatus("Closing application...", statusText);
                await KillOldProcessAsync(options.ProcessId, logPath, logText, logScroll);
            }
            else
            {
                await AppendLog(logPath, "No PID provided. Skipping process shutdown (standalone mode).", logText, logScroll);
            }

            await UpdateStatus("Resolving latest release...", statusText);
            var assetUrl = options.AssetUrl ?? await GetLatestZipAssetUrlAsync(options.Owner, options.Repo, options.GitHubToken, logPath, logText, logScroll);
            await AppendLog(logPath, $"Selected asset URL: {assetUrl}", logText, logScroll);

            await UpdateStatus("Downloading release...", statusText);

            var tempDir = Path.Combine(Path.GetTempPath(), $"UGE_Update_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            await AppendLog(logPath, $"Temp dir: {tempDir}", logText, logScroll);

            var zipPath = Path.Combine(tempDir, "release.zip");
            await AppendLog(logPath, $"Zip target path: {zipPath}", logText, logScroll);
            await DownloadWithProgressAsync(assetUrl, zipPath, options.GitHubToken, progressBar, progressInfo, logPath, logText, logScroll);

            await UpdateStatus("Extracting package...", statusText);
            var extractDir = Path.Combine(tempDir, "extracted");
            await AppendLog(logPath, $"Extract dir: {extractDir}", logText, logScroll);
            ZipFile.ExtractToDirectory(zipPath, extractDir, overwriteFiles: true);

            var packageRoot = GetPackageRoot(extractDir);
            await AppendLog(logPath, $"Package root: {packageRoot}", logText, logScroll);

            await UpdateStatus("Installing update...", statusText);
            var copied = CopyDirectoryWithLogging(packageRoot, options.AppDir, logPath, logText, logScroll);
            await AppendLog(logPath, $"Installed {copied} files to '{options.AppDir}'.", logText, logScroll);

            // Post-install verification: show a sample file timestamp
            var sampleFile = Directory.EnumerateFiles(options.AppDir, "*", SearchOption.AllDirectories).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
            if (sampleFile != null)
            {
                var ts = File.GetLastWriteTime(sampleFile);
                await AppendLog(logPath, $"Most recently modified file: {sampleFile} (LastWriteTime: {ts})", logText, logScroll);
            }

            var exeToLaunch = ResolveExeToLaunch(options.AppDir, options.ExePath, "UniversalGametypeEditor.exe", logPath);
            if (exeToLaunch == null)
            {
                await AppendLog(logPath, "Failed to resolve exe to launch.", logText, logScroll);
                await UpdateStatus("Failed to relaunch application (exe not found).", statusText);
                return;
            }

            await UpdateStatus($"Relaunching: {exeToLaunch}", statusText);
            await AppendLog(logPath, $"Relaunch target: {exeToLaunch}", logText, logScroll);

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = exeToLaunch,
                    WorkingDirectory = Path.GetDirectoryName(exeToLaunch)!,
                    UseShellExecute = true
                };

                await AppendLog(logPath, $"Starting exe: {startInfo.FileName} (wd: {startInfo.WorkingDirectory})", logText, logScroll);
                Process.Start(startInfo);
                await Task.Delay(250);
            }
            catch (Exception ex)
            {
                await AppendLog(logPath, $"Relaunch failed: {ex}", logText, logScroll);
                throw;
            }

            await UpdateStatus("Update completed.", statusText);
            await AppendLog(logPath, "Updater completed successfully.", logText, logScroll);
        }
        catch (Exception ex)
        {
            await AppendLog(logPath, "Updater failed: " + ex, logText, logScroll);
            await UpdateStatus("Updater failed: " + ex.Message, statusText);
        }
    }

    private static int CopyDirectoryWithLogging(string sourceDir, string targetDir, string logPath, TextBlock? logText = null, ScrollViewer? logScroll = null)
    {
        int copied = 0;
        foreach (var dir in Directory.EnumerateDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(sourceDir, dir);
            Directory.CreateDirectory(Path.Combine(targetDir, rel));
        }

        foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            try
            {
                var rel = Path.GetRelativePath(sourceDir, file);
                var dest = Path.Combine(targetDir, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

                if (File.Exists(dest))
                {
                    var attr = File.GetAttributes(dest);
                    if (attr.HasFlag(FileAttributes.ReadOnly))
                    {
                        File.SetAttributes(dest, attr & ~FileAttributes.ReadOnly);
                    }
                }

                File.Copy(file, dest, overwrite: true);
                copied++;
            }
            catch (Exception ex)
            {
                var formatted = $"[{DateTime.Now}] Copy failed for '{file}': {ex}{Environment.NewLine}";
                File.AppendAllText(logPath, formatted);
                Debug.WriteLine(formatted);
                Trace.WriteLine(formatted);
                if (logText != null)
                {
                    logText.Dispatcher.Invoke(() =>
                    {
                        logText.Text += formatted;
                        logScroll?.ScrollToEnd();
                    });
                }
            }
        }
        return copied;
    }

    private static (string AppDir, string ExePath)? DetectAppFromUpdaterLocation(string preferredExeName, string logPath, TextBlock? logText = null, ScrollViewer? logScroll = null)
    {
        try
        {
            var entry = Assembly.GetEntryAssembly()?.Location
                        ?? Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrWhiteSpace(entry)) return null;

            var updaterDir = Path.GetDirectoryName(Path.GetFullPath(entry))!;
            AppendLogSync(logPath, $"Updater binary located at: {entry}", logText, logScroll);
            AppendLogSync(logPath, $"Updater directory: {updaterDir}", logText, logScroll);

            var siblingWinX64 = Path.Combine(updaterDir, "win-x64");
            var siblingExe = Path.Combine(siblingWinX64, preferredExeName);
            if (Directory.Exists(siblingWinX64) && File.Exists(siblingExe))
            {
                AppendLogSync(logPath, $"Found preferred exe in 'win-x64': {siblingExe}", logText, logScroll);
                return (siblingWinX64, siblingExe);
            }

            var preferredSame = Path.Combine(updaterDir, preferredExeName);
            if (File.Exists(preferredSame))
            {
                AppendLogSync(logPath, $"Found preferred exe in updater directory: {preferredSame}", logText, logScroll);
                return (updaterDir, preferredSame);
            }

            foreach (var sub in Directory.GetDirectories(updaterDir))
            {
                var candidate = Path.Combine(sub, preferredExeName);
                if (File.Exists(candidate))
                {
                    AppendLogSync(logPath, $"Found preferred exe in subdir: {candidate}", logText, logScroll);
                    return (sub, candidate);
                }
            }

            var allMatches = Directory.EnumerateFiles(updaterDir, preferredExeName, SearchOption.AllDirectories)
                                      .OrderBy(p => p.Length)
                                      .ToList();
            if (allMatches.Count > 0)
            {
                var exe = allMatches[0];
                AppendLogSync(logPath, $"Found preferred exe via deep search: {exe}", logText, logScroll);
                return (Path.GetDirectoryName(exe)!, exe);
            }

            var exes = Directory.EnumerateFiles(updaterDir, "*.exe", SearchOption.AllDirectories)
                                .Where(p => !Path.GetFileName(p).Equals("UGE.Updater.exe", StringComparison.OrdinalIgnoreCase)
                                         && !Path.GetFileName(p).Equals("Updater.exe", StringComparison.OrdinalIgnoreCase))
                                .Select(p => new FileInfo(p))
                                .OrderByDescending(fi => fi.Length)
                                .ToList();
            if (exes.Count > 0)
            {
                var exe = exes[0].FullName;
                AppendLogSync(logPath, $"Fallback selected largest exe: {exe}", logText, logScroll);
                return (Path.GetDirectoryName(exe)!, exe);
            }

            AppendLogSync(logPath, "No application executable found near updater.", logText, logScroll);
            return null;
        }
        catch (Exception ex)
        {
            File.AppendAllText(logPath, $"[{DateTime.Now}] DetectAppFromUpdaterLocation failed: {ex}{Environment.NewLine}");
            return null;
        }
    }

    private static async Task KillOldProcessAsync(int pid, string logPath, TextBlock logText, ScrollViewer logScroll)
    {
        try
        {
            var proc = Process.GetProcessById(pid);
            if (proc.HasExited) return;

            await AppendLog(logPath, $"Requesting app (PID {pid}) to exit...", logText, logScroll);
            try { proc.CloseMainWindow(); } catch { /* ignore */ }

            if (!proc.WaitForExit(5000))    
            {
                await AppendLog(logPath, "Force killing old process...", logText, logScroll);
                proc.Kill(true);
                proc.WaitForExit(5000);
            }
        }
        catch (ArgumentException)
        {
            // already exited or invalid pid
        }
    }

    private static async Task<string> GetLatestZipAssetUrlAsync(string owner, string repo, string? token, string logPath, TextBlock logText, ScrollViewer logScroll)
    {
        using var http = CreateHttpClient(token);
        var url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
        await AppendLog(logPath, $"GitHub API: GET {url}", logText, logScroll);
        using var resp = await http.GetAsync(url);
        await AppendLog(logPath, $"GitHub API response: {(int)resp.StatusCode} {resp.ReasonPhrase}", logText, logScroll);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync();
        var release = await JsonSerializer.DeserializeAsync<Release>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                      ?? throw new InvalidOperationException("Failed to read release JSON.");

        await AppendLog(logPath, $"Resolved release tag: {release.tag_name}, prerelease={release.prerelease}, assets={release.assets?.Length ?? 0}", logText, logScroll);

        var asset = release.assets?.FirstOrDefault(a => a.name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    ?? release.assets?.FirstOrDefault();

        if (asset == null)
            throw new InvalidOperationException("No assets found on latest release.");

        await AppendLog(logPath, $"Latest asset: {asset.browser_download_url}", logText, logScroll);
        return asset.browser_download_url;
    }

    private static HttpClient CreateHttpClient(string? token)
    {
        var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("UGE.Updater", "1.0"));
        // Accept JSON for GitHub API endpoints
        http.DefaultRequestHeaders.Accept.Clear();
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        if (!string.IsNullOrWhiteSpace(token))
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return http;
    }

    private static async Task DownloadWithProgressAsync(
        string url,
        string destination,
        string? token,
        ProgressBar progressBar,
        TextBlock progressInfo,
        string logPath,
        TextBlock logText,
        ScrollViewer logScroll)
    {
        using var http = CreateHttpClient(token);

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Accept.Clear();
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
        await AppendLog(logPath, $"Downloading asset: {url}", logText, logScroll);

        using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
        await AppendLog(logPath, $"Asset response: {(int)resp.StatusCode} {resp.ReasonPhrase}", logText, logScroll);
        resp.EnsureSuccessStatusCode();

        var totalBytes = resp.Content.Headers.ContentLength;
        await AppendLog(logPath, $"Content-Length: {(totalBytes.HasValue ? totalBytes.Value.ToString() : "<unknown>")}", logText, logScroll);

        if (totalBytes.HasValue && totalBytes.Value > 0)
        {
            await progressBar.Dispatcher.InvokeAsync(() =>
            {
                progressBar.IsIndeterminate = false;
                progressBar.Minimum = 0;
                progressBar.Maximum = totalBytes.Value;
                progressBar.Value = 0;
            });
        }
        else
        {
            await progressBar.Dispatcher.InvokeAsync(() => progressBar.IsIndeterminate = true);
        }

        var sw = Stopwatch.StartNew();
        long totalRead = 0;
        var buffer = new byte[81920];

        await using var contentStream = await resp.Content.ReadAsStreamAsync();
        await using var fileStream = File.Create(destination);

        while (true)
        {
            var read = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length));
            if (read == 0) break;

            await fileStream.WriteAsync(buffer.AsMemory(0, read));
            totalRead += read;

            if (totalBytes.HasValue && totalBytes.Value > 0)
            {
                var pct = totalRead * 100.0 / totalBytes.Value;
                var bytesPerSec = totalRead / Math.Max(1.0, sw.Elapsed.TotalSeconds);
                var eta = TimeSpan.FromSeconds((totalBytes.Value - totalRead) / Math.Max(1.0, bytesPerSec));

                await progressBar.Dispatcher.InvokeAsync(() => progressBar.Value = totalRead);
                await progressInfo.Dispatcher.InvokeAsync(() =>
                {
                    progressInfo.Text = $"{FormatSize(totalRead)} / {FormatSize(totalBytes.Value)}  ({pct:0.0}%)  {FormatSize(bytesPerSec)}/s  ETA {FormatEta(eta)}";
                });
            }
            else
            {
                await progressInfo.Dispatcher.InvokeAsync(() =>
                {
                    progressInfo.Text = $"{FormatSize(totalRead)} downloaded...";
                });
            }
        }

        sw.Stop();
        await AppendLog(logPath, $"Download completed: {totalRead} bytes in {sw.Elapsed}.", logText, logScroll);
        await AppendLog(logPath, $"Saved to: {destination}", logText, logScroll);
    }

    private static string GetPackageRoot(string extractDir)
    {
        // Walk down through single-child directories (no files at the current level)
        // Example: extracted\net6.0-windows\win-x64 -> returns the innermost 'win-x64' folder
        var current = extractDir;
        try
        {
            while (true)
            {
                // Only look at top-level of the current directory
                var files = Directory.EnumerateFiles(current, "*", SearchOption.TopDirectoryOnly).Take(2).ToList();
                var dirs = Directory.EnumerateDirectories(current, "*", SearchOption.TopDirectoryOnly).Take(2).ToList();

                if (files.Count == 0 && dirs.Count == 1)
                {
                    // Drill into the single child directory
                    current = dirs[0];
                    continue;
                }

                // Stop when the current level has files or multiple subdirectories
                return current;
            }
        }
        catch
        {
            // On any error, fall back to the original extraction directory
            return extractDir;
        }
    }

    private static string? ResolveExeToLaunch(string appDir, string intendedExePath, string preferredName, string logPath)
    {
        try
        {
            // 0) If the intended exe path exists (from arguments), prefer it
            if (!string.IsNullOrWhiteSpace(intendedExePath) && File.Exists(intendedExePath) && Path.GetExtension(intendedExePath).Equals(".exe", StringComparison.OrdinalIgnoreCase))
            {
                return intendedExePath;
            }

            // 1) Prefer a specifically named exe at root of appDir
            if (!string.IsNullOrWhiteSpace(appDir))
            {
                var preferred = Path.Combine(appDir, preferredName);
                if (File.Exists(preferred))
                {
                    return preferred;
                }

                // If a dll with the preferred name exists, try to use the exe next to it
                var preferredDll = Path.Combine(appDir, Path.ChangeExtension(preferredName, ".dll"));
                if (File.Exists(preferredDll))
                {
                    var candidateExe = Path.ChangeExtension(preferredDll, ".exe");
                    if (File.Exists(candidateExe))
                    {
                        File.AppendAllText(logPath, $"[{DateTime.Now}] Preferred dll found; using adjacent exe: {candidateExe}{Environment.NewLine}");
                        return candidateExe;
                    }
                    File.AppendAllText(logPath, $"[{DateTime.Now}] Preferred dll found without exe: {preferredDll}{Environment.NewLine}");
                }
            }

            // 2) Search recursively for the preferred exe
            if (!string.IsNullOrWhiteSpace(appDir))
            {
                var matches = Directory.EnumerateFiles(appDir, preferredName, SearchOption.AllDirectories).ToList();
                if (matches.Count > 0)
                {
                    return matches[0];
                }

                // If preferred dll found deeper, try adjacent exe
                var dllMatches = Directory.EnumerateFiles(appDir, Path.ChangeExtension(preferredName, ".dll"), SearchOption.AllDirectories).ToList();
                if (dllMatches.Count > 0)
                {
                    var dllPath = dllMatches[0];
                    var candidateExe = Path.ChangeExtension(dllPath, ".exe");
                    if (File.Exists(candidateExe))
                    {
                        File.AppendAllText(logPath, $"[{DateTime.Now}] Found dll: {dllPath}; using adjacent exe: {candidateExe}{Environment.NewLine}");
                        return candidateExe;
                    }
                    File.AppendAllText(logPath, $"[{DateTime.Now}] Found dll without adjacent exe: {dllPath}{Environment.NewLine}");
                }
            }

            // 3) As a final fallback, choose the largest .exe in appDir (excluding updater)
            if (!string.IsNullOrWhiteSpace(appDir))
            {
                var exes = Directory.EnumerateFiles(appDir, "*.exe", SearchOption.AllDirectories)
                                    .Where(p => !Path.GetFileName(p).Equals("UGE.Updater.exe", StringComparison.OrdinalIgnoreCase)
                                             && !Path.GetFileName(p).Equals("Updater.exe", StringComparison.OrdinalIgnoreCase))
                                    .Select(p => new FileInfo(p))
                                    .OrderByDescending(fi => fi.Length)
                                    .ToList();

                if (exes.Count > 0)
                {
                    var selected = exes[0].FullName;
                    File.AppendAllText(logPath, $"[{DateTime.Now}] Fallback selected exe: {selected}{Environment.NewLine}");
                    return selected;
                }
            }

            File.AppendAllText(logPath, $"[{DateTime.Now}] No executable (.exe) found to launch within '{appDir}'.{Environment.NewLine}");
            return null;
        }
        catch (Exception ex)
        {
            File.AppendAllText(logPath, $"[{DateTime.Now}] ResolveExeToLaunch failed: {ex}{Environment.NewLine}");
            return null;
        }
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

    private static string FormatSize(double bytes)
    {
        string[] units = { "B", "KB", "MB", "GB" };
        int u = 0;
        while (bytes >= 1024 && u < units.Length - 1)
        {
            bytes /= 1024;
            u++;
        }
        return $"{bytes:0.0} {units[u]}";
    }

    private static string FormatEta(TimeSpan ts) =>
        ts.TotalHours >= 1 ? $"{(int)ts.TotalHours}h {ts.Minutes}m" :
        ts.TotalMinutes >= 1 ? $"{ts.Minutes}m {ts.Seconds}s" :
        $"{ts.Seconds}s";

    private static Task UpdateStatus(string text, TextBlock status) =>
        status.Dispatcher.InvokeAsync(() => status.Text = text).Task;

    private static Task AppendLog(string path, string line, TextBlock? logText = null, ScrollViewer? logScroll = null)
    {
        var formatted = $"[{DateTime.Now}] {line}{Environment.NewLine}";
        var fileTask = File.AppendAllTextAsync(path, formatted);
        Debug.WriteLine(formatted);
        Trace.WriteLine(formatted);

        if (logText != null)
        {
            logText.Dispatcher.Invoke(() =>
            {
                logText.Text += formatted;
                logScroll?.ScrollToEnd();
            });
        }

        return fileTask;
    }

    private static void AppendLogSync(string path, string line, TextBlock? logText = null, ScrollViewer? logScroll = null)
    {
        var formatted = $"[{DateTime.Now}] {line}{Environment.NewLine}";
        File.AppendAllText(path, formatted);
        Debug.WriteLine(formatted);
        Trace.WriteLine(formatted);

        if (logText != null)
        {
            logText.Dispatcher.Invoke(() =>
            {
                logText.Text += formatted;
                logScroll?.ScrollToEnd();
            });
        }
    }
}
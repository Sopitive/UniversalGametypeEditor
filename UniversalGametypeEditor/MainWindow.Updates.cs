using System;
using System.Diagnostics;
using System.IO;
using System.Linq; // needed for FirstOrDefault
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace UniversalGametypeEditor
{
    public sealed partial class MainWindow
    {
        private sealed record ReleaseAsset(string name, string browser_download_url);
        private sealed record Release(string tag_name, bool prerelease, ReleaseAsset[] assets);

        public async void CheckForUpdates(object sender, RoutedEventArgs e)
        {
            try
            {
                var current = GetCurrentVersion();
                var latest = await GetLatestVersionAsync("Sopitive", "UniversalGametypeEditor");

                if (latest.Version == null || current == null)
                {
                    MessageBox.Show("Unable to determine version.", "Updates", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (latest.Version <= current)
                {
                    MessageBox.Show($"You are up to date (v{current}).", "Updates", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show(
                    $"A new version (v{latest.Version}) is available. Update now?",
                    "Updates",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                var exePath = GetExePath();
                var appDir = Path.GetDirectoryName(exePath)!;
                var parentDir = Directory.GetParent(appDir)!.FullName;

                var updaterExe = FindUpdaterExe(parentDir);
                if (updaterExe == null)
                {
                    MessageBox.Show(
                        $"Updater not found in parent directory:\n{parentDir}\n\nExpected 'UGE.Updater.exe' or '*Updater*.exe'.",
                        "Updates",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                var assetUrl = latest.AssetUrl ?? "";

                var args =
                    $"--pid {Process.GetCurrentProcess().Id} " +
                    $"--appDir \"{appDir}\" " +
                    $"--exe \"{exePath}\" " +
                    $"--owner Sopitive --repo UniversalGametypeEditor " +
                    $"{(string.IsNullOrWhiteSpace(assetUrl) ? "" : $"--assetUrl \"{assetUrl}\"")}";

                var psi = new ProcessStartInfo
                {
                    FileName = updaterExe,
                    Arguments = args,
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(updaterExe)!
                };

                Process.Start(psi);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to check for updates:\n{ex.Message}", "Updates", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static Version? GetCurrentVersion()
        {
            try
            {
                var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                var fvi = FileVersionInfo.GetVersionInfo(asm.Location);
                if (Version.TryParse(fvi.ProductVersion, out var v)) return v;
                if (Version.TryParse(fvi.FileVersion, out v)) return v;
                return asm.GetName().Version;
            }
            catch { return null; }
        }

        private static string GetExePath()
        {
            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            return asm.Location;
        }

        private static string? FindUpdaterExe(string parentDir)
        {
            var primary = Path.Combine(parentDir, "UGE.Updater.exe");
            if (File.Exists(primary)) return primary;

            foreach (var path in Directory.EnumerateFiles(parentDir, "*Updater*.exe", SearchOption.TopDirectoryOnly))
                return path;

            return null;
        }

        private static async Task<(Version? Version, string? AssetUrl)> GetLatestVersionAsync(string owner, string repo)
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("UniversalGametypeEditor", "UpdateChecker"));
            var url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

            using var resp = await http.GetAsync(url);
            resp.EnsureSuccessStatusCode();

            await using var stream = await resp.Content.ReadAsStreamAsync();
            var release = await JsonSerializer.DeserializeAsync<Release>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (release == null) return (null, null);

            var tag = (release.tag_name ?? "").Trim();
            if (tag.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                tag = tag[1..];

            Version? latest = Version.TryParse(tag, out var v) ? v : null;
            string? assetUrl = null;

            var asset = release.assets?.FirstOrDefault(a => a.name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        ?? release.assets?.FirstOrDefault();

            if (asset != null) assetUrl = asset.browser_download_url;

            return (latest, assetUrl);
        }
    }
}
using FlowLink.Data.Contracts;
using Velopack;
using Velopack.Sources;

namespace FlowLink.Platforms.Windows.Services;

/// <summary>
/// Auto-update service powered by Velopack.
/// Replaces the Microsoft Store API-based update mechanism.
/// 
/// Update source: GitHub Releases (or any HTTP URL).
/// When the repo becomes public, set UpdateUrl to the actual GitHub releases URL.
/// Example: "https://github.com/PLACEHOLDER/FlowLink/releases"
/// </summary>
public partial class WindowsUpdateService : ObservableObject, IUpdateService
{
    // ── Configure this URL when the GitHub repo is made public ──────────────────
    // Leave empty to disable auto-update silently (no errors, no crash).
    private const string UpdateUrl = ""; // e.g. "https://github.com/PLACEHOLDER/FlowLink/releases"

    private UpdateManager? _updateManager;
    private UpdateInfo? _pendingUpdate;

    [ObservableProperty]
    private bool isUpdateAvailable;

    [ObservableProperty]
    private bool isUpdating;

    public bool IsMandatory => false;

    public WindowsUpdateService()
    {
        if (!string.IsNullOrWhiteSpace(UpdateUrl))
        {
            _updateManager = new UpdateManager(new GithubSource(UpdateUrl, null, false));
        }
    }

    /// <summary>
    /// Checks GitHub Releases for a newer version.
    /// Silently does nothing if UpdateUrl is not configured.
    /// </summary>
    public async Task CheckForUpdatesAsync()
    {
        if (_updateManager is null) return;

        try
        {
            _pendingUpdate = await _updateManager.CheckForUpdatesAsync();
            IsUpdateAvailable = _pendingUpdate is not null;

            if (IsUpdateAvailable)
                ShowUpdateAvailableNotification(_pendingUpdate!.TargetFullRelease.Version.ToString());
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Velopack] CheckForUpdates failed: {ex.Message}");
            IsUpdateAvailable = false;
        }
    }

    /// <summary>
    /// Downloads the pending update and applies it on next restart.
    /// The app restarts automatically after applying.
    /// </summary>
    public async Task DownloadUpdatesAsync()
    {
        if (_updateManager is null || _pendingUpdate is null) return;

        IsUpdating = true;
        try
        {
            await _updateManager.DownloadUpdatesAsync(_pendingUpdate);

            // Apply and restart — Velopack handles the upgrade atomically
            _updateManager.ApplyUpdatesAndRestart(_pendingUpdate);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Velopack] DownloadUpdates failed: {ex.Message}");
        }
        finally
        {
            IsUpdating = false;
        }
    }

    private static void ShowUpdateAvailableNotification(string version)
    {
        try
        {
            var builder = new Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder()
                .AddText("UpdateNotification.Title".GetLocalizedResource())
                .AddText(string.Format("UpdateNotification.Subtitle".GetLocalizedResource(), version))
                .SetTag("app-update")
                .SetGroup("update")
                .AddButton(new Microsoft.Windows.AppNotifications.Builder.AppNotificationButton(
                    "UpdateNotification.Action".GetLocalizedResource())
                    .AddArgument("notificationType", "update")
                    .AddArgument("action", "download"));

            var notification = builder.BuildNotification();
            notification.ExpiresOnReboot = true;
            Microsoft.Windows.AppNotifications.AppNotificationManager.Default.Show(notification);
        }
        catch
        {
            // Notification system may not be registered; ignore
        }
    }
}

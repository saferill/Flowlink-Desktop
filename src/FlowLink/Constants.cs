namespace FlowLink;
public static class Constants
{
    public static class BatteryAlerts
    {
        public const int DefaultThreshold = 20;
        public const int MinThreshold = 5;
        public const int MaxThreshold = 50;
    }

    public static class Notification
    {
        public const string FileTransferGroup = "file-transfer";
        public const string BatteryGroup = "battery";

        public static string GetBatteryTag(string deviceId) => $"battery_{deviceId}";
        public const string IncomingPhoneCallGroup = "incoming-phone-call";
    }

    public static class ToastNotificationType
    {
        public const string FileTransfer = "FileTransfer";
        public const string RemoteNotification = "RemoteNotification";
        public const string Clipboard = "Clipboard";
        public const string Update = "Update";
        public const string IncomingPhoneCall = "IncomingPhoneCall";
    }
    public static class LocalSettings
    {
        public const string DateTimeFormat = "datetimeformat";

        public const string SettingsFolderName = "settings";
        public const string UserSettingsFileName = "user_settings.json";
        public const string PhoneFrameScrollTeachingTipShown = "PhoneFrameScrollTeachingTipShown";
        public const string MainNavigationSelection = "MainNavigationSelection";
        public const string DatabaseFileName = "flowlink.db";
        public static readonly string ConnectionString = $"Filename={Path.Combine(ApplicationData.Current.LocalFolder.Path, DatabaseFileName)}";
    }

    public static class ExternalUrl
    {
        public const string ReleasesUrl = @"https://github.com/safe_rill/FlowLink/releases/latest";
        public const string AndroidGitHubRepoUrl = @"https://github.com/safe_rill/FlowLink-Android";
        public const string GitHubRepoUrl = @"https://github.com/safe_rill/FlowLink";
        public const string DiscordUrl = @"https://discord.gg/MuvMqv4MES";
        public const string FeatureRequestUrl = @"https://github.com/safe_rill/FlowLink/issues";
        public const string BugReportUrl = @"https://github.com/safe_rill/FlowLink/issues";
        public const string PrivacyPolicyUrl = @"https://github.com/safe_rill/FlowLink/blob/master/Privacy.md";
        public const string LicenseUrl = @"https://github.com/safe_rill/FlowLink/blob/master/LICENSE";
        public const string DonateUrl = @"https://linktr.ee/safe_rill";
    }

    public static class UserEnvironmentPaths
    {
        public static readonly string DownloadsPath = GetDownloadsPath();
        public static readonly string UserProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        public static readonly string DefaultRemoteDevicePath = Path.Combine(UserProfilePath, "RemoteDevices");
        private static string GetDownloadsPath()
        {
            string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(homePath, "Downloads");
            
        }
    }
}

using FlowLink.Data.Contracts;
using FlowLink.Dialogs;

namespace FlowLink.Data.Models.Actions;

public partial class ProcessAction : BaseAction, IActionDialog
{
    public string Path { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string StartInDirectory { get; set; } = string.Empty;
    public Dictionary<string, string> EnvironmentVariables { get; set; } = [];
    public bool UseShellExecute { get; set; } = false;
    public bool CreateNoWindow { get; set; } = true;

    public Task ExecuteAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                var targetPath = Path;
                if (!System.IO.File.Exists(targetPath))
                {
                    var systemFile = System.IO.Path.Combine(Environment.SystemDirectory, targetPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? targetPath : targetPath + ".exe");
                    if (System.IO.File.Exists(systemFile))
                    {
                        targetPath = systemFile;
                    }
                }

                var psi = new ProcessStartInfo(targetPath)
                {
                    Arguments = Arguments ?? string.Empty,
                    UseShellExecute = UseShellExecute,
                    CreateNoWindow = CreateNoWindow,
                    WorkingDirectory = string.IsNullOrWhiteSpace(StartInDirectory) ? Environment.SystemDirectory : StartInDirectory,
                };

                foreach (var (key, value) in EnvironmentVariables)
                {
                    psi.EnvironmentVariables[key] = value;
                }

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error executing process: {ex.Message}");
            }
        });
    }

    public async Task<BaseAction?> ShowDialogAsync(XamlRoot xamlRoot)
    {
        var dialog = new ProcessActionDialog(this)
        {
            XamlRoot = xamlRoot
        };

        if (await dialog.ShowAsync() is ContentDialogResult.Primary)
        {
            return dialog.Result;
        }

        return null;
    }
}

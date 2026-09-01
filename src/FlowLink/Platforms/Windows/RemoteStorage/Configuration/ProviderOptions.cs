using System.ComponentModel.DataAnnotations;

namespace FlowLink.Platforms.Windows.RemoteStorage.Configuration;

public record ProviderOptions
{
    [Required]
    public string ProviderId { get; set; } = string.Empty;
}

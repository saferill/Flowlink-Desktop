using FlowLink.Data.Contracts;
using FlowLink.Services;

namespace FlowLink.Platforms.Desktop.Services;

public class DesktopActionService(
    IGeneralSettingsService generalSettingsService, 
    IUserSettingsService userSettingsService,
    ISessionManager sessionManager, 
    ILogger<DesktopActionService> logger) : BaseActionService(generalSettingsService, userSettingsService, sessionManager, logger)
{
}

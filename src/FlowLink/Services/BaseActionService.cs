using FlowLink.Data.Contracts;
using FlowLink.Data.Models;
using FlowLink.Data.Models.Actions;

namespace FlowLink.Services;

public abstract class BaseActionService(
    IGeneralSettingsService generalSettingsService,
    IUserSettingsService userSettingsService,
    ISessionManager sessionManager,
    ILogger logger) : IActionService
{
    public virtual Task InitializeAsync()
    {
        sessionManager.ConnectionStatusChanged += OnConnectionStatusChanged;
        if (ApplicationData.Current.LocalSettings.Values["DefaultActionsLoaded"] is null)
        {
            ApplicationData.Current.LocalSettings.Values["DefaultActionsLoaded"] = true;
            var defaultActions = DefaultActionsProvider.GetDefaultActions();
            userSettingsService.GeneralSettingsService.Actions = [.. defaultActions];
        }

        return Task.CompletedTask;
    }

    private void OnConnectionStatusChanged(object? sender, PairedDevice device)
    {
        if (device.IsConnected)
        {
            var actions = generalSettingsService.Actions;
            if (actions is null || actions.Count == 0)
            {
                var defaultActions = DefaultActionsProvider.GetDefaultActions();
                userSettingsService.GeneralSettingsService.Actions = [.. defaultActions];
                actions = userSettingsService.GeneralSettingsService.Actions;
            }

            foreach (var action in actions)
            {
                var actionMessage = new ActionInfo { ActionId = action.Id, ActionName = action.Name };
                device.SendMessage(actionMessage);
            }
        }
    }

    public virtual void HandleActionMessage(ActionInfo action)
    {
        logger.Info($"Executing action: {action.ActionName} ({action.ActionId})");
        var actionToExecute = generalSettingsService.Actions?.FirstOrDefault(a => a.Id.Equals(action.ActionId, StringComparison.OrdinalIgnoreCase));

        if (actionToExecute is ProcessAction processAction)
        {
            processAction.ExecuteAsync();
            return;
        }

        // Fallback to default actions
        var defaultAction = DefaultActionsProvider.GetDefaultActions().FirstOrDefault(a => a.Id.Equals(action.ActionId, StringComparison.OrdinalIgnoreCase));
        if (defaultAction is ProcessAction defaultProcessAction)
        {
            defaultProcessAction.ExecuteAsync();
        }
    }
} 

using FlowLink.Data.Models;

namespace FlowLink.Data.Contracts;

public interface IActionService
{
    /// <summary>
    /// Handle actions
    /// </summary>
    void HandleActionMessage(ActionInfo action);

    /// <summary>
    /// Initializes the service.
    /// </summary>
    Task InitializeAsync();
}

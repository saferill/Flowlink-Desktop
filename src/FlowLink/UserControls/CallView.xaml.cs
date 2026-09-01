using FlowLink.ViewModels;

namespace FlowLink.UserControls;

public sealed partial class CallView : UserControl
{
    public CallWindowViewModel ViewModel { get; }

    public CallView(CallWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}

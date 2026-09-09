using JobApplyAI.Mobile.ViewModels;

namespace JobApplyAI.Mobile.Views;

public partial class ResumesPage : ContentPage
{
    private readonly ResumesViewModel _viewModel;

    public ResumesPage(ResumesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadCommand.Execute(null);
    }
}

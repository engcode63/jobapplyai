using JobApplyAI.Mobile.ViewModels;

namespace JobApplyAI.Mobile.Views;

public partial class ApplicationsPage : ContentPage
{
    private readonly ApplicationsViewModel _viewModel;

    public ApplicationsPage(ApplicationsViewModel viewModel)
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

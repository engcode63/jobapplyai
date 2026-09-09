using JobApplyAI.Mobile.ViewModels;

namespace JobApplyAI.Mobile.Views;

public partial class CoverLettersPage : ContentPage
{
    private readonly CoverLettersViewModel _viewModel;

    public CoverLettersPage(CoverLettersViewModel viewModel)
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

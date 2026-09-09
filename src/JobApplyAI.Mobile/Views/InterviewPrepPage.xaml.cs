using JobApplyAI.Mobile.ViewModels;

namespace JobApplyAI.Mobile.Views;

public partial class InterviewPrepPage : ContentPage
{
    private readonly InterviewPrepViewModel _viewModel;

    public InterviewPrepPage(InterviewPrepViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadPostingsCommand.Execute(null);
    }
}

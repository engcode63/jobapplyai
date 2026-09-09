using JobApplyAI.Mobile.ViewModels;

namespace JobApplyAI.Mobile.Views;

public partial class JobSearchPage : ContentPage
{
    public JobSearchPage(JobSearchViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using StudentInnovation.WpfApp.Services;
using StudentInnovation.WpfApp.ViewModels;
using StudentInnovation.Shared.Models.Dtos;
using System.ComponentModel;

namespace StudentInnovation.WpfApp.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private const double FormPanelClosedX = 520;

    public MainWindow(ApiClient apiClient, LoginResponse loginResponse)
    {
        InitializeComponent();
        _viewModel = new MainViewModel(apiClient, loginResponse);
        DataContext = _viewModel;
        _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainViewModel.IsFormOpen))
        {
            return;
        }

        Dispatcher.BeginInvoke(() =>
        {
            AnimateFormPanel(_viewModel.IsFormOpen);
        });
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadAsync();
        if (_viewModel.IsAdmin)
        {
            await _viewModel.LoadUsersAsync();
        }

        // 确保初始状态与 ViewModel 一致
        AnimateFormPanel(_viewModel.IsFormOpen);
    }

    private void AnimateFormPanel(bool isOpen)
    {
        if (isOpen)
        {
            FormPanel.IsHitTestVisible = true;
            AnimateTranslate(FormPanelTransform, FormPanelClosedX, 0);
            AnimateOpacity(FormPanel, 0, 1);
        }
        else
        {
            FormPanel.IsHitTestVisible = false;
            AnimateTranslate(FormPanelTransform, 0, FormPanelClosedX);
            AnimateOpacity(FormPanel, 1, 0);
        }
    }

    private static void AnimateTranslate(TranslateTransform transform, double fromX, double toX)
    {
        var anim = new DoubleAnimation
        {
            From = fromX,
            To = toX,
            Duration = TimeSpan.FromMilliseconds(220),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
        };
        transform.BeginAnimation(TranslateTransform.XProperty, anim);
    }

    private static void AnimateOpacity(System.Windows.UIElement element, double from, double to)
    {
        var anim = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = TimeSpan.FromMilliseconds(220),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
        };
        element.BeginAnimation(System.Windows.UIElement.OpacityProperty, anim);
    }
}

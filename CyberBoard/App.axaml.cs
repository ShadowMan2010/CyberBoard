using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using CyberBoard.Core;
using CyberBoard.Models;
using CyberBoard.ViewModels;
using CyberBoard.Views;
using Microsoft.Extensions.DependencyInjection;

namespace CyberBoard;

public partial class App : Application
{
    public static ServiceProvider? Services { get; private set; }
    public static MainViewModel? MainVM { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        services.AddCyberBoardCore();
        Services = services.BuildServiceProvider();

        var vm = Services.GetRequiredService<MainViewModel>();
        MainVM = vm;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow
            {
                DataContext = vm
            };
            desktop.MainWindow = mainWindow;
            mainWindow.Show();

            vm.Theme.ThemeChanged += (_, mode) =>
            {
                mainWindow.RequestedThemeVariant = mode switch
                {
                    ThemeMode.Dark => ThemeVariant.Dark,
                    ThemeMode.Light => ThemeVariant.Light,
                    _ => ThemeVariant.Default
                };
                UpdateThemeResources(mode);
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void UpdateThemeResources(ThemeMode mode)
    {
        var isDark = mode == ThemeMode.Dark ||
                     (mode == ThemeMode.Auto &&
                      Avalonia.Application.Current?.RequestedThemeVariant == ThemeVariant.Dark);

        var resources = Application.Current?.Resources;
        if (resources == null) return;

        if (isDark)
        {
            resources["TitleBarBackground"] = new SolidColorBrush(0xFF1E1E1E);
            resources["PanelBackground"] = new SolidColorBrush(0xFF2D2D2D);
            resources["CanvasBackground"] = new SolidColorBrush(0xFF1A1A1A);
            resources["StatusBarBackground"] = new SolidColorBrush(0xFF1E1E1E);
            resources["TextColor"] = new SolidColorBrush(0xFFFFFFFF);
            resources["BorderBrush"] = new SolidColorBrush(0xFF3D3D3D);
            resources["LayerItemBackground"] = new SolidColorBrush(0xFF383838);
            resources["ToolButtonHover"] = new SolidColorBrush(0xFF3D3D3D);
            resources["ToolButtonChecked"] = new SolidColorBrush(0xFF4A4A4A);
            resources["ScrollBarBackground"] = new SolidColorBrush(0xFF2D2D2D);
            resources["ControlBackground"] = new SolidColorBrush(0xFF2D2D2D);
            resources["ControlForeground"] = new SolidColorBrush(0xFFFFFFFF);
        }
        else
        {
            resources["TitleBarBackground"] = new SolidColorBrush(0xFFFFFFFF);
            resources["PanelBackground"] = new SolidColorBrush(0xFFF5F5F5);
            resources["CanvasBackground"] = new SolidColorBrush(0xFFF0F0F0);
            resources["StatusBarBackground"] = new SolidColorBrush(0xFFFFFFFF);
            resources["TextColor"] = new SolidColorBrush(0xFF000000);
            resources["BorderBrush"] = new SolidColorBrush(0xFFE0E0E0);
            resources["LayerItemBackground"] = new SolidColorBrush(0xFFF0F0F0);
            resources["ToolButtonHover"] = new SolidColorBrush(0xFFE0E0E0);
            resources["ToolButtonChecked"] = new SolidColorBrush(0xFFD0D0D0);
            resources["ScrollBarBackground"] = new SolidColorBrush(0xFFF0F0F0);
            resources["ControlBackground"] = new SolidColorBrush(0xFFFFFFFF);
            resources["ControlForeground"] = new SolidColorBrush(0xFF000000);
        }
    }
}

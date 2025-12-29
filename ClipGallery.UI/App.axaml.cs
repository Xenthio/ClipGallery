using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using ClipGallery.Core.Services;
using ClipGallery.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace ClipGallery.UI;

public partial class App : Application
{
    public new static App Current => (App)Application.Current!;
    public IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        // Register Core Services
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IClipScannerService, ClipScannerService>();
        services.AddSingleton<IMetadataService, MetadataService>();
        services.AddSingleton<IThumbnailService, ThumbnailService>();
        services.AddSingleton<IThumbnailPriorityService, ThumbnailPriorityService>();
        services.AddSingleton<IAudioExtractionService, AudioExtractionService>();
        services.AddSingleton<ITranscodeService, TranscodeService>();

        // Register ViewModels
        // Register ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<SettingsViewModel>();

        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // BindingPlugins.DataValidators.RemoveAt(0); // Optional: Disable data validation
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
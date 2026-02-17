using System.Windows;
using System.IO;
using System.Windows.Controls;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EscapeGameKiosk.Contracts;
using EscapeGameKiosk.Services;
using EscapeGameKiosk.State;
using EscapeGameKiosk.ViewModels;

namespace EscapeGameKiosk;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
  private IHost? _host;

  protected override void OnStartup(StartupEventArgs e)
  {
    base.OnStartup(e);

    _host = Host.CreateDefaultBuilder()
      .ConfigureAppConfiguration((context, config) =>
      {
        config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
      })
      .ConfigureServices((context, services) =>
      {
        // Configuration
        services.Configure<AppSettings>(context.Configuration.GetSection("AppSettings"));
        services.AddSingleton(sp =>
        {
          var settings = new AppSettings();
          context.Configuration.GetSection("AppSettings").Bind(settings);
          return settings;
        });

        // Services
        services.AddSingleton<IConfigurationValidator, ConfigurationValidator>();
        services.AddSingleton<IPasswordValidationService>(sp =>
          new PasswordValidationService(
            Constants.Lockout.BaseSeconds,
            Constants.Lockout.StepSeconds,
            Constants.Lockout.Threshold));
        services.AddSingleton<IExitSequenceService>(sp =>
          new ExitSequenceService(
            Constants.Security.DefaultExitSequence,
            TimeSpan.FromSeconds(Constants.Security.ExitTimeoutSeconds)));
        services.AddSingleton<IVideoSourceService, VideoSourceService>();
        services.AddSingleton<IKeyboardSecurityService, KeyboardSecurityService>();

        // State management
        services.AddSingleton<IKioskStateManager, KioskStateManager>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<PasswordViewModel>();
        services.AddTransient<VideoViewModel>();
        services.AddTransient<ExitViewModel>();

        // Views
        services.AddTransient<PasswordScreenView>();
        services.AddTransient<VideoScreenView>();
        services.AddTransient<ExitScreenView>();

        // Controllers and MainWindow - Use factory pattern to resolve circular dependency
        services.AddSingleton<MainWindow>(sp =>
        {
          var logger = sp.GetRequiredService<ILogger<MainWindow>>();
          var settings = sp.GetRequiredService<AppSettings>();
          var passwordService = sp.GetRequiredService<IPasswordValidationService>();
          var exitSequenceService = sp.GetRequiredService<IExitSequenceService>();
          var videoSourceService = sp.GetRequiredService<IVideoSourceService>();
          var keyboardSecurityService = sp.GetRequiredService<IKeyboardSecurityService>();
          var kioskStateManager = sp.GetRequiredService<IKioskStateManager>();

          // Create ViewModels
          var passwordViewModel = new PasswordViewModel(
            sp.GetRequiredService<ILogger<PasswordViewModel>>(),
            passwordService,
            settings);

          var videoViewModel = new VideoViewModel(
            sp.GetRequiredService<ILogger<VideoViewModel>>());

          var exitViewModel = new ExitViewModel(
            sp.GetRequiredService<ILogger<ExitViewModel>>(),
            passwordService,
            settings);

          // Create Views with ViewModels
          var passwordScreenView = new PasswordScreenView(passwordViewModel);
          var videoScreenView = new VideoScreenView(videoViewModel);
          var exitScreenView = new ExitScreenView(exitViewModel);

          // Create MainWindow first (needed for BaseHost/OverlayHost)
          var mainWindow = new MainWindow();

          // Create controllers after XAML initialization
          var screenStackController = new ScreenStackController(
            mainWindow.BaseHost,
            mainWindow.OverlayHost);

          var navigationController = new ScreenNavigationController(
            screenStackController,
            passwordScreenView,
            exitScreenView,
            new UserControl[] { videoScreenView });

          // Initialize MainWindow with all dependencies
          mainWindow.Initialize(
            logger,
            settings,
            passwordService,
            exitSequenceService,
            videoSourceService,
            keyboardSecurityService,
            kioskStateManager,
            navigationController,
            passwordScreenView,
            videoScreenView,
            exitScreenView,
            passwordViewModel,
            videoViewModel,
            exitViewModel);

          return mainWindow;
        });
      })
      .ConfigureLogging((context, logging) =>
      {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddDebug();
        logging.SetMinimumLevel(LogLevel.Debug);

        // Add file logging
        var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "log.txt");
        logging.AddFile(logPath, minimumLevel: LogLevel.Information);
      })
      .Build();

    // Validate configuration before showing MainWindow
    var logger = _host.Services.GetRequiredService<ILogger<App>>();
    var settings = _host.Services.GetRequiredService<AppSettings>();
    var validator = _host.Services.GetRequiredService<IConfigurationValidator>();

    var validationResult = validator.Validate(settings);
    if (!validationResult.IsValid)
    {
      string errorMessage = "Configuration validation failed:\n\n" +
                           string.Join("\n", validationResult.Errors) +
                           "\n\nThe application will now exit.";

      logger.LogError("Configuration validation failed: {Errors}", string.Join("; ", validationResult.Errors));

      MessageBox.Show(
        errorMessage,
        "Configuration Error - EscapeGameKiosk",
        MessageBoxButton.OK,
        MessageBoxImage.Error);

      Shutdown(1);
      return;
    }

    logger.LogInformation("Configuration validation successful");
    logger.LogInformation("Application starting with dependency injection enabled");

    // Show MainWindow
    var mainWindow = _host.Services.GetRequiredService<MainWindow>();
    mainWindow.Show();
  }

  protected override void OnExit(ExitEventArgs e)
  {
    _host?.Dispose();
    base.OnExit(e);
  }
}

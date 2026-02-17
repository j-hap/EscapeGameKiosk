using EscapeGameKiosk.ViewModels;
using Microsoft.Extensions.Logging;

namespace EscapeGameKiosk.Tests.ViewModels;

public sealed class MainViewModelTests
{
  [Fact]
  public void ShowPasswordScreen_SetsVisibilityFlags()
  {
    // Arrange
    ILogger<MainViewModel> logger = Mock.Of<ILogger<MainViewModel>>();
    var vm = new MainViewModel(logger);

    vm.IsPasswordScreenVisible = false;
    vm.IsVideoScreenVisible = true;
    vm.IsExitScreenVisible = true;

    // Act
    vm.ShowPasswordScreen();

    // Assert
    vm.IsPasswordScreenVisible.Should().BeTrue();
    vm.IsVideoScreenVisible.Should().BeFalse();
    vm.IsExitScreenVisible.Should().BeFalse();
  }

  [Fact]
  public void ShowVideoScreen_SetsVisibilityFlags()
  {
    // Arrange
    ILogger<MainViewModel> logger = Mock.Of<ILogger<MainViewModel>>();
    var vm = new MainViewModel(logger);

    // Act
    vm.ShowVideoScreen();

    // Assert
    vm.IsPasswordScreenVisible.Should().BeFalse();
    vm.IsVideoScreenVisible.Should().BeTrue();
    vm.IsExitScreenVisible.Should().BeFalse();
  }

  [Fact]
  public void ShowExitScreen_OnlySetsExitOverlayFlag()
  {
    // Arrange
    ILogger<MainViewModel> logger = Mock.Of<ILogger<MainViewModel>>();
    var vm = new MainViewModel(logger);

    // Act
    vm.ShowExitScreen();

    // Assert
    vm.IsExitScreenVisible.Should().BeTrue();
  }

  [Fact]
  public void HideExitScreen_ClearsExitOverlayFlag()
  {
    // Arrange
    ILogger<MainViewModel> logger = Mock.Of<ILogger<MainViewModel>>();
    var vm = new MainViewModel(logger);

    vm.IsExitScreenVisible = true;

    // Act
    vm.HideExitScreen();

    // Assert
    vm.IsExitScreenVisible.Should().BeFalse();
  }

  [Fact]
  public void RequestClose_SetsAllowClose()
  {
    // Arrange
    ILogger<MainViewModel> logger = Mock.Of<ILogger<MainViewModel>>();
    var vm = new MainViewModel(logger);

    // Act
    vm.RequestClose();

    // Assert
    vm.AllowClose.Should().BeTrue();
  }
}

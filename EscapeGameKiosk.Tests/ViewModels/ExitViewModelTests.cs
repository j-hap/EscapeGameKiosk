using EscapeGameKiosk.Services;
using EscapeGameKiosk.ViewModels;
using Microsoft.Extensions.Logging;

namespace EscapeGameKiosk.Tests.ViewModels;

public sealed class ExitViewModelTests
{
  [StaFact]
  public void ConfirmExitCommand_WithCorrectPassword_RaisesExitConfirmed()
  {
    // Arrange
    var settings = new AppSettings { Password = "admin" };

    var passwordService = new Mock<IPasswordValidationService>(MockBehavior.Strict);
    passwordService
      .Setup(s => s.ValidatePassword("admin", "admin"))
      .Returns(true);

    ILogger<ExitViewModel> logger = Mock.Of<ILogger<ExitViewModel>>();

    var vm = new ExitViewModel(logger, passwordService.Object, settings)
    {
      ExitPassword = "admin"
    };

    bool exitRaised = false;
    vm.ExitConfirmed += (_, _) => exitRaised = true;

    // Act
    vm.ConfirmExitCommand.Execute(null);

    // Assert
    exitRaised.Should().BeTrue();
    passwordService.VerifyAll();
  }

  [StaFact]
  public void ConfirmExitCommand_WithIncorrectPassword_ClearsExitPassword()
  {
    // Arrange
    var settings = new AppSettings { Password = "admin" };

    var passwordService = new Mock<IPasswordValidationService>(MockBehavior.Strict);
    passwordService
      .Setup(s => s.ValidatePassword("wrong", "admin"))
      .Returns(false);

    ILogger<ExitViewModel> logger = Mock.Of<ILogger<ExitViewModel>>();

    var vm = new ExitViewModel(logger, passwordService.Object, settings)
    {
      ExitPassword = "wrong"
    };

    // Act
    vm.ConfirmExitCommand.Execute(null);

    // Assert
    vm.ExitPassword.Should().BeEmpty();
    passwordService.VerifyAll();
  }

  [StaFact]
  public void CancelExitCommand_ClearsExitPassword_AndRaisesExitCancelled()
  {
    // Arrange
    var settings = new AppSettings { Password = "admin" };
    var passwordService = Mock.Of<IPasswordValidationService>();
    ILogger<ExitViewModel> logger = Mock.Of<ILogger<ExitViewModel>>();

    var vm = new ExitViewModel(logger, passwordService, settings)
    {
      ExitPassword = "admin"
    };

    bool cancelRaised = false;
    vm.ExitCancelled += (_, _) => cancelRaised = true;

    // Act
    vm.CancelExitCommand.Execute(null);

    // Assert
    cancelRaised.Should().BeTrue();
    vm.ExitPassword.Should().BeEmpty();
  }
}

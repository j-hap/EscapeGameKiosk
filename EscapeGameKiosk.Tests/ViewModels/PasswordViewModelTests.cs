using EscapeGameKiosk.Services;
using EscapeGameKiosk.ViewModels;
using Microsoft.Extensions.Logging;

namespace EscapeGameKiosk.Tests.ViewModels;

public sealed class PasswordViewModelTests
{
  [StaFact]
  public void UnlockCommand_WithCorrectPassword_RaisesUnlockRequested_AndClearsPassword()
  {
    // Arrange
    var settings = new AppSettings { Password = "admin" };

    var passwordService = new Mock<IPasswordValidationService>(MockBehavior.Strict);
    passwordService
      .Setup(s => s.ValidatePassword("admin", "admin"))
      .Returns(true);
    passwordService
      .Setup(s => s.ResetFailedAttempts());

    ILogger<PasswordViewModel> logger = Mock.Of<ILogger<PasswordViewModel>>();

    var vm = new PasswordViewModel(logger, passwordService.Object, settings)
    {
      Password = "admin"
    };

    bool unlockRaised = false;
    vm.UnlockRequested += (_, _) => unlockRaised = true;

    // Act
    vm.UnlockCommand.Execute(null);

    // Assert
    unlockRaised.Should().BeTrue();
    vm.Password.Should().BeEmpty();
    vm.RevealedPassword.Should().BeEmpty();
    vm.IsAccessDeniedActive.Should().BeFalse();

    passwordService.VerifyAll();
  }

  [StaFact]
  public void UnlockCommand_WithIncorrectPassword_ShowsAccessDenied_AndTriggersWiggle()
  {
    // Arrange
    var settings = new AppSettings { Password = "admin" };

    var passwordService = new Mock<IPasswordValidationService>(MockBehavior.Strict);
    passwordService
      .Setup(s => s.ValidatePassword("wrong", "admin"))
      .Returns(false);
    passwordService
      .Setup(s => s.RegisterFailedAttempt())
      .Returns(5);
    passwordService
      .SetupGet(s => s.FailedAttemptCount)
      .Returns(1);

    ILogger<PasswordViewModel> logger = Mock.Of<ILogger<PasswordViewModel>>();

    var vm = new PasswordViewModel(logger, passwordService.Object, settings)
    {
      Password = "wrong"
    };

    bool wiggleRaised = false;
    vm.TriggerWiggleAnimation += (_, _) => wiggleRaised = true;

    // Act
    vm.UnlockCommand.Execute(null);

    // Assert
    wiggleRaised.Should().BeTrue();
    vm.IsAccessDeniedActive.Should().BeTrue();
    vm.IsUnlockButtonEnabled.Should().BeFalse();
    vm.RemainingSeconds.Should().BeGreaterThan(0);
    vm.HeaderText.Should().Contain(Constants.UI.AccessDeniedText);

    passwordService.VerifyAll();
  }

  [StaFact]
  public void ClearPassword_RaisesPasswordCleared_Event()
  {
    // Arrange
    var settings = new AppSettings { Password = "admin" };
    var passwordService = Mock.Of<IPasswordValidationService>();
    ILogger<PasswordViewModel> logger = Mock.Of<ILogger<PasswordViewModel>>();

    var vm = new PasswordViewModel(logger, passwordService, settings)
    {
      Password = "abc",
      RevealedPassword = "abc"
    };

    bool clearedRaised = false;
    vm.PasswordCleared += (_, _) => clearedRaised = true;

    // Act
    vm.ClearPassword();

    // Assert
    clearedRaised.Should().BeTrue();
    vm.Password.Should().BeEmpty();
    vm.RevealedPassword.Should().BeEmpty();
  }
}

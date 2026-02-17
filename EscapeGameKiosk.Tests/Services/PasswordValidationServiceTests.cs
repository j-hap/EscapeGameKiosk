using EscapeGameKiosk.Services;
using FluentAssertions;
using Xunit;

namespace EscapeGameKiosk.Tests.Services;

public class PasswordValidationServiceTests
{
  private const int BaseSeconds = 5;
  private const int StepSeconds = 5;
  private const int Threshold = 3;
  private const string ExpectedPassword = "admin";

  [Fact]
  public void ValidatePassword_WithCorrectPassword_ReturnsTrue()
  {
    // Arrange
    var service = new PasswordValidationService(BaseSeconds, StepSeconds, Threshold);
    const string correctPassword = ExpectedPassword;

    // Act
    bool result = service.ValidatePassword(correctPassword, ExpectedPassword);

    // Assert
    result.Should().BeTrue();
  }

  [Fact]
  public void ValidatePassword_WithIncorrectPassword_ReturnsFalse()
  {
    // Arrange
    var service = new PasswordValidationService(BaseSeconds, StepSeconds, Threshold);
    const string incorrectPassword = "wrong";

    // Act
    bool result = service.ValidatePassword(incorrectPassword, ExpectedPassword);

    // Assert
    result.Should().BeFalse();
  }

  [Fact]
  public void ValidatePassword_WithEmptyPassword_ReturnsFalse()
  {
    // Arrange
    var service = new PasswordValidationService(BaseSeconds, StepSeconds, Threshold);

    // Act
    bool result = service.ValidatePassword("", ExpectedPassword);

    // Assert
    result.Should().BeFalse();
  }

  [Theory]
  [InlineData("admin", true)]
  [InlineData("Admin", false)]
  [InlineData("ADMIN", false)]
  [InlineData("admin ", false)]
  [InlineData(" admin", false)]
  public void ValidatePassword_IsCaseSensitive(string password, bool expected)
  {
    // Arrange
    var service = new PasswordValidationService(BaseSeconds, StepSeconds, Threshold);

    // Act
    bool result = service.ValidatePassword(password, ExpectedPassword);

    // Assert
    result.Should().Be(expected);
  }

  [Fact]
  public void RegisterFailedAttempt_WithNoPriorFailures_ReturnsBaseSeconds()
  {
    // Arrange
    var service = new PasswordValidationService(BaseSeconds, StepSeconds, Threshold);

    // Act
    int lockoutSeconds = service.RegisterFailedAttempt();

    // Assert
    lockoutSeconds.Should().Be(BaseSeconds);
    service.FailedAttemptCount.Should().Be(1);
    service.IsLockedOut.Should().BeTrue();
  }

  [Fact]
  public void RegisterFailedAttempt_UnderThreshold_DoesNotIncreaseTier()
  {
    // Arrange
    var service = new PasswordValidationService(BaseSeconds, StepSeconds, Threshold);

    // Act - 2 failures (under threshold of 3)
    int first = service.RegisterFailedAttempt();
    int second = service.RegisterFailedAttempt();

    // Assert
    first.Should().Be(BaseSeconds);
    second.Should().Be(BaseSeconds);
    service.FailedAttemptCount.Should().Be(2);
  }

  [Fact]
  public void RegisterFailedAttempt_AtThreshold_IncreasesByOneStep()
  {
    // Arrange
    var service = new PasswordValidationService(BaseSeconds, StepSeconds, Threshold);

    // Act - 3 failures (at threshold)
    service.RegisterFailedAttempt();
    service.RegisterFailedAttempt();
    int third = service.RegisterFailedAttempt();

    // Assert
    third.Should().Be(BaseSeconds + StepSeconds);
    service.FailedAttemptCount.Should().Be(3);
  }

  [Fact]
  public void RegisterFailedAttempt_InSecondTier_ReturnsIncreasedDuration()
  {
    // Arrange
    var service = new PasswordValidationService(BaseSeconds, StepSeconds, Threshold);

    // Act - 6 failures -> tier 2 (integer division: 6/3 == 2)
    int lockoutSeconds = 0;
    for (int i = 0; i < 6; i++)
    {
      lockoutSeconds = service.RegisterFailedAttempt();
    }

    // Assert
    lockoutSeconds.Should().Be(BaseSeconds + (2 * StepSeconds));
    service.FailedAttemptCount.Should().Be(6);
  }

  [Fact]
  public void RegisterFailedAttempt_InThirdTier_ReturnsFurtherIncreasedDuration()
  {
    // Arrange
    var service = new PasswordValidationService(BaseSeconds, StepSeconds, Threshold);

    // Act - 9 failures -> tier 3 (integer division: 9/3 == 3)
    int lockoutSeconds = 0;
    for (int i = 0; i < 9; i++)
    {
      lockoutSeconds = service.RegisterFailedAttempt();
    }

    // Assert
    lockoutSeconds.Should().Be(BaseSeconds + (3 * StepSeconds));
    service.FailedAttemptCount.Should().Be(9);
  }

  [Fact]
  public void ResetFailedAttempts_AfterFailures_ClearsLockoutState()
  {
    // Arrange
    var service = new PasswordValidationService(BaseSeconds, StepSeconds, Threshold);

    // Act - Fail several times then reset
    service.RegisterFailedAttempt();
    service.RegisterFailedAttempt();
    service.RegisterFailedAttempt();
    service.ResetFailedAttempts();

    // Assert
    service.FailedAttemptCount.Should().Be(0);
    service.IsLockedOut.Should().BeFalse();
  }

  [Fact]
  public void FailedAttemptCount_Property_TracksFailedPasswordAttempts()
  {
    // Arrange
    var service = new PasswordValidationService(BaseSeconds, StepSeconds, Threshold);

    // Act & Assert
    service.FailedAttemptCount.Should().Be(0);
    service.IsLockedOut.Should().BeFalse();

    service.RegisterFailedAttempt();
    service.FailedAttemptCount.Should().Be(1);
    service.IsLockedOut.Should().BeTrue();

    service.RegisterFailedAttempt();
    service.FailedAttemptCount.Should().Be(2);

    service.ResetFailedAttempts();
    service.FailedAttemptCount.Should().Be(0);
    service.IsLockedOut.Should().BeFalse();
  }

  [Fact]
  public void RegisterFailedAttempt_WithHighNumberOfFailures_UsesExpectedFormula()
  {
    // Arrange
    var service = new PasswordValidationService(BaseSeconds, StepSeconds, Threshold);

    // Act - Fail 100 times
    int lockoutSeconds = 0;
    for (int i = 0; i < 100; i++)
    {
      lockoutSeconds = service.RegisterFailedAttempt();
    }

    // Assert
    // tier = failedAttempts/threshold (integer division)
    int expectedTier = 100 / Threshold;
    int expectedSeconds = BaseSeconds + (expectedTier * StepSeconds);
    lockoutSeconds.Should().Be(expectedSeconds);
    service.FailedAttemptCount.Should().Be(100);
  }
}

namespace EscapeGameKiosk.Services;

/// <summary>
/// Implementation of password validation and lockout management.
/// </summary>
public sealed class PasswordValidationService : IPasswordValidationService
{
  private readonly int _baseLockoutSeconds;
  private readonly int _lockoutStepSeconds;
  private readonly int _lockoutThreshold;
  private int _failedAttempts;

  public PasswordValidationService(
    int baseLockoutSeconds = 5,
    int lockoutStepSeconds = 5,
    int lockoutThreshold = 3)
  {
    _baseLockoutSeconds = baseLockoutSeconds;
    _lockoutStepSeconds = lockoutStepSeconds;
    _lockoutThreshold = lockoutThreshold;
    _failedAttempts = 0;
  }

  public bool ValidatePassword(string providedPassword, string expectedPassword)
  {
    return providedPassword == expectedPassword;
  }

  public int RegisterFailedAttempt()
  {
    _failedAttempts++;
    return CalculateLockoutSeconds();
  }

  public void ResetFailedAttempts()
  {
    _failedAttempts = 0;
  }

  public int FailedAttemptCount => _failedAttempts;

  public bool IsLockedOut => _failedAttempts > 0;

  private int CalculateLockoutSeconds()
  {
    int tier = _failedAttempts / _lockoutThreshold;
    return _baseLockoutSeconds + (tier * _lockoutStepSeconds);
  }
}

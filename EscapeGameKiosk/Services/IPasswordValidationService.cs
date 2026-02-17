namespace EscapeGameKiosk.Services;

/// <summary>
/// Service for password validation and lockout management.
/// </summary>
public interface IPasswordValidationService
{
  /// <summary>
  /// Validates if the provided password matches the expected password.
  /// </summary>
  /// <param name="providedPassword">The password to validate.</param>
  /// <param name="expectedPassword">The expected password.</param>
  /// <returns>True if passwords match; otherwise, false.</returns>
  bool ValidatePassword(string providedPassword, string expectedPassword);

  /// <summary>
  /// Registers a failed password attempt and returns the lockout duration.
  /// </summary>
  /// <returns>The lockout duration in seconds.</returns>
  int RegisterFailedAttempt();

  /// <summary>
  /// Resets the failed attempt counter (called after successful login).
  /// </summary>
  void ResetFailedAttempts();

  /// <summary>
  /// Gets the current number of failed attempts.
  /// </summary>
  int FailedAttemptCount { get; }

  /// <summary>
  /// Gets whether the service is currently in lockout state.
  /// </summary>
  bool IsLockedOut { get; }
}

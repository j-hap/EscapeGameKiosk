namespace EscapeGameKiosk.Services;

/// <summary>
/// Validates application configuration settings.
/// </summary>
public interface IConfigurationValidator
{
  /// <summary>
  /// Validates the application settings.
  /// </summary>
  /// <param name="settings">The settings to validate.</param>
  /// <returns>A validation result containing success status and error messages.</returns>
  ValidationResult Validate(AppSettings settings);
}

/// <summary>
/// Represents the result of a configuration validation.
/// </summary>
public sealed class ValidationResult
{
  /// <summary>
  /// Gets a value indicating whether validation was successful.
  /// </summary>
  public bool IsValid { get; init; }

  /// <summary>
  /// Gets the list of validation error messages.
  /// </summary>
  public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

  /// <summary>
  /// Creates a successful validation result.
  /// </summary>
  public static ValidationResult Success() => new() { IsValid = true };

  /// <summary>
  /// Creates a failed validation result with error messages.
  /// </summary>
  /// <param name="errors">The validation error messages.</param>
  public static ValidationResult Failure(params string[] errors) =>
    new() { IsValid = false, Errors = errors };

  /// <summary>
  /// Creates a failed validation result with error messages.
  /// </summary>
  /// <param name="errors">The validation error messages.</param>
  public static ValidationResult Failure(IEnumerable<string> errors) =>
    new() { IsValid = false, Errors = errors.ToArray() };
}

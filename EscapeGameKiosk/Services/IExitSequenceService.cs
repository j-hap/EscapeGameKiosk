namespace EscapeGameKiosk.Services;

/// <summary>
/// Service for tracking the secret exit sequence.
/// </summary>
public interface IExitSequenceService
{
  /// <summary>
  /// Registers a corner tap and checks if the exit sequence is complete.
  /// </summary>
  /// <param name="region">The corner region (0=top-left, 1=top-right, 2=bottom-right, 3=bottom-left).</param>
  /// <returns>True if the sequence is complete; otherwise, false.</returns>
  bool RegisterTap(int region);

  /// <summary>
  /// Resets the exit sequence progress.
  /// </summary>
  void Reset();

  /// <summary>
  /// Gets the current progress in the exit sequence (0 to sequence length).
  /// </summary>
  int CurrentProgress { get; }

  /// <summary>
  /// Gets the total length of the exit sequence.
  /// </summary>
  int SequenceLength { get; }

  /// <summary>
  /// Gets whether the sequence tracking has timed out and needs reset.
  /// </summary>
  bool HasTimedOut { get; }

  /// <summary>
  /// Updates the last tap timestamp (should be called on each tap).
  /// </summary>
  void UpdateTimestamp();
}

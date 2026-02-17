using System;

namespace EscapeGameKiosk.Services;

/// <summary>
/// Implementation of exit sequence tracking.
/// </summary>
public sealed class ExitSequenceService : IExitSequenceService
{
  private readonly int[] _exitSequence;
  private readonly TimeSpan _timeout;
  private int _currentProgress;
  private DateTime _lastTapTime;

  public ExitSequenceService(int[]? exitSequence = null, TimeSpan? timeout = null)
  {
    _exitSequence = exitSequence ?? [0, 2, 1, 3];
    _timeout = timeout ?? TimeSpan.FromSeconds(4);
    _currentProgress = 0;
    _lastTapTime = DateTime.MinValue;
  }

  public bool RegisterTap(int region)
  {
    if (HasTimedOut)
    {
      Reset();
    }

    UpdateTimestamp();

    int expectedRegion = _exitSequence[_currentProgress];
    
    if (region == expectedRegion)
    {
      _currentProgress++;
      
      if (_currentProgress >= _exitSequence.Length)
      {
        return true;
      }
      
      return false;
    }

    // Mismatch: check if it's the start of the sequence
    if (region == _exitSequence[0])
    {
      _currentProgress = 1;
    }
    else
    {
      _currentProgress = 0;
    }

    return false;
  }

  public void Reset()
  {
    _currentProgress = 0;
  }

  public void UpdateTimestamp()
  {
    _lastTapTime = DateTime.UtcNow;
  }

  public int CurrentProgress => _currentProgress;

  public int SequenceLength => _exitSequence.Length;

  public bool HasTimedOut => DateTime.UtcNow - _lastTapTime > _timeout;
}

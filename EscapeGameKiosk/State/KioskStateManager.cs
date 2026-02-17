using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace EscapeGameKiosk.State;

public sealed class KioskStateManager : IKioskStateManager
{
  private readonly ILogger<KioskStateManager> _logger;
  private readonly List<KioskState> _history;

  private KioskState _currentState;

  // Used to return from ExitConfirmation back to the base state.
  private KioskState? _stateBeforeExitConfirmation;

  public KioskStateManager(ILogger<KioskStateManager> logger)
  {
    _logger = logger;
    _currentState = KioskState.Initializing;
    _history = new List<KioskState> { _currentState };
  }

  public KioskState CurrentState => _currentState;

  public IReadOnlyList<KioskState> StateHistory => _history;

  public event EventHandler<KioskStateChangedEventArgs>? StateChanged;

  public bool CanTransitionTo(KioskState newState)
  {
    if (newState == _currentState)
    {
      return true;
    }

    // Special-case: ExitConfirmation is an overlay state that can be entered from most states.
    if (newState == KioskState.ExitConfirmation)
    {
      return _currentState is KioskState.PasswordEntry or KioskState.VideoPlayback or KioskState.Locked;
    }

    // Special-case: leaving ExitConfirmation returns to the state we came from.
    if (_currentState == KioskState.ExitConfirmation)
    {
      return newState == (_stateBeforeExitConfirmation ?? KioskState.PasswordEntry);
    }

    return (_currentState, newState) switch
    {
      (KioskState.Initializing, KioskState.PasswordEntry) => true,
      (KioskState.PasswordEntry, KioskState.VideoPlayback) => true,
      (KioskState.VideoPlayback, KioskState.PasswordEntry) => true,
      (KioskState.PasswordEntry, KioskState.Locked) => true,
      (KioskState.Locked, KioskState.PasswordEntry) => true,
      _ => false
    };
  }

  public bool TryTransitionTo(KioskState newState, string reason, out string? error)
  {
    error = null;

    if (!CanTransitionTo(newState))
    {
      error = $"Transition not allowed: {_currentState} -> {newState}";
      _logger.LogWarning("{Error} (Reason: {Reason})", error, reason);
      return false;
    }

    ForceTransitionTo(newState, reason);
    return true;
  }

  public void ForceTransitionTo(KioskState newState, string reason)
  {
    if (newState == _currentState)
    {
      _logger.LogDebug("State unchanged: {State} (Reason: {Reason})", _currentState, reason);
      return;
    }

    KioskState previous = _currentState;

    if (newState == KioskState.ExitConfirmation)
    {
      _stateBeforeExitConfirmation = _currentState;
    }

    _currentState = newState;
    _history.Add(newState);

    _logger.LogInformation("State transition: {Previous} -> {New} (Reason: {Reason})", previous, newState, reason);
    StateChanged?.Invoke(this, new KioskStateChangedEventArgs(previous, newState, reason));
  }

  public bool TryRollback(string reason, out KioskState rolledBackTo)
  {
    rolledBackTo = _currentState;

    // Rollback from ExitConfirmation goes back to the base state we came from.
    if (_currentState == KioskState.ExitConfirmation)
    {
      KioskState target = _stateBeforeExitConfirmation ?? KioskState.PasswordEntry;
      ForceTransitionTo(target, reason);
      rolledBackTo = target;
      return true;
    }

    // Otherwise, rollback to the previous distinct state in history.
    for (int i = _history.Count - 2; i >= 0; i--)
    {
      if (_history[i] != _currentState)
      {
        ForceTransitionTo(_history[i], reason);
        rolledBackTo = _history[i];
        return true;
      }
    }

    return false;
  }
}

using System;
using System.Collections.Generic;

namespace EscapeGameKiosk.State;

public interface IKioskStateManager
{
  KioskState CurrentState { get; }

  IReadOnlyList<KioskState> StateHistory { get; }

  event EventHandler<KioskStateChangedEventArgs>? StateChanged;

  bool CanTransitionTo(KioskState newState);

  bool TryTransitionTo(KioskState newState, string reason, out string? error);

  void ForceTransitionTo(KioskState newState, string reason);

  bool TryRollback(string reason, out KioskState rolledBackTo);
}

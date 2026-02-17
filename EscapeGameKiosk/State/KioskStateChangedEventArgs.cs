using System;

namespace EscapeGameKiosk.State;

public sealed class KioskStateChangedEventArgs : EventArgs
{
  public KioskStateChangedEventArgs(KioskState previousState, KioskState newState, string reason)
  {
    PreviousState = previousState;
    NewState = newState;
    Reason = reason;
  }

  public KioskState PreviousState { get; }

  public KioskState NewState { get; }

  public string Reason { get; }
}

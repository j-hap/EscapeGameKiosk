using EscapeGameKiosk.State;
using Microsoft.Extensions.Logging;

namespace EscapeGameKiosk.Tests.State;

public sealed class KioskStateManagerTests
{
  [Fact]
  public void Constructor_StartsInInitializing_WithHistory()
  {
    ILogger<KioskStateManager> logger = Mock.Of<ILogger<KioskStateManager>>();
    var manager = new KioskStateManager(logger);

    manager.CurrentState.Should().Be(KioskState.Initializing);
    manager.StateHistory.Should().NotBeNull();
    manager.StateHistory.Should().ContainInOrder(KioskState.Initializing);
  }

  [Fact]
  public void TryTransitionTo_Allows_Initializing_To_PasswordEntry()
  {
    ILogger<KioskStateManager> logger = Mock.Of<ILogger<KioskStateManager>>();
    var manager = new KioskStateManager(logger);

    bool ok = manager.TryTransitionTo(KioskState.PasswordEntry, "Startup", out string? error);

    ok.Should().BeTrue();
    error.Should().BeNull();
    manager.CurrentState.Should().Be(KioskState.PasswordEntry);
  }

  [Fact]
  public void TryTransitionTo_Rejects_InvalidTransition()
  {
    ILogger<KioskStateManager> logger = Mock.Of<ILogger<KioskStateManager>>();
    var manager = new KioskStateManager(logger);

    bool ok = manager.TryTransitionTo(KioskState.VideoPlayback, "Invalid", out string? error);

    ok.Should().BeFalse();
    error.Should().NotBeNull();
    manager.CurrentState.Should().Be(KioskState.Initializing);
  }

  [Fact]
  public void TryRollback_FromExitConfirmation_ReturnsToPreviousBaseState()
  {
    ILogger<KioskStateManager> logger = Mock.Of<ILogger<KioskStateManager>>();
    var manager = new KioskStateManager(logger);

    manager.ForceTransitionTo(KioskState.PasswordEntry, "Startup");
    manager.TryTransitionTo(KioskState.ExitConfirmation, "ExitSequenceComplete", out _).Should().BeTrue();

    bool rolledBack = manager.TryRollback("ExitCancelled", out KioskState rolledBackTo);

    rolledBack.Should().BeTrue();
    rolledBackTo.Should().Be(KioskState.PasswordEntry);
    manager.CurrentState.Should().Be(KioskState.PasswordEntry);
  }

  [Fact]
  public void TryRollback_FromExitConfirmation_ReturnsToVideoPlayback_WhenEnteredFromVideoPlayback()
  {
    ILogger<KioskStateManager> logger = Mock.Of<ILogger<KioskStateManager>>();
    var manager = new KioskStateManager(logger);

    manager.ForceTransitionTo(KioskState.PasswordEntry, "Startup");
    manager.ForceTransitionTo(KioskState.VideoPlayback, "UnlockRequested");
    manager.TryTransitionTo(KioskState.ExitConfirmation, "ExitSequenceComplete", out _).Should().BeTrue();

    manager.TryRollback("ExitCancelled", out KioskState rolledBackTo).Should().BeTrue();
    rolledBackTo.Should().Be(KioskState.VideoPlayback);
    manager.CurrentState.Should().Be(KioskState.VideoPlayback);
  }

  [Fact]
  public void StateHistory_TracksTransitions()
  {
    ILogger<KioskStateManager> logger = Mock.Of<ILogger<KioskStateManager>>();
    var manager = new KioskStateManager(logger);

    manager.ForceTransitionTo(KioskState.PasswordEntry, "Startup");
    manager.ForceTransitionTo(KioskState.VideoPlayback, "UnlockRequested");

    manager.StateHistory.Should().ContainInOrder(
      KioskState.Initializing,
      KioskState.PasswordEntry,
      KioskState.VideoPlayback);
  }
}

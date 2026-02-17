using EscapeGameKiosk.Services;
using FluentAssertions;
using Xunit;

namespace EscapeGameKiosk.Tests.Services;

public class ExitSequenceServiceTests
{
  private static readonly int[] DefaultSequence = [0, 2, 1, 3];
  private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(4);

  [Fact]
  public void Constructor_InitializesWithCorrectSequence()
  {
    // Arrange & Act
    var service = new ExitSequenceService(DefaultSequence, DefaultTimeout);

    // Assert
    service.SequenceLength.Should().Be(4);
    service.CurrentProgress.Should().Be(0);
  }

  [Fact]
  public void RegisterTap_WithCorrectFirstRegion_IncreasesProgress()
  {
    // Arrange
    var service = new ExitSequenceService(DefaultSequence, DefaultTimeout);

    // Act
    bool complete = service.RegisterTap(0); // Top-left (first in sequence)

    // Assert
    complete.Should().BeFalse();
    service.CurrentProgress.Should().Be(1);
  }

  [Fact]
  public void RegisterTap_WithIncorrectFirstRegion_DoesNotIncreaseProgress()
  {
    // Arrange
    var service = new ExitSequenceService(DefaultSequence, DefaultTimeout);

    // Act
    bool complete = service.RegisterTap(1); // Wrong region

    // Assert
    complete.Should().BeFalse();
    service.CurrentProgress.Should().Be(0);
  }

  [Fact]
  public void RegisterTap_CompletingSequence_ReturnsTrue()
  {
    // Arrange
    var service = new ExitSequenceService(DefaultSequence, DefaultTimeout);

    // Act - Complete sequence: 0, 2, 1, 3
    service.RegisterTap(0).Should().BeFalse();
    service.RegisterTap(2).Should().BeFalse();
    service.RegisterTap(1).Should().BeFalse();
    bool complete = service.RegisterTap(3);

    // Assert
    complete.Should().BeTrue();
    service.CurrentProgress.Should().Be(4);
  }

  [Fact]
  public void RegisterTap_SequenceWithError_ResetsProgress()
  {
    // Arrange
    var service = new ExitSequenceService(DefaultSequence, DefaultTimeout);

    // Act
    service.RegisterTap(0); // Correct
    service.CurrentProgress.Should().Be(1);

    service.RegisterTap(1); // Wrong (should be 2)

    // Assert
    service.CurrentProgress.Should().Be(0);
  }

  [Fact]
  public void RegisterTap_PartialSequenceWithError_ResetsAndCanRestartCorrectly()
  {
    // Arrange
    var service = new ExitSequenceService(DefaultSequence, DefaultTimeout);

    // Act - Start, make error, restart
    service.RegisterTap(0); // 1st correct
    service.RegisterTap(2); // 2nd correct
    service.RegisterTap(0); // Wrong (should be 1) - resets

    // Current implementation restarts at progress=1 if the mismatched tap is also the first region.
    service.CurrentProgress.Should().Be(1);

    // Restart correctly
    service.RegisterTap(0); // 1st correct
    service.RegisterTap(2); // 2nd correct
    service.RegisterTap(1); // 3rd correct
    bool complete = service.RegisterTap(3); // 4th correct

    // Assert
    complete.Should().BeTrue();
  }

  [Fact]
  public void Reset_AfterProgress_ResetsToZero()
  {
    // Arrange
    var service = new ExitSequenceService(DefaultSequence, DefaultTimeout);

    // Act
    service.RegisterTap(0);
    service.RegisterTap(2);
    service.CurrentProgress.Should().Be(2);

    service.Reset();

    // Assert
    service.CurrentProgress.Should().Be(0);
  }

  [Fact]
  public void Reset_AfterCompletion_AllowsSequenceToBeRepeated()
  {
    // Arrange
    var service = new ExitSequenceService(DefaultSequence, DefaultTimeout);

    // Act - Complete once
    service.RegisterTap(0);
    service.RegisterTap(2);
    service.RegisterTap(1);
    service.RegisterTap(3).Should().BeTrue();

    service.Reset();

    // Complete again
    service.RegisterTap(0);
    service.RegisterTap(2);
    service.RegisterTap(1);
    bool complete = service.RegisterTap(3);

    // Assert
    complete.Should().BeTrue();
  }

  [Fact]
  public void RegisterTap_AfterTimeout_ResetsProgress()
  {
    // Arrange
    var shortTimeout = TimeSpan.FromMilliseconds(100);
    var service = new ExitSequenceService(DefaultSequence, shortTimeout);

    // Act
    service.RegisterTap(0); // Start sequence
    service.CurrentProgress.Should().Be(1);

    Thread.Sleep(150); // Wait for timeout

    service.RegisterTap(2); // Try to continue

    // Assert - Should have reset due to timeout
    service.CurrentProgress.Should().Be(0);
  }

  [Fact]
  public void RegisterTap_WithinTimeout_MaintainsProgress()
  {
    // Arrange
    var longTimeout = TimeSpan.FromSeconds(10);
    var service = new ExitSequenceService(DefaultSequence, longTimeout);

    // Act
    service.RegisterTap(0);
    Thread.Sleep(50); // Wait but stay within timeout
    service.RegisterTap(2);

    // Assert
    service.CurrentProgress.Should().Be(2);
  }

  [Fact]
  public void SequenceLength_ReturnsCorrectLength()
  {
    // Arrange
    var customSequence = new[] { 1, 2, 3 };
    var service = new ExitSequenceService(customSequence, DefaultTimeout);

    // Act & Assert
    service.SequenceLength.Should().Be(3);
  }

  [Fact]
  public void RegisterTap_WithCustomSequence_WorksCorrectly()
  {
    // Arrange
    var customSequence = new[] { 3, 1, 2, 0 }; // Different order
    var service = new ExitSequenceService(customSequence, DefaultTimeout);

    // Act & Assert
    service.RegisterTap(3).Should().BeFalse();
    service.CurrentProgress.Should().Be(1);

    service.RegisterTap(1).Should().BeFalse();
    service.CurrentProgress.Should().Be(2);

    service.RegisterTap(2).Should().BeFalse();
    service.CurrentProgress.Should().Be(3);

    service.RegisterTap(0).Should().BeTrue();
    service.CurrentProgress.Should().Be(4);
  }

  [Fact]
  public void RegisterTap_SingleElementSequence_CompletesImmediately()
  {
    // Arrange
    var singleSequence = new[] { 2 };
    var service = new ExitSequenceService(singleSequence, DefaultTimeout);

    // Act
    bool complete = service.RegisterTap(2);

    // Assert
    complete.Should().BeTrue();
    service.SequenceLength.Should().Be(1);
    service.CurrentProgress.Should().Be(1);
  }

  [Fact]
  public void RegisterTap_RepeatedCorrectTaps_TracksProgressCorrectly()
  {
    // Arrange - Sequence with repeated values
    var repeatedSequence = new[] { 0, 0, 1, 1 };
    var service = new ExitSequenceService(repeatedSequence, DefaultTimeout);

    // Act & Assert
    service.RegisterTap(0).Should().BeFalse();
    service.CurrentProgress.Should().Be(1);

    service.RegisterTap(0).Should().BeFalse();
    service.CurrentProgress.Should().Be(2);

    service.RegisterTap(1).Should().BeFalse();
    service.CurrentProgress.Should().Be(3);

    service.RegisterTap(1).Should().BeTrue();
    service.CurrentProgress.Should().Be(4);
  }
}

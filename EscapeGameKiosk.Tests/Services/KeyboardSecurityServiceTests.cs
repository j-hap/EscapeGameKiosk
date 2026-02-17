using EscapeGameKiosk;
using EscapeGameKiosk.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EscapeGameKiosk.Tests.Services;

public class KeyboardSecurityServiceTests
{
  private readonly Mock<ILogger<KeyboardSecurityService>> _mockLogger;

  public KeyboardSecurityServiceTests()
  {
    _mockLogger = new Mock<ILogger<KeyboardSecurityService>>();
  }

  [Fact]
  public void ShouldBlockKey_WithLeftWindowsKey_ReturnsTrue()
  {
    // Arrange
    var service = new KeyboardSecurityService(_mockLogger.Object);

    // Act
    bool result = service.ShouldBlockKey(Constants.VirtualKeys.LeftWin, false, false);

    // Assert
    result.Should().BeTrue();
  }

  [Fact]
  public void ShouldBlockKey_WithRightWindowsKey_ReturnsTrue()
  {
    // Arrange
    var service = new KeyboardSecurityService(_mockLogger.Object);

    // Act
    bool result = service.ShouldBlockKey(Constants.VirtualKeys.RightWin, false, false);

    // Assert
    result.Should().BeTrue();
  }

  [Fact]
  public void ShouldBlockKey_WithAltTab_ReturnsTrue()
  {
    // Arrange
    var service = new KeyboardSecurityService(_mockLogger.Object);

    // Act
    bool result = service.ShouldBlockKey(Constants.VirtualKeys.Tab, isAltDown: true, isCtrlDown: false);

    // Assert
    result.Should().BeTrue();
  }

  [Fact]
  public void ShouldBlockKey_WithTabWithoutAlt_ReturnsFalse()
  {
    // Arrange
    var service = new KeyboardSecurityService(_mockLogger.Object);

    // Act
    bool result = service.ShouldBlockKey(Constants.VirtualKeys.Tab, isAltDown: false, isCtrlDown: false);

    // Assert
    result.Should().BeFalse();
  }

  [Fact]
  public void ShouldBlockKey_WithAltF4_ReturnsTrue()
  {
    // Arrange
    var service = new KeyboardSecurityService(_mockLogger.Object);

    // Act
    bool result = service.ShouldBlockKey(Constants.VirtualKeys.F4, isAltDown: true, isCtrlDown: false);

    // Assert
    result.Should().BeTrue();
  }

  [Fact]
  public void ShouldBlockKey_WithF4WithoutAlt_ReturnsFalse()
  {
    // Arrange
    var service = new KeyboardSecurityService(_mockLogger.Object);

    // Act
    bool result = service.ShouldBlockKey(Constants.VirtualKeys.F4, isAltDown: false, isCtrlDown: false);

    // Assert
    result.Should().BeFalse();
  }

  [Fact]
  public void ShouldBlockKey_WithCtrlEscape_ReturnsTrue()
  {
    // Arrange
    var service = new KeyboardSecurityService(_mockLogger.Object);

    // Act
    bool result = service.ShouldBlockKey(Constants.VirtualKeys.Escape, isAltDown: false, isCtrlDown: true);

    // Assert
    result.Should().BeTrue();
  }

  [Fact]
  public void ShouldBlockKey_WithAltEscape_ReturnsTrue()
  {
    // Arrange
    var service = new KeyboardSecurityService(_mockLogger.Object);

    // Act
    bool result = service.ShouldBlockKey(Constants.VirtualKeys.Escape, isAltDown: true, isCtrlDown: false);

    // Assert
    result.Should().BeTrue();
  }

  [Fact]
  public void ShouldBlockKey_WithEscapeAlone_ReturnsFalse()
  {
    // Arrange
    var service = new KeyboardSecurityService(_mockLogger.Object);

    // Act
    bool result = service.ShouldBlockKey(Constants.VirtualKeys.Escape, isAltDown: false, isCtrlDown: false);

    // Assert
    result.Should().BeFalse();
  }

  [Theory]
  [InlineData(0x41)] // 'A' key
  [InlineData(0x42)] // 'B' key
  [InlineData(0x30)] // '0' key
  [InlineData(0x31)] // '1' key
  [InlineData(0x20)] // Space
  [InlineData(0x0D)] // Enter
  [InlineData(0x08)] // Backspace
  public void ShouldBlockKey_WithRegularKeys_ReturnsFalse(int virtualKeyCode)
  {
    // Arrange
    var service = new KeyboardSecurityService(_mockLogger.Object);

    // Act
    bool result = service.ShouldBlockKey(virtualKeyCode, false, false);

    // Assert
    result.Should().BeFalse();
  }

  [Fact]
  public void ShouldBlockKey_WithControl_AloneDoesNotBlock()
  {
    // Arrange
    var service = new KeyboardSecurityService(_mockLogger.Object);

    // Act
    bool result = service.ShouldBlockKey(Constants.VirtualKeys.Control, false, false);

    // Assert
    result.Should().BeFalse();
  }

  [Fact]
  public void ShouldBlockKey_WithAlt_AloneDoesNotBlock()
  {
    // Arrange
    var service = new KeyboardSecurityService(_mockLogger.Object);

    // Act
    bool result = service.ShouldBlockKey(Constants.VirtualKeys.Menu, false, false);

    // Assert
    result.Should().BeFalse();
  }

  [Fact]
  public void Start_CanBeCalledMultipleTimes_LogsWarning()
  {
    // Arrange
    var service = new KeyboardSecurityService(_mockLogger.Object);

    // Act
    service.Start();
    service.Start(); // Second call

    // Assert - Should log warning on second call
    _mockLogger.Verify(
      x => x.Log(
        LogLevel.Warning,
        It.IsAny<EventId>(),
        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("already started")),
        null,
        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
      Times.Once);
  }

  [Fact]
  public void Stop_BeforeStart_DoesNotThrow()
  {
    // Arrange
    var service = new KeyboardSecurityService(_mockLogger.Object);

    // Act
    Action act = () => service.Stop();

    // Assert
    act.Should().NotThrow();
  }

  [Fact]
  public void Dispose_StopsKeyboardHook()
  {
    // Arrange
    var service = new KeyboardSecurityService(_mockLogger.Object);
    service.Start();

    // Act
    service.Dispose();

    // Assert - Should log stop message
    _mockLogger.Verify(
      x => x.Log(
        LogLevel.Information,
        It.IsAny<EventId>(),
        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("stopped")),
        null,
        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
      Times.Once);
  }

  [Fact]
  public void Dispose_CalledMultipleTimes_IsSafe()
  {
    // Arrange
    var service = new KeyboardSecurityService(_mockLogger.Object);
    service.Start();

    // Act
    service.Dispose();
    Action act = () => service.Dispose();

    // Assert
    act.Should().NotThrow();
  }

  [Theory]
  [InlineData(Constants.VirtualKeys.LeftWin, false, false, true)]
  [InlineData(Constants.VirtualKeys.RightWin, false, false, true)]
  [InlineData(Constants.VirtualKeys.Tab, true, false, true)]
  [InlineData(Constants.VirtualKeys.F4, true, false, true)]
  [InlineData(Constants.VirtualKeys.Escape, false, true, true)]
  [InlineData(Constants.VirtualKeys.Escape, true, false, true)]
  [InlineData(0x41, false, false, false)] // 'A' key
  public void ShouldBlockKey_VariousCombinations_ReturnsExpectedResult(
    int virtualKeyCode,
    bool isAltDown,
    bool isCtrlDown,
    bool expectedBlocked)
  {
    // Arrange
    var service = new KeyboardSecurityService(_mockLogger.Object);

    // Act
    bool result = service.ShouldBlockKey(virtualKeyCode, isAltDown, isCtrlDown);

    // Assert
    result.Should().Be(expectedBlocked);
  }
}

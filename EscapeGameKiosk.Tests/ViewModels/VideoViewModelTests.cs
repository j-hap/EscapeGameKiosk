using EscapeGameKiosk.ViewModels;
using Microsoft.Extensions.Logging;

namespace EscapeGameKiosk.Tests.ViewModels;

public sealed class VideoViewModelTests
{
  [StaFact]
  public void PlayPauseCommand_WhenVideoNotLoaded_DoesNothing()
  {
    // Arrange
    ILogger<VideoViewModel> logger = Mock.Of<ILogger<VideoViewModel>>();
    var vm = new VideoViewModel(logger);

    bool playRaised = false;
    bool pauseRaised = false;
    vm.PlayRequested += (_, _) => playRaised = true;
    vm.PauseRequested += (_, _) => pauseRaised = true;

    // Act
    vm.PlayPauseCommand.Execute(null);

    // Assert
    playRaised.Should().BeFalse();
    pauseRaised.Should().BeFalse();
    vm.IsPlaying.Should().BeFalse();
  }

  [StaFact]
  public void PlayPauseCommand_WhenSourceIsSet_TogglesPlayAndPause()
  {
    // Arrange
    ILogger<VideoViewModel> logger = Mock.Of<ILogger<VideoViewModel>>();
    var vm = new VideoViewModel(logger);
    vm.SetVideoSource(new Uri("file:///C:/fake/video.mp4"));

    int playCount = 0;
    int pauseCount = 0;
    vm.PlayRequested += (_, _) => playCount++;
    vm.PauseRequested += (_, _) => pauseCount++;

    // Act
    vm.PlayPauseCommand.Execute(null);

    // Assert
    playCount.Should().Be(1);
    pauseCount.Should().Be(0);
    vm.IsPlaying.Should().BeTrue();
  }

  [StaFact]
  public void PlayPauseCommand_WhenLoaded_TogglesPlayAndPause_AndUpdatesButtonContent()
  {
    // Arrange
    ILogger<VideoViewModel> logger = Mock.Of<ILogger<VideoViewModel>>();
    var vm = new VideoViewModel(logger);

    vm.OnMediaOpened(durationMs: 10_000);

    int playCount = 0;
    int pauseCount = 0;
    vm.PlayRequested += (_, _) => playCount++;
    vm.PauseRequested += (_, _) => pauseCount++;

    // Act 1: Play
    vm.PlayPauseCommand.Execute(null);

    // Assert 1
    playCount.Should().Be(1);
    pauseCount.Should().Be(0);
    vm.IsPlaying.Should().BeTrue();
    vm.PlayPauseButtonContent.Should().Be(Constants.UI.PauseSymbol);

    // Act 2: Pause
    vm.PlayPauseCommand.Execute(null);

    // Assert 2
    playCount.Should().Be(1);
    pauseCount.Should().Be(1);
    vm.IsPlaying.Should().BeFalse();
    vm.PlayPauseButtonContent.Should().Be(Constants.UI.PlaySymbol);
  }

  [StaFact]
  public void StopVideoCommand_WhenLoaded_StopsAndResetsPosition()
  {
    // Arrange
    ILogger<VideoViewModel> logger = Mock.Of<ILogger<VideoViewModel>>();
    var vm = new VideoViewModel(logger);

    vm.OnMediaOpened(durationMs: 10_000);
    vm.PlayPauseCommand.Execute(null); // Start playing
    vm.Position = 1234;

    int stopCount = 0;
    vm.StopRequested += (_, _) => stopCount++;

    // Act
    vm.StopVideoCommand.Execute(null);

    // Assert
    stopCount.Should().Be(1);
    vm.IsPlaying.Should().BeFalse();
    vm.Position.Should().Be(0);
    vm.PlayPauseButtonContent.Should().Be(Constants.UI.PlaySymbol);
  }

  [StaFact]
  public void LockCommand_RaisesLockRequested()
  {
    // Arrange
    ILogger<VideoViewModel> logger = Mock.Of<ILogger<VideoViewModel>>();
    var vm = new VideoViewModel(logger);

    bool lockRaised = false;
    vm.LockRequested += (_, _) => lockRaised = true;

    // Act
    vm.LockCommand.Execute(null);

    // Assert
    lockRaised.Should().BeTrue();
  }
}

using System;
using Xunit;
using DaisyView.ViewModels;

namespace DaisyView.Tests;

/// <summary>
/// Unit tests for the RelayCommand implementation
/// </summary>
public class RelayCommandTests
{
    [Fact]
    public void RelayCommand_ExecutesAction()
    {
        // Arrange
        var executed = false;
        var command = new RelayCommand(_ => executed = true);

        // Act
        command.Execute(null);

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public void RelayCommand_CanExecuteReturnsTrueByDefault()
    {
        // Arrange
        var command = new RelayCommand(_ => { });

        // Act
        var canExecute = command.CanExecute(null);

        // Assert
        Assert.True(canExecute);
    }

    [Fact]
    public void RelayCommand_RespectsCanExecutePredicate()
    {
        // Arrange
        var command = new RelayCommand(_ => { }, param => false);

        // Act
        var canExecute = command.CanExecute(null);

        // Assert
        Assert.False(canExecute);
    }

    [Fact]
    public void RelayCommand_ThrowsOnNullExecute()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RelayCommand(null!));
    }

    [Fact]
    public void RelayCommandGeneric_ExecutesActionWithParameter()
    {
        // Arrange
        var receivedValue = 0;
        var command = new RelayCommand<int>(value => receivedValue = value);

        // Act
        command.Execute(42);

        // Assert
        Assert.Equal(42, receivedValue);
    }

    [Fact]
    public void RelayCommandGeneric_RespectsCanExecutePredicate()
    {
        // Arrange
        var command = new RelayCommand<string>(
            _ => { },
            param => param != null && param.Length > 0
        );

        // Act & Assert
        Assert.False(command.CanExecute(""));
        Assert.True(command.CanExecute("test"));
    }
}

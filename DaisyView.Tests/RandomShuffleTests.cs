using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DaisyView.Tests;

/// <summary>
/// Unit tests for the random shuffle algorithm
/// </summary>
public class RandomShuffleTests
{
    [Fact]
    public void FisherYatesShuffle_ProducesDifferentOrder()
    {
        // Arrange
        var fileNames = new List<string> { "image1.jpg", "image2.jpg", "image3.jpg", "image4.jpg", "image5.jpg" };
        var originalOrder = string.Join(",", fileNames);
        var random = new Random();
        var randomOrder = fileNames.ToList();

        // Act
        // Fisher-Yates shuffle
        for (int i = randomOrder.Count - 1; i > 0; i--)
        {
            int randomIndex = random.Next(i + 1);
            // Swap
            var temp = randomOrder[i];
            randomOrder[i] = randomOrder[randomIndex];
            randomOrder[randomIndex] = temp;
        }

        var shuffledOrder = string.Join(",", randomOrder);

        // Assert - with 5 items, there's a very small chance of getting the same order
        // We'll allow for up to 10 attempts before asserting failure
        var attempts = 0;
        while (shuffledOrder == originalOrder && attempts < 10)
        {
            attempts++;
            randomOrder = fileNames.ToList();
            random = new Random();
            
            for (int i = randomOrder.Count - 1; i > 0; i--)
            {
                int randomIndex = random.Next(i + 1);
                var temp = randomOrder[i];
                randomOrder[i] = randomOrder[randomIndex];
                randomOrder[randomIndex] = temp;
            }
            
            shuffledOrder = string.Join(",", randomOrder);
        }

        Assert.NotEqual(originalOrder, shuffledOrder);
        
        // Also verify all items are still present
        Assert.Equal(fileNames.OrderBy(x => x), randomOrder.OrderBy(x => x));
    }

    [Fact]
    public void FisherYatesShuffle_PreservesAllItems()
    {
        // Arrange
        var fileNames = new List<string> { "a.jpg", "b.jpg", "c.jpg", "d.jpg" };
        var random = new Random();
        var randomOrder = fileNames.ToList();

        // Act
        for (int i = randomOrder.Count - 1; i > 0; i--)
        {
            int randomIndex = random.Next(i + 1);
            var temp = randomOrder[i];
            randomOrder[i] = randomOrder[randomIndex];
            randomOrder[randomIndex] = temp;
        }

        // Assert
        Assert.Equal(fileNames.Count, randomOrder.Count);
        Assert.Equal(fileNames.OrderBy(x => x), randomOrder.OrderBy(x => x));
    }
}

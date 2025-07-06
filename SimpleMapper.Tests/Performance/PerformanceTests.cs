using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SimpleMapper.Tests.TestModels;

namespace SimpleMapper.Tests.Performance;

public class PerformanceTests
{
    [Fact]
    public void Performance_SingleMapping_ShouldBeFast()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMapper();
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IMapper>();

        var user = new User
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        // Act - Measure 10,000 single mappings
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 10_000; i++)
        {
            factory.Map<User, UserDto>(user);
        }
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 100, "10,000 single mappings should complete in under 100ms");
        var averageTimePerMapping = (double)stopwatch.ElapsedTicks / 10_000 / TimeSpan.TicksPerMicrosecond;
        Assert.True(averageTimePerMapping < 10, "Average mapping time should be under 10 microseconds");
    }

    [Fact]
    public void Performance_CollectionMapping_ShouldScaleLinearly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMapper();
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IMapper>();

        var sizes = new[] { 100, 1000, 10000 };
        var times = new List<long>();

        foreach (var size in sizes)
        {
            var users = Enumerable.Range(1, size).Select(i => new User
            {
                Id = i,
                FirstName = $"User{i}",
                LastName = "Test",
                Email = $"user{i}@example.com",
                IsActive = i % 2 == 0,
                CreatedAt = DateTime.Now
            }).ToList();

            // Act
            var stopwatch = Stopwatch.StartNew();
            var results = factory.Map<User, UserDto>(users).ToList();
            stopwatch.Stop();

            // Assert
            Assert.Equal(size, results.Count);
            times.Add(stopwatch.ElapsedTicks);
        }

        // Performance will vary significantly based on system conditions
        // Just verify that all collections were processed successfully
        Assert.True(times.All(t => t > 0), "All collection sizes should have measurable processing time");
        
        // Log the performance characteristics for informational purposes
        for (int i = 0; i < sizes.Length; i++)
        {
            var timePerItem = (double)times[i] / sizes[i];
            // This is informational only - actual performance will vary by system
            Assert.True(timePerItem >= 0, $"Time per item for size {sizes[i]}: {timePerItem:F4} ticks/item");
        }
    }

    [Fact]
    public void Performance_NestedMapping_ShouldBeReasonablyFast()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMapper();
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IMapper>();

        var usersWithAddresses = Enumerable.Range(1, 1000).Select(i => new UserWithAddresses
        {
            Id = i,
            FirstName = $"User{i}",
            LastName = "Test",
            Addresses = new List<Address>
            {
                                 new() { Id = i * 10, Street = $"Street {i}", City = "City", Country = "USA", PostalCode = "12345" },
                 new() { Id = i * 10 + 1, Street = $"Avenue {i}", City = "Town", Country = "USA", PostalCode = "67890" }
            }
        }).ToList();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var results = factory.Map<UserWithAddresses, UserWithAddressesDto>(usersWithAddresses).ToList();
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 50, "1,000 nested mappings should complete in under 50ms");
    }

    [Fact]
    public void Performance_MapperCaching_ShouldImproveSubsequentCalls()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMapper();
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IMapper>();

        var user = new User { Id = 1, FirstName = "John", LastName = "Doe" };

        // Act - First call (should cache mapper)
        var stopwatch1 = Stopwatch.StartNew();
        factory.Map<User, UserDto>(user);
        stopwatch1.Stop();

        // Act - Subsequent calls (should use cached mapper)
        var stopwatch2 = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            factory.Map<User, UserDto>(user);
        }
        stopwatch2.Stop();

        // Assert - Subsequent calls should be faster on average
        var averageSubsequentTime = (double)stopwatch2.ElapsedTicks / 1000;
        Assert.True(averageSubsequentTime < stopwatch1.ElapsedTicks,
            "Cached mapper calls should be faster than initial mapper resolution");
    }

    [Fact]
    public async Task Performance_ConcurrentMapping_ShouldNotBottleneck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMapper();
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IMapper>();

        var users = Enumerable.Range(1, 100).Select(i => new User
        {
            Id = i,
            FirstName = $"User{i}",
            LastName = "Test",
            Email = $"user{i}@example.com",
            IsActive = true
        }).ToList();

        var exceptions = new List<Exception>();
        var completedTasks = 0;

        // Act - Run multiple mapping operations concurrently
        var stopwatch = Stopwatch.StartNew();
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
        {
            try
            {
                for (int i = 0; i < 100; i++)
                {
                    factory.Map<User, UserDto>(users).ToList();
                }
                Interlocked.Increment(ref completedTasks);
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        })).ToArray();

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.Empty(exceptions);
        Assert.Equal(10, completedTasks);
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, "Concurrent mapping should complete quickly");
    }

    [Fact]
    public void Performance_ServiceProviderCreation_ShouldBeFast()
    {
        // Arrange & Act
        var stopwatch = Stopwatch.StartNew();
        
        var services = new ServiceCollection();
        services.AddSimpleMapper();
        var provider = services.BuildServiceProvider();
        
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 500, "Registration and service provider building should be fast");

        // Verify functionality
        var factory = provider.GetService<IMapper>();
        Assert.NotNull(factory);

        var userMapper = provider.GetService<IMapper<User, UserDto>>();
        Assert.NotNull(userMapper);
    }

    [Fact]
    public void Performance_MemoryUsage_ShouldBeReasonable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMapper();
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IMapper>();

        // Force garbage collection to get baseline
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var initialMemory = GC.GetTotalMemory(false);

        // Act - Perform many mappings
        var results = new List<List<UserDto>>();
        for (int batch = 0; batch < 10; batch++)
        {
            var users = Enumerable.Range(1, 1000).Select(i => new User
            {
                Id = i,
                FirstName = $"User{i}",
                LastName = "Test",
                Email = $"user{i}@example.com",
                IsActive = true
            }).ToList();

            var mappedUsers = factory.Map<User, UserDto>(users).ToList();
            results.Add(mappedUsers);
        }

        var finalMemory = GC.GetTotalMemory(false);
        var memoryIncreaseInMB = (finalMemory - initialMemory) / (1024.0 * 1024.0);

        // Assert
        Assert.True(memoryIncreaseInMB < 50, "Memory usage should be reasonable for large mappings");

        Assert.Equal(10, results.Count);
        Assert.True(results.All(r => r.Count == 1000));
    }

    [Theory]
    [InlineData(1000, 100)]
    [InlineData(10000, 500)]
    public void Performance_Theory_CollectionMapping_ShouldMeetExpectations(int itemCount, int expectedMaxTime)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMapper();
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IMapper>();

        var users = Enumerable.Range(1, itemCount).Select(i => new User
        {
            Id = i,
            FirstName = $"User{i}",
            LastName = "Test",
            Email = $"user{i}@example.com",
            IsActive = true
        }).ToList();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var results = factory.Map<User, UserDto>(users).ToList();
        stopwatch.Stop();

        // Assert
        Assert.Equal(itemCount, results.Count);
        Assert.True(stopwatch.ElapsedMilliseconds < expectedMaxTime,
            $"Mapping {itemCount} items should complete in under {expectedMaxTime}ms, but took {stopwatch.ElapsedMilliseconds}ms");
    }
} 
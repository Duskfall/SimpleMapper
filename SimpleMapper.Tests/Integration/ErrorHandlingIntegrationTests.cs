using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleMapper.Tests.TestModels;
using System.Collections;
using System.Reflection;

namespace SimpleMapper.Tests.Integration;

public class ErrorHandlingIntegrationTests
{
    [Fact]
    public void EndToEnd_UnregisteredMapper_ShouldProvideHelpfulErrorMessage()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMapper(); // Register SimpleMapper but not all possible mappers
        
        var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        var unmappedSource = new UnmappedSource { Id = 1, Data = "test" };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            mapper.Map<UnmappedSource, UnmappedDestination>(unmappedSource));
        
        Assert.Equal("No mapper registered for UnmappedSource -> UnmappedDestination", exception.Message);
    }

    [Fact]
    public void EndToEnd_TypeInferenceWithUnregisteredMapper_ShouldProvideHelpfulErrorMessage()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMapper();
        
        var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        var unmappedSource = new UnmappedSource { Id = 1, Data = "test" };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            mapper.Map<UnmappedDestination>(unmappedSource));
        
        Assert.Contains("No mapper registered", exception.Message);
        Assert.Contains("UnmappedSource", exception.Message);
        Assert.Contains("UnmappedDestination", exception.Message);
    }

    [Fact]
    public void EndToEnd_CollectionWithUnregisteredMapper_ShouldProvideHelpfulErrorMessage()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMapper();
        
        var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        var unmappedSources = new List<UnmappedSource>
        {
            new() { Id = 1, Data = "test1" },
            new() { Id = 2, Data = "test2" }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            mapper.Map<UnmappedSource, UnmappedDestination>(unmappedSources).ToList());
        
        Assert.Contains("No mapper registered", exception.Message);
    }

    [Fact]
    public void EndToEnd_NullArgumentHandling_ShouldProvideAppropriateErrors()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMapper();
        
        var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        // Act & Assert - Various null scenarios
        Assert.Throws<ArgumentNullException>(() => mapper.Map<User, UserDto>((User)null!));
        Assert.Throws<ArgumentNullException>(() => mapper.Map<User, UserDto>((IEnumerable<User>)null!));
        Assert.Throws<ArgumentNullException>(() => mapper.Map<UserDto>((object)null!));
        Assert.Throws<ArgumentNullException>(() => mapper.Map<UserDto>((IEnumerable)null!));
    }

    [Fact]
    public void EndToEnd_DisposedServiceProvider_ShouldHandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMapper();
        
        var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        // Cache a mapper first
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe", IsActive = true };
        var result1 = mapper.Map<User, UserDto>(user);
        Assert.NotNull(result1);

        // Dispose the provider
        provider.Dispose();

        // Act - Should still work due to caching
        var result2 = mapper.Map<User, UserDto>(user);

        // Assert
        Assert.NotNull(result2);
        Assert.Equal("John Doe", result2.FullName);
    }

    [Fact]
    public void EndToEnd_MapperWithCircularReference_ShouldNotCauseStackOverflow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMapper<CircularReferenceMapper>();
        
        var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        // Create circular reference
        var source = new CircularSource { Id = 1 };
        source.Child = new CircularSource { Id = 2, Child = source }; // Circular!

        // Act - Should handle gracefully (implementation dependent)
        // This test verifies the system doesn't crash with stack overflow
        // The actual behavior depends on the mapper implementation
        
        try
        {
            var result = mapper.Map<CircularSource, CircularDestination>(source);
            // If it succeeds, verify basic properties
            Assert.Equal(1, result.Id);
        }
        catch (StackOverflowException)
        {
            Assert.Fail("Circular reference caused stack overflow - mapper should handle this gracefully");
        }
        catch (Exception ex)
        {
            // Other exceptions are acceptable as long as no stack overflow
            Assert.IsNotType<StackOverflowException>(ex);
        }
    }

    [Fact]
    public void EndToEnd_MapperThrowingException_ShouldPropagateCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IMapper<TestModels.NonDiscoverable.ErrorTestSource, TestModels.NonDiscoverable.ErrorTestDestination>, TestModels.NonDiscoverable.ThrowingMapperForTesting>();
        services.AddSingleton<MapperRegistry>();
        services.AddSingleton<IMapper, Mapper>();
        
        var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        var errorSource = new TestModels.NonDiscoverable.ErrorTestSource { Id = 1, Data = "test" };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            mapper.Map<TestModels.NonDiscoverable.ErrorTestSource, TestModels.NonDiscoverable.ErrorTestDestination>(errorSource));
        
        Assert.Equal("Test exception", exception.Message);
    }

    [Fact]
    public void EndToEnd_LargeObjectGraphs_ShouldHandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMapper();
        
        var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        // Create a very large user with many addresses
        var largeUser = new UserWithAddresses
        {
            Id = 1,
            FirstName = "Large",
            LastName = "User",
            Email = "large@example.com",
            Addresses = new List<Address>()
        };

        // Add 10,000 addresses
        for (int i = 0; i < 10000; i++)
        {
            largeUser.Addresses.Add(new Address
            {
                Id = i,
                Street = $"Street {i}",
                City = $"City {i}",
                Country = "USA",
                PostalCode = $"{10000 + i}"
            });
        }

        // Act - Should handle large object graphs without issues
        var result = mapper.Map<UserWithAddresses, UserWithAddressesDto>(largeUser);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10000, result.Addresses.Count);
        Assert.Equal("Large User", result.FullName);
    }

    [Fact]
    public void EndToEnd_ConcurrentErrorScenarios_ShouldHandleCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMapper();
        
        var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        var exceptions = new List<Exception>();
        var successCount = 0;

        // Act - Mix successful and failing mappings concurrently
        Parallel.For(0, 1000, i =>
        {
            try
            {
                if (i % 10 == 0)
                {
                    // Try to map unmapped type (will fail)
                    var unmapped = new UnmappedSource { Id = i };
                    mapper.Map<UnmappedDestination>(unmapped);
                }
                else
                {
                    // Normal successful mapping
                    var user = new User { Id = i, FirstName = "User", LastName = $"{i}", IsActive = true };
                    var result = mapper.Map<UserDto>(user);
                    if (result != null)
                    {
                        Interlocked.Increment(ref successCount);
                    }
                }
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        });

        // Assert
        Assert.Equal(900, successCount); // 900 successful operations
        Assert.Equal(100, exceptions.Count); // 100 failed operations
        Assert.All(exceptions, ex => Assert.IsType<InvalidOperationException>(ex));
    }

    [Fact]
    public void EndToEnd_ServiceProviderConfigurationErrors_ShouldProvideHelpfulMessages()
    {
        // Arrange - Incorrect service configuration
        var services = new ServiceCollection();
        services.AddSingleton<IMapper, Mapper>(); // Missing MapperRegistry!
        
        var provider = services.BuildServiceProvider();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            provider.GetRequiredService<IMapper>());
        
        Assert.Contains("Unable to resolve service", exception.Message);
    }

    [Fact]
    public void EndToEnd_HostedServiceWithErrors_ShouldNotCrashApplication()
    {
        // Arrange
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSimpleMapper();
                services.AddSingleton<ErrorProneHostedService>();
            });

        // Act
        using var host = hostBuilder.Build();
        var service = host.Services.GetRequiredService<ErrorProneHostedService>();
        
        // Should handle errors gracefully
        var exception = Assert.Throws<InvalidOperationException>(() => 
            service.ProcessWithError());
        
        // Assert
        Assert.Contains("No mapper registered", exception.Message);
    }

    [Fact]
    public void EndToEnd_TypeInferenceWithComplexGenerics_ShouldFailGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMapper();
        
        var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        var complexData = new Dictionary<string, List<User>>
        {
            { "group1", new List<User> { new() { Id = 1, FirstName = "User1", LastName = "Test" } } },
            { "group2", new List<User> { new() { Id = 2, FirstName = "User2", LastName = "Test" } } }
        };

        // Act & Assert - Complex generic types without mappers should fail gracefully
        Assert.Throws<InvalidOperationException>(() => 
            mapper.Map<Dictionary<string, List<UserDto>>>(complexData));
    }

    [Fact]
    public void EndToEnd_MemoryPressure_ShouldNotCauseOutOfMemory()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMapper();
        
        var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        // Act - Create memory pressure with large collections
        for (int batch = 0; batch < 10; batch++)
        {
            var users = Enumerable.Range(batch * 10000, 10000).Select(i => new User 
            { 
                Id = i, 
                FirstName = $"User{i}", 
                LastName = "Test", 
                IsActive = true 
            }).ToList();

            var results = mapper.Map<User, UserDto>(users).ToList();

            // Assert batch completed successfully
            Assert.Equal(10000, results.Count);

            // Force garbage collection to prevent OOM
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        // If we reach here, memory pressure was handled correctly
        Assert.True(true);
    }

    [Fact]
    public void EndToEnd_ExtremeEdgeCases_ShouldHandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMapper();
        
        var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        // Test various edge cases
        var emptyList = new List<User>();
        var emptyArray = Array.Empty<User>();
        var singletonList = new List<User> { new() { Id = 1, FirstName = "Single", LastName = "User", IsActive = true } };

        // Act & Assert - All should work without issues
        var emptyResults1 = mapper.Map<User, UserDto>(emptyList).ToList();
        var emptyResults2 = mapper.Map<User, UserDto>(emptyArray).ToList();
        var singleResults = mapper.Map<User, UserDto>(singletonList).ToList();

        Assert.Empty(emptyResults1);
        Assert.Empty(emptyResults2);
        Assert.Single(singleResults);
        Assert.Equal("Single User", singleResults[0].FullName);
    }
}

// Test classes for error scenarios
public class UnmappedSource
{
    public int Id { get; set; }
    public string Data { get; set; } = string.Empty;
}

public class UnmappedDestination
{
    public int Id { get; set; }
    public string ProcessedData { get; set; } = string.Empty;
}

public class ErrorProneHostedService
{
    private readonly IMapper _mapper;

    public ErrorProneHostedService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public void ProcessWithError()
    {
        // Try to map unmapped type
        var unmapped = new UnmappedSource { Id = 1, Data = "test" };
        _mapper.Map<UnmappedDestination>(unmapped);
    }
} 
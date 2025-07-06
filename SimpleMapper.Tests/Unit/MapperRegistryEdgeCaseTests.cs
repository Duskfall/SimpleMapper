using Microsoft.Extensions.DependencyInjection;
using SimpleMapper.Tests.TestModels;

namespace SimpleMapper.Tests.Unit;

public class MapperRegistryEdgeCaseTests
{
    [Fact]
    public void MapperRegistry_GetMapper_WithNullServiceProvider_ShouldWorkWithCachedMappers()
    {
        // Arrange - Create registry with valid provider first
        var services = new ServiceCollection();
        services.AddSingleton<IMapper<User, UserDto>, UserMapper>();
        var provider = services.BuildServiceProvider();
        var registry = new MapperRegistry(provider);

        // Get mapper once to cache it
        var cachedMapper = registry.GetMapper<User, UserDto>();
        Assert.NotNull(cachedMapper);

        // Dispose the provider to simulate provider becoming unavailable
        provider.Dispose();

        // Act - Should still work due to caching
        var result = registry.GetMapper<User, UserDto>();

        // Assert
        Assert.NotNull(result);
        Assert.Same(cachedMapper, result); // Should be the same cached instance
    }

    [Fact]
    public void MapperRegistry_GetMapper_CalledConcurrently_ShouldReturnSameInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IMapper<User, UserDto>, UserMapper>();
        var provider = services.BuildServiceProvider();
        var registry = new MapperRegistry(provider);

        var mappers = new IMapper<User, UserDto>[100];
        var exceptions = new List<Exception>();

        // Act - Get mapper concurrently from multiple threads
        Parallel.For(0, 100, i =>
        {
            try
            {
                mappers[i] = registry.GetMapper<User, UserDto>();
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
        Assert.Empty(exceptions); // No exceptions should occur
        Assert.All(mappers, m => Assert.NotNull(m));
        
        // All instances should be the same (due to caching)
        var firstMapper = mappers[0];
        Assert.All(mappers, m => Assert.Same(firstMapper, m));
    }

    [Fact]
    public void MapperRegistry_GetMapper_WithMultipleInterfaceImplementations_ShouldWorkCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IMapper<User, UserDto>, UserMapper>();
        services.AddSingleton<IMapper<Product, ProductDto>, ProductMapper>();
        var provider = services.BuildServiceProvider();
        var registry = new MapperRegistry(provider);

        // Act - Get different mapper types
        var userMapper = registry.GetMapper<User, UserDto>();
        var productMapper = registry.GetMapper<Product, ProductDto>();

        // Assert
        Assert.NotNull(userMapper);
        Assert.NotNull(productMapper);
        Assert.IsType<UserMapper>(userMapper);
        Assert.IsType<ProductMapper>(productMapper);
        Assert.NotSame(userMapper, productMapper);
    }

    [Fact]
    public void MapperRegistry_GetMapper_WithGenericTypes_ShouldWorkCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IMapper<List<User>, List<UserDto>>, ListUserMapper>();
        var provider = services.BuildServiceProvider();
        var registry = new MapperRegistry(provider);

        // Act
        var mapper = registry.GetMapper<List<User>, List<UserDto>>();

        // Assert
        Assert.NotNull(mapper);
        Assert.IsType<ListUserMapper>(mapper);
    }

    [Fact]
    public void MapperRegistry_GetMapper_WithComplexGenericTypes_ShouldWorkCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IMapper<Dictionary<string, User>, Dictionary<string, UserDto>>, DictionaryUserMapper>();
        var provider = services.BuildServiceProvider();
        var registry = new MapperRegistry(provider);

        // Act
        var mapper = registry.GetMapper<Dictionary<string, User>, Dictionary<string, UserDto>>();

        // Assert
        Assert.NotNull(mapper);
        Assert.IsType<DictionaryUserMapper>(mapper);
    }

    [Fact]
    public void MapperRegistry_GetMapper_AfterServiceProviderDisposal_ShouldStillWorkWithCachedMappers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IMapper<User, UserDto>, UserMapper>();
        var provider = services.BuildServiceProvider();
        var registry = new MapperRegistry(provider);

        // Cache the mapper first
        var originalMapper = registry.GetMapper<User, UserDto>();

        // Dispose the provider
        provider.Dispose();

        // Act - Should work because mapper is cached
        var cachedMapper = registry.GetMapper<User, UserDto>();

        // Assert
        Assert.NotNull(cachedMapper);
        Assert.Same(originalMapper, cachedMapper);
    }

    [Fact]
    public void MapperRegistry_GetMapper_WithFailingServiceProvider_ShouldThrowMeaningfulException()
    {
        // Arrange
        var mockProvider = new Mock<IServiceProvider>();
        mockProvider.Setup(x => x.GetService(typeof(IMapper<User, UserDto>)))
                   .Returns((object?)null); // No mapper registered

        var registry = new MapperRegistry(mockProvider.Object);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => registry.GetMapper<User, UserDto>());
        Assert.Equal("No mapper registered for User -> UserDto", exception.Message);
    }

    [Fact]
    public void MapperRegistry_GetMapper_WithServiceProviderThrowingException_ShouldPropagateException()
    {
        // Arrange
        var mockProvider = new Mock<IServiceProvider>();
        mockProvider.Setup(x => x.GetService(typeof(IMapper<User, UserDto>)))
                   .Throws(new InvalidOperationException("Service provider error"));

        var registry = new MapperRegistry(mockProvider.Object);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => registry.GetMapper<User, UserDto>());
        Assert.Equal("Service provider error", exception.Message);
    }

    [Fact]
    public void MapperRegistry_GetMapper_StressTest_ShouldHandleHighVolume()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IMapper<User, UserDto>, UserMapper>();
        services.AddSingleton<IMapper<Product, ProductDto>, ProductMapper>();
        var provider = services.BuildServiceProvider();
        var registry = new MapperRegistry(provider);

        const int iterations = 10000;
        var results = new (IMapper<User, UserDto> userMapper, IMapper<Product, ProductDto> productMapper)[iterations];

        // Act - High volume concurrent access
        Parallel.For(0, iterations, i =>
        {
            results[i] = (
                registry.GetMapper<User, UserDto>(),
                registry.GetMapper<Product, ProductDto>()
            );
        });

        // Assert
        Assert.All(results, result =>
        {
            Assert.NotNull(result.userMapper);
            Assert.NotNull(result.productMapper);
            Assert.IsType<UserMapper>(result.userMapper);
            Assert.IsType<ProductMapper>(result.productMapper);
        });

        // Verify all user mappers are the same instance (caching working)
        var firstUserMapper = results[0].userMapper;
        Assert.All(results, result => Assert.Same(firstUserMapper, result.userMapper));
    }
}

// Test mappers for complex generic scenarios
public class ListUserMapper : BaseMapper<List<User>, List<UserDto>>
{
    public override List<UserDto> Map(List<User> source)
    {
        return source.Select(u => new UserDto
        {
            Id = u.Id,
            FullName = $"{u.FirstName} {u.LastName}",
            Email = u.Email,
            Status = u.IsActive ? "Active" : "Inactive"
        }).ToList();
    }
}

public class DictionaryUserMapper : BaseMapper<Dictionary<string, User>, Dictionary<string, UserDto>>
{
    public override Dictionary<string, UserDto> Map(Dictionary<string, User> source)
    {
        return source.ToDictionary(
            kvp => kvp.Key,
            kvp => new UserDto
            {
                Id = kvp.Value.Id,
                FullName = $"{kvp.Value.FirstName} {kvp.Value.LastName}",
                Email = kvp.Value.Email,
                Status = kvp.Value.IsActive ? "Active" : "Inactive"
            }
        );
    }
} 
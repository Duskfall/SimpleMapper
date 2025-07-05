using Microsoft.Extensions.DependencyInjection;
using SimpleMapper.Tests.TestModels;

namespace SimpleMapper.Tests.Unit;

public class MapperTests
{
    [Fact]
    public void Mapper_Constructor_WithNullRegistry_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Mapper(null!));
    }

    [Fact]
    public void Mapper_ShouldImplementIMapperInterface()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var registry = new MapperRegistry(provider);

        // Act
        var factory = new Mapper(registry);

        // Assert
        Assert.IsAssignableFrom<IMapper>(factory);
    }

    [Fact]
    public void Mapper_Map_WithNullSource_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IMapper<User, UserDto>, UserMapper>();
        var provider = services.BuildServiceProvider();
        var registry = new MapperRegistry(provider);
        var factory = new Mapper(registry);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => factory.Map<User, UserDto>((User)null!));
        Assert.Equal("source", exception.ParamName);
    }

    [Fact]
    public void Mapper_Map_WithRegisteredMapper_ShouldMapCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IMapper<User, UserDto>, UserMapper>();
        var provider = services.BuildServiceProvider();
        var registry = new MapperRegistry(provider);
        var factory = new Mapper(registry);

        var user = new User
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            IsActive = true,
            CreatedAt = new DateTime(2023, 1, 1)
        };

        // Act
        var result = factory.Map<User, UserDto>(user);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("John Doe", result.FullName);
        Assert.Equal("john@example.com", result.Email);
        Assert.Equal("Active", result.Status);
    }

    [Fact]
    public void Mapper_Map_WithUnregisteredMapper_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var registry = new MapperRegistry(provider);
        var factory = new Mapper(registry);

        var user = new User { Id = 1 };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => factory.Map<User, UserDto>(user));
        Assert.Equal("No mapper registered for User -> UserDto", exception.Message);
    }

    [Fact]
    public void Mapper_Map_Collection_WithNullSource_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMapper(); // ✅ Automatic discovery
        var provider = services.BuildServiceProvider();
        var registry = new MapperRegistry(provider);
        var factory = new Mapper(registry);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => factory.Map<User, UserDto>((IEnumerable<User>)null!));
        Assert.Equal("sources", exception.ParamName);
    }

    [Fact]
    public void Mapper_Map_Collection_ShouldMapAllItems()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMapper(); // ✅ Automatic discovery
        var provider = services.BuildServiceProvider();
        var registry = new MapperRegistry(provider);
        var factory = new Mapper(registry);

        var users = new List<User>
        {
            new() { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com", IsActive = true, CreatedAt = new DateTime(2023, 1, 1) },
            new() { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane@example.com", IsActive = false, CreatedAt = new DateTime(2023, 2, 1) }
        };

        // Act
        var result = factory.Map<User, UserDto>(users).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        
        Assert.Equal(1, result[0].Id);
        Assert.Equal("John Doe", result[0].FullName);
        Assert.Equal("Active", result[0].Status);
        
        Assert.Equal(2, result[1].Id);
        Assert.Equal("Jane Smith", result[1].FullName);
        Assert.Equal("Inactive", result[1].Status);
    }

    [Fact]
    public void Mapper_Map_Collection_WithEmptyCollection_ShouldReturnEmptyCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMapper(); // ✅ Automatic discovery
        var provider = services.BuildServiceProvider();
        var registry = new MapperRegistry(provider);
        var factory = new Mapper(registry);

        var users = new List<User>();

        // Act
        var result = factory.Map<User, UserDto>(users);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Mapper_Map_ShouldCacheMapperAfterFirstRetrieval()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mapper = new UserMapper();
        
        mockServiceProvider.Setup(x => x.GetService(typeof(IMapper<User, UserDto>)))
                          .Returns(mapper);

        var registry = new MapperRegistry(mockServiceProvider.Object);
        var factory = new Mapper(registry);

        var user = new User { Id = 1, FirstName = "John", LastName = "Doe" };

        // Act - Call twice
        var result1 = factory.Map<User, UserDto>(user);
        var result2 = factory.Map<User, UserDto>(user);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        
        // Verify the service provider was only called once (caching working)
        mockServiceProvider.Verify(x => x.GetService(typeof(IMapper<User, UserDto>)), Times.Once);
    }
}

public class MapperRegistryTests
{
    [Fact]
    public void MapperRegistry_Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MapperRegistry(null!));
    }

    [Fact]
    public void MapperRegistry_GetMapper_WithRegisteredMapper_ShouldReturnMapper()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMapper(); // ✅ Automatic discovery
        var provider = services.BuildServiceProvider();
        var registry = new MapperRegistry(provider);

        // Act
        var mapper = registry.GetMapper<User, UserDto>();

        // Assert
        Assert.NotNull(mapper);
        Assert.IsType<UserMapper>(mapper);
    }

    [Fact]
    public void MapperRegistry_GetMapper_WithUnregisteredMapper_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var registry = new MapperRegistry(provider);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => registry.GetMapper<User, UserDto>());
        Assert.Equal("No mapper registered for User -> UserDto", exception.Message);
    }

    [Fact]
    public void MapperRegistry_GetMapper_ShouldReturnSameInstanceOnMultipleCalls()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMapper(); // ✅ Automatic discovery
        var provider = services.BuildServiceProvider();
        var registry = new MapperRegistry(provider);

        // Act
        var mapper1 = registry.GetMapper<User, UserDto>();
        var mapper2 = registry.GetMapper<User, UserDto>();

        // Assert
        Assert.Same(mapper1, mapper2);
    }

    [Fact]
    public void MapperRegistry_GetMapper_WithDifferentTypes_ShouldReturnDifferentMappers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMapper(); // ✅ Automatic discovery
        var provider = services.BuildServiceProvider();
        var registry = new MapperRegistry(provider);

        // Act
        var userMapper = registry.GetMapper<User, UserDto>();
        var productMapper = registry.GetMapper<Product, ProductDto>();

        // Assert
        Assert.IsType<UserMapper>(userMapper);
        Assert.IsType<ProductMapper>(productMapper);
        Assert.NotSame(userMapper, productMapper);
    }


} 
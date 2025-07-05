using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SimpleMapper.Tests.TestModels;

namespace SimpleMapper.Tests.Unit;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSimpleMapper_ShouldRegisterRequiredServicesAndDiscoverMappers()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSimpleMapper();

        // Assert
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<MapperRegistry>());
        Assert.NotNull(provider.GetService<IMapper>());
        Assert.IsType<Mapper>(provider.GetService<IMapper>());
        
        // Should auto-discover mappers from calling assembly
        var userMapper = provider.GetService<IMapper<User, UserDto>>();
        Assert.NotNull(userMapper);
        Assert.IsType<UserMapper>(userMapper);
    }

    [Fact]
    public void AddSimpleMapper_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddSimpleMapper();

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddSimpleMapper_ShouldDiscoverAllMappersFromCallingAssembly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSimpleMapper();

        // Assert
        var provider = services.BuildServiceProvider();
        
        // Should find all BaseMapper implementations
        var userMapper = provider.GetService<IMapper<User, UserDto>>();
        Assert.NotNull(userMapper);
        Assert.IsType<UserMapper>(userMapper);

        var addressMapper = provider.GetService<IMapper<Address, AddressDto>>();
        Assert.NotNull(addressMapper);
        Assert.IsType<AddressMapper>(addressMapper);

        var productMapper = provider.GetService<IMapper<Product, ProductDto>>();
        Assert.NotNull(productMapper);
        Assert.IsType<ProductMapper>(productMapper);
    }

    [Fact]
    public void AddSimpleMapper_WithSpecificAssembly_ShouldDiscoverMappersFromThatAssembly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSimpleMapper<User>();

        // Assert
        var provider = services.BuildServiceProvider();
        
        var userMapper = provider.GetService<IMapper<User, UserDto>>();
        Assert.NotNull(userMapper);
        Assert.IsType<UserMapper>(userMapper);
    }

    [Fact]
    public void AddSimpleMapper_WithMultipleAssemblies_ShouldDiscoverMappersFromAllAssemblies()
    {
        // Arrange
        var services = new ServiceCollection();
        var assemblies = new[] { Assembly.GetExecutingAssembly() };

        // Act
        services.AddSimpleMapper(assemblies);

        // Assert
        var provider = services.BuildServiceProvider();
        
        var userMapper = provider.GetService<IMapper<User, UserDto>>();
        Assert.NotNull(userMapper);
        Assert.IsType<UserMapper>(userMapper);
    }

    [Fact]
    public void AddSimpleMapper_ShouldNotRegisterAbstractClasses()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSimpleMapper();

        // Assert
        var provider = services.BuildServiceProvider();
        var serviceDescriptors = services.Where(s => s.ServiceType == typeof(BaseMapper<User, UserDto>)).ToList();

        Assert.Empty(serviceDescriptors);
    }

    [Fact]
    public void AddSimpleMapper_ShouldNotRegisterGenericTypeDefinitions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSimpleMapper();

        // Assert
        var provider = services.BuildServiceProvider();
        var baseMapperDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(BaseMapper<,>));

        Assert.Null(baseMapperDescriptor);
    }

    [Fact]
    public void AddSimpleMapper_WithMapperHavingDependencies_ShouldRegisterCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSimpleMapper();

        // Assert
        var provider = services.BuildServiceProvider();
        
        // UserWithAddressesMapper has IMapper dependency  
        var mapper = provider.GetService<IMapper<UserWithAddresses, UserWithAddressesDto>>();
        Assert.NotNull(mapper);
        Assert.IsType<UserWithAddressesMapper>(mapper);
        
        // Test that it can actually work with nested mapping
        var userWithAddresses = new UserWithAddresses
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Addresses = new List<Address>
            {
                new() { Id = 1, Street = "123 Main St", City = "Anytown", Country = "USA", PostalCode = "12345" }
            }
        };

        var result = mapper.Map(userWithAddresses);
        Assert.NotNull(result);
        Assert.Single(result.Addresses);
        Assert.Equal("123 Main St, Anytown, 12345", result.Addresses[0].FullAddress);
    }

    [Fact]
    public void AddSimpleMapper_CalledMultipleTimes_ShouldNotRegisterDuplicates()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - Add multiple times
        services.AddSimpleMapper();
        services.AddSimpleMapper();

        // Assert - Should not throw and should work normally
        var provider = services.BuildServiceProvider();
        var mapper = provider.GetService<IMapper<User, UserDto>>();
        Assert.NotNull(mapper);
        Assert.IsType<UserMapper>(mapper);
        
        // Verify only one registration per interface
        var allUserMapperServices = services.Where(s => s.ServiceType == typeof(IMapper<User, UserDto>)).ToList();
        Assert.Single(allUserMapperServices);
    }

    [Fact]
    public void AddSimpleMapper_WithSpecificAssembly_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddSimpleMapper<User>();

        // Assert
        Assert.Same(services, result);

        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IMapper>());
        Assert.NotNull(provider.GetService<IMapper<User, UserDto>>());
        Assert.NotNull(provider.GetService<IMapper<Address, AddressDto>>());
    }

    [Fact]
    public void AddSimpleMapper_WithMultipleAssemblies_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var assemblies = new[] { Assembly.GetExecutingAssembly() };

        // Act
        var result = services.AddSimpleMapper(assemblies);

        // Assert
        Assert.Same(services, result);
        
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IMapper>());
        Assert.NotNull(provider.GetService<IMapper<User, UserDto>>());
    }

    [Fact]
    public void AddSimpleMapper_ShouldRegisterAllMappersAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSimpleMapper();

        // Assert
        var provider = services.BuildServiceProvider();
        
        // Get the same mapper instance multiple times
        var mapper1 = provider.GetService<IMapper<User, UserDto>>();
        var mapper2 = provider.GetService<IMapper<User, UserDto>>();
        
        Assert.NotNull(mapper1);
        Assert.NotNull(mapper2);
        Assert.Same(mapper1, mapper2); // Should be the same singleton instance
    }

    [Fact]
    public void AddSimpleMapper_ShouldWorkWithComplexMapperDependencies()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSimpleMapper();

        // Assert
        var provider = services.BuildServiceProvider();
        var mapper = provider.GetService<IMapper>();
        Assert.NotNull(mapper);

        // Test complex nested mapping that requires multiple mappers
        var userWithAddresses = new UserWithAddresses
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Addresses = new List<Address>
            {
                new() { Id = 1, Street = "123 Main St", City = "Anytown", Country = "USA", PostalCode = "12345" },
                new() { Id = 2, Street = "456 Oak Ave", City = "Somewhere", Country = "USA", PostalCode = "67890" }
            }
        };

        var result = mapper.Map<UserWithAddresses, UserWithAddressesDto>(userWithAddresses);
        Assert.NotNull(result);
        Assert.Equal(2, result.Addresses.Count);
        Assert.Equal("123 Main St, Anytown, 12345", result.Addresses[0].FullAddress);
        Assert.Equal("Anytown, USA", result.Addresses[0].Location);
    }

    [Fact]
    public void AddSimpleMapper_ShouldPreventDuplicateMapperRegistration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSimpleMapper();

        // Assert - Multiple registrations should not cause issues
        var provider = services.BuildServiceProvider();
        
        // Verify each mapper type is registered only once
        var userMapperServices = services.Where(s => s.ServiceType == typeof(IMapper<User, UserDto>)).ToList();
        Assert.Single(userMapperServices);
        
        var addressMapperServices = services.Where(s => s.ServiceType == typeof(IMapper<Address, AddressDto>)).ToList();
        Assert.Single(addressMapperServices);
    }
} 
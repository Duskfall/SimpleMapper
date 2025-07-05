using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleMapper.Tests.TestModels;

namespace SimpleMapper.Tests.Integration;

public class IntegrationTests
{
    [Fact]
    public void EndToEnd_AutoDiscovery_ShouldWorkWithRealApplication()
    {
        // Arrange - Simulate a real application setup
        var services = new ServiceCollection();
        
        // Add SimpleMapper with auto-discovery
        services.AddSimpleMapper();
        
        // Add application services
        services.AddScoped<TestApplicationService>();
        
        var provider = services.BuildServiceProvider();

        // Act
        var appService = provider.GetRequiredService<TestApplicationService>();
        
        var user = new User
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            IsActive = true,
            CreatedAt = new DateTime(2023, 1, 1)
        };

        var result = appService.ProcessUser(user);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("John Doe", result.FullName);
        Assert.Equal("john@example.com", result.Email);
        Assert.Equal("Active", result.Status);
        Assert.Equal("2023-01-01", result.CreatedDate);
    }

    [Fact]
    public void EndToEnd_NestedMapping_ShouldWorkCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMapper();
        services.AddScoped<ComplexMappingService>();
        
        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<ComplexMappingService>();

        var userWithAddresses = new UserWithAddresses
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Addresses = new List<Address>
            {
                new() { Id = 1, Street = "123 Main St", City = "Anytown", Country = "USA", PostalCode = "12345" },
                new() { Id = 2, Street = "456 Oak Ave", City = "Somewhere", Country = "USA", PostalCode = "67890" }
            }
        };

        // Act
        var result = service.ProcessUserWithAddresses(userWithAddresses);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("John Doe", result.FullName);
        Assert.Equal(2, result.Addresses.Count);
        
        Assert.Equal("123 Main St, Anytown, 12345", result.Addresses[0].FullAddress);
        Assert.Equal("Anytown, USA", result.Addresses[0].Location);
        
        Assert.Equal("456 Oak Ave, Somewhere, 67890", result.Addresses[1].FullAddress);
        Assert.Equal("Somewhere, USA", result.Addresses[1].Location);
    }

    [Fact]
    public void EndToEnd_CollectionMapping_ShouldHandleLargeCollections()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMapper();
        
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IMapper>();

        // Create a large collection
        var users = Enumerable.Range(1, 1000).Select(i => new User
        {
            Id = i,
            FirstName = $"User{i}",
            LastName = $"LastName{i}",
            Email = $"user{i}@example.com",
            IsActive = i % 2 == 0,
            CreatedAt = DateTime.Now.AddDays(-i)
        }).ToList();

        // Act
        var start = DateTime.Now;
        var results = factory.Map<User, UserDto>(users).ToList();
        var duration = DateTime.Now - start;

        // Assert
        Assert.Equal(1000, results.Count);
        Assert.True(duration < TimeSpan.FromSeconds(1)); // Should be fast
        
        Assert.Equal("User1 LastName1", results[0].FullName);
        Assert.Equal("Inactive", results[0].Status);
        
        Assert.Equal("User2 LastName2", results[1].FullName);
        Assert.Equal("Active", results[1].Status);
    }

    [Fact]
    public void EndToEnd_SimpleRegistration_ShouldWorkCorrectly()
    {
        // Arrange - Simple registration approach
        var services = new ServiceCollection();
        
        // Simple registration - all mappers discovered automatically
        services.AddSimpleMapper();
        
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IMapper>();

        // Act & Assert
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe", IsActive = true };
        var userResult = factory.Map<User, UserDto>(user);
        Assert.Equal("John Doe", userResult.FullName);

        var product = new Product { Id = 1, Name = "Test Product", Price = 99.99m, Category = "Electronics" };
        var productResult = factory.Map<Product, ProductDto>(product);
        Assert.Equal("Test Product", productResult.Name);
        Assert.Equal("$99.99", productResult.FormattedPrice);
    }

    [Fact]
    public void EndToEnd_DependencyInjection_ShouldWorkWithComplexDependencies()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMapper();
        
        // Add complex dependencies
        services.AddSingleton<ITestService, TestService>();
        services.AddScoped<IDatabaseService, MockDatabaseService>();
        services.AddScoped<ILoggingService, MockLoggingService>();
        services.AddScoped<ComplexService>();
        
        var provider = services.BuildServiceProvider();
        var complexService = provider.GetRequiredService<ComplexService>();

        // Act
        var result = complexService.ProcessWithComplexDependencies();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Database", result);
        Assert.Contains("Logging", result);
        Assert.Contains("Mapping", result);
    }

    [Fact]
    public void EndToEnd_ErrorScenarios_ShouldHandleGracefully()
    {
        // Arrange - Register SimpleMapper services but no mappers
        var services = new ServiceCollection();
        services.AddSingleton<MapperRegistry>();
        services.AddSingleton<IMapper, Mapper>();
        
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IMapper>();

        // Act & Assert
        var user = new User { Id = 1 };
        var act = () => factory.Map<User, UserDto>(user);
        
        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Equal("No mapper registered for User -> UserDto", exception.Message);
    }

    [Fact]
    public void EndToEnd_ThreadSafety_ShouldHandleConcurrentAccess()
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
            LastName = $"Test",
            Email = $"user{i}@example.com",
            IsActive = true
        }).ToList();

        var results = new List<UserDto>[10];

        // Act - Concurrent mapping
        Parallel.For(0, 10, i =>
        {
            results[i] = factory.Map<User, UserDto>(users).ToList();
        });

        // Assert
        for (int i = 0; i < 10; i++)
        {
            Assert.Equal(100, results[i].Count);
            Assert.Equal("User1 Test", results[i][0].FullName);
            Assert.Equal("User100 Test", results[i][99].FullName);
        }
    }

    [Fact]
    public void EndToEnd_HostedService_ShouldWorkInHostedEnvironment()
    {
        // Arrange - Simulate ASP.NET Core / Generic Host environment
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSimpleMapper();
                services.AddSingleton<TestHostedService>();
            });

        // Act
        using var host = hostBuilder.Build();
        var hostedService = host.Services.GetRequiredService<TestHostedService>();
        
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe", IsActive = true };
        var result = hostedService.ProcessUser(user);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John Doe", result.FullName);
    }

    [Fact]
    public void EndToEnd_SpecificAssembly_ShouldWorkCorrectly()
    {
        // Arrange - Test using specific assembly registration
        var services = new ServiceCollection();
        services.AddSimpleMapper<User>(); // Scan assembly containing User type
        
        var provider = services.BuildServiceProvider();
        var mapper = provider.GetService<IMapper<User, UserDto>>();
        Assert.NotNull(mapper);

        // Should also have other mappers from the same assembly
        var productMapper = provider.GetService<IMapper<Product, ProductDto>>();
        Assert.NotNull(productMapper);
    }

    [Fact]
    public void EndToEnd_MultipleAssemblies_ShouldWorkCorrectly()
    {
        // Arrange - Test multiple assemblies
        var services = new ServiceCollection();
        var assemblies = new[] { Assembly.GetExecutingAssembly(), typeof(User).Assembly };
        services.AddSimpleMapper(assemblies);
        
        var provider = services.BuildServiceProvider();
        var mapper = provider.GetService<IMapper<User, UserDto>>();
        Assert.NotNull(mapper);
    }
}

// Test services for integration testing
public class TestApplicationService
{
    private readonly IMapper _mapper;

    public TestApplicationService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public UserDto ProcessUser(User user)
    {
        return _mapper.Map<User, UserDto>(user);
    }
}

public class ComplexMappingService
{
    private readonly IMapper _mapper;

    public ComplexMappingService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public UserWithAddressesDto ProcessUserWithAddresses(UserWithAddresses user)
    {
        return _mapper.Map<UserWithAddresses, UserWithAddressesDto>(user);
    }
}

public interface IDatabaseService
{
    string GetData();
}

public class MockDatabaseService : IDatabaseService
{
    public string GetData() => "Database data";
}

public interface ILoggingService
{
    void Log(string message);
    string GetLoggedMessages();
}

public class MockLoggingService : ILoggingService
{
    private readonly List<string> _messages = new();

    public void Log(string message) => _messages.Add(message);
    public string GetLoggedMessages() => string.Join(", ", _messages);
}

public class ComplexService
{
    private readonly IMapper _mapper;
    private readonly IDatabaseService _databaseService;
    private readonly ILoggingService _loggingService;

    public ComplexService(
        IMapper mapper,
        IDatabaseService databaseService,
        ILoggingService loggingService)
    {
        _mapper = mapper;
        _databaseService = databaseService;
        _loggingService = loggingService;
    }

    public string ProcessWithComplexDependencies()
    {
        _loggingService.Log("Processing started");
        
        var data = _databaseService.GetData();
        var user = new User { Id = 1, FirstName = "Test", LastName = "User", IsActive = true };
        var userDto = _mapper.Map<User, UserDto>(user);
        
        _loggingService.Log("Mapping completed");
        
        return $"Database: {data}, Logging: {_loggingService.GetLoggedMessages()}, Mapping: {userDto.FullName}";
    }
}

public class TestHostedService
{
    private readonly IMapper _mapper;

    public TestHostedService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public UserDto ProcessUser(User user)
    {
        return _mapper.Map<User, UserDto>(user);
    }
} 
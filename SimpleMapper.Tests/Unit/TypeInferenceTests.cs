using Microsoft.Extensions.DependencyInjection;
using SimpleMapper;

namespace SimpleMapper.Tests.Unit;

public class TypeInferenceTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMapper _mapper;

    public TypeInferenceTests()
    {
        var services = new ServiceCollection();
        services.AddSimpleMapper();

        _serviceProvider = services.BuildServiceProvider();
        _mapper = _serviceProvider.GetRequiredService<IMapper>();
    }

    [Fact]
    public void Map_WithTypeInference_SingleObject_ShouldWork()
    {
        // Arrange
        var source = new TestSource { Id = 1, Name = "Test" };

        // Act - using type inference
        var result = _mapper.Map<TestDestination>(source);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public void Map_WithTypeInference_Collection_ShouldWork()
    {
        // Arrange
        var sources = new List<TestSource>
        {
            new() { Id = 1, Name = "Test1" },
            new() { Id = 2, Name = "Test2" }
        };

        // Act - using type inference
        var results = _mapper.Map<TestDestination>(sources).ToList();

        // Assert
        Assert.NotNull(results);
        Assert.Equal(2, results.Count);
        Assert.Equal(1, results[0].Id);
        Assert.Equal("Test1", results[0].Name);
        Assert.Equal(2, results[1].Id);
        Assert.Equal("Test2", results[1].Name);
    }

    [Fact]
    public void Map_WithTypeInference_Array_ShouldWork()
    {
        // Arrange
        var sources = new TestSource[]
        {
            new() { Id = 1, Name = "Test1" },
            new() { Id = 2, Name = "Test2" }
        };

        // Act - using type inference
        var results = _mapper.Map<TestDestination>(sources).ToList();

        // Assert
        Assert.NotNull(results);
        Assert.Equal(2, results.Count);
        Assert.Equal(1, results[0].Id);
        Assert.Equal("Test1", results[0].Name);
    }

    [Fact]
    public void Map_WithTypeInference_EmptyGenericCollection_ShouldReturnEmpty()
    {
        // Arrange - empty generic collection should still be able to infer type
        var sources = new List<TestSource>();

        // Act - using type inference
        var results = _mapper.Map<TestDestination>(sources).ToList();

        // Assert
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Fact]
    public void Map_WithTypeInference_EmptyNonGenericCollection_ShouldThrow()
    {
        // Arrange - using non-generic ArrayList which can't provide type information when empty
        var sources = new System.Collections.ArrayList();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            _mapper.Map<TestDestination>(sources).ToList());
        
        Assert.Contains("Cannot infer source type", exception.Message);
    }

    [Fact]
    public void Map_WithTypeInference_NullSource_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _mapper.Map<TestDestination>((object)null!));
    }

    [Fact]
    public void Map_WithTypeInference_NullCollection_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _mapper.Map<TestDestination>((System.Collections.IEnumerable)null!));
    }

    [Fact]
    public void Map_WithTypeInference_CollectionWithNulls_ShouldFilterNulls()
    {
        // Arrange
        var sources = new TestSource?[]
        {
            new() { Id = 1, Name = "Test1" },
            null,
            new() { Id = 2, Name = "Test2" }
        };

        // Act - using type inference
        var results = _mapper.Map<TestDestination>(sources).ToList();

        // Assert
        Assert.NotNull(results);
        Assert.Equal(2, results.Count); // Nulls should be filtered out
        Assert.Equal(1, results[0].Id);
        Assert.Equal(2, results[1].Id);
    }

    // Enum-specific tests
    [Fact]
    public void Map_WithTypeInference_EnumToString_ShouldWork()
    {
        // Arrange
        var source = new TestModels.User 
        { 
            Id = 1, 
            FirstName = "John", 
            LastName = "Doe", 
            Email = "john@example.com",
            Role = TestModels.UserRole.Admin,
            IsActive = true,
            CreatedAt = new DateTime(2023, 1, 1)
        };

        // Act - using type inference
        var result = _mapper.Map<TestModels.UserDto>(source);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("John Doe", result.FullName);
        Assert.Equal("Admin", result.RoleName); // Enum converted to string
    }

    [Fact]
    public void Map_WithTypeInference_EnumToEnum_ShouldWork()
    {
        // Arrange
        var source = new TestModels.Order
        {
            Id = 1,
            CustomerName = "Jane Smith",
            Amount = 99.99m,
            Status = TestModels.OrderStatus.Processing,
            Priority = TestModels.Priority.High,
            CreatedAt = new DateTime(2023, 1, 1, 10, 30, 0)
        };

        // Act - using type inference
        var result = _mapper.Map<TestModels.OrderDto>(source);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Jane Smith", result.CustomerName);
        Assert.Equal(TestModels.OrderStatus.Processing, result.Status); // Enum-to-enum mapping
        Assert.Equal("High Priority", result.PriorityLevel); // Enum to string via pattern matching
    }

    [Fact]
    public void Map_WithTypeInference_EnumCollection_ShouldWork()
    {
        // Arrange
        var source = new TestModels.UserPreferences
        {
            UserId = 1,
            NotificationMethods = new List<TestModels.NotificationMethod> 
            { 
                TestModels.NotificationMethod.Email, 
                TestModels.NotificationMethod.SMS,
                TestModels.NotificationMethod.Push
            },
            PreferredRole = TestModels.UserRole.Admin
        };

        // Act - using type inference
        var result = _mapper.Map<TestModels.UserPreferencesDto>(source);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.UserId);
        Assert.Equal(3, result.NotificationTypes.Count);
        Assert.Contains("Email", result.NotificationTypes);
        Assert.Contains("SMS", result.NotificationTypes);
        Assert.Contains("Push", result.NotificationTypes);
        Assert.Equal("Administrator", result.PreferredRoleDescription);
    }

    [Fact]
    public void Map_WithTypeInference_EnumCollectionArray_ShouldWork()
    {
        // Arrange
        var sources = new TestModels.UserPreferences[]
        {
            new() 
            { 
                UserId = 1, 
                NotificationMethods = new List<TestModels.NotificationMethod> { TestModels.NotificationMethod.Email },
                PreferredRole = TestModels.UserRole.User
            },
            new() 
            { 
                UserId = 2, 
                NotificationMethods = new List<TestModels.NotificationMethod> { TestModels.NotificationMethod.SMS, TestModels.NotificationMethod.InApp },
                PreferredRole = TestModels.UserRole.Admin
            }
        };

        // Act - using type inference
        var results = _mapper.Map<TestModels.UserPreferencesDto>(sources).ToList();

        // Assert
        Assert.NotNull(results);
        Assert.Equal(2, results.Count);
        
        Assert.Equal(1, results[0].UserId);
        Assert.Single(results[0].NotificationTypes);
        Assert.Contains("Email", results[0].NotificationTypes);
        Assert.Equal("Standard User", results[0].PreferredRoleDescription);
        
        Assert.Equal(2, results[1].UserId);
        Assert.Equal(2, results[1].NotificationTypes.Count);
        Assert.Contains("SMS", results[1].NotificationTypes);
        Assert.Contains("InApp", results[1].NotificationTypes);
        Assert.Equal("Administrator", results[1].PreferredRoleDescription);
    }
}

// Public test models and mappers for auto-discovery
public class TestSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TestDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class NestedSource
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
}

public class NestedDestination
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
}

public class TestSourceToDestinationMapper : BaseMapper<TestSource, TestDestination>
{
    public override TestDestination Map(TestSource source)
    {
        return new TestDestination
        {
            Id = source.Id,
            Name = source.Name
        };
    }
}

public class NestedSourceToDestinationMapper : BaseMapper<NestedSource, NestedDestination>
{
    public override NestedDestination Map(NestedSource source)
    {
        return new NestedDestination
        {
            Id = source.Id,
            Value = source.Value
        };
    }
} 
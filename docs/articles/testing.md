# Testing Guide

This guide covers how to effectively test your SimpleMapper implementations.

## Why SimpleMapper is Easy to Test

SimpleMapper is designed with testability in mind:

- **No Reflection**: All mapping is compile-time safe
- **No Dependencies**: Mappers are pure functions with no external dependencies  
- **Interface-based**: Easy to mock `IMapper` for testing services
- **Predictable**: Same input always produces the same output

## Unit Testing Mappers

### Basic Mapper Testing

```csharp
[TestClass]
public class UserMapperTests
{
    private UserMapper _mapper;
    
    [TestInitialize]
    public void Setup()
    {
        _mapper = new UserMapper();
    }
    
    [TestMethod]
    public void Map_ShouldMapAllProperties_WhenValidEntityProvided()
    {
        // Arrange
        var entity = new UserEntity
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            IsActive = true,
            CreatedAt = new DateTime(2023, 1, 15)
        };
        
        // Act
        var result = _mapper.Map(entity);
        
        // Assert
        Assert.AreEqual(1, result.Id);
        Assert.AreEqual("John Doe", result.FullName);
        Assert.AreEqual("john.doe@example.com", result.Email);
        Assert.AreEqual("Active", result.Status);
        Assert.AreEqual("2023-01-15", result.FormattedCreatedDate);
    }
    
    [TestMethod]
    public void Map_ShouldHandleInactiveUser_Correctly()
    {
        // Arrange
        var entity = new UserEntity
        {
            Id = 2,
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@example.com",
            IsActive = false,
            CreatedAt = DateTime.Now
        };
        
        // Act
        var result = _mapper.Map(entity);
        
        // Assert
        Assert.AreEqual("Inactive", result.Status);
    }
}
```

### Testing Edge Cases

```csharp
[TestClass]
public class UserMapperEdgeCaseTests
{
    private UserMapper _mapper;
    
    [TestInitialize]
    public void Setup()
    {
        _mapper = new UserMapper();
    }
    
    [TestMethod]
    public void Map_ShouldHandleNullFirstName_Gracefully()
    {
        // Arrange
        var entity = new UserEntity
        {
            Id = 1,
            FirstName = null,
            LastName = "Doe",
            Email = "doe@example.com",
            IsActive = true
        };
        
        // Act
        var result = _mapper.Map(entity);
        
        // Assert
        Assert.AreEqual("Doe", result.FullName.Trim());
    }
    
    [TestMethod]
    public void Map_ShouldHandleEmptyStrings_Correctly()
    {
        // Arrange
        var entity = new UserEntity
        {
            Id = 1,
            FirstName = "",
            LastName = "",
            Email = "",
            IsActive = true
        };
        
        // Act
        var result = _mapper.Map(entity);
        
        // Assert
        Assert.AreEqual("", result.FullName.Trim());
        Assert.AreEqual("", result.Email);
    }
    
    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Map_ShouldThrowException_WhenEntityIsNull()
    {
        // Act
        _mapper.Map(null);
        
        // Assert is handled by ExpectedException
    }
}
```

### Testing Mappers with Complex Logic

```csharp
[TestClass]
public class OrderMapperTests
{
    private OrderMapper _mapper;
    
    [TestInitialize]
    public void Setup()
    {
        _mapper = new OrderMapper();
    }
    
    [TestMethod]
    public void Map_ShouldCalculateTotal_Correctly()
    {
        // Arrange
        var order = new OrderEntity
        {
            Id = 1,
            Items = new List<OrderItem>
            {
                new() { Quantity = 2, UnitPrice = 10.50m },
                new() { Quantity = 1, UnitPrice = 5.00m },
                new() { Quantity = 3, UnitPrice = 15.00m }
            },
            Status = OrderStatus.Processing
        };
        
        // Act
        var result = _mapper.Map(order);
        
        // Assert
        Assert.AreEqual(71.00m, result.Total); // (2*10.50) + (1*5.00) + (3*15.00)
        Assert.AreEqual(3, result.ItemCount);
        Assert.AreEqual("Processing", result.Status);
    }
    
    [TestMethod]
    public void Map_ShouldHandleEmptyItemsList_Correctly()
    {
        // Arrange
        var order = new OrderEntity
        {
            Id = 1,
            Items = new List<OrderItem>(),
            Status = OrderStatus.Pending
        };
        
        // Act
        var result = _mapper.Map(order);
        
        // Assert
        Assert.AreEqual(0m, result.Total);
        Assert.AreEqual(0, result.ItemCount);
    }
    
    [DataTestMethod]
    [DataRow(OrderStatus.Pending, "Pending")]
    [DataRow(OrderStatus.Processing, "Processing")]
    [DataRow(OrderStatus.Shipped, "Shipped")]
    [DataRow(OrderStatus.Delivered, "Delivered")]
    [DataRow(OrderStatus.Cancelled, "Cancelled")]
    public void Map_ShouldMapStatus_Correctly(OrderStatus status, string expectedDisplay)
    {
        // Arrange
        var order = new OrderEntity
        {
            Id = 1,
            Items = new List<OrderItem>(),
            Status = status
        };
        
        // Act
        var result = _mapper.Map(order);
        
        // Assert
        Assert.AreEqual(expectedDisplay, result.Status);
    }
}
```

## Testing Mappers with Dependencies

### Nested Object Mapping Tests

```csharp
[TestClass]
public class UserWithAddressMapperTests
{
    private UserWithAddressMapper _mapper;
    private Mock<IMapper> _mockMapper;
    
    [TestInitialize]
    public void Setup()
    {
        _mockMapper = new Mock<IMapper>();
        _mapper = new UserWithAddressMapper(_mockMapper.Object);
    }
    
    [TestMethod]
    public void Map_ShouldMapUserAndAddresses_Correctly()
    {
        // Arrange
        var addresses = new List<AddressEntity>
        {
            new() { Id = 1, Street = "123 Main St", City = "Anytown" },
            new() { Id = 2, Street = "456 Oak Ave", City = "Somewhere" }
        };
        
        var user = new UserEntity
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Addresses = addresses
        };
        
        var expectedAddressDtos = new List<AddressDto>
        {
            new() { Id = 1, FullAddress = "123 Main St, Anytown" },
            new() { Id = 2, FullAddress = "456 Oak Ave, Somewhere" }
        };
        
        // Setup mock to return the expected address DTOs
        _mockMapper.Setup(m => m.Map<AddressDto>(addresses))
                   .Returns(expectedAddressDtos);
        
        // Act
        var result = _mapper.Map(user);
        
        // Assert
        Assert.AreEqual(1, result.Id);
        Assert.AreEqual("John Doe", result.Name);
        Assert.AreEqual("john@example.com", result.Email);
        Assert.AreEqual(2, result.Addresses.Count);
        Assert.AreEqual("123 Main St, Anytown", result.Addresses[0].FullAddress);
        Assert.AreEqual("456 Oak Ave, Somewhere", result.Addresses[1].FullAddress);
        
        // Verify the mapper was called correctly
        _mockMapper.Verify(m => m.Map<AddressDto>(addresses), Times.Once);
    }
}
```

## Testing Services That Use Mappers

### Mocking IMapper in Service Tests

```csharp
[TestClass]
public class UserServiceTests
{
    private UserService _userService;
    private Mock<IMapper> _mockMapper;
    private Mock<IUserRepository> _mockRepository;
    
    [TestInitialize]
    public void Setup()
    {
        _mockMapper = new Mock<IMapper>();
        _mockRepository = new Mock<IUserRepository>();
        _userService = new UserService(_mockMapper.Object, _mockRepository.Object);
    }
    
    [TestMethod]
    public async Task GetUserAsync_ShouldReturnMappedDto_WhenUserExists()
    {
        // Arrange
        var userId = 1;
        var userEntity = new UserEntity
        {
            Id = userId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com"
        };
        
        var expectedDto = new UserDto
        {
            Id = userId,
            FullName = "John Doe",
            Email = "john@example.com"
        };
        
        _mockRepository.Setup(r => r.GetByIdAsync(userId))
                      .ReturnsAsync(userEntity);
        
        _mockMapper.Setup(m => m.Map<UserDto>(userEntity))
                   .Returns(expectedDto);
        
        // Act
        var result = await _userService.GetUserAsync(userId);
        
        // Assert
        Assert.AreEqual(expectedDto.Id, result.Id);
        Assert.AreEqual(expectedDto.FullName, result.FullName);
        Assert.AreEqual(expectedDto.Email, result.Email);
        
        // Verify interactions
        _mockRepository.Verify(r => r.GetByIdAsync(userId), Times.Once);
        _mockMapper.Verify(m => m.Map<UserDto>(userEntity), Times.Once);
    }
    
    [TestMethod]
    public async Task GetAllUsersAsync_ShouldReturnMappedList_WhenUsersExist()
    {
        // Arrange
        var userEntities = new List<UserEntity>
        {
            new() { Id = 1, FirstName = "John", LastName = "Doe" },
            new() { Id = 2, FirstName = "Jane", LastName = "Smith" }
        };
        
        var userDtos = new List<UserDto>
        {
            new() { Id = 1, FullName = "John Doe" },
            new() { Id = 2, FullName = "Jane Smith" }
        };
        
        _mockRepository.Setup(r => r.GetAllAsync())
                      .ReturnsAsync(userEntities);
        
        _mockMapper.Setup(m => m.Map<UserDto>(userEntities))
                   .Returns(userDtos);
        
        // Act
        var result = await _userService.GetAllUsersAsync();
        
        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("John Doe", result[0].FullName);
        Assert.AreEqual("Jane Smith", result[1].FullName);
        
        _mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
        _mockMapper.Verify(m => m.Map<UserDto>(userEntities), Times.Once);
    }
}
```

## Integration Testing

### Testing with Real Dependencies

```csharp
[TestClass]
public class UserServiceIntegrationTests
{
    private UserService _userService;
    private IMapper _mapper;
    private Mock<IUserRepository> _mockRepository;
    
    [TestInitialize]
    public void Setup()
    {
        // Use real mapper with real dependencies
        var services = new ServiceCollection();
        services.AddSimpleMapper<UserMapper>(); // Add mappers from assembly containing UserMapper
        
        var serviceProvider = services.BuildServiceProvider();
        _mapper = serviceProvider.GetRequiredService<IMapper>();
        
        _mockRepository = new Mock<IUserRepository>();
        _userService = new UserService(_mapper, _mockRepository.Object);
    }
    
    [TestMethod]
    public async Task GetUserAsync_WithRealMapper_ShouldMapCorrectly()
    {
        // Arrange
        var userEntity = new UserEntity
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            IsActive = true,
            CreatedAt = new DateTime(2023, 1, 15)
        };
        
        _mockRepository.Setup(r => r.GetByIdAsync(1))
                      .ReturnsAsync(userEntity);
        
        // Act - using real mapper
        var result = await _userService.GetUserAsync(1);
        
        // Assert
        Assert.AreEqual(1, result.Id);
        Assert.AreEqual("John Doe", result.FullName);
        Assert.AreEqual("john@example.com", result.Email);
        Assert.AreEqual("Active", result.Status);
        Assert.AreEqual("2023-01-15", result.FormattedCreatedDate);
    }
}
```

### Testing Mapper Registration

```csharp
[TestClass]
public class MapperRegistrationTests
{
    [TestMethod]
    public void AddSimpleMapper_ShouldRegisterAllMappers_InAssembly()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act
        services.AddSimpleMapper<UserMapper>();
        var serviceProvider = services.BuildServiceProvider();
        
        // Assert - Check that specific mappers are registered
        var userMapper = serviceProvider.GetService<IMapper<UserEntity, UserDto>>();
        var addressMapper = serviceProvider.GetService<IMapper<AddressEntity, AddressDto>>();
        var mainMapper = serviceProvider.GetService<IMapper>();
        
        Assert.IsNotNull(userMapper);
        Assert.IsNotNull(addressMapper);
        Assert.IsNotNull(mainMapper);
        
        // Verify they work
        var user = new UserEntity { Id = 1, FirstName = "Test", LastName = "User" };
        var userDto = userMapper.Map(user);
        Assert.AreEqual("Test User", userDto.FullName);
    }
    
    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void AddSimpleMapper_ShouldThrowException_WhenDuplicateMappersFound()
    {
        // This test would require having duplicate mappers in the test assembly
        // to verify the duplicate detection works
        var services = new ServiceCollection();
        
        // This should throw because we have duplicate mappers
        services.AddSimpleMapper<DuplicateMapper1>();
    }
}
```

## Performance Testing

### Benchmarking Mapper Performance

```csharp
[TestClass]
public class MapperPerformanceTests
{
    private const int Iterations = 100_000;
    
    [TestMethod]
    public void Map_PerformanceTest_ShouldCompleteWithinExpectedTime()
    {
        // Arrange
        var mapper = new UserMapper();
        var user = new UserEntity
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        
        // Act
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < Iterations; i++)
        {
            var result = mapper.Map(user);
        }
        
        stopwatch.Stop();
        
        // Assert - Should complete in reasonable time (adjust threshold as needed)
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, 
            $"Mapping {Iterations} items took {stopwatch.ElapsedMilliseconds}ms");
        
        Console.WriteLine($"Mapped {Iterations} items in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Average: {(double)stopwatch.ElapsedMilliseconds / Iterations:F4}ms per mapping");
    }
    
    [TestMethod]
    public void Map_CollectionPerformance_ShouldHandleLargeCollections()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMapper<UserMapper>();
        var serviceProvider = services.BuildServiceProvider();
        var mapper = serviceProvider.GetRequiredService<IMapper>();
        
        var users = Enumerable.Range(1, 10_000)
            .Select(i => new UserEntity
            {
                Id = i,
                FirstName = $"User{i}",
                LastName = "Test",
                Email = $"user{i}@example.com",
                IsActive = i % 2 == 0
            })
            .ToList();
        
        // Act
        var stopwatch = Stopwatch.StartNew();
        var results = mapper.Map<UserDto>(users).ToList();
        stopwatch.Stop();
        
        // Assert
        Assert.AreEqual(10_000, results.Count);
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000, 
            $"Mapping 10,000 items took {stopwatch.ElapsedMilliseconds}ms");
        
        Console.WriteLine($"Mapped 10,000 items in {stopwatch.ElapsedMilliseconds}ms");
    }
}
```

## Test Utilities and Helpers

### Creating Test Data Builders

```csharp
public class UserEntityBuilder
{
    private UserEntity _user = new();
    
    public static UserEntityBuilder Default() => new();
    
    public UserEntityBuilder WithId(int id)
    {
        _user.Id = id;
        return this;
    }
    
    public UserEntityBuilder WithName(string firstName, string lastName)
    {
        _user.FirstName = firstName;
        _user.LastName = lastName;
        return this;
    }
    
    public UserEntityBuilder WithEmail(string email)
    {
        _user.Email = email;
        return this;
    }
    
    public UserEntityBuilder IsActive(bool isActive = true)
    {
        _user.IsActive = isActive;
        return this;
    }
    
    public UserEntityBuilder CreatedAt(DateTime createdAt)
    {
        _user.CreatedAt = createdAt;
        return this;
    }
    
    public UserEntity Build() => _user;
}

// Usage in tests:
[TestMethod]
public void TestWithBuilder()
{
    var user = UserEntityBuilder.Default()
        .WithId(1)
        .WithName("John", "Doe")
        .WithEmail("john@example.com")
        .IsActive()
        .CreatedAt(new DateTime(2023, 1, 15))
        .Build();
        
    var result = _mapper.Map(user);
    // Assert...
}
```

### Custom Assertions

```csharp
public static class MapperAssertions
{
    public static void ShouldMapTo<TSource, TDestination>(
        this IMapper<TSource, TDestination> mapper,
        TSource source,
        Action<TDestination> assertions)
    {
        var result = mapper.Map(source);
        assertions(result);
    }
    
    public static void ShouldHaveEqualProperties(this UserDto actual, UserDto expected)
    {
        Assert.AreEqual(expected.Id, actual.Id);
        Assert.AreEqual(expected.FullName, actual.FullName);
        Assert.AreEqual(expected.Email, actual.Email);
        Assert.AreEqual(expected.Status, actual.Status);
    }
}

// Usage:
[TestMethod]
public void TestWithCustomAssertions()
{
    var user = UserEntityBuilder.Default().WithName("John", "Doe").Build();
    
    _mapper.ShouldMapTo(user, result =>
    {
        Assert.AreEqual("John Doe", result.FullName);
        Assert.IsNotNull(result.Email);
    });
}
```

## Best Practices for Testing Mappers

1. **Test All Properties**: Ensure every property is mapped correctly
2. **Test Edge Cases**: Null values, empty strings, boundary conditions
3. **Test Business Logic**: Any calculations or transformations in mappers
4. **Use Test Builders**: Create reusable test data builders for complex entities
5. **Mock External Dependencies**: When testing mappers with IMapper injection
6. **Performance Test**: Verify mappers perform well with large datasets
7. **Integration Test**: Test the complete mapping pipeline occasionally

By following these testing patterns, you can ensure your SimpleMapper implementations are robust, performant, and maintainable. 
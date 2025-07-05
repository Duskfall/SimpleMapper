using SimpleMapper.Tests.TestModels;

namespace SimpleMapper.Tests.Unit;

public class BaseMapperTests
{
    [Fact]
    public void BaseMapper_ShouldImplementIMapperInterface()
    {
        // Arrange & Act
        var mapper = new UserMapper();

        // Assert
        Assert.IsAssignableFrom<IMapper<User, UserDto>>(mapper);
    }

    [Fact]
    public void BaseMapper_Map_ShouldMapCorrectly()
    {
        // Arrange
        var mapper = new UserMapper();
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
        var result = mapper.Map(user);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("John Doe", result.FullName);
        Assert.Equal("john@example.com", result.Email);
        Assert.Equal("Active", result.Status);
        Assert.Equal("2023-01-01", result.CreatedDate);
    }

    [Fact]
    public void BaseMapper_Map_WithInactiveUser_ShouldSetInactiveStatus()
    {
        // Arrange
        var mapper = new UserMapper();
        var user = new User
        {
            Id = 2,
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane@example.com",
            IsActive = false,
            CreatedAt = new DateTime(2023, 6, 15)
        };

        // Act
        var result = mapper.Map(user);

        // Assert
        Assert.Equal("Inactive", result.Status);
        Assert.Equal("Jane Smith", result.FullName);
    }

    [Fact]
    public void BaseMapper_Map_WithEmptyNames_ShouldHandleGracefully()
    {
        // Arrange
        var mapper = new UserMapper();
        var user = new User
        {
            Id = 3,
            FirstName = "",
            LastName = "",
            Email = "test@example.com",
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        // Act
        var result = mapper.Map(user);

        // Assert
        Assert.Equal(" ", result.FullName); // Space between empty first and last name
        Assert.Equal("test@example.com", result.Email);
    }

    [Theory]
    [InlineData(true, "Active")]
    [InlineData(false, "Inactive")]
    public void BaseMapper_Map_ShouldHandleDifferentActiveStates(bool isActive, string expectedStatus)
    {
        // Arrange
        var mapper = new UserMapper();
        var user = new User
        {
            Id = 1,
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            IsActive = isActive,
            CreatedAt = DateTime.Now
        };

        // Act
        var result = mapper.Map(user);

                // Assert
        Assert.Equal(expectedStatus, result.Status);
    }

    [Fact]
    public void ThrowingMapper_ShouldPropagateException()
    {
        // Arrange
        var throwingMapper = new ThrowingMapperForTesting();
        var user = new User();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => throwingMapper.Map(user));
        Assert.Equal("Test exception", exception.Message);
    }
} 
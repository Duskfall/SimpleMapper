using SimpleMapper;

namespace SimpleMapper.Tests.Unit;

public class MapperKeyTests
{
    [Fact]
    public void MapperKey_Constructor_WithValidTypes_ShouldCreateKey()
    {
        // Arrange
        var sourceType = typeof(string);
        var destinationType = typeof(int);

        // Act
        var key = new MapperKey(sourceType, destinationType);

        // Assert
        Assert.Equal(sourceType, key.SourceType);
        Assert.Equal(destinationType, key.DestinationType);
        Assert.NotEqual(0, key.HashCode);
    }

    [Fact]
    public void MapperKey_Constructor_WithNullSourceType_ShouldThrowArgumentNullException()
    {
        // Arrange
        var destinationType = typeof(int);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new MapperKey(null!, destinationType));
        Assert.Equal("sourceType", exception.ParamName);
    }

    [Fact]
    public void MapperKey_Constructor_WithNullDestinationType_ShouldThrowArgumentNullException()
    {
        // Arrange
        var sourceType = typeof(string);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new MapperKey(sourceType, null!));
        Assert.Equal("destinationType", exception.ParamName);
    }

    [Fact]
    public void MapperKey_Equals_WithSameTypes_ShouldReturnTrue()
    {
        // Arrange
        var key1 = new MapperKey(typeof(string), typeof(int));
        var key2 = new MapperKey(typeof(string), typeof(int));

        // Act & Assert
        Assert.True(key1.Equals(key2));
        Assert.True(key1 == key2);
        Assert.False(key1 != key2);
    }

    [Fact]
    public void MapperKey_Equals_WithDifferentSourceTypes_ShouldReturnFalse()
    {
        // Arrange
        var key1 = new MapperKey(typeof(string), typeof(int));
        var key2 = new MapperKey(typeof(bool), typeof(int));

        // Act & Assert
        Assert.False(key1.Equals(key2));
        Assert.False(key1 == key2);
        Assert.True(key1 != key2);
    }

    [Fact]
    public void MapperKey_Equals_WithDifferentDestinationTypes_ShouldReturnFalse()
    {
        // Arrange
        var key1 = new MapperKey(typeof(string), typeof(int));
        var key2 = new MapperKey(typeof(string), typeof(bool));

        // Act & Assert
        Assert.False(key1.Equals(key2));
        Assert.False(key1 == key2);
        Assert.True(key1 != key2);
    }

    [Fact]
    public void MapperKey_Equals_WithObject_ShouldWorkCorrectly()
    {
        // Arrange
        var key1 = new MapperKey(typeof(string), typeof(int));
        var key2 = new MapperKey(typeof(string), typeof(int));
        object key2AsObject = key2;

        // Act & Assert
        Assert.True(key1.Equals(key2AsObject));
        Assert.False(key1.Equals((object)"not a mapper key"));
        Assert.False(key1.Equals(null));
    }

    [Fact]
    public void MapperKey_GetHashCode_WithSameTypes_ShouldReturnSameHash()
    {
        // Arrange
        var key1 = new MapperKey(typeof(string), typeof(int));
        var key2 = new MapperKey(typeof(string), typeof(int));

        // Act & Assert
        Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
        Assert.Equal(key1.HashCode, key2.HashCode);
    }

    [Fact]
    public void MapperKey_GetHashCode_WithDifferentTypes_ShouldReturnDifferentHash()
    {
        // Arrange
        var key1 = new MapperKey(typeof(string), typeof(int));
        var key2 = new MapperKey(typeof(bool), typeof(int));
        var key3 = new MapperKey(typeof(string), typeof(bool));

        // Act & Assert
        Assert.NotEqual(key1.GetHashCode(), key2.GetHashCode());
        Assert.NotEqual(key1.GetHashCode(), key3.GetHashCode());
    }

    [Fact]
    public void MapperKey_ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var key = new MapperKey(typeof(string), typeof(int));

        // Act
        var result = key.ToString();

        // Assert
        Assert.Equal("String -> Int32", result);
    }

    [Fact]
    public void MapperKey_UsedInDictionary_ShouldWorkAsExpected()
    {
        // Arrange
        var dict = new Dictionary<MapperKey, string>();
        var key1 = new MapperKey(typeof(string), typeof(int));
        var key2 = new MapperKey(typeof(string), typeof(int)); // Same types
        var key3 = new MapperKey(typeof(bool), typeof(int)); // Different source

        // Act
        dict[key1] = "value1";
        dict[key3] = "value3";

        // Assert
        Assert.Equal("value1", dict[key1]);
        Assert.Equal("value1", dict[key2]); // Should find same value due to equality
        Assert.Equal("value3", dict[key3]);
        Assert.Equal(2, dict.Count); // Only 2 unique keys
    }

    [Theory]
    [InlineData(typeof(int), typeof(string))]
    [InlineData(typeof(List<string>), typeof(List<int>))]
    [InlineData(typeof(Dictionary<string, int>), typeof(Dictionary<int, string>))]
    [InlineData(typeof(TestModels.User), typeof(TestModels.UserDto))]
    public void MapperKey_WithVariousTypes_ShouldHandleCorrectly(Type sourceType, Type destinationType)
    {
        // Act
        var key = new MapperKey(sourceType, destinationType);

        // Assert
        Assert.Equal(sourceType, key.SourceType);
        Assert.Equal(destinationType, key.DestinationType);
        Assert.NotEqual(0, key.HashCode);
        Assert.Contains(sourceType.Name, key.ToString());
        Assert.Contains(destinationType.Name, key.ToString());
    }

    [Fact]
    public void MapperKey_WithGenericTypes_ShouldWorkCorrectly()
    {
        // Arrange
        var sourceType = typeof(List<TestModels.User>);
        var destinationType = typeof(List<TestModels.UserDto>);

        // Act
        var key = new MapperKey(sourceType, destinationType);

        // Assert
        Assert.Equal(sourceType, key.SourceType);
        Assert.Equal(destinationType, key.DestinationType);
        Assert.Contains("List", key.ToString());
    }

    [Fact]
    public void MapperKey_EqualitySymmetry_ShouldWork()
    {
        // Arrange
        var key1 = new MapperKey(typeof(string), typeof(int));
        var key2 = new MapperKey(typeof(string), typeof(int));

        // Act & Assert - Test symmetry
        Assert.True(key1.Equals(key2));
        Assert.True(key2.Equals(key1));
        Assert.True(key1 == key2);
        Assert.True(key2 == key1);
    }

    [Fact]
    public void MapperKey_EqualityTransitivity_ShouldWork()
    {
        // Arrange
        var key1 = new MapperKey(typeof(string), typeof(int));
        var key2 = new MapperKey(typeof(string), typeof(int));
        var key3 = new MapperKey(typeof(string), typeof(int));

        // Act & Assert - Test transitivity
        Assert.True(key1.Equals(key2));
        Assert.True(key2.Equals(key3));
        Assert.True(key1.Equals(key3));
    }
} 
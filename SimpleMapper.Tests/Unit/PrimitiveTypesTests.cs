using Microsoft.Extensions.DependencyInjection;
using SimpleMapper;
using SimpleMapper.Tests.TestModels;

namespace SimpleMapper.Tests.Unit;

public class PrimitiveTypesTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMapper _mapper;

    public PrimitiveTypesTests()
    {
        var services = new ServiceCollection();
        services.AddSimpleMapper();

        _serviceProvider = services.BuildServiceProvider();
        _mapper = _serviceProvider.GetRequiredService<IMapper>();
    }

    [Fact]
    public void Map_NumericTypes_ShouldMapAllPrimitiveTypes()
    {
        // Arrange
        var source = new NumericTypesSource();

        // Act
        var result = _mapper.Map<NumericTypesSource, NumericTypesDestination>(source);

        // Assert - Test all signed integer types
        Assert.Equal(source.SByteValue.ToString(), result.SByteString);
        Assert.Equal(source.ShortValue.ToString(), result.ShortString);
        Assert.Equal(source.IntValue.ToString(), result.IntString);
        Assert.Equal(source.LongValue.ToString(), result.LongString);

        // Test all unsigned integer types
        Assert.Equal(source.ByteValue.ToString(), result.ByteString);
        Assert.Equal(source.UShortValue.ToString(), result.UShortString);
        Assert.Equal(source.UIntValue.ToString(), result.UIntString);
        Assert.Equal(source.ULongValue.ToString(), result.ULongString);

        // Test floating point types
        Assert.Equal(source.FloatValue.ToString("F5"), result.FloatString);
        Assert.Equal(source.DoubleValue.ToString("F10"), result.DoubleString);
        Assert.Equal(source.DecimalValue.ToString("F6"), result.DecimalString);

        // Test other value types
        Assert.Equal(source.BoolValue.ToString(), result.BoolString);
        Assert.Equal(source.CharValue.ToString(), result.CharString);

        // Test nullable types
        Assert.Equal("42", result.NullableIntString);
        Assert.Equal("NULL", result.NullableDecimalString);
        Assert.Equal("False", result.NullableBoolString);
        Assert.Equal("Z", result.NullableCharString);

        // Test DateTime, TimeSpan, Guid
        Assert.Equal("2023-12-25 10:30:45", result.DateTimeString);
        Assert.Equal("01:30:45", result.TimeSpanString);
        Assert.Equal("12345678-1234-5678-9012-123456789012", result.GuidString);

        // Test struct mappings
        Assert.Equal(10, result.PointDto.X);
        Assert.Equal(20, result.PointDto.Y);
        Assert.Equal("Point(10, 20)", result.PointDto.Description);
        Assert.Equal("1.5x2x3.5", result.DimensionsDto.FormattedSize);
        Assert.Equal(10.5, result.DimensionsDto.Volume, 1);
    }

    [Fact]
    public void Map_NumericTypes_WithTypeInference_ShouldWork()
    {
        // Arrange
        var source = new NumericTypesSource();

        // Act - using type inference
        var result = _mapper.Map<NumericTypesDestination>(source);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(source.IntValue.ToString(), result.IntString);
        Assert.Equal(source.DecimalValue.ToString("F6"), result.DecimalString);
        Assert.Equal(source.BoolValue.ToString(), result.BoolString);
    }

    [Fact]
    public void Map_TypeConversions_ShouldHandleImplicitConversions()
    {
        // Arrange
        var source = new TypeConversionSource();

        // Act
        var result = _mapper.Map<TypeConversionSource, TypeConversionDestination>(source);

        // Assert
        Assert.Equal(42L, result.LongValue); // int to long
        Assert.Equal(3.14, result.DoubleValue, 2); // float to double
        Assert.Equal(255, result.IntValue); // byte to int
        Assert.Equal('X', result.CharValue); // string to char
        Assert.Equal(100, result.NullableValue); // int to nullable int
        Assert.Equal(99.99m, result.DecimalValue); // double to decimal
    }

    [Fact]
    public void Map_TypeConversions_WithTypeInference_ShouldWork()
    {
        // Arrange
        var source = new TypeConversionSource();

        // Act - using type inference
        var result = _mapper.Map<TypeConversionDestination>(source);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42L, result.LongValue);
        Assert.Equal('X', result.CharValue);
    }

    [Fact]
    public void Map_Collections_ShouldMapDifferentCollectionTypes()
    {
        // Arrange
        var source = new CollectionsSource();

        // Act
        var result = _mapper.Map<CollectionsSource, CollectionsDestination>(source);

        // Assert
        // Test List<int> to List<string>
        Assert.Equal(5, result.IntStringList.Count);
        Assert.Equal(new[] { "1", "2", "3", "4", "5" }, result.IntStringList);

        // Test int[] to string[]
        Assert.Equal(3, result.IntStringArray.Length);
        Assert.Equal(new[] { "10", "20", "30" }, result.IntStringArray);

        // Test HashSet<string> transformation
        Assert.Equal(3, result.UpperStringSet.Count);
        Assert.Contains("A", result.UpperStringSet);
        Assert.Contains("B", result.UpperStringSet);
        Assert.Contains("C", result.UpperStringSet);

        // Test Dictionary<string, int> to Dictionary<string, string>
        Assert.Equal(2, result.StringIntStringDict.Count);
        Assert.Equal("1", result.StringIntStringDict["one"]);
        Assert.Equal("2", result.StringIntStringDict["two"]);

        // Test List<struct> to List<struct DTO>
        Assert.Equal(2, result.StructDtoList.Count);
        Assert.Equal(1, result.StructDtoList[0].X);
        Assert.Equal(2, result.StructDtoList[0].Y);
        Assert.Equal("Point(1, 2)", result.StructDtoList[0].Description);

        // Test nullable array
        Assert.Equal(3, result.NullableStringArray.Length);
        Assert.Equal("1.1", result.NullableStringArray[0]);
        Assert.Equal("NULL", result.NullableStringArray[1]);
        Assert.Equal("3.3", result.NullableStringArray[2]);
    }

    [Fact]
    public void Map_Collections_WithTypeInference_ShouldWork()
    {
        // Arrange
        var source = new CollectionsSource();

        // Act - using type inference
        var result = _mapper.Map<CollectionsDestination>(source);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.IntStringList.Count);
        Assert.Equal("1", result.IntStringList[0]);
    }

    [Fact]
    public void Map_CollectionArray_WithTypeInference_ShouldWork()
    {
        // Arrange
        var sources = new CollectionsSource[]
        {
            new() { IntList = new List<int> { 1, 2 } },
            new() { IntList = new List<int> { 3, 4 } }
        };

        // Act - using type inference with array
        var results = _mapper.Map<CollectionsDestination>(sources).ToList();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal(2, results[0].IntStringList.Count);
        Assert.Equal("1", results[0].IntStringList[0]);
        Assert.Equal("2", results[0].IntStringList[1]);
        Assert.Equal(2, results[1].IntStringList.Count);
        Assert.Equal("3", results[1].IntStringList[0]);
        Assert.Equal("4", results[1].IntStringList[1]);
    }

    [Fact]
    public void Map_Structs_ShouldMapValueTypes()
    {
        // Arrange
        var pointSource = new Point { X = 100, Y = 200 };

        // Act - Note: We need a dedicated mapper for direct struct mapping
        // For now, test struct mapping as part of the comprehensive test above
        var numericSource = new NumericTypesSource();
        var result = _mapper.Map<NumericTypesSource, NumericTypesDestination>(numericSource);

        // Assert
        Assert.Equal(10, result.PointDto.X);
        Assert.Equal(20, result.PointDto.Y);
        Assert.NotEmpty(result.PointDto.Description);
    }

    [Fact]
    public void Map_NullableTypes_ShouldHandleNullsCorrectly()
    {
        // Arrange
        var source = new NumericTypesSource
        {
            NullableInt = null,
            NullableDecimal = 99.99m,
            NullableBool = null,
            NullableChar = null
        };

        // Act
        var result = _mapper.Map<NumericTypesSource, NumericTypesDestination>(source);

        // Assert
        Assert.Equal("NULL", result.NullableIntString);
        Assert.Equal("99.99", result.NullableDecimalString);
        Assert.Equal("NULL", result.NullableBoolString);
        Assert.Equal("NULL", result.NullableCharString);
    }

    [Fact]
    public void Map_ExtremeValues_ShouldHandleMinMaxValues()
    {
        // Arrange
        var source = new NumericTypesSource
        {
            SByteValue = sbyte.MaxValue,
            ShortValue = short.MinValue,
            IntValue = int.MaxValue,
            LongValue = long.MinValue,
            ByteValue = byte.MaxValue,
            UShortValue = ushort.MaxValue,
            UIntValue = uint.MaxValue,
            ULongValue = ulong.MaxValue,
            FloatValue = float.MaxValue,
            DoubleValue = double.MinValue,
            DecimalValue = decimal.MaxValue
        };

        // Act
        var result = _mapper.Map<NumericTypesSource, NumericTypesDestination>(source);

        // Assert
        Assert.Equal(sbyte.MaxValue.ToString(), result.SByteString);
        Assert.Equal(short.MinValue.ToString(), result.ShortString);
        Assert.Equal(int.MaxValue.ToString(), result.IntString);
        Assert.Equal(long.MinValue.ToString(), result.LongString);
        Assert.Equal(byte.MaxValue.ToString(), result.ByteString);
        Assert.Equal(ushort.MaxValue.ToString(), result.UShortString);
        Assert.Equal(uint.MaxValue.ToString(), result.UIntString);
        Assert.Equal(ulong.MaxValue.ToString(), result.ULongString);
    }

    [Fact]
    public void Map_SpecialFloatingPointValues_ShouldHandleCorrectly()
    {
        // Arrange
        var source = new NumericTypesSource
        {
            FloatValue = float.NaN,
            DoubleValue = double.PositiveInfinity
        };

        // Act
        var result = _mapper.Map<NumericTypesSource, NumericTypesDestination>(source);

        // Assert
        Assert.Equal(float.NaN.ToString("F5"), result.FloatString);
        Assert.Equal(double.PositiveInfinity.ToString("F10"), result.DoubleString);
    }

    [Fact]
    public void Map_EmptyAndNullCollections_ShouldHandleGracefully()
    {
        // Arrange
        var source = new CollectionsSource
        {
            IntList = new List<int>(),
            IntArray = Array.Empty<int>(),
            StringSet = new HashSet<string>(),
            StringIntDict = new Dictionary<string, int>(),
            StructList = new List<Point>(),
            NullableArray = Array.Empty<decimal?>()
        };

        // Act
        var result = _mapper.Map<CollectionsSource, CollectionsDestination>(source);

        // Assert
        Assert.Empty(result.IntStringList);
        Assert.Empty(result.IntStringArray);
        Assert.Empty(result.UpperStringSet);
        Assert.Empty(result.StringIntStringDict);
        Assert.Empty(result.StructDtoList);
        Assert.Empty(result.NullableStringArray);
    }
} 
using SimpleMapper;

namespace SimpleMapper.Tests.TestModels
{

// Enum types for testing
public enum UserRole
{
    Guest = 0,
    User = 1,
    Admin = 2,
    SuperAdmin = 3
}

public enum OrderStatus
{
    Pending = 0,
    Processing = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}

public enum Priority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum NotificationMethod
{
    Email,
    SMS,
    Push,
    InApp
}

// Struct types for testing value type scenarios
public struct Point
{
    public int X { get; set; }
    public int Y { get; set; }
}

public struct PointDto
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Description { get; set; }
}

public struct Dimensions
{
    public float Width { get; set; }
    public float Height { get; set; }
    public double Depth { get; set; }
}

public struct DimensionsDto
{
    public string FormattedSize { get; set; }
    public double Volume { get; set; }
}

// Test models for comprehensive primitive type coverage
public class NumericTypesSource
{
    // Signed integer types
    public sbyte SByteValue { get; set; } = -42;
    public short ShortValue { get; set; } = -1000;
    public int IntValue { get; set; } = -50000;
    public long LongValue { get; set; } = -1000000L;
    
    // Unsigned integer types  
    public byte ByteValue { get; set; } = 200;
    public ushort UShortValue { get; set; } = 60000;
    public uint UIntValue { get; set; } = 4000000000U;
    public ulong ULongValue { get; set; } = 10000000000UL;
    
    // Floating point types
    public float FloatValue { get; set; } = 3.14159f;
    public double DoubleValue { get; set; } = 3.14159265359;
    public decimal DecimalValue { get; set; } = 123.456789m;
    
    // Other value types
    public bool BoolValue { get; set; } = true;
    public char CharValue { get; set; } = 'A';
    
    // Nullable value types
    public int? NullableInt { get; set; } = 42;
    public decimal? NullableDecimal { get; set; } = null;
    public bool? NullableBool { get; set; } = false;
    public char? NullableChar { get; set; } = 'Z';
    
    // DateTime and other common types
    public DateTime DateTimeValue { get; set; } = new DateTime(2023, 12, 25, 10, 30, 45);
    public TimeSpan TimeSpanValue { get; set; } = new TimeSpan(1, 30, 45);
    public Guid GuidValue { get; set; } = new Guid("12345678-1234-5678-9012-123456789012");
    
    // Struct values
    public Point PointValue { get; set; } = new Point { X = 10, Y = 20 };
    public Dimensions DimensionsValue { get; set; } = new Dimensions { Width = 1.5f, Height = 2.0f, Depth = 3.5 };
}

public class NumericTypesDestination
{
    // All types converted to strings for validation
    public string SByteString { get; set; } = string.Empty;
    public string ShortString { get; set; } = string.Empty;
    public string IntString { get; set; } = string.Empty;
    public string LongString { get; set; } = string.Empty;
    
    public string ByteString { get; set; } = string.Empty;
    public string UShortString { get; set; } = string.Empty;
    public string UIntString { get; set; } = string.Empty;
    public string ULongString { get; set; } = string.Empty;
    
    public string FloatString { get; set; } = string.Empty;
    public string DoubleString { get; set; } = string.Empty;
    public string DecimalString { get; set; } = string.Empty;
    
    public string BoolString { get; set; } = string.Empty;
    public string CharString { get; set; } = string.Empty;
    
    // Nullable handling
    public string NullableIntString { get; set; } = string.Empty;
    public string NullableDecimalString { get; set; } = string.Empty;
    public string NullableBoolString { get; set; } = string.Empty;
    public string NullableCharString { get; set; } = string.Empty;
    
    // Date/time formatting
    public string DateTimeString { get; set; } = string.Empty;
    public string TimeSpanString { get; set; } = string.Empty;
    public string GuidString { get; set; } = string.Empty;
    
    // Struct mappings
    public PointDto PointDto { get; set; } = new PointDto();
    public DimensionsDto DimensionsDto { get; set; } = new DimensionsDto();
}

// Test models for type conversion scenarios
public class TypeConversionSource
{
    public int IntToLong { get; set; } = 42;
    public float FloatToDouble { get; set; } = 3.14f;
    public byte ByteToInt { get; set; } = 255;
    public string StringToChar { get; set; } = "X";
    public int IntToNullable { get; set; } = 100;
    public double DoubleToDecimal { get; set; } = 99.99;
}

public class TypeConversionDestination
{
    public long LongValue { get; set; }
    public double DoubleValue { get; set; }
    public int IntValue { get; set; }
    public char CharValue { get; set; }
    public int? NullableValue { get; set; }
    public decimal DecimalValue { get; set; }
}

// Test models for collections of different types
public class CollectionsSource
{
    public List<int> IntList { get; set; } = new() { 1, 2, 3, 4, 5 };
    public int[] IntArray { get; set; } = { 10, 20, 30 };
    public HashSet<string> StringSet { get; set; } = new() { "A", "B", "C" };
    public Dictionary<string, int> StringIntDict { get; set; } = new() { ["one"] = 1, ["two"] = 2 };
    public List<Point> StructList { get; set; } = new() { new Point { X = 1, Y = 2 }, new Point { X = 3, Y = 4 } };
    public decimal?[] NullableArray { get; set; } = { 1.1m, null, 3.3m };
}

public class CollectionsDestination
{
    public List<string> IntStringList { get; set; } = new();
    public string[] IntStringArray { get; set; } = Array.Empty<string>();
    public HashSet<string> UpperStringSet { get; set; } = new();
    public Dictionary<string, string> StringIntStringDict { get; set; } = new();
    public List<PointDto> StructDtoList { get; set; } = new();
    public string[] NullableStringArray { get; set; } = Array.Empty<string>();
}

// Test models for basic mapping
public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
}

public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CreatedDate { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
}

// Test models for enum-to-enum mapping
public class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public Priority Priority { get; set; } = Priority.Medium;
    public DateTime CreatedAt { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string FormattedAmount { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string PriorityLevel { get; set; } = string.Empty;
    public string CreatedDate { get; set; } = string.Empty;
}

// Test models for enum collections
public class UserPreferences
{
    public int UserId { get; set; }
    public List<NotificationMethod> NotificationMethods { get; set; } = new();
    public UserRole PreferredRole { get; set; } = UserRole.User;
}

public class UserPreferencesDto
{
    public int UserId { get; set; }
    public List<string> NotificationTypes { get; set; } = new();
    public string PreferredRoleDescription { get; set; } = string.Empty;
}

// Test models for nested mapping
public class Address
{
    public int Id { get; set; }
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
}

public class AddressDto
{
    public int Id { get; set; }
    public string FullAddress { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}

public class UserWithAddresses
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<Address> Addresses { get; set; } = new();
}

public class UserWithAddressesDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<AddressDto> Addresses { get; set; } = new();
}

// Test models for inheritance scenarios
public class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FormattedPrice { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Age { get; set; } = string.Empty;
}

// Test mappers for all the new types
public class NumericTypesMapper : BaseMapper<NumericTypesSource, NumericTypesDestination>
{
    public override NumericTypesDestination Map(NumericTypesSource source)
    {
        return new NumericTypesDestination
        {
            SByteString = source.SByteValue.ToString(),
            ShortString = source.ShortValue.ToString(),
            IntString = source.IntValue.ToString(),
            LongString = source.LongValue.ToString(),
            
            ByteString = source.ByteValue.ToString(),
            UShortString = source.UShortValue.ToString(),
            UIntString = source.UIntValue.ToString(),
            ULongString = source.ULongValue.ToString(),
            
            FloatString = source.FloatValue.ToString("F5"),
            DoubleString = source.DoubleValue.ToString("F10"),
            DecimalString = source.DecimalValue.ToString("F6"),
            
            BoolString = source.BoolValue.ToString(),
            CharString = source.CharValue.ToString(),
            
            NullableIntString = source.NullableInt?.ToString() ?? "NULL",
            NullableDecimalString = source.NullableDecimal?.ToString() ?? "NULL",
            NullableBoolString = source.NullableBool?.ToString() ?? "NULL",
            NullableCharString = source.NullableChar?.ToString() ?? "NULL",
            
            DateTimeString = source.DateTimeValue.ToString("yyyy-MM-dd HH:mm:ss"),
            TimeSpanString = source.TimeSpanValue.ToString(@"hh\:mm\:ss"),
            GuidString = source.GuidValue.ToString("D"),
            
            PointDto = new PointDto 
            { 
                X = source.PointValue.X, 
                Y = source.PointValue.Y,
                Description = $"Point({source.PointValue.X}, {source.PointValue.Y})"
            },
            DimensionsDto = new DimensionsDto
            {
                FormattedSize = $"{source.DimensionsValue.Width}x{source.DimensionsValue.Height}x{source.DimensionsValue.Depth}",
                Volume = source.DimensionsValue.Width * source.DimensionsValue.Height * source.DimensionsValue.Depth
            }
        };
    }
}

public class TypeConversionMapper : BaseMapper<TypeConversionSource, TypeConversionDestination>
{
    public override TypeConversionDestination Map(TypeConversionSource source)
    {
        return new TypeConversionDestination
        {
            LongValue = source.IntToLong,
            DoubleValue = source.FloatToDouble,
            IntValue = source.ByteToInt,
            CharValue = string.IsNullOrEmpty(source.StringToChar) ? '\0' : source.StringToChar[0],
            NullableValue = source.IntToNullable,
            DecimalValue = (decimal)source.DoubleToDecimal
        };
    }
}

public class CollectionsMapper : BaseMapper<CollectionsSource, CollectionsDestination>
{
    public override CollectionsDestination Map(CollectionsSource source)
    {
        return new CollectionsDestination
        {
            IntStringList = source.IntList.Select(i => i.ToString()).ToList(),
            IntStringArray = source.IntArray.Select(i => i.ToString()).ToArray(),
            UpperStringSet = source.StringSet.Select(s => s.ToUpper()).ToHashSet(),
            StringIntStringDict = source.StringIntDict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString()),
            StructDtoList = source.StructList.Select(p => new PointDto 
            { 
                X = p.X, 
                Y = p.Y, 
                Description = $"Point({p.X}, {p.Y})" 
            }).ToList(),
            NullableStringArray = source.NullableArray.Select(d => d?.ToString() ?? "NULL").ToArray()
        };
    }
}

public class UserMapper : BaseMapper<User, UserDto>
{
    public override UserDto Map(User source)
    {
        return new UserDto
        {
            Id = source.Id,
            FullName = $"{source.FirstName} {source.LastName}",
            Email = source.Email,
            Status = source.IsActive ? "Active" : "Inactive",
            CreatedDate = source.CreatedAt.ToString("yyyy-MM-dd"),
            RoleName = source.Role.ToString()
        };
    }
}

public class OrderMapper : BaseMapper<Order, OrderDto>
{
    public override OrderDto Map(Order source)
    {
        return new OrderDto
        {
            Id = source.Id,
            CustomerName = source.CustomerName,
            FormattedAmount = $"${source.Amount:F2}",
            Status = source.Status, // Enum-to-enum mapping
            PriorityLevel = source.Priority switch
            {
                Priority.Low => "Low Priority",
                Priority.Medium => "Medium Priority", 
                Priority.High => "High Priority",
                Priority.Critical => "Critical Priority",
                _ => "Unknown Priority"
            },
            CreatedDate = source.CreatedAt.ToString("yyyy-MM-dd HH:mm")
        };
    }
}

public class UserPreferencesMapper : BaseMapper<UserPreferences, UserPreferencesDto>
{
    public override UserPreferencesDto Map(UserPreferences source)
    {
        return new UserPreferencesDto
        {
            UserId = source.UserId,
            NotificationTypes = source.NotificationMethods.Select(nm => nm.ToString()).ToList(),
            PreferredRoleDescription = source.PreferredRole switch
            {
                UserRole.Guest => "Guest User",
                UserRole.User => "Standard User",
                UserRole.Admin => "Administrator",
                UserRole.SuperAdmin => "Super Administrator",
                _ => "Unknown Role"
            }
        };
    }
}

public class AddressMapper : BaseMapper<Address, AddressDto>
{
    public override AddressDto Map(Address source)
    {
        return new AddressDto
        {
            Id = source.Id,
            FullAddress = $"{source.Street}, {source.City}, {source.PostalCode}",
            Location = $"{source.City}, {source.Country}"
        };
    }
}

public class UserWithAddressesMapper : BaseMapper<UserWithAddresses, UserWithAddressesDto>
{
    // Use a single stateless AddressMapper instance for nested mapping to avoid constructor injection
    private static readonly AddressMapper _addressMapper = new();

    // Parameterless constructor keeps mapper free of DI dependencies (pure mapping function)

    public override UserWithAddressesDto Map(UserWithAddresses source)
    {
        return new UserWithAddressesDto
        {
            Id = source.Id,
            FullName = $"{source.FirstName} {source.LastName}",
            Email = source.Email,
            Addresses = source.Addresses.Select(addr => _addressMapper.Map(addr)).ToList()
        };
    }
}

public class ProductMapper : BaseMapper<Product, ProductDto>
{
    public override ProductDto Map(Product source)
    {
        return new ProductDto
        {
            Id = source.Id,
            Name = source.Name,
            FormattedPrice = $"${source.Price:F2}",
            Category = source.Category,
            Age = $"{(DateTime.Now - source.CreatedAt).Days} days old"
        };
    }
}

// Mapper with dependency for testing DI scenarios - moved to separate namespace to avoid auto-discovery

// Test service interface for dependency injection testing
public interface ITestService
{
    string GetStatus(bool isActive);
}

public class TestService : ITestService
{
    public string GetStatus(bool isActive) => isActive ? "Online" : "Offline";
}

// Models for error testing
public class InvalidSource
{
    public string Value { get; set; } = string.Empty;
}

public class InvalidDestination
{
    public string Value { get; set; } = string.Empty;
}

// Test models for circular reference scenarios
public class CircularSource
{
    public int Id { get; set; }
    public CircularSource? Child { get; set; }
}

public class CircularDestination
{
    public int Id { get; set; }
    public CircularDestination? Child { get; set; }
}

public class CircularReferenceMapper : BaseMapper<CircularSource, CircularDestination>
{
    // Thread-safe mapping by using a per-call visited set instead of a shared HashSet
    public override CircularDestination Map(CircularSource source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        return MapInternal(source, new HashSet<CircularSource>());
    }

    private CircularDestination MapInternal(CircularSource source, HashSet<CircularSource> visited)
    {
        if (visited.Contains(source))
        {
            return new CircularDestination { Id = source.Id, Child = null };
        }

        visited.Add(source);
        try
        {
            return new CircularDestination
            {
                Id = source.Id,
                Child = source.Child != null ? MapInternal(source.Child, visited) : null
            };
        }
        finally
        {
            visited.Remove(source);
        }
    }
}
}

namespace SimpleMapper.Tests.TestModels.NonDiscoverable
{
    // Test types for error testing that don't conflict with main mappers
    public class ErrorTestSource
    {
        public int Id { get; set; }
        public string Data { get; set; } = string.Empty;
    }

    public class ErrorTestDestination
    {
        public int Id { get; set; }
        public string ProcessedData { get; set; } = string.Empty;
    }

    // Mapper that throws exception for testing error handling - not auto-discoverable
    public class ThrowingMapperForTesting : IMapper<ErrorTestSource, ErrorTestDestination>
    {
        public ErrorTestDestination Map(ErrorTestSource source)
        {
            throw new InvalidOperationException("Test exception");
        }
    }
} 
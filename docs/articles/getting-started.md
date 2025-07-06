# Getting Started

This guide will walk you through the basics of using SimpleMapper in your .NET applications.

## Installation

SimpleMapper is available as a NuGet package. Install it using one of the following methods:

### Package Manager Console
```powershell
Install-Package SimpleMapper
```

### .NET CLI
```bash
dotnet add package SimpleMapper
```

### PackageReference
```xml
<PackageReference Include="SimpleMapper" Version="1.1.0" />
```

## Basic Setup

### 1. Register SimpleMapper in Dependency Injection

In your `Program.cs` (or `Startup.cs` for older projects):

```csharp
using SimpleMapper;

var builder = WebApplication.CreateBuilder(args);

// Register SimpleMapper - this automatically discovers all mappers in your assembly
builder.Services.AddSimpleMapper();

var app = builder.Build();
```

### 2. Create Your First Mapper

Create classes that inherit from `BaseMapper<TSource, TDestination>`:

```csharp
// Your entity (from database, API, etc.)
public class UserEntity
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Your DTO (for API responses, etc.)
public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string FormattedCreatedDate { get; set; } = string.Empty;
}

// Your mapper
public class UserMapper : BaseMapper<UserEntity, UserDto>
{
    public override UserDto Map(UserEntity source)
    {
        return new UserDto
        {
            Id = source.Id,
            FullName = $"{source.FirstName} {source.LastName}",
            Email = source.Email,
            Status = source.IsActive ? "Active" : "Inactive",
            FormattedCreatedDate = source.CreatedAt.ToString("yyyy-MM-dd")
        };
    }
}
```

### 3. Use the Mapper in Your Services

Inject `IMapper` into your services and use it:

```csharp
public class UserService
{
    private readonly IMapper _mapper;
    private readonly IUserRepository _userRepository;
    
    public UserService(IMapper mapper, IUserRepository userRepository)
    {
        _mapper = mapper;
        _userRepository = userRepository;
    }
    
    public async Task<UserDto> GetUserAsync(int id)
    {
        var entity = await _userRepository.GetByIdAsync(id);
        return _mapper.Map<UserDto>(entity); // Type inference!
    }
    
    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var entities = await _userRepository.GetAllAsync();
        return _mapper.Map<UserDto>(entities).ToList(); // Collection mapping!
    }
}
```

## Key Concepts

### Type Inference

SimpleMapper supports type inference, making your code cleaner:

```csharp
// Instead of specifying both types:
var dto = _mapper.Map<UserEntity, UserDto>(entity);

// You can just specify the destination type:
var dto = _mapper.Map<UserDto>(entity);
```

### Collection Mapping

Mapping collections is straightforward:

```csharp
// Map a single object
var userDto = _mapper.Map<UserDto>(userEntity);

// Map a collection
var userDtos = _mapper.Map<UserDto>(userEntities).ToList();
```

### Automatic Discovery

SimpleMapper automatically finds and registers all your mappers:

- Scans the calling assembly for classes implementing `IMapper<TSource, TDestination>`
- Registers them in the DI container
- Validates that there are no duplicate mappers for the same type pair

## Common Patterns

### 1. Simple Property Mapping

```csharp
public class ProductMapper : BaseMapper<Product, ProductDto>
{
    public override ProductDto Map(Product source)
    {
        return new ProductDto
        {
            Id = source.Id,
            Name = source.Name,
            Price = source.Price,
            IsAvailable = source.Stock > 0
        };
    }
}
```

### 2. Calculated Properties

```csharp
public class OrderMapper : BaseMapper<Order, OrderDto>
{
    public override OrderDto Map(Order source)
    {
        return new OrderDto
        {
            Id = source.Id,
            CustomerName = source.Customer.Name,
            Total = source.Items.Sum(i => i.Price * i.Quantity),
            ItemCount = source.Items.Count,
            Status = GetStatusText(source.Status)
        };
    }
    
    private static string GetStatusText(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Pending => "Pending",
            OrderStatus.Processing => "Processing",
            OrderStatus.Shipped => "Shipped",
            OrderStatus.Delivered => "Delivered",
            _ => "Unknown"
        };
    }
}
```

### 3. Nested Object Mapping

For complex objects with nested properties:

```csharp
public class UserWithAddressMapper : BaseMapper<UserEntity, UserWithAddressDto>
{
    private readonly IMapper _mapper;
    
    public UserWithAddressMapper(IMapper mapper)
    {
        _mapper = mapper;
    }
    
    public override UserWithAddressDto Map(UserEntity source)
    {
        return new UserWithAddressDto
        {
            Id = source.Id,
            Name = $"{source.FirstName} {source.LastName}",
            Email = source.Email,
            // Map nested objects using the injected mapper
            Addresses = _mapper.Map<AddressDto>(source.Addresses).ToList()
        };
    }
}
```

## Important Rules

### ❌ Don't Do This

```csharp
// DON'T inject services into mappers
public class BadUserMapper : BaseMapper<UserEntity, UserDto>
{
    private readonly IEmailService _emailService; // ❌ No!
    
    public BadUserMapper(IEmailService emailService) // ❌ Won't compile!
    {
        _emailService = emailService;
    }
}

// DON'T use async operations in mappers
public class BadAsyncMapper : BaseMapper<UserEntity, UserDto>
{
    public override async Task<UserDto> Map(UserEntity source) // ❌ No async!
    {
        await SomeAsyncOperation(); // ❌ No I/O operations!
        return new UserDto();
    }
}
```

### ✅ Do This Instead

```csharp
// Keep mappers pure - no dependencies, no I/O
public class GoodUserMapper : BaseMapper<UserEntity, UserDto>
{
    public override UserDto Map(UserEntity source)
    {
        return new UserDto
        {
            Id = source.Id,
            Name = $"{source.FirstName} {source.LastName}"
        };
    }
}

// Handle async operations in your service layer
public class UserService
{
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;
    
    public UserService(IMapper mapper, IEmailService emailService)
    {
        _mapper = mapper;
        _emailService = emailService;
    }
    
    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        // 1. Create entity
        var entity = new UserEntity { /* ... */ };
        
        // 2. Save to database
        await _userRepository.SaveAsync(entity);
        
        // 3. Send email
        await _emailService.SendWelcomeEmailAsync(entity.Email);
        
        // 4. Map to DTO (fast, synchronous)
        return _mapper.Map<UserDto>(entity);
    }
}
```

## Next Steps

- Learn about [Advanced Usage](advanced-usage.md) for complex scenarios
- Read the [Best Practices](best-practices.md) guide for performance tips
- Check out the [Testing Guide](testing.md) for unit testing strategies
- See the [API Reference](../api/SimpleMapper.html) for complete documentation 
# SimpleMapper

A high-performance, reflection-free object mapper library for .NET with automatic registration support. Perfect for clean architecture applications that need fast, type-safe object mapping.

## Features

✅ **Zero Reflection** - All mappings are compile-time safe and blazingly fast  
✅ **Type Inference** - Clean syntax with automatic source type detection  
✅ **Automatic Discovery** - MediatR-style automatic mapper registration  
✅ **Easy Mocking** - Interface-based design perfect for unit testing  
✅ **Nested Mapping** - Support for complex object graphs with collections  
✅ **DI Integration** - First-class dependency injection support  
✅ **Minimal Dependencies** - Only depends on Microsoft.Extensions.DependencyInjection.Abstractions  

## Installation

```bash
dotnet add package SimpleMapper
```

## Quick Start

### 1. Register the Mapper (Program.cs)

```csharp
// Option 1: Automatic discovery from current assembly
builder.Services.AddSimpleMapperWithAutoDiscovery();

// Option 2: Automatic discovery from specific assembly
builder.Services.AddSimpleMapperWithAutoDiscovery<UserEntity>();

// Option 3: Manual registration (with automatic mapper inference)
builder.Services.AddSimpleMapper()
               .AddMapper<UserEntity, UserDto>();
```

### 2. Create Your Models

```csharp
public class UserEntity
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public bool IsActive { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Status { get; set; }
}
```

### 3. Create Your Mapper

```csharp
public class UserMapper : BaseMapper<UserEntity, UserDto>
{
    public override UserDto Map(UserEntity source)
    {
        return new UserDto
        {
            Id = source.Id,
            FullName = $"{source.FirstName} {source.LastName}",
            Email = source.Email,
            Status = source.IsActive ? "Active" : "Inactive"
        };
    }
}
```

### 4. Use in Your Services

```csharp
public class UserService
{
    private readonly IMapper _mapper;

    public UserService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public UserDto GetUser(UserEntity entity)
    {
        // Traditional explicit typing
        return _mapper.Map<UserEntity, UserDto>(entity);
        
        // Or with type inference (cleaner!)
        return _mapper.Map<UserDto>(entity);
    }

    public IEnumerable<UserDto> GetUsers(IEnumerable<UserEntity> entities)
    {
        // Traditional explicit typing
        return _mapper.Map<UserEntity, UserDto>(entities);
        
        // Or with type inference (cleaner!)
        return _mapper.Map<UserDto>(entities);
    }
}
```

## Type Inference (New!)

SimpleMapper now supports type inference, allowing you to write cleaner code by only specifying the destination type:

### Before (Explicit Types)
```csharp
var userDto = _mapper.Map<UserEntity, UserDto>(user);
var addressDtos = _mapper.Map<Address, AddressDto>(user.Addresses);
```

### After (Type Inference)
```csharp
var userDto = _mapper.Map<UserDto>(user);
var addressDtos = _mapper.Map<AddressDto>(user.Addresses);
```

### Benefits
- **Cleaner code**: Less generic noise, more readable
- **Perfect for LINQ**: Works great in mapping expressions
- **Collections**: Automatically infers element types from collections
- **Same performance**: Zero overhead - same compiled code

### Usage Examples
```csharp
// Single object mapping
var dto = _mapper.Map<UserDto>(entity);

// Collection mapping  
var dtos = _mapper.Map<AddressDto>(addresses).ToList();

// In complex mapping scenarios
var complexDto = new UserWithAddressesDto
{
    User = _mapper.Map<UserDto>(user),
    Addresses = _mapper.Map<AddressDto>(user.Addresses).ToList()
};

// Works with any IEnumerable
var results = users.Select(u => _mapper.Map<UserDto>(u));
```

Note: For empty non-generic collections (like `ArrayList`), explicit typing is still required as the source type cannot be inferred.

## Mapper Registration & Validation

SimpleMapper enforces **one mapper per source/destination pair** to prevent ambiguity and ensure predictable behavior.

### Automatic Mapper Inference

You can register mappers without specifying the implementation class:

```csharp
// Clean, inferred registration
services.AddMapper<User, UserDto>();  // Automatically finds UserMapper

// Instead of verbose explicit registration  
services.AddMapper<User, UserDto, UserMapper>();  // Old approach
```

### Duplicate Detection

SimpleMapper validates that only one mapper exists per type pair:

```csharp
// ✅ This works
services.AddMapper<User, UserDto>();

// ❌ This throws InvalidOperationException
services.AddMapper<User, UserDto>();  // "A mapper for User -> UserDto is already registered"

// ❌ Multiple implementations also throw
public class UserMapper : BaseMapper<User, UserDto> { ... }
public class AnotherUserMapper : BaseMapper<User, UserDto> { ... }  // Not allowed!
```

### Benefits

- **Cleaner API** - Less verbose registration
- **Automatic validation** - Prevents duplicate mappers
- **Compile-time safety** - Ensures mapper exists at registration time
- **Better error messages** - Clear guidance when mappers are missing or duplicated

## Preventing Common Mistakes

### ❌ DO NOT Inject ANY Services into Mappers

```csharp
// ❌ WRONG - Mappers should not have ANY dependencies
public class BadUserMapper : BaseMapper<User, UserDto>
{
    private readonly IAsyncService _asyncService; // ❌ No services!
    private readonly ILogger _logger; // ❌ No services!
    private readonly IConfiguration _config; // ❌ No services!
    
    public BadUserMapper(IAsyncService asyncService, ILogger logger) // ❌ No constructor params!
    {
        _asyncService = asyncService;
        _logger = logger;
    }
    
    public override UserDto Map(User source)
    {
        // ❌ Blocking async call - can deadlock!
        var data = _asyncService.GetDataAsync(source.Id).Result;
        _logger.LogInformation("Mapping user"); // ❌ Side effects in mapping!
        return new UserDto { Name = data };
    }
}
```

### ✅ CORRECT - Pure Mappers with No Dependencies

```csharp
// ✅ CORRECT - No constructor parameters, no dependencies
public class GoodUserMapper : BaseMapper<User, UserDto>
{
    // ✅ Parameterless constructor (enforced by BaseMapper)
    public override UserDto Map(User source)
    {
        return new UserDto
        {
            Id = source.Id,
            FullName = $"{source.FirstName} {source.LastName}",
            Email = source.Email,
            Status = source.IsActive ? "Active" : "Inactive"
        };
    }
}
```

### ✅ Handle Complex Logic in Service Layer

```csharp
// ✅ CORRECT - All dependencies and async logic in service layer
public class UserService
{
    private readonly IUserRepository _repo;
    private readonly IProfileService _profileService;
    private readonly ILogger<UserService> _logger;
    private readonly IMapper _mapper;
    
    public UserService(IUserRepository repo, IProfileService profileService, 
                      ILogger<UserService> logger, IMapper mapper)
    {
        _repo = repo;
        _profileService = profileService;
        _logger = logger;
        _mapper = mapper;
    }
    
    public async Task<UserWithProfileDto> GetUserWithProfileAsync(int id)
    {
        _logger.LogInformation("Getting user {UserId}", id);
        
        // Step 1: Async I/O operations
        var user = await _repo.GetAsync(id);
        var profile = await _profileService.GetProfileAsync(id);
        
        // Step 2: Pure, fast mapping (no side effects)
        var userDto = _mapper.Map<UserDto>(user);
        
        // Step 3: Compose result with additional data
        return new UserWithProfileDto
        {
            User = userDto,
            ProfileData = profile.Data,
            LastUpdated = profile.LastUpdated,
            LoadedAt = DateTime.UtcNow // Service-level concerns
        };
    }
}
```

### Compile-Time Protection

SimpleMapper's `BaseMapper<TSource, TDestination>` enforces a parameterless constructor:

```csharp
// ❌ This will cause a compile error:
public class BadMapper : BaseMapper<User, UserDto>
{
    // ❌ Compiler error - BaseMapper doesn't have a constructor that takes parameters
    public BadMapper(IService service) : base(service) // Won't compile!
    {
    }
}

// ✅ This compiles correctly:
public class GoodMapper : BaseMapper<User, UserDto>
{
    // ✅ Parameterless constructor (can be implicit)
    public override UserDto Map(User source) => new UserDto { Id = source.Id };
}
```

### Static Analysis (Optional)

For additional protection, SimpleMapper includes Roslyn analyzers that detect anti-patterns:

- **SM001**: Constructor parameters in mapper (any parameters)
- **SM002**: Async method in mapper  
- **SM003**: Blocking async call in mapper (`.Result`, `.Wait()`)

These analyzers provide compile-time warnings and errors to catch mistakes early:

```csharp
// ❌ SM001 Error: "Mapper 'BadMapper' should not have constructor parameters"
public class BadMapper : BaseMapper<User, UserDto>
{
    public BadMapper(IService service) { } // ❌ Analyzer catches this
}

// ❌ SM002 Error: "Mapper 'AsyncMapper' contains async method"  
public class AsyncMapper : BaseMapper<User, UserDto>
{
    public async Task<UserDto> MapAsync(User source) { } // ❌ Analyzer catches this
}

// ❌ SM003 Error: "Blocking async call in mapper"
public class BlockingMapper : BaseMapper<User, UserDto>
{
    public override UserDto Map(User source)
    {
        var result = SomeAsyncMethod().Result; // ❌ Analyzer catches this
        return new UserDto();
    }
}
```

### Why This Approach Works

1. **Compile-time enforcement** - You literally cannot inject services
2. **Clear separation** - Mappers are pure, services handle complexity  
3. **No temptation** - Developers can't accidentally inject async services
4. **Simple mental model** - Mappers = data transformation only
5. **Performance** - Pure functions are fast and predictable

## Advanced Usage

### Handling Async Scenarios

SimpleMapper is intentionally **synchronous** for performance and simplicity. Here are recommended patterns for common async scenarios:

#### Loading Related Data
```csharp
// ✅ Load data first, then map
public async Task<UserDto> GetUserWithOrdersAsync(int userId)
{
    var user = await _userRepo.GetAsync(userId);
    var orders = await _orderRepo.GetByUserIdAsync(userId);
    
    var userDto = _mapper.Map<UserDto>(user);
    userDto.Orders = _mapper.Map<OrderDto>(orders).ToList();
    return userDto;
}
```

#### External API Enrichment
```csharp
// ✅ Map first, enrich second
public async Task<UserDto> GetEnrichedUserAsync(int userId)
{
    var user = await _userRepo.GetAsync(userId);
    var userDto = _mapper.Map<UserDto>(user);
    
    userDto.ExternalData = await _externalApi.GetDataAsync(userDto.Id);
    return userDto;
}
```

#### EF Core Projections (Recommended for Performance)
```csharp
// ✅ Direct projections - fastest approach
return await _context.Users
    .Where(u => u.IsActive)
    .Select(u => new UserDto
    {
        Id = u.Id,
        FullName = u.FirstName + " " + u.LastName,
        Email = u.Email,
        OrderCount = u.Orders.Count(),  // Calculated in SQL
        LastOrderDate = u.Orders.Max(o => o.CreatedDate)
    })
    .ToListAsync();

// ✅ Hybrid approach - project then map
var lightweightData = await _context.Users
    .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email })
    .ToListAsync();
    
return lightweightData.Select(d => _mapper.Map<UserDto>(d)).ToList();
```

**Why we don't support automatic EF projections:**
- **Complexity** - Expression tree generation is extremely complex
- **Performance** - Manual projections are faster and more predictable  
- **Debugging** - Explicit projections are easier to debug and optimize
- **Dependencies** - Keeps SimpleMapper lightweight and ORM-agnostic

### Nested Object Mapping

For complex objects with nested mappings, inject `IMapper`:

```csharp
public class UserWithAddressesMapper : BaseMapper<UserEntity, UserDto>
{
    private readonly IMapper _mapper;

    public UserWithAddressesMapper(IMapper mapper)
    {
        _mapper = mapper;
    }

    public override UserDto Map(UserEntity source)
    {
        return new UserDto
        {
            Id = source.Id,
            FullName = $"{source.FirstName} {source.LastName}",
            Email = source.Email,
            // Map nested collections
            Addresses = _mapper.Map<Address, AddressDto>(source.Addresses).ToList()
        };
    }
}
```

### Feature Folder Organization

Perfect for vertical slice architecture:

```
MyApp/
├── Features/
│   ├── Users/
│   │   ├── UserEntity.cs
│   │   ├── UserDto.cs
│   │   ├── UserMapper.cs      ← Auto-discovered!
│   │   └── UserService.cs
│   └── Products/
│       ├── ProductEntity.cs
│       ├── ProductDto.cs
│       ├── ProductMapper.cs   ← Auto-discovered!
│       └── ProductService.cs
```

## Testing

SimpleMapper is designed for easy testing with full mocking support:

```csharp
[Test]
public void Should_Map_User_Correctly()
{
    // Arrange
    var mockMapper = new Mock<IMapper>();
    mockMapper.Setup(x => x.Map<UserEntity, UserDto>(It.IsAny<UserEntity>()))
                    .Returns((UserEntity user) => new UserDto { Id = user.Id });

    var service = new UserService(mockMapper.Object);
    var entity = new UserEntity { Id = 1, FirstName = "John" };

    // Act
    var result = service.GetUser(entity);

    // Assert
    Assert.AreEqual(1, result.Id);
}
```

## Registration Options

| Method | Description | Use Case |
|--------|-------------|----------|
| `AddSimpleMapper()` | Basic registration | Manual mapper registration |
| `AddMapper<TSource, TDestination>()` | Register single mapper (inferred) | Selective registration with validation |
| `AddSimpleMapperWithAutoDiscovery()` | Auto-discovery from calling assembly | Single-assembly applications |
| `AddSimpleMapperWithAutoDiscovery<T>()` | Auto-discovery from specified assembly | Multi-assembly applications |
| `AddMappersFromAssembly(assembly)` | Scan specific assembly | Granular control |
| `AddMappersFromAssemblies(assemblies)` | Scan multiple assemblies | Complex applications |

## Performance

- **Zero reflection at runtime** - All mapping is done through compiled code
- **Singleton registration** - Mappers are registered once and reused
- **Thread-safe** - Safe for concurrent use
- **Minimal allocations** - Efficient memory usage

## License

MIT License - see LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. 
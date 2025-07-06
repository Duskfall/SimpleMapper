# Migration Guide

This guide helps you migrate from other mapping libraries to SimpleMapper.

## From AutoMapper

### Key Differences

| AutoMapper | SimpleMapper |
|------------|--------------|
| Configuration-based | Code-based |
| Reflection at runtime | Compile-time safe |
| Global configuration | Explicit mappers |
| Automatic mapping | Manual property mapping |

### Migration Steps

#### 1. Replace Configuration with Mappers

**AutoMapper:**
```csharp
// Configuration
CreateMap<User, UserDto>()
    .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));
```

**SimpleMapper:**
```csharp
// Explicit mapper class
public class UserMapper : BaseMapper<User, UserDto>
{
    public override UserDto Map(User source)
    {
        return new UserDto
        {
            Id = source.Id,
            FullName = $"{source.FirstName} {source.LastName}",
            Email = source.Email
        };
    }
}
```

#### 2. Replace Service Registration

**AutoMapper:**
```csharp
services.AddAutoMapper(typeof(Program));
```

**SimpleMapper:**
```csharp
services.AddSimpleMapper();
```

#### 3. Update Usage

**AutoMapper:**
```csharp
public class UserService
{
    private readonly IMapper _mapper;
    
    public UserDto GetUser(User user)
    {
        return _mapper.Map<UserDto>(user);
    }
}
```

**SimpleMapper:**
```csharp
public class UserService
{
    private readonly IMapper _mapper;
    
    public UserDto GetUser(User user)
    {
        return _mapper.Map<UserDto>(user); // Same interface!
    }
}
```

### Benefits After Migration

- **Faster startup** - No reflection scanning
- **Better performance** - Direct method calls instead of reflection
- **Compile-time safety** - Errors caught at build time
- **Easier debugging** - Step through your mapping code
- **No configuration bugs** - All mapping logic is explicit

## From Mapster

### Key Differences

| Mapster | SimpleMapper |
|---------|--------------|
| Convention-based | Explicit mapping |
| Source generation | Manual implementation |
| Automatic mapping | Full control |

### Migration Steps

#### 1. Replace Adapt with Explicit Mappers

**Mapster:**
```csharp
var dto = user.Adapt<UserDto>();
```

**SimpleMapper:**
```csharp
public class UserMapper : BaseMapper<User, UserDto>
{
    public override UserDto Map(User source)
    {
        return new UserDto
        {
            Id = source.Id,
            Name = source.Name,
            Email = source.Email
        };
    }
}

// Usage:
var dto = _mapper.Map<UserDto>(user);
```

#### 2. Replace Global Configuration

**Mapster:**
```csharp
TypeAdapterConfig<User, UserDto>
    .NewConfig()
    .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}");
```

**SimpleMapper:**
```csharp
public class UserMapper : BaseMapper<User, UserDto>
{
    public override UserDto Map(User source)
    {
        return new UserDto
        {
            FullName = $"{source.FirstName} {source.LastName}"
        };
    }
}
```

## Common Migration Patterns

### Complex Mapping Logic

**Before (any library):**
```csharp
// Hidden in configuration
```

**After (SimpleMapper):**
```csharp
public class OrderMapper : BaseMapper<Order, OrderDto>
{
    public override OrderDto Map(Order source)
    {
        return new OrderDto
        {
            Id = source.Id,
            Total = source.Items.Sum(i => i.Price * i.Quantity),
            Status = GetStatusText(source.Status),
            CustomerName = source.Customer?.Name ?? "Unknown"
        };
    }
    
    private static string GetStatusText(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Pending => "Pending",
            OrderStatus.Shipped => "Shipped",
            _ => "Unknown"
        };
    }
}
```

### Nested Object Mapping

**Before:**
```csharp
// Usually automatic or configured
```

**After:**
```csharp
public class UserWithOrdersMapper : BaseMapper<User, UserWithOrdersDto>
{
    private readonly IMapper _mapper;
    
    public UserWithOrdersMapper(IMapper mapper)
    {
        _mapper = mapper;
    }
    
    public override UserWithOrdersDto Map(User source)
    {
        return new UserWithOrdersDto
        {
            Id = source.Id,
            Name = source.Name,
            Orders = _mapper.Map<OrderDto>(source.Orders).ToList()
        };
    }
}
```

## Performance Comparison

| Library | Startup Time | Mapping Speed | Memory Usage |
|---------|-------------|---------------|--------------|
| AutoMapper | Slow (reflection) | Medium | High |
| Mapster | Medium (source gen) | Fast | Medium |
| **SimpleMapper** | **Instant** | **Fastest** | **Lowest** |

## Migration Checklist

- [ ] Replace configuration classes with mapper classes
- [ ] Update service registration
- [ ] Test all mapping scenarios
- [ ] Update unit tests (much easier with SimpleMapper!)
- [ ] Remove old mapping library dependencies
- [ ] Update documentation

## Need Help?

If you encounter issues during migration:

1. Check the [Getting Started](getting-started.md) guide
2. Review [Best Practices](best-practices.md)
3. Look at the [Examples](https://github.com/yourusername/SimpleMapper/tree/main/SimpleMapper)
4. [Open an issue](https://github.com/yourusername/SimpleMapper/issues) on GitHub 
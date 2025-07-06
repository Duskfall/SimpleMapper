# SimpleMapper Documentation

Welcome to the official documentation for **SimpleMapper** - a high-performance, reflection-free object mapper library for .NET.

## What is SimpleMapper?

SimpleMapper is a lightweight, fast, and easy-to-use object mapping library that provides:

- **Zero reflection at runtime** - All mapping is compile-time safe
- **Thread-safe concurrent operations** - Built for high-performance applications
- **Automatic mapper discovery** - Just call `AddSimpleMapper()` and you're done
- **Type inference support** - Clean, intuitive API
- **Easy testing** - Interface-based design for simple mocking
- **Compile-time rule enforcement** - Optional analyzer prevents anti-patterns
- **No dependencies** - Only uses Microsoft.Extensions.DependencyInjection.Abstractions

## Quick Start

### Installation

```bash
dotnet add package SimpleMapper
```

### Basic Usage

```csharp
// 1. Define your mapper
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

// 2. Register in DI (discovers all mappers automatically)
builder.Services.AddSimpleMapper();

// 3. Use in your services
public class UserService
{
    private readonly IMapper _mapper;
    
    public UserService(IMapper mapper)
    {
        _mapper = mapper;
    }
    
    public UserDto GetUser(UserEntity entity)
    {
        return _mapper.Map<UserEntity, UserDto>(entity);
    }
}
```

## Key Features

### Type Inference
```csharp
// Instead of this:
var dto = _mapper.Map<UserEntity, UserDto>(entity);

// You can write this:
var dto = _mapper.Map<UserDto>(entity);
```

### Collection Mapping
```csharp
var dtos = _mapper.Map<UserDto>(entities).ToList();
```

### Nested Object Mapping
```csharp
public class UserMapper : BaseMapper<UserEntity, UserDto>
{
    private readonly IMapper _mapper;
    
    public UserMapper(IMapper mapper)
    {
        _mapper = mapper;
    }
    
    public override UserDto Map(UserEntity source)
    {
        return new UserDto
        {
            Id = source.Id,
            Name = source.Name,
            Addresses = _mapper.Map<AddressDto>(source.Addresses).ToList()
        };
    }
}
```

## Documentation Sections

- **[Getting Started](articles/getting-started.md)** - Complete setup and basic usage guide
- **[Advanced Usage](articles/advanced-usage.md)** - Complex scenarios and patterns
- **[Best Practices](articles/best-practices.md)** - Performance tips and recommended patterns
- **[Migration Guide](articles/migration.md)** - Migrating from other mapping libraries
- **[Analyzer Setup](articles/analyzer-setup.md)** - Compile-time rule enforcement
- **[API Reference](api/SimpleMapper.html)** - Complete API documentation

## Why Choose SimpleMapper?

| Feature | SimpleMapper | AutoMapper | Mapster |
|---------|--------------|------------|---------|
| **Runtime Performance** | ⚡ Fastest | 🐌 Slower | ⚡ Fast |
| **Startup Performance** | ⚡ Instant | 🐌 Reflection scan | ⚡ Fast |
| **Compile-time Safety** | ✅ Full | ❌ Runtime errors | ✅ Good |
| **Easy Testing** | ✅ No mocking needed | ❌ Complex setup | ✅ Good |
| **Dependencies** | ✅ Minimal | ❌ Heavy | ✅ Minimal |
| **Learning Curve** | ✅ Simple | ❌ Complex config | ✅ Simple |

## Community

- **GitHub**: [https://github.com/yourusername/SimpleMapper](https://github.com/yourusername/SimpleMapper)
- **Issues**: [Report bugs or request features](https://github.com/yourusername/SimpleMapper/issues)
- **NuGet**: [SimpleMapper package](https://www.nuget.org/packages/SimpleMapper)

## License

SimpleMapper is released under the [MIT License](https://github.com/yourusername/SimpleMapper/blob/main/LICENSE). 
# Advanced Usage

This guide covers advanced scenarios and patterns for using SimpleMapper in complex applications.

## Assembly-Specific Registration

When working with multiple assemblies, you may need more control over mapper discovery:

```csharp
// Register mappers from specific assembly
builder.Services.AddSimpleMapper<SomeTypeInTargetAssembly>();

// Register mappers from multiple assemblies
builder.Services.AddSimpleMapper(
    typeof(UserMapper).Assembly,
    typeof(ProductMapper).Assembly,
    typeof(OrderMapper).Assembly
);
```

## Type Inference Patterns

SimpleMapper supports advanced type inference scenarios:

```csharp
public class UserService
{
    private readonly IMapper _mapper;
    
    public UserService(IMapper mapper)
    {
        _mapper = mapper;
    }
    
    // Type inference with single objects
    public UserDto GetUser(UserEntity entity)
    {
        return _mapper.Map<UserDto>(entity); // Source type inferred
    }
    
    // Type inference with collections
    public List<UserDto> GetUsers(IEnumerable<UserEntity> entities)
    {
        return _mapper.Map<UserDto>(entities).ToList();
    }
    
    // Works with any IEnumerable
    public List<AddressDto> GetAddresses(HashSet<Address> addresses)
    {
        return _mapper.Map<AddressDto>(addresses).ToList();
    }
}
```

## Complex Nested Mapping

For deeply nested object hierarchies:

```csharp
public class OrderWithDetailsMapper : BaseMapper<OrderEntity, OrderDetailDto>
{
    private readonly IMapper _mapper;
    
    public OrderWithDetailsMapper(IMapper mapper)
    {
        _mapper = mapper;
    }
    
    public override OrderDetailDto Map(OrderEntity source)
    {
        return new OrderDetailDto
        {
            Id = source.Id,
            OrderNumber = source.OrderNumber,
            
            // Map customer
            Customer = _mapper.Map<CustomerDto>(source.Customer),
            
            // Map items with product details
            Items = _mapper.Map<OrderItemDetailDto>(source.Items).ToList(),
            
            // Map shipping address
            ShippingAddress = _mapper.Map<AddressDto>(source.ShippingAddress),
            
            // Calculated fields
            SubTotal = source.Items.Sum(i => i.Quantity * i.UnitPrice),
            TaxAmount = CalculateTax(source),
            Total = CalculateTotal(source)
        };
    }
    
    private static decimal CalculateTax(OrderEntity order)
    {
        // Complex tax calculation logic
        return order.Items.Sum(i => i.Quantity * i.UnitPrice) * 0.08m;
    }
    
    private static decimal CalculateTotal(OrderEntity order)
    {
        var subtotal = order.Items.Sum(i => i.Quantity * i.UnitPrice);
        return subtotal + (subtotal * 0.08m);
    }
}
```

## Conditional Mapping

Handle different mapping scenarios based on context:

```csharp
public class UserContextualMapper : BaseMapper<UserEntity, UserDto>
{
    public override UserDto Map(UserEntity source)
    {
        var dto = new UserDto
        {
            Id = source.Id,
            FullName = $"{source.FirstName} {source.LastName}",
            Email = source.Email,
            Status = source.IsActive ? "Active" : "Inactive"
        };
        
        // Conditional mapping based on user role
        if (source.Role == UserRole.Admin)
        {
            dto.AdminLevel = source.AdminLevel;
            dto.Permissions = string.Join(",", source.Permissions);
        }
        
        // Conditional sensitive data exposure
        if (source.IsPublicProfile)
        {
            dto.Phone = source.Phone;
            dto.Address = $"{source.Address?.City}, {source.Address?.Country}";
        }
        
        return dto;
    }
}
```

## Performance Optimization Techniques

### 1. Mapper Caching for Expensive Operations

```csharp
public class ProductWithCategoriesMapper : BaseMapper<Product, ProductWithCategoriesDto>
{
    // Static cache for category lookups
    private static readonly ConcurrentDictionary<int, string> CategoryCache = new();
    
    public override ProductWithCategoriesDto Map(Product source)
    {
        return new ProductWithCategoriesDto
        {
            Id = source.Id,
            Name = source.Name,
            Price = source.Price,
            CategoryPath = GetCategoryPath(source.CategoryId),
            FormattedPrice = FormatPrice(source.Price, source.Currency)
        };
    }
    
    private static string GetCategoryPath(int categoryId)
    {
        return CategoryCache.GetOrAdd(categoryId, id =>
        {
            // Expensive operation - build category hierarchy
            return BuildCategoryPath(id);
        });
    }
    
    private static string BuildCategoryPath(int categoryId)
    {
        // Simulate building category path
        return $"Category_{categoryId}";
    }
    
    private static string FormatPrice(decimal price, string currency)
    {
        return currency switch
        {
            "USD" => $"${price:F2}",
            "EUR" => $"€{price:F2}",
            "GBP" => $"£{price:F2}",
            _ => $"{price:F2} {currency}"
        };
    }
}
```

### 2. Batch Operations

```csharp
public class UserBatchService
{
    private readonly IMapper _mapper;
    private readonly IUserRepository _repository;
    
    public UserBatchService(IMapper mapper, IUserRepository repository)
    {
        _mapper = mapper;
        _repository = repository;
    }
    
    public async Task<List<UserDto>> GetUserBatchAsync(int[] userIds)
    {
        // Load all users in one query
        var users = await _repository.GetByIdsAsync(userIds);
        
        // Map all at once - more efficient than individual mapping
        return _mapper.Map<UserDto>(users).ToList();
    }
    
    public async Task<Dictionary<int, UserDto>> GetUserDictionaryAsync(int[] userIds)
    {
        var users = await _repository.GetByIdsAsync(userIds);
        var userDtos = _mapper.Map<UserDto>(users);
        
        return userDtos.ToDictionary(u => u.Id);
    }
}
```

## Integration with EF Core

Optimize database queries with projection:

```csharp
public class UserService
{
    private readonly DbContext _context;
    private readonly IMapper _mapper;
    
    public UserService(DbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }
    
    // Pattern 1: Database projection then mapping
    public async Task<List<UserSummaryDto>> GetUserSummariesAsync()
    {
        // Project to anonymous type first (database-level)
        var projections = await _context.Users
            .Where(u => u.IsActive)
            .Select(u => new UserProjection
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                OrderCount = u.Orders.Count(),
                TotalSpent = u.Orders.Sum(o => o.Total)
            })
            .ToListAsync();
        
        // Then use SimpleMapper for complex transformations
        return _mapper.Map<UserSummaryDto>(projections).ToList();
    }
    
    // Pattern 2: Load minimal data, then enrich
    public async Task<UserDetailDto> GetUserDetailAsync(int userId)
    {
        // Load basic user data
        var user = await _context.Users
            .Include(u => u.Addresses)
            .FirstOrDefaultAsync(u => u.Id == userId);
            
        if (user == null) return null;
        
        // Map to DTO
        var userDto = _mapper.Map<UserDetailDto>(user);
        
        // Enrich with additional data
        userDto.RecentOrders = await GetRecentOrdersAsync(userId);
        
        return userDto;
    }
    
    private async Task<List<OrderSummaryDto>> GetRecentOrdersAsync(int userId)
    {
        var orders = await _context.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .ToListAsync();
            
        return _mapper.Map<OrderSummaryDto>(orders).ToList();
    }
}

// Supporting projection class
public class UserProjection
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalSpent { get; set; }
}
```

## Error Handling Strategies

### Graceful Error Handling

```csharp
public class RobustUserMapper : BaseMapper<UserEntity, UserDto>
{
    public override UserDto Map(UserEntity source)
    {
        try
        {
            return new UserDto
            {
                Id = source.Id,
                FullName = GetSafeFullName(source),
                Email = GetSafeEmail(source),
                Status = GetSafeStatus(source),
                FormattedCreatedDate = GetSafeFormattedDate(source.CreatedAt)
            };
        }
        catch (Exception ex)
        {
            // Log error and return safe default
            // (In production, use proper logging framework)
            Console.WriteLine($"Error mapping user {source?.Id}: {ex.Message}");
            
            return new UserDto
            {
                Id = source?.Id ?? 0,
                FullName = "Error loading user",
                Email = "N/A",
                Status = "Unknown",
                FormattedCreatedDate = "N/A"
            };
        }
    }
    
    private static string GetSafeFullName(UserEntity user)
    {
        var firstName = user.FirstName?.Trim() ?? "";
        var lastName = user.LastName?.Trim() ?? "";
        
        if (string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(lastName))
            return "Unknown User";
            
        return $"{firstName} {lastName}".Trim();
    }
    
    private static string GetSafeEmail(UserEntity user)
    {
        return string.IsNullOrWhiteSpace(user.Email) ? "No email" : user.Email;
    }
    
    private static string GetSafeStatus(UserEntity user)
    {
        return user.IsActive ? "Active" : "Inactive";
    }
    
    private static string GetSafeFormattedDate(DateTime? date)
    {
        return date?.ToString("yyyy-MM-dd") ?? "No date";
    }
}
```

## Multi-Tenant Scenarios

Handle tenant-specific mapping logic:

```csharp
public class TenantAwareUserMapper : BaseMapper<UserEntity, UserDto>
{
    public override UserDto Map(UserEntity source)
    {
        var dto = new UserDto
        {
            Id = source.Id,
            FullName = $"{source.FirstName} {source.LastName}",
            Email = source.Email,
            Status = source.IsActive ? "Active" : "Inactive"
        };
        
        // Tenant-specific customizations
        dto.DisplayName = GetTenantSpecificDisplayName(source);
        dto.Permissions = GetTenantSpecificPermissions(source);
        
        return dto;
    }
    
    private static string GetTenantSpecificDisplayName(UserEntity user)
    {
        // Different tenants might have different display name formats
        return user.TenantId switch
        {
            1 => $"{user.FirstName} {user.LastName}", // Tenant 1: First Last
            2 => $"{user.LastName}, {user.FirstName}", // Tenant 2: Last, First
            3 => $"{user.FirstName} {user.LastName[0]}.", // Tenant 3: First L.
            _ => $"{user.FirstName} {user.LastName}"
        };
    }
    
    private static string GetTenantSpecificPermissions(UserEntity user)
    {
        // Tenant-specific permission formatting
        return user.TenantId switch
        {
            1 => string.Join(", ", user.Permissions),
            2 => string.Join(" | ", user.Permissions),
            _ => string.Join("; ", user.Permissions)
        };
    }
}
```

These advanced patterns allow you to handle complex real-world scenarios while maintaining the simplicity and performance benefits of SimpleMapper. 
# Best Practices

This guide covers recommended patterns and practices for using SimpleMapper effectively in production applications.

## Mapper Design Principles

### 1. Keep Mappers Pure

Mappers should be pure functions with no side effects:

```csharp
// ✅ GOOD: Pure mapping logic
public class ProductMapper : BaseMapper<Product, ProductDto>
{
    public override ProductDto Map(Product source)
    {
        return new ProductDto
        {
            Id = source.Id,
            Name = source.Name,
            Price = source.Price,
            Category = source.Category?.Name ?? "Uncategorized",
            IsAvailable = source.Stock > 0 && source.IsActive
        };
    }
}

// ❌ BAD: Side effects and dependencies
public class BadProductMapper : BaseMapper<Product, ProductDto>
{
    private readonly ILogger _logger; // ❌ No dependencies!
    
    public override ProductDto Map(Product source)
    {
        _logger.LogInformation("Mapping product {Id}", source.Id); // ❌ No logging!
        
        // ❌ No database calls!
        var category = _dbContext.Categories.Find(source.CategoryId);
        
        return new ProductDto { /* ... */ };
    }
}
```

### 2. Use Meaningful Mapper Names

```csharp
// ✅ GOOD: Clear, descriptive names
public class UserEntityToUserDtoMapper : BaseMapper<UserEntity, UserDto> { }
public class ProductToListItemMapper : BaseMapper<Product, ProductListItemDto> { }
public class OrderToInvoiceMapper : BaseMapper<Order, InvoiceDto> { }

// ❌ BAD: Generic or unclear names
public class UserMapper : BaseMapper<UserEntity, UserDto> { } // Which direction?
public class Mapper1 : BaseMapper<Product, ProductDto> { } // What does it do?
```

### 3. One Responsibility Per Mapper

Each mapper should handle one specific transformation:

```csharp
// ✅ GOOD: Specific, focused mappers
public class UserToSummaryMapper : BaseMapper<User, UserSummaryDto> { }
public class UserToDetailMapper : BaseMapper<User, UserDetailDto> { }
public class UserToApiResponseMapper : BaseMapper<User, UserApiDto> { }

// ❌ BAD: Mapper that tries to do everything
public class UniversalUserMapper : BaseMapper<User, object>
{
    public override object Map(User source)
    {
        // Too much branching logic - split into separate mappers
        if (Context.IsApiRequest)
            return new UserApiDto { /* ... */ };
        else if (Context.IsAdminRequest)
            return new UserAdminDto { /* ... */ };
        else
            return new UserDto { /* ... */ };
    }
}
```

## Performance Optimization

### 1. Minimize Object Allocation

```csharp
// ✅ GOOD: Efficient string handling
public class UserMapper : BaseMapper<UserEntity, UserDto>
{
    public override UserDto Map(UserEntity source)
    {
        return new UserDto
        {
            Id = source.Id,
            FullName = $"{source.FirstName} {source.LastName}", // Single allocation
            Email = source.Email,
            Status = source.IsActive ? "Active" : "Inactive" // Reuse constants if possible
        };
    }
}

// ❌ BAD: Unnecessary allocations
public class InefficiientUserMapper : BaseMapper<UserEntity, UserDto>
{
    public override UserDto Map(UserEntity source)
    {
        var fullName = source.FirstName + " " + source.LastName; // Extra allocation
        var status = new string(source.IsActive ? "Active".ToCharArray() : "Inactive".ToCharArray()); // Wasteful
        
        return new UserDto
        {
            Id = source.Id,
            FullName = fullName,
            Email = source.Email,
            Status = status
        };
    }
}
```

### 2. Use Static Helper Methods for Complex Logic

```csharp
public class OrderMapper : BaseMapper<Order, OrderDto>
{
    public override OrderDto Map(Order source)
    {
        return new OrderDto
        {
            Id = source.Id,
            Total = CalculateTotal(source.Items),
            Status = GetStatusDisplayText(source.Status),
            FormattedDate = FormatOrderDate(source.CreatedAt)
        };
    }
    
    private static decimal CalculateTotal(IEnumerable<OrderItem> items)
    {
        return items.Sum(item => item.Quantity * item.UnitPrice);
    }
    
    private static string GetStatusDisplayText(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Pending => "Pending",
            OrderStatus.Processing => "Processing",
            OrderStatus.Shipped => "Shipped",
            OrderStatus.Delivered => "Delivered",
            OrderStatus.Cancelled => "Cancelled",
            _ => "Unknown"
        };
    }
    
    private static string FormatOrderDate(DateTime date)
    {
        return date.ToString("yyyy-MM-dd HH:mm");
    }
}
```

### 3. Consider Caching for Expensive Computations

```csharp
public class ProductMapper : BaseMapper<Product, ProductDto>
{
    // Cache expensive calculations that don't change
    private static readonly ConcurrentDictionary<string, string> FormattedCategoryCache = new();
    
    public override ProductDto Map(Product source)
    {
        return new ProductDto
        {
            Id = source.Id,
            Name = source.Name,
            Price = source.Price,
            FormattedCategory = GetFormattedCategory(source.Category)
        };
    }
    
    private static string GetFormattedCategory(string category)
    {
        if (string.IsNullOrEmpty(category))
            return "Uncategorized";
            
        return FormattedCategoryCache.GetOrAdd(category, cat => 
            CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cat.ToLower()));
    }
}
```

## Error Handling

### 1. Handle Null Values Gracefully

```csharp
public class UserMapper : BaseMapper<UserEntity, UserDto>
{
    public override UserDto Map(UserEntity source)
    {
        // Handle null source at the mapper level if needed
        if (source == null)
            throw new ArgumentNullException(nameof(source));
            
        return new UserDto
        {
            Id = source.Id,
            FullName = GetFullName(source),
            Email = source.Email ?? string.Empty,
            Department = source.Department?.Name ?? "No Department",
            ManagerName = source.Manager?.FullName ?? "No Manager"
        };
    }
    
    private static string GetFullName(UserEntity user)
    {
        var firstName = user.FirstName?.Trim() ?? string.Empty;
        var lastName = user.LastName?.Trim() ?? string.Empty;
        
        if (string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(lastName))
            return "Unknown User";
            
        return $"{firstName} {lastName}".Trim();
    }
}
```

### 2. Validate Input When Necessary

```csharp
public class CriticalDataMapper : BaseMapper<SensitiveEntity, PublicDto>
{
    public override PublicDto Map(SensitiveEntity source)
    {
        // Validate critical business rules
        if (source.Id <= 0)
            throw new InvalidOperationException("Entity ID must be positive");
            
        if (string.IsNullOrEmpty(source.PublicName))
            throw new InvalidOperationException("Public name is required");
            
        return new PublicDto
        {
            Id = source.Id,
            Name = source.PublicName,
            IsActive = source.IsActive,
            // Don't expose sensitive data
            PublicDescription = SanitizeDescription(source.InternalDescription)
        };
    }
    
    private static string SanitizeDescription(string description)
    {
        if (string.IsNullOrEmpty(description))
            return string.Empty;
            
        // Remove internal markers or sensitive information
        return description
            .Replace("[INTERNAL]", "")
            .Replace("[CONFIDENTIAL]", "")
            .Trim();
    }
}
```

## Collection Handling

### 1. Efficient Collection Mapping

```csharp
public class UserWithOrdersMapper : BaseMapper<UserEntity, UserWithOrdersDto>
{
    private readonly IMapper _mapper;
    
    public UserWithOrdersMapper(IMapper mapper)
    {
        _mapper = mapper;
    }
    
    public override UserWithOrdersDto Map(UserEntity source)
    {
        return new UserWithOrdersDto
        {
            Id = source.Id,
            Name = $"{source.FirstName} {source.LastName}",
            Email = source.Email,
            // Use ToList() to avoid multiple enumeration
            Orders = _mapper.Map<OrderSummaryDto>(source.Orders).ToList(),
            OrderCount = source.Orders.Count, // Direct count, not orders.Count()
            TotalSpent = source.Orders.Sum(o => o.Total) // Calculate once
        };
    }
}
```

### 2. Handle Large Collections Appropriately

```csharp
// For potentially large collections, consider pagination at the service level
public class UserService
{
    private readonly IMapper _mapper;
    
    public UserService(IMapper mapper)
    {
        _mapper = mapper;
    }
    
    // ✅ GOOD: Handle pagination in service, not mapper
    public async Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize)
    {
        var pagedUsers = await _userRepository.GetPagedAsync(page, pageSize);
        
        return new PagedResult<UserDto>
        {
            Items = _mapper.Map<UserDto>(pagedUsers.Items).ToList(),
            TotalCount = pagedUsers.TotalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
```

## Organization and Maintainability

### 1. Organize Mappers by Feature

```
/Mappers
  /Users
    UserEntityToUserDtoMapper.cs
    UserEntityToUserSummaryMapper.cs
    CreateUserRequestToUserEntityMapper.cs
  /Orders
    OrderEntityToOrderDtoMapper.cs
    OrderEntityToInvoiceMapper.cs
    CreateOrderRequestToOrderEntityMapper.cs
  /Products
    ProductEntityToProductDtoMapper.cs
    ProductEntityToProductListItemMapper.cs
```

### 2. Use Consistent Naming Conventions

```csharp
// Pattern: {Source}To{Destination}Mapper
public class UserEntityToUserDtoMapper : BaseMapper<UserEntity, UserDto> { }
public class ProductToProductListItemMapper : BaseMapper<Product, ProductListItemDto> { }
public class CreateOrderRequestToOrderEntityMapper : BaseMapper<CreateOrderRequest, OrderEntity> { }

// Or: {Purpose}Mapper
public class UserSummaryMapper : BaseMapper<UserEntity, UserSummaryDto> { }
public class ProductCatalogMapper : BaseMapper<Product, ProductCatalogDto> { }
public class InvoiceGenerationMapper : BaseMapper<Order, InvoiceDto> { }
```

### 3. Document Complex Mapping Logic

```csharp
/// <summary>
/// Maps order entities to invoice DTOs for PDF generation.
/// Applies business rules for tax calculation and formatting.
/// </summary>
public class OrderToInvoiceMapper : BaseMapper<OrderEntity, InvoiceDto>
{
    public override InvoiceDto Map(OrderEntity source)
    {
        return new InvoiceDto
        {
            InvoiceNumber = GenerateInvoiceNumber(source.Id, source.CreatedAt),
            // Tax calculation follows business rule BR-INV-001
            SubTotal = source.Items.Sum(i => i.Quantity * i.UnitPrice),
            TaxAmount = CalculateTax(source),
            Total = CalculateTotal(source),
            // Format according to local regulations
            FormattedTotal = FormatCurrency(CalculateTotal(source), source.Customer.Country)
        };
    }
    
    /// <summary>
    /// Calculates tax based on customer location and product categories.
    /// Implements business rule BR-TAX-002.
    /// </summary>
    private static decimal CalculateTax(OrderEntity order)
    {
        // Complex tax calculation logic here
        var subtotal = order.Items.Sum(i => i.Quantity * i.UnitPrice);
        var taxRate = GetTaxRate(order.Customer.Country, order.Items);
        return subtotal * taxRate;
    }
}
```

## Testing Best Practices

### 1. Mappers Are Easy to Unit Test

```csharp
[Test]
public void UserMapper_ShouldMapAllProperties_WhenValidEntityProvided()
{
    // Arrange
    var mapper = new UserEntityToUserDtoMapper();
    var entity = new UserEntity
    {
        Id = 1,
        FirstName = "John",
        LastName = "Doe",
        Email = "john.doe@example.com",
        IsActive = true
    };
    
    // Act
    var result = mapper.Map(entity);
    
    // Assert
    Assert.That(result.Id, Is.EqualTo(1));
    Assert.That(result.FullName, Is.EqualTo("John Doe"));
    Assert.That(result.Email, Is.EqualTo("john.doe@example.com"));
    Assert.That(result.Status, Is.EqualTo("Active"));
}
```

### 2. Test Edge Cases

```csharp
[Test]
public void UserMapper_ShouldHandleNullLastName_Gracefully()
{
    // Arrange
    var mapper = new UserEntityToUserDtoMapper();
    var entity = new UserEntity
    {
        Id = 1,
        FirstName = "John",
        LastName = null, // Edge case
        Email = "john@example.com",
        IsActive = true
    };
    
    // Act
    var result = mapper.Map(entity);
    
    // Assert
    Assert.That(result.FullName, Is.EqualTo("John"));
}
```

## Common Anti-Patterns to Avoid

### 1. Don't Use Mappers as Service Locators

```csharp
// ❌ BAD: Using mapper to orchestrate business logic
public class BadOrderMapper : BaseMapper<CreateOrderRequest, OrderEntity>
{
    public override OrderEntity Map(CreateOrderRequest source)
    {
        // ❌ Don't do business logic in mappers
        if (source.Items.Sum(i => i.Quantity * i.Price) > 1000)
        {
            // ❌ Don't send emails from mappers
            EmailService.SendHighValueOrderAlert(source.CustomerId);
        }
        
        return new OrderEntity { /* ... */ };
    }
}

// ✅ GOOD: Keep mappers pure, handle business logic in services
public class OrderService
{
    public async Task<OrderDto> CreateOrderAsync(CreateOrderRequest request)
    {
        // Business logic in service
        var entity = _mapper.Map<OrderEntity>(request);
        
        // Save to database
        await _orderRepository.SaveAsync(entity);
        
        // Business rule: send alert for high-value orders
        if (entity.Total > 1000)
        {
            await _emailService.SendHighValueOrderAlertAsync(entity.CustomerId);
        }
        
        return _mapper.Map<OrderDto>(entity);
    }
}
```

### 2. Don't Create Generic "Universal" Mappers

```csharp
// ❌ BAD: Generic mapper that tries to do everything
public class UniversalMapper
{
    public T Map<T>(object source, string context)
    {
        // Massive switch statement or reflection - avoid this!
    }
}

// ✅ GOOD: Specific, type-safe mappers
public class UserEntityToUserDtoMapper : BaseMapper<UserEntity, UserDto> { }
public class ProductEntityToProductDtoMapper : BaseMapper<ProductEntity, ProductDto> { }
```

By following these best practices, you'll create maintainable, performant, and testable mapping code that scales well with your application. 
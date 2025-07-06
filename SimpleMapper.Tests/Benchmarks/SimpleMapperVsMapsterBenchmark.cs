using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using SimpleMapper;
using SimpleMapper.Tests.TestModels;

namespace SimpleMapper.Tests.Benchmarks;

[Config(typeof(Config))]
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[RankColumn]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
public class SimpleMapperVsMapsterBenchmark
{
    private class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.Default.WithToolchain(InProcessEmitToolchain.Instance));
        }
    }

    private IMapper _simpleMapper = null!;
    private IServiceProvider _serviceProvider = null!;

    // Test data
    private User _singleUser = null!;
    private User[] _userArray = null!;
    private List<User> _userList = null!;
    private NumericTypesSource _numericSource = null!;
    private Order _singleOrder = null!;
    private List<Order> _orderList = null!;
    private UserWithAddresses _complexUser = null!;
    private List<UserWithAddresses> _complexUserList = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Setup SimpleMapper
        var services = new ServiceCollection();
        services.AddSimpleMapper();
        _serviceProvider = services.BuildServiceProvider();
        _simpleMapper = _serviceProvider.GetRequiredService<IMapper>();

        // Setup Mapster
        TypeAdapterConfig.GlobalSettings.Default.MapToConstructor(true);
        TypeAdapterConfig<User, UserDto>.NewConfig()
            .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}")
            .Map(dest => dest.Status, src => src.IsActive ? "Active" : "Inactive")
            .Map(dest => dest.CreatedDate, src => src.CreatedAt.ToString("yyyy-MM-dd"))
            .Map(dest => dest.RoleName, src => src.Role.ToString());

        TypeAdapterConfig<Order, OrderDto>.NewConfig()
            .Map(dest => dest.FormattedAmount, src => $"${src.Amount:F2}")
            .Map(dest => dest.PriorityLevel, src => GetPriorityString(src.Priority))
            .Map(dest => dest.CreatedDate, src => src.CreatedAt.ToString("yyyy-MM-dd HH:mm"));

        TypeAdapterConfig<NumericTypesSource, NumericTypesDestination>.NewConfig()
            .Map(dest => dest.SByteString, src => src.SByteValue.ToString())
            .Map(dest => dest.IntString, src => src.IntValue.ToString())
            .Map(dest => dest.DecimalString, src => src.DecimalValue.ToString("F6"))
            .Map(dest => dest.BoolString, src => src.BoolValue.ToString())
            .Map(dest => dest.DateTimeString, src => src.DateTimeValue.ToString("yyyy-MM-dd HH:mm:ss"))
            .Map(dest => dest.NullableIntString, src => src.NullableInt.HasValue ? src.NullableInt.Value.ToString() : "NULL");

        TypeAdapterConfig<UserWithAddresses, UserWithAddressesDto>.NewConfig()
            .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}");

        TypeAdapterConfig<Address, AddressDto>.NewConfig()
            .Map(dest => dest.FullAddress, src => $"{src.Street}, {src.City}, {src.PostalCode}")
            .Map(dest => dest.Location, src => $"{src.City}, {src.Country}");

        // Initialize test data
        _singleUser = new User
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            IsActive = true,
            CreatedAt = DateTime.Now,
            Role = UserRole.User
        };

        _userArray = Enumerable.Range(1, 1000).Select(i => new User
        {
            Id = i,
            FirstName = $"FirstName{i}",
            LastName = $"LastName{i}",
            Email = $"user{i}@example.com",
            IsActive = i % 2 == 0,
            CreatedAt = DateTime.Now.AddDays(-i),
            Role = (UserRole)(i % 4)
        }).ToArray();

        _userList = _userArray.ToList();

        _numericSource = new NumericTypesSource();

        _singleOrder = new Order
        {
            Id = 1,
            CustomerName = "Test Customer",
            Amount = 99.99m,
            Status = OrderStatus.Pending,
            Priority = Priority.High,
            CreatedAt = DateTime.Now
        };

        _orderList = Enumerable.Range(1, 500).Select(i => new Order
        {
            Id = i,
            CustomerName = $"Customer{i}",
            Amount = i * 10.5m,
            Status = (OrderStatus)(i % 5),
            Priority = (Priority)((i % 4) + 1),
            CreatedAt = DateTime.Now.AddHours(-i)
        }).ToList();

        _complexUser = new UserWithAddresses
        {
            Id = 1,
            FirstName = "Complex",
            LastName = "User",
            Email = "complex@example.com",
            Addresses = new List<Address>
            {
                new() { Id = 1, Street = "123 Main St", City = "Boston", Country = "USA", PostalCode = "02101" },
                new() { Id = 2, Street = "456 Oak Ave", City = "Cambridge", Country = "USA", PostalCode = "02138" }
            }
        };

        _complexUserList = Enumerable.Range(1, 200).Select(i => new UserWithAddresses
        {
            Id = i,
            FirstName = $"User{i}",
            LastName = $"Last{i}",
            Email = $"user{i}@test.com",
            Addresses = Enumerable.Range(1, 2).Select(j => new Address
            {
                Id = j,
                Street = $"{j * 100} Street {i}",
                City = $"City{i}",
                Country = "USA",
                PostalCode = $"{10000 + i:D5}"
            }).ToList()
        }).ToList();
    }

    private static string GetPriorityString(Priority priority)
    {
        return priority switch
        {
            Priority.Low => "Low Priority",
            Priority.Medium => "Medium Priority",
            Priority.High => "High Priority",
            Priority.Critical => "Critical Priority",
            _ => "Unknown Priority"
        };
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    #region Single Object Mapping

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Single", "User")]
    public UserDto SimpleMapper_SingleUser_Explicit()
    {
        return _simpleMapper.Map<User, UserDto>(_singleUser);
    }

    [Benchmark]
    [BenchmarkCategory("Single", "User")]
    public UserDto SimpleMapper_SingleUser_TypeInference()
    {
        return _simpleMapper.Map<UserDto>(_singleUser);
    }

    [Benchmark]
    [BenchmarkCategory("Single", "User")]
    public UserDto Mapster_SingleUser()
    {
        return _singleUser.Adapt<UserDto>();
    }

    [Benchmark]
    [BenchmarkCategory("Single", "Numeric")]
    public NumericTypesDestination SimpleMapper_NumericTypes_Explicit()
    {
        return _simpleMapper.Map<NumericTypesSource, NumericTypesDestination>(_numericSource);
    }

    [Benchmark]
    [BenchmarkCategory("Single", "Numeric")]
    public NumericTypesDestination SimpleMapper_NumericTypes_TypeInference()
    {
        return _simpleMapper.Map<NumericTypesDestination>(_numericSource);
    }

    [Benchmark]
    [BenchmarkCategory("Single", "Numeric")]
    public NumericTypesDestination Mapster_NumericTypes()
    {
        return _numericSource.Adapt<NumericTypesDestination>();
    }

    [Benchmark]
    [BenchmarkCategory("Single", "Order")]
    public OrderDto SimpleMapper_SingleOrder_Explicit()
    {
        return _simpleMapper.Map<Order, OrderDto>(_singleOrder);
    }

    [Benchmark]
    [BenchmarkCategory("Single", "Order")]
    public OrderDto SimpleMapper_SingleOrder_TypeInference()
    {
        return _simpleMapper.Map<OrderDto>(_singleOrder);
    }

    [Benchmark]
    [BenchmarkCategory("Single", "Order")]
    public OrderDto Mapster_SingleOrder()
    {
        return _singleOrder.Adapt<OrderDto>();
    }

    #endregion

    #region Bulk Mapping - Arrays

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Bulk", "Array")]
    public UserDto[] SimpleMapper_UserArray_Explicit()
    {
        return _simpleMapper.Map<User, UserDto>(_userArray).ToArray();
    }

    [Benchmark]
    [BenchmarkCategory("Bulk", "Array")]
    public UserDto[] SimpleMapper_UserArray_TypeInference()
    {
        return _simpleMapper.Map<UserDto>(_userArray).ToArray();
    }

    [Benchmark]
    [BenchmarkCategory("Bulk", "Array")]
    public UserDto[] Mapster_UserArray()
    {
        return _userArray.Adapt<UserDto[]>();
    }

    #endregion

    #region Bulk Mapping - Lists

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Bulk", "List")]
    public List<UserDto> SimpleMapper_UserList_Explicit()
    {
        return _simpleMapper.Map<User, UserDto>(_userList).ToList();
    }

    [Benchmark]
    [BenchmarkCategory("Bulk", "List")]
    public List<UserDto> SimpleMapper_UserList_TypeInference()
    {
        return _simpleMapper.Map<UserDto>(_userList).ToList();
    }

    [Benchmark]
    [BenchmarkCategory("Bulk", "List")]
    public List<UserDto> Mapster_UserList()
    {
        return _userList.Adapt<List<UserDto>>();
    }

    [Benchmark]
    [BenchmarkCategory("Bulk", "Order")]
    public List<OrderDto> SimpleMapper_OrderList_Explicit()
    {
        return _simpleMapper.Map<Order, OrderDto>(_orderList).ToList();
    }

    [Benchmark]
    [BenchmarkCategory("Bulk", "Order")]
    public List<OrderDto> SimpleMapper_OrderList_TypeInference()
    {
        return _simpleMapper.Map<OrderDto>(_orderList).ToList();
    }

    [Benchmark]
    [BenchmarkCategory("Bulk", "Order")]
    public List<OrderDto> Mapster_OrderList()
    {
        return _orderList.Adapt<List<OrderDto>>();
    }

    #endregion

    #region Complex Nested Mapping

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Complex", "Single")]
    public UserWithAddressesDto SimpleMapper_ComplexUser_Explicit()
    {
        return _simpleMapper.Map<UserWithAddresses, UserWithAddressesDto>(_complexUser);
    }

    [Benchmark]
    [BenchmarkCategory("Complex", "Single")]
    public UserWithAddressesDto SimpleMapper_ComplexUser_TypeInference()
    {
        return _simpleMapper.Map<UserWithAddressesDto>(_complexUser);
    }

    [Benchmark]
    [BenchmarkCategory("Complex", "Single")]
    public UserWithAddressesDto Mapster_ComplexUser()
    {
        return _complexUser.Adapt<UserWithAddressesDto>();
    }

    [Benchmark]
    [BenchmarkCategory("Complex", "List")]
    public List<UserWithAddressesDto> SimpleMapper_ComplexUserList_Explicit()
    {
        return _simpleMapper.Map<UserWithAddresses, UserWithAddressesDto>(_complexUserList).ToList();
    }

    [Benchmark]
    [BenchmarkCategory("Complex", "List")]
    public List<UserWithAddressesDto> SimpleMapper_ComplexUserList_TypeInference()
    {
        return _simpleMapper.Map<UserWithAddressesDto>(_complexUserList).ToList();
    }

    [Benchmark]
    [BenchmarkCategory("Complex", "List")]
    public List<UserWithAddressesDto> Mapster_ComplexUserList()
    {
        return _complexUserList.Adapt<List<UserWithAddressesDto>>();
    }

    #endregion

    #region Type Inference Performance Focus

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("TypeInference", "Performance")]
    public UserDto SimpleMapper_TypeInference_Cached()
    {
        // This will benefit from our cached method dispatch optimization
        return _simpleMapper.Map<UserDto>(_singleUser);
    }

    [Benchmark]
    [BenchmarkCategory("TypeInference", "Performance")]
    public UserDto SimpleMapper_TypeInference_Fresh()
    {
        // Create a new mapper instance to test cold start performance
        var services = new ServiceCollection();
        services.AddSimpleMapper();
        using var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();
        return mapper.Map<UserDto>(_singleUser);
    }

    [Benchmark]
    [BenchmarkCategory("TypeInference", "Performance")]
    public UserDto Mapster_TypeInference_Equivalent()
    {
        return _singleUser.Adapt<UserDto>();
    }

    #endregion

    #region Memory Allocation Tests

    [Benchmark]
    [BenchmarkCategory("Memory", "Allocation")]
    public UserDto SimpleMapper_MemoryTest()
    {
        // Test memory allocation with our optimized cache keys
        return _simpleMapper.Map<User, UserDto>(_singleUser);
    }

    [Benchmark]
    [BenchmarkCategory("Memory", "Allocation")]
    public UserDto Mapster_MemoryTest()
    {
        return _singleUser.Adapt<UserDto>();
    }

    #endregion
} 
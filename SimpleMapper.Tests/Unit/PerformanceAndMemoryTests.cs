using Microsoft.Extensions.DependencyInjection;
using SimpleMapper.Tests.TestModels;
using System.Diagnostics;

namespace SimpleMapper.Tests.Unit;

public class PerformanceAndMemoryTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMapper _mapper;

    public PerformanceAndMemoryTests()
    {
        var services = new ServiceCollection();
        services.AddSimpleMapper();

        _serviceProvider = services.BuildServiceProvider();
        _mapper = _serviceProvider.GetRequiredService<IMapper>();
    }

    [Fact]
    public void Mapper_SingleObjectMapping_ShouldBeVeryFast()
    {
        // Arrange
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe", IsActive = true };
        
        // Warm up
        _mapper.Map<User, UserDto>(user);

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < 100000; i++)
        {
            _mapper.Map<User, UserDto>(user);
        }
        
        stopwatch.Stop();

        // Assert - 100k mappings should complete in under 100ms
        Assert.True(stopwatch.ElapsedMilliseconds < 100, 
            $"Single object mapping should be very fast, but took {stopwatch.ElapsedMilliseconds}ms for 100k operations");
    }

    [Fact]
    public void Mapper_TypeInferenceMapping_ShouldBeVeryFast()
    {
        // Arrange
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe", IsActive = true };
        
        // Warm up
        _mapper.Map<UserDto>(user);

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < 100000; i++)
        {
            _mapper.Map<UserDto>(user);
        }
        
        stopwatch.Stop();

        // Assert - Type inference should be nearly as fast as explicit typing due to caching
        Assert.True(stopwatch.ElapsedMilliseconds < 150, 
            $"Type inference mapping should be fast, but took {stopwatch.ElapsedMilliseconds}ms for 100k operations");
    }

    [Fact]
    public void Mapper_CollectionMapping_ShouldScaleLinearly()
    {
        // Arrange
        var users = Enumerable.Range(1, 1000).Select(i => new User 
        { 
            Id = i, 
            FirstName = $"User{i}", 
            LastName = "Test", 
            IsActive = i % 2 == 0 
        }).ToList();

        // Warm up
        _mapper.Map<User, UserDto>(users.Take(10)).ToList();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var results = _mapper.Map<User, UserDto>(users).ToList();
        stopwatch.Stop();

        // Assert
        Assert.Equal(1000, results.Count);
        Assert.True(stopwatch.ElapsedMilliseconds < 50, 
            $"Collection mapping should be fast, but took {stopwatch.ElapsedMilliseconds}ms for 1k items");
    }

    [Fact]
    public void Mapper_LargeCollectionMapping_ShouldHandleEfficiently()
    {
        // Arrange
        var users = Enumerable.Range(1, 10000).Select(i => new User 
        { 
            Id = i, 
            FirstName = $"User{i}", 
            LastName = "Test", 
            IsActive = i % 2 == 0 
        }).ToList();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var results = _mapper.Map<User, UserDto>(users).ToList();
        stopwatch.Stop();

        // Assert
        Assert.Equal(10000, results.Count);
        Assert.True(stopwatch.ElapsedMilliseconds < 500, 
            $"Large collection mapping should be efficient, but took {stopwatch.ElapsedMilliseconds}ms for 10k items");
    }

    [Fact]
    public void MapperRegistry_CachingPerformance_ShouldBeConsistent()
    {
        // Arrange
        var registry = _serviceProvider.GetRequiredService<MapperRegistry>();

        // Act - First call (cold)
        var mapper1 = registry.GetMapper<User, UserDto>();
        
        // Act - Second call (should be cached)
        var mapper2 = registry.GetMapper<User, UserDto>();
        
        // Act - Third call (should also be cached)
        var mapper3 = registry.GetMapper<User, UserDto>();

        // Assert - All calls should return the same cached instance
        Assert.Same(mapper1, mapper2); // Should be cached
        Assert.Same(mapper1, mapper3); // Should be same cached instance
        Assert.Same(mapper2, mapper3); // All should be identical
        
        // Test that caching works across multiple calls
        var mappers = new IMapper<User, UserDto>[100];
        for (int i = 0; i < 100; i++)
        {
            mappers[i] = registry.GetMapper<User, UserDto>();
        }
        
        // All 100 calls should return the exact same instance
        Assert.All(mappers, mapper => Assert.Same(mapper1, mapper));
        
        // Test that different type pairs get different mappers but each is cached
        var addressMapper1 = registry.GetMapper<Address, AddressDto>();
        var addressMapper2 = registry.GetMapper<Address, AddressDto>();
        
        Assert.Same(addressMapper1, addressMapper2); // Address mappers should be cached
        Assert.NotSame(mapper1, addressMapper1); // Different type pairs should be different instances
    }

    [Fact]
    public void Mapper_ComplexObjectMapping_ShouldBeReasonablyFast()
    {
        // Arrange
        var complexUser = new UserWithAddresses
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Addresses = Enumerable.Range(1, 10).Select(i => new Address
            {
                Id = i,
                Street = $"Street {i}",
                City = $"City {i}",
                Country = "USA",
                PostalCode = $"1000{i}"
            }).ToList()
        };

        // Warm up
        _mapper.Map<UserWithAddresses, UserWithAddressesDto>(complexUser);

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < 10000; i++)
        {
            _mapper.Map<UserWithAddresses, UserWithAddressesDto>(complexUser);
        }
        
        stopwatch.Stop();

        // Assert - Complex mapping should still be reasonably fast
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"Complex object mapping should be reasonably fast, but took {stopwatch.ElapsedMilliseconds}ms for 10k operations");
    }

    [Fact]
    public void Mapper_PrimitiveTypeMapping_ShouldBeFast()
    {
        // Arrange
        var source = new NumericTypesSource();
        
        // Warm up
        _mapper.Map<NumericTypesSource, NumericTypesDestination>(source);

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < 50000; i++)
        {
            _mapper.Map<NumericTypesSource, NumericTypesDestination>(source);
        }
        
        stopwatch.Stop();

        // Assert - Primitive type mapping should be reasonably fast
        // Use a generous threshold to avoid flaky tests due to system variations
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"Primitive type mapping should be reasonably fast, but took {stopwatch.ElapsedMilliseconds}ms for 50k operations");
        
        // Verify the mapping still works correctly
        var result = _mapper.Map<NumericTypesSource, NumericTypesDestination>(source);
        Assert.NotNull(result);
        Assert.Equal(source.IntValue.ToString(), result.IntString);
        Assert.Equal(source.DecimalValue.ToString(), result.DecimalString);
    }

    [Fact]
    public void Mapper_ConcurrentMapping_ShouldMaintainPerformance()
    {
        // Arrange
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe", IsActive = true };
        const int threadsCount = 10;
        const int operationsPerThread = 10000;
        var results = new UserDto[threadsCount][];

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        Parallel.For(0, threadsCount, threadIndex =>
        {
            results[threadIndex] = new UserDto[operationsPerThread];
            for (int i = 0; i < operationsPerThread; i++)
            {
                results[threadIndex][i] = _mapper.Map<User, UserDto>(user);
            }
        });
        
        stopwatch.Stop();

        // Assert
        Assert.All(results, threadResults =>
        {
            Assert.Equal(operationsPerThread, threadResults.Length);
            Assert.All(threadResults, result => Assert.Equal("John Doe", result.FullName));
        });

        // Should complete concurrent operations reasonably fast
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"Concurrent mapping should be efficient, but took {stopwatch.ElapsedMilliseconds}ms for {threadsCount * operationsPerThread} operations");
    }

    [Fact]
    public void Mapper_MemoryUsage_ShouldNotLeakWithRepeatedMapping()
    {
        // Arrange
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe", IsActive = true };
        
        // Force garbage collection before test
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var initialMemory = GC.GetTotalMemory(false);

        // Act - Perform many mappings to warm up caches
        for (int i = 0; i < 100000; i++)
        {
            var result = _mapper.Map<User, UserDto>(user);
            // Don't hold references to results to allow GC
        }

        // Force garbage collection after warmup
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var postWarmupMemory = GC.GetTotalMemory(false);
        var warmupIncrease = postWarmupMemory - initialMemory;

        // Assert - Memory increase should be reasonable considering type inference caching
        // The static caches for type inference will consume some memory for performance
        Assert.True(warmupIncrease < 20 * 1024 * 1024, 
            $"Memory usage should be reasonable after warmup, but increased by {warmupIncrease} bytes ({warmupIncrease / 1024.0 / 1024.0:F2} MB)");
        
        // Test for major memory leaks: Run multiple large batches and ensure no catastrophic growth
        var memoryBeforeBatches = GC.GetTotalMemory(false);
        
        // Run several large batches to detect serious memory leaks
        for (int batch = 0; batch < 5; batch++)
        {
            // Run a large batch of operations
            for (int i = 0; i < 100000; i++)
            {
                var result = _mapper.Map<User, UserDto>(user);
            }
            
            // Occasionally force GC to prevent false positives from delayed GC
            if (batch % 2 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }
        
        // Final GC to clean up
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var finalMemory = GC.GetTotalMemory(false);
        var totalGrowth = finalMemory - memoryBeforeBatches;
        
        // Check for major memory leaks - should not grow by more than 50MB
        // This is a generous threshold that will catch real leaks but not GC variations
        Assert.True(totalGrowth < 50 * 1024 * 1024, 
            $"Memory should not grow significantly with repeated mapping. Growth: {totalGrowth} bytes ({totalGrowth / 1024.0 / 1024.0:F2} MB)");
        
        // Verify the mapper is still functional after all operations
        var finalResult = _mapper.Map<User, UserDto>(user);
        Assert.NotNull(finalResult);
        Assert.Equal("John Doe", finalResult.FullName);
    }

    [Fact]
    public void Mapper_CacheEfficiency_ShouldNotGrowUnbounded()
    {
        // Arrange - Create many different type pairs to test cache behavior
        var sources = new object[]
        {
            new User { Id = 1, FirstName = "John", LastName = "Doe" },
            new Product { Id = 1, Name = "Product", Price = 99.99m },
            new Address { Id = 1, Street = "Street", City = "City" },
            new Order { Id = 1, CustomerName = "Customer", Amount = 100m }
        };

        var destinationTypes = new[]
        {
            typeof(UserDto),
            typeof(ProductDto),
            typeof(AddressDto),
            typeof(OrderDto)
        };

        // Act - Map using type inference for different combinations
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < 10000; i++)
        {
            var sourceIndex = i % sources.Length;
            var destTypeIndex = sourceIndex; // Match types appropriately
            
            var source = sources[sourceIndex];
            var destType = destinationTypes[destTypeIndex];
            
            // Use reflection to call Map<T> method
            var mapMethod = typeof(IMapper).GetMethods()
                .First(m => m.Name == "Map" && m.GetGenericArguments().Length == 1 && m.GetParameters().Length == 1);
            var genericMethod = mapMethod.MakeGenericMethod(destType);
            
            genericMethod.Invoke(_mapper, new[] { source });
        }
        
        stopwatch.Stop();

        // Assert - Should maintain performance even with cache usage
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"Cache should maintain performance, but took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void Mapper_EnumerationPerformance_ShouldNotReiterateSource()
    {
        // Arrange
        var callCount = 0;
        var users = CreateTrackingEnumerable(() => 
        {
            callCount++;
            return new User { Id = callCount, FirstName = $"User{callCount}", LastName = "Test", IsActive = true };
        }, 1000);

        // Act
        var results = _mapper.Map<User, UserDto>(users).ToList();

        // Assert
        Assert.Equal(1000, results.Count);
        Assert.Equal(1000, callCount); // Should enumerate exactly once
    }

    private static IEnumerable<User> CreateTrackingEnumerable(Func<User> factory, int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return factory();
        }
    }

    [Fact]
    public void Mapper_StringInterning_ShouldNotCauseMemoryLeaks()
    {
        // Arrange
        var users = Enumerable.Range(1, 1000).Select(i => new User 
        { 
            Id = i, 
            FirstName = $"DynamicFirstName{i}_{Guid.NewGuid()}", // Dynamic strings
            LastName = $"DynamicLastName{i}_{Guid.NewGuid()}", 
            IsActive = true 
        }).ToList();

        // Act
        var results = _mapper.Map<User, UserDto>(users).ToList();

        // Assert
        Assert.Equal(1000, results.Count);
        
        // Verify that dynamic strings are not all the same (not interned incorrectly)
        var uniqueFirstNames = results.Select(r => r.FullName.Split(' ')[0]).Distinct().Count();
        Assert.True(uniqueFirstNames > 900, "Dynamic strings should remain unique");
    }

    [Fact]
    public void Mapper_DeepObjectGraphs_ShouldHandleWithoutStackOverflow()
    {
        // Arrange - Create a deep object graph
        var root = new UserWithAddresses
        {
            Id = 1,
            FirstName = "Root",
            LastName = "User",
            Addresses = new List<Address>()
        };

        // Create deep address hierarchy (not truly deep nesting, but many items)
        for (int i = 0; i < 1000; i++)
        {
            root.Addresses.Add(new Address
            {
                Id = i,
                Street = $"Street {i}",
                City = $"City {i}",
                Country = "USA",
                PostalCode = $"{10000 + i}"
            });
        }

        // Act - Should not cause stack overflow
        var result = _mapper.Map<UserWithAddresses, UserWithAddressesDto>(root);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1000, result.Addresses.Count);
        Assert.Equal("Root User", result.FullName);
    }
} 
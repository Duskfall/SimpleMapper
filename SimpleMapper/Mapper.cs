using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleMapper;

/// <summary>
/// High-performance cache key for mapper registration avoiding string allocations.
/// Uses struct semantics with pre-computed hash codes for optimal performance.
/// </summary>
public readonly struct MapperKey : IEquatable<MapperKey>
{
    /// <summary>
    /// The source type for the mapping.
    /// </summary>
    public readonly Type SourceType;
    
    /// <summary>
    /// The destination type for the mapping.
    /// </summary>
    public readonly Type DestinationType;
    
    /// <summary>
    /// Pre-computed hash code for optimal performance.
    /// </summary>
    public readonly int HashCode;

    /// <summary>
    /// Initializes a new instance of the MapperKey struct.
    /// </summary>
    /// <param name="sourceType">The source type for the mapping</param>
    /// <param name="destinationType">The destination type for the mapping</param>
    /// <exception cref="ArgumentNullException">Thrown when sourceType or destinationType is null</exception>
    public MapperKey(Type sourceType, Type destinationType)
    {
        SourceType = sourceType ?? throw new ArgumentNullException(nameof(sourceType));
        DestinationType = destinationType ?? throw new ArgumentNullException(nameof(destinationType));
        HashCode = System.HashCode.Combine(sourceType, destinationType);
    }

    /// <summary>
    /// Determines whether the current MapperKey is equal to another MapperKey.
    /// </summary>
    /// <param name="other">The MapperKey to compare with</param>
    /// <returns>True if the MapperKeys are equal, false otherwise</returns>
    public bool Equals(MapperKey other) => 
        SourceType == other.SourceType && DestinationType == other.DestinationType;

    /// <summary>
    /// Determines whether the current MapperKey is equal to the specified object.
    /// </summary>
    /// <param name="obj">The object to compare with</param>
    /// <returns>True if the objects are equal, false otherwise</returns>
    public override bool Equals(object? obj) => 
        obj is MapperKey other && Equals(other);

    /// <summary>
    /// Returns the pre-computed hash code for this MapperKey.
    /// </summary>
    /// <returns>The hash code</returns>
    public override int GetHashCode() => HashCode;

    /// <summary>
    /// Returns a string representation of this MapperKey.
    /// </summary>
    /// <returns>A string in the format "SourceType -> DestinationType"</returns>
    public override string ToString() => 
        $"{SourceType.Name} -> {DestinationType.Name}";

    /// <summary>
    /// Determines whether two MapperKey instances are equal.
    /// </summary>
    /// <param name="left">The left MapperKey to compare</param>
    /// <param name="right">The right MapperKey to compare</param>
    /// <returns>True if the instances are equal, false otherwise</returns>
    public static bool operator ==(MapperKey left, MapperKey right) => left.Equals(right);
    
    /// <summary>
    /// Determines whether two MapperKey instances are not equal.
    /// </summary>
    /// <param name="left">The left MapperKey to compare</param>
    /// <param name="right">The right MapperKey to compare</param>
    /// <returns>True if the instances are not equal, false otherwise</returns>
    public static bool operator !=(MapperKey left, MapperKey right) => !left.Equals(right);
}

/// <summary>
/// Registry for managing mapper instances with high-performance caching.
/// </summary>
public class MapperRegistry
{
    private readonly ConcurrentDictionary<MapperKey, object> _mappers = new();
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the MapperRegistry.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving mapper instances</param>
    /// <exception cref="ArgumentNullException">Thrown when serviceProvider is null</exception>
    public MapperRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Gets a mapper for the specified source and destination types.
    /// Uses high-performance struct-based caching to avoid string allocations.
    /// </summary>
    /// <typeparam name="TSource">The source type</typeparam>
    /// <typeparam name="TDestination">The destination type</typeparam>
    /// <returns>The mapper instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when no mapper is registered for the type pair</exception>
    public IMapper<TSource, TDestination> GetMapper<TSource, TDestination>()
    {
        var key = new MapperKey(typeof(TSource), typeof(TDestination));

        // Atomically resolve or retrieve from the cache
        var mapperObj = _mappers.GetOrAdd(key, static (k, state) =>
        {
            var sp = (IServiceProvider)state!;
            var resolved = sp.GetService<IMapper<TSource, TDestination>>();
            if (resolved == null)
            {
                throw new InvalidOperationException($"No mapper registered for {typeof(TSource).Name} -> {typeof(TDestination).Name}");
            }
            return resolved;
        }, _serviceProvider);

        return (IMapper<TSource, TDestination>)mapperObj;
    }
}

/// <summary>
/// Factory for creating and managing mappers.
/// </summary>
public class Mapper : IMapper
{
    private readonly MapperRegistry _registry;
    
    // High-performance cached method dispatch for type inference
    private static readonly ConcurrentDictionary<MapperKey, Func<object, object, object>> _typeInferenceMethods = new();
    private static readonly ConcurrentDictionary<MapperKey, Func<object, System.Collections.IEnumerable, object>> _collectionInferenceMethods = new();

    // Prevent unbounded memory usage – clear caches when they grow beyond the soft limit
    private const int MaxCachedEntries = 2048;

    /// <summary>
    /// Initializes a new instance of the Mapper.
    /// </summary>
    /// <param name="registry">The mapper registry</param>
    public Mapper(MapperRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <summary>
    /// Maps a source object to a destination object.
    /// </summary>
    /// <typeparam name="TSource">The source type</typeparam>
    /// <typeparam name="TDestination">The destination type</typeparam>
    /// <param name="source">The source object</param>
    /// <returns>The mapped destination object</returns>
    /// <exception cref="ArgumentNullException">Thrown when source is null</exception>
    public TDestination Map<TSource, TDestination>(TSource source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        
        var mapper = _registry.GetMapper<TSource, TDestination>();
        return mapper.Map(source);
    }

    /// <summary>
    /// Maps a collection of source objects to a collection of destination objects.
    /// </summary>
    /// <typeparam name="TSource">The source type</typeparam>
    /// <typeparam name="TDestination">The destination type</typeparam>
    /// <param name="sources">The source objects</param>
    /// <returns>The mapped destination objects</returns>
    /// <exception cref="ArgumentNullException">Thrown when sources is null</exception>
    public IEnumerable<TDestination> Map<TSource, TDestination>(IEnumerable<TSource> sources)
    {
        if (sources == null) throw new ArgumentNullException(nameof(sources));
        
        var mapper = _registry.GetMapper<TSource, TDestination>();
        return sources.Select(mapper.Map);
    }

    /// <summary>
    /// Maps a source object to a destination object with optimized type inference.
    /// The source type is inferred from the parameter using cached method dispatch.
    /// </summary>
    /// <typeparam name="TDestination">The destination type</typeparam>
    /// <param name="source">The source object</param>
    /// <returns>The mapped destination object</returns>
    /// <exception cref="ArgumentNullException">Thrown when source is null</exception>
    public TDestination Map<TDestination>(object source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        
        var sourceType = source.GetType();
        var destinationType = typeof(TDestination);
        var key = new MapperKey(sourceType, destinationType);
        
        // Get or create cached method dispatch
        var method = _typeInferenceMethods.GetOrAdd(key, CreateTypeInferenceMethod);
        
        // Simple eviction strategy: clear the whole cache when it grows too large.
        if (_typeInferenceMethods.Count > MaxCachedEntries)
        {
            _typeInferenceMethods.Clear();
        }

        return (TDestination)method(this, source);
    }

    /// <summary>
    /// Maps a collection of source objects to a collection of destination objects with optimized type inference.
    /// The source type is inferred from the collection parameter using cached method dispatch.
    /// </summary>
    /// <typeparam name="TDestination">The destination type</typeparam>
    /// <param name="sources">The source objects</param>
    /// <returns>The mapped destination objects</returns>
    /// <exception cref="ArgumentNullException">Thrown when sources is null</exception>
    public IEnumerable<TDestination> Map<TDestination>(System.Collections.IEnumerable sources)
    {
        if (sources == null) throw new ArgumentNullException(nameof(sources));
        
        // Get the element type from the collection
        var sourceType = GetElementType(sources);
        if (sourceType == null)
        {
            throw new InvalidOperationException("Cannot infer source type from the collection. The collection appears to be empty or contains null values.");
        }
        
        var destinationType = typeof(TDestination);
        var key = new MapperKey(sourceType, destinationType);
        
        // Get or create cached method dispatch
        var method = _collectionInferenceMethods.GetOrAdd(key, CreateCollectionInferenceMethod);
        
        // Simple eviction strategy: clear the whole cache when it grows too large.
        if (_collectionInferenceMethods.Count > MaxCachedEntries)
        {
            _collectionInferenceMethods.Clear();
        }

        return (IEnumerable<TDestination>)method(this, sources);
    }

    /// <summary>
    /// Creates a cached method dispatch for type inference to avoid reflection on every call.
    /// This method is called only once per type pair and then cached.
    /// </summary>
    private static Func<object, object, object> CreateTypeInferenceMethod(MapperKey key)
    {
        var sourceType = key.SourceType;
        var destinationType = key.DestinationType;
        
        // Find the strongly typed Map<TSource, TDestination>(TSource source) method
        var mapMethod = typeof(Mapper).GetMethods()
            .Where(m => m.Name == nameof(Map) && 
                       m.GetGenericArguments().Length == 2 && 
                       m.GetParameters().Length == 1 &&
                       m.GetParameters()[0].ParameterType.IsGenericParameter)
            .Single()
            .MakeGenericMethod(sourceType, destinationType);
        
        return (mapper, source) => 
        {
            try
            {
                return mapMethod.Invoke(mapper, new[] { source })!;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                // Unwrap the inner exception to preserve the original exception type
                throw ex.InnerException;
            }
        };
    }

    /// <summary>
    /// Creates a cached method dispatch for collection type inference to avoid reflection on every call.
    /// This method is called only once per type pair and then cached.
    /// </summary>
    private static Func<object, System.Collections.IEnumerable, object> CreateCollectionInferenceMethod(MapperKey key)
    {
        var sourceType = key.SourceType;
        var destinationType = key.DestinationType;
        
        // Find the strongly typed Map<TSource, TDestination>(IEnumerable<TSource> sources) method
        var enumerableType = typeof(IEnumerable<>).MakeGenericType(sourceType);
        var mapMethod = typeof(Mapper).GetMethods()
            .Where(m => m.Name == nameof(Map) && 
                       m.GetGenericArguments().Length == 2 && 
                       m.GetParameters().Length == 1 &&
                       m.GetParameters()[0].ParameterType.IsGenericType &&
                       m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            .Single()
            .MakeGenericMethod(sourceType, destinationType);
        
        return (mapper, sources) =>
        {
            try
            {
                // Convert to strongly typed enumerable and map
                var stronglyTypedSources = sources.Cast<object>().Where(x => x != null);
                var typedCollection = ConvertToTypedEnumerable(stronglyTypedSources, sourceType);
                return mapMethod.Invoke(mapper, new[] { typedCollection })!;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                // Unwrap the inner exception to preserve the original exception type
                throw ex.InnerException;
            }
        };
    }

    private static Type? GetElementType(System.Collections.IEnumerable sources)
    {
        // First, try to get type from generic interface
        var enumerableType = sources.GetType();
        
        if (enumerableType.IsGenericType)
        {
            var genericArgs = enumerableType.GetGenericArguments();
            if (genericArgs.Length > 0)
            {
                return genericArgs[0];
            }
        }
        
        // Check implemented interfaces for IEnumerable<T>
        var genericEnumerableInterface = enumerableType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        
        if (genericEnumerableInterface != null)
        {
            return genericEnumerableInterface.GetGenericArguments()[0];
        }
        
        // Fallback: get type from first non-null element
        foreach (var item in sources)
        {
            if (item != null)
            {
                return item.GetType();
            }
        }
        
        return null;
    }
    
    private static object ConvertToTypedEnumerable(IEnumerable<object> source, Type elementType)
    {
        var castMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.Cast))!
            .MakeGenericMethod(elementType);
        
        return castMethod.Invoke(null, new object[] { source })!;
    }
} 
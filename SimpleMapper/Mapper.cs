using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleMapper;

/// <summary>
/// Registry for managing mapper instances without reflection.
/// </summary>
public class MapperRegistry
{
    private readonly ConcurrentDictionary<string, object> _mappers = new();
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
    /// </summary>
    /// <typeparam name="TSource">The source type</typeparam>
    /// <typeparam name="TDestination">The destination type</typeparam>
    /// <returns>The mapper instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when no mapper is registered for the type pair</exception>
    public IMapper<TSource, TDestination> GetMapper<TSource, TDestination>()
    {
        var key = $"{typeof(TSource).FullName}->{typeof(TDestination).FullName}";
        
        if (_mappers.TryGetValue(key, out var mapper))
        {
            return (IMapper<TSource, TDestination>)mapper;
        }

        // Try to get from DI container
        var diMapper = _serviceProvider.GetService<IMapper<TSource, TDestination>>();
        if (diMapper != null)
        {
            _mappers[key] = diMapper;
            return diMapper;
        }

        throw new InvalidOperationException($"No mapper registered for {typeof(TSource).Name} -> {typeof(TDestination).Name}");
    }
}

/// <summary>
/// Factory for creating and managing mappers.
/// </summary>
public class Mapper : IMapper
{
    private readonly MapperRegistry _registry;

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
    /// Maps a source object to a destination object with type inference.
    /// The source type is inferred from the parameter.
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
        
        // Use reflection to call the generic Map<TSource, TDestination> method with the inferred types
        var mapMethod = typeof(Mapper).GetMethods()
            .First(m => m.Name == nameof(Map) && 
                       m.GetGenericArguments().Length == 2 && 
                       m.GetParameters().Length == 1 &&
                       !m.GetParameters()[0].ParameterType.IsGenericType)
            .MakeGenericMethod(sourceType, destinationType);
        
        return (TDestination)mapMethod.Invoke(this, new[] { source })!;
    }

    /// <summary>
    /// Maps a collection of source objects to a collection of destination objects with type inference.
    /// The source type is inferred from the collection parameter.
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
        
        // Convert to strongly typed enumerable and map
        var stronglyTypedSources = sources.Cast<object>().Where(x => x != null);
        
        // Use reflection to call the generic Map method with the inferred types
        var mapMethod = typeof(Mapper).GetMethods()
            .First(m => m.Name == nameof(Map) && 
                       m.GetGenericArguments().Length == 2 && 
                       m.GetParameters().Length == 1 &&
                       m.GetParameters()[0].ParameterType.IsGenericType)
            .MakeGenericMethod(sourceType, destinationType);
        
        var typedCollection = ConvertToTypedEnumerable(stronglyTypedSources, sourceType);
        return (IEnumerable<TDestination>)mapMethod.Invoke(this, new[] { typedCollection })!;
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
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleMapper;

/// <summary>
/// Extension methods for registering SimpleMapper services with the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SimpleMapper services and automatically discovers all mappers from the calling assembly.
    /// This is the primary method for most applications - it registers SimpleMapper and finds all your mappers automatically.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    /// <example>
    /// <code>
    /// // In Program.cs or Startup.cs
    /// builder.Services.AddSimpleMapper();
    /// </code>
    /// </example>
    public static IServiceCollection AddSimpleMapper(this IServiceCollection services)
    {
        services.AddSingleton<MapperRegistry>();
        services.AddSingleton<IMapper, Mapper>();
        
        // Auto-discover mappers from the calling assembly
        return services.AddMappersFromAssembly(Assembly.GetCallingAssembly());
    }

    /// <summary>
    /// Adds SimpleMapper services and automatically discovers all mappers from the assembly containing the specified type.
    /// Use this when you need to scan a specific assembly for mappers.
    /// </summary>
    /// <typeparam name="TAssemblyMarker">A type from the assembly to scan for mappers</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    /// <example>
    /// <code>
    /// // Scan the assembly containing UserMapper for all mappers
    /// builder.Services.AddSimpleMapper&lt;UserMapper&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddSimpleMapper<TAssemblyMarker>(this IServiceCollection services)
    {
        services.AddSingleton<MapperRegistry>();
        services.AddSingleton<IMapper, Mapper>();
        
        return services.AddMappersFromAssembly(typeof(TAssemblyMarker).Assembly);
    }

    /// <summary>
    /// Adds SimpleMapper services and automatically discovers all mappers from the specified assemblies.
    /// Use this for advanced scenarios where you need to scan multiple specific assemblies.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assemblies">The assemblies to scan for mappers</param>
    /// <returns>The service collection for chaining</returns>
    /// <example>
    /// <code>
    /// // Scan multiple assemblies
    /// builder.Services.AddSimpleMapper(assembly1, assembly2, assembly3);
    /// </code>
    /// </example>
    public static IServiceCollection AddSimpleMapper(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddSingleton<MapperRegistry>();
        services.AddSingleton<IMapper, Mapper>();
        
        foreach (var assembly in assemblies)
        {
            services.AddMappersFromAssembly(assembly);
        }
        
        return services;
    }

    /// <summary>
    /// Automatically discovers and registers all mappers from the specified assembly.
    /// Scans for classes that implement IMapper&lt;TSource, TDestination&gt;.
    /// Validates that only one mapper exists per source/destination type pair.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assembly">The assembly to scan for mappers</param>
    /// <returns>The service collection for chaining</returns>
    /// <exception cref="InvalidOperationException">Thrown when multiple mappers are found for the same source/destination pair</exception>
    private static IServiceCollection AddMappersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var mapperTypes = assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && !type.IsGenericTypeDefinition)
            .Where(type => type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMapper<,>)))
            .ToList();

        // Group mappers by their interface to detect duplicates
        var mappersByInterface = new Dictionary<Type, List<Type>>();

        foreach (var mapperType in mapperTypes)
        {
            var mapperInterfaces = mapperType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMapper<,>))
                .ToList();

            foreach (var mapperInterface in mapperInterfaces)
            {
                if (!mappersByInterface.ContainsKey(mapperInterface))
                {
                    mappersByInterface[mapperInterface] = new List<Type>();
                }
                mappersByInterface[mapperInterface].Add(mapperType);
            }
        }

        // Validate no duplicates
        var duplicates = mappersByInterface.Where(kvp => kvp.Value.Count > 1).ToList();
        if (duplicates.Any())
        {
            var duplicateMessages = duplicates.Select(kvp =>
            {
                var interfaceArgs = kvp.Key.GetGenericArguments();
                var sourceType = interfaceArgs[0].Name;
                var destType = interfaceArgs[1].Name;
                var mapperNames = string.Join(", ", kvp.Value.Select(t => t.Name));
                return $"{sourceType} -> {destType}: {mapperNames}";
            });

            throw new InvalidOperationException($"Multiple mappers found for the same source/destination pairs:\n{string.Join("\n", duplicateMessages)}\n" +
                                              "Only one mapper per source/destination pair is allowed.");
        }

        // Register all mappers
        foreach (var kvp in mappersByInterface)
        {
            var mapperInterface = kvp.Key;
            var mapperType = kvp.Value[0]; // We've validated there's only one
            
            // Check if already registered to avoid duplicates across multiple calls
            if (!services.Any(s => s.ServiceType == mapperInterface))
            {
                services.AddSingleton(mapperInterface, mapperType);
            }
        }

        return services;
    }
} 
namespace SimpleMapper;

/// <summary>
/// Defines a contract for mapping between source and destination types.
/// </summary>
/// <typeparam name="TSource">The source type to map from</typeparam>
/// <typeparam name="TDestination">The destination type to map to</typeparam>
public interface IMapper<in TSource, out TDestination>
{
    /// <summary>
    /// Maps a source object to a destination object.
    /// </summary>
    /// <param name="source">The source object to map from</param>
    /// <returns>The mapped destination object</returns>
    TDestination Map(TSource source);
}

/// <summary>
/// Defines a contract for the mapper that provides access to all registered mappers.
/// </summary>
public interface IMapper
{
    /// <summary>
    /// Maps a source object to a destination object.
    /// </summary>
    /// <typeparam name="TSource">The source type</typeparam>
    /// <typeparam name="TDestination">The destination type</typeparam>
    /// <param name="source">The source object</param>
    /// <returns>The mapped destination object</returns>
    TDestination Map<TSource, TDestination>(TSource source);

    /// <summary>
    /// Maps a collection of source objects to a collection of destination objects.
    /// </summary>
    /// <typeparam name="TSource">The source type</typeparam>
    /// <typeparam name="TDestination">The destination type</typeparam>
    /// <param name="sources">The source objects</param>
    /// <returns>The mapped destination objects</returns>
    IEnumerable<TDestination> Map<TSource, TDestination>(IEnumerable<TSource> sources);

    /// <summary>
    /// Maps a source object to a destination object with type inference.
    /// The source type is inferred from the parameter.
    /// </summary>
    /// <typeparam name="TDestination">The destination type</typeparam>
    /// <param name="source">The source object</param>
    /// <returns>The mapped destination object</returns>
    TDestination Map<TDestination>(object source);

    /// <summary>
    /// Maps a collection of source objects to a collection of destination objects with type inference.
    /// The source type is inferred from the collection parameter.
    /// </summary>
    /// <typeparam name="TDestination">The destination type</typeparam>
    /// <param name="sources">The source objects</param>
    /// <returns>The mapped destination objects</returns>
    IEnumerable<TDestination> Map<TDestination>(System.Collections.IEnumerable sources);
} 
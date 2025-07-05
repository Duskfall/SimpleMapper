namespace SimpleMapper;

/// <summary>
/// Base class for implementing mappers. Inherit from this class and implement the Map method.
/// 
/// IMPORTANT: Mappers should NOT have constructor parameters or inject services.
/// Mappers should be pure data transformation functions with no dependencies.
/// 
/// ❌ DON'T DO THIS:
/// <code>
/// public class BadMapper : BaseMapper&lt;User, UserDto&gt;
/// {
///     private readonly IService _service; // ❌ No dependencies!
///     public BadMapper(IService service) { _service = service; } // ❌ No constructor params!
/// }
/// </code>
/// 
/// ✅ DO THIS:
/// <code>
/// public class GoodMapper : BaseMapper&lt;User, UserDto&gt;
/// {
///     public override UserDto Map(User source) => new UserDto { Id = source.Id };
/// }
/// </code>
/// </summary>
/// <typeparam name="TSource">Source type to map from</typeparam>
/// <typeparam name="TDestination">Destination type to map to</typeparam>
public abstract class BaseMapper<TSource, TDestination> : IMapper<TSource, TDestination>
{
    /// <summary>
    /// Parameterless constructor to prevent dependency injection.
    /// If you need external services, handle async operations in your service layer instead.
    /// </summary>
    protected BaseMapper()
    {
        // Parameterless constructor prevents service injection
    }

    /// <summary>
    /// Maps a source object to a destination object.
    /// This should be a pure function with no side effects or I/O operations.
    /// </summary>
    /// <param name="source">The source object to map from</param>
    /// <returns>The mapped destination object</returns>
    public abstract TDestination Map(TSource source);
}

 
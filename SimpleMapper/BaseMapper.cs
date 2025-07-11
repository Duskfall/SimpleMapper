namespace SimpleMapper;

/// <summary>
/// Base class for implementing mappers. Inherit from this class and implement the Map method.
/// 
/// IMPORTANT: Mappers should NOT have constructor parameters or inject services.
/// Mappers should be pure data transformation functions with no dependencies.
/// 
/// IMPORTANT: Prefer keeping mappers free of heavy dependencies.  If your mapping logic requires additional helpers, you *may* inject lightweight services via the constructor, but keep the work synchronous and side-effect free.
///
/// ❌ ANTI-PATTERN (slow I/O / async blocking inside a mapper):
/// <code>
/// public class BadMapper : BaseMapper&lt;User, UserDto&gt;
/// {
///     private readonly IExternalHttpService _http;
///     public BadMapper(IExternalHttpService http) => _http = http;  // Heavy dependency
///     public override UserDto Map(User src) => _http.GetFromApi(src.Id).Result; // ❌ Blocking async / I/O
/// }
/// </code>
///
/// ✅ RECOMMENDED:
/// <code>
/// public class GoodMapper : BaseMapper&lt;User, UserDto&gt;
/// {
///     private readonly IClock _clock; // lightweight, synchronous dependency is fine
///     public GoodMapper(IClock clock) => _clock = clock;
///     public override UserDto Map(User src) => new () { Id = src.Id, Created = _clock.Now };
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

 
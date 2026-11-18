using Stellar.Kernel.Data.Catalog;

namespace Stellar.Kernel.LoggingSystem;

/// <summary>
/// TODO: summary
/// </summary>
public interface ILoggerCatalog
    : ICatalog<string, ILogger>
{
    /// <summary>
    /// TODO: summary
    /// </summary>
    void AddLogger(ILogger logger);

    /// <summary>
    /// TODO: summary
    /// </summary>
    ILogger? GetLogger(Identifier identifier);

    /// <summary>
    /// TODO: summary
    /// </summary>
    ILogger? PopLogger(Identifier identifier);
}

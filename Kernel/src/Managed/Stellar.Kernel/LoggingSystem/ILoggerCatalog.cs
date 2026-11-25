using Stellar.Kernel.Data.Catalog;

namespace Stellar.Kernel.LoggingSystem;

/// <summary>
/// The loggers of one engine, by name: where its messages end up (a console, a file, a logger of a game).
/// </summary>
/// <remarks>
/// The logging module publishes it in the services of the engine. A module adds a logger when it starts and takes it
/// out when it stops; the one that added a logger owns it.
/// </remarks>
public interface ILoggerCatalog
    : ICatalog<string, ILogger>;

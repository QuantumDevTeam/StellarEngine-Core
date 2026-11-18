#if NET
using System.Collections.Generic;
using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.Quantization;
using Stellar.Kernel.Services;

namespace Stellar.Kernel.LoggingSystem;

/// <summary>
/// TODO: summary
/// </summary>
public sealed class BaseLogger
    : ILogger
{
    /// <summary>
    /// TODO: summary
    /// </summary>
    public static readonly Identifier BaseLoggerIdentifier = Identifier.New();

    private sealed class BaseLoggerMeta
        : IMetaQuant
    {
        /// <inheritdoc />
        public Identifier UID => BaseLoggerIdentifier;

        /// <summary>
        /// TODO: summary
        /// </summary>
        public bool IsActive = true;
    }

    /// <inheritdoc />
    public Identifier UID => BaseLoggerIdentifier;

    /// <inheritdoc />
    public IMetaQuant Meta { get; } = new BaseLoggerMeta();

    /// <inheritdoc />
    public IReadOnlyList<IMetaQuant> Metas { get; } = [];

    /// <inheritdoc />
    public TMeta? FindMeta<TMeta>() where TMeta : class, IMetaQuant =>
        typeof(TMeta) == typeof(BaseLoggerMeta)
            ? (TMeta)Meta
            : null;

    /// <inheritdoc />
    public void Log(LogLevel level, string message)
    {
        if (!((BaseLoggerMeta)Meta).IsActive) return;
        foreach (var logger in EngineScope.Current?.Services?.Get<ILoggerCatalog>().Values ?? []) // todo: может упасть, если сервиса нету
        {
            logger.Log(level, message);
        }
    }

    /// <inheritdoc />
    public void LogWithoutFormat(string message)
    {
        if (!((BaseLoggerMeta)Meta).IsActive) return;
        foreach (var logger in EngineScope.Current?.Services?.Get<ILoggerCatalog>().Values ?? [])
        {
            logger.LogWithoutFormat(message);
        }
    }

    /// <inheritdoc />
    public void Start()
    {
        ((BaseLoggerMeta)Meta).IsActive = true;
        this.Debug($"Logging started. logger: {UID}");
    }

    /// <inheritdoc />
    public void Stop()
    {
        ((BaseLoggerMeta)Meta).IsActive = false;
        this.Debug($"Logging finished. logger: {UID}");
    }
}
#endif

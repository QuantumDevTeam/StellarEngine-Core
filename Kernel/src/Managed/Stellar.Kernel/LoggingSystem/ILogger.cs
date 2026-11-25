using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.LoggingSystem;

/// <summary>
/// Takes messages: the logger of an engine, or one of those it gives them to (<see cref="ILoggerCatalog"/>).
/// </summary>
/// <remarks>
/// Every member is safe to call from any thread. <c>LoggerExtensions</c> has a method for every level.
/// </remarks>
public interface ILogger
    : IQuant
{
    /// <summary>
    /// Logs a message in the format of its level (<see cref="LoggerFormat"/>).
    /// </summary>
    /// <param name="level">How serious the message is.</param>
    /// <param name="message">The message.</param>
    void Log(LogLevel level, string message);

    /// <summary>
    /// Logs a message as it is: no format and no level, so no level turns it away.
    /// </summary>
    /// <param name="message">The message.</param>
    void LogWithoutFormat(string message);

    /// <summary>
    /// Starts taking messages. A logger may be started again after <see cref="Stop"/>.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops taking messages, and writes out the ones taken before.
    /// </summary>
    void Stop();
}

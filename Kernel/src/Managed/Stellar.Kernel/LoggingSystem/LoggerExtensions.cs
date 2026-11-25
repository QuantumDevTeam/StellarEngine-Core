#if NET
using System;

namespace Stellar.Kernel.LoggingSystem;

/// <summary>
/// A method of <see cref="ILogger"/> for every level.
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Logs a message of <see cref="LogLevel.Info"/>: what happens in the course of things.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="message">The message.</param>
    public static void Info(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Info, message);
    }

    /// <summary>
    /// Logs a message of <see cref="LogLevel.Debug"/>: for those who work on the code.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="message">The message.</param>
    public static void Debug(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Debug, message);
    }

    /// <summary>
    /// Logs a message of <see cref="LogLevel.Success"/>: a success worth telling.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="message">The message.</param>
    public static void Success(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Success, message);
    }

    /// <summary>
    /// Logs a message of <see cref="LogLevel.Warning"/>: something wrong the engine goes on with.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="message">The message.</param>
    public static void Warning(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Warning, message);
    }

    /// <summary>
    /// Logs a message of <see cref="LogLevel.Error"/>: a failure.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="message">The message.</param>
    public static void Error(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Error, message);
    }

    /// <summary>
    /// Logs a message of <see cref="LogLevel.Exception"/> with the exception behind it.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="message">What was being done.</param>
    /// <param name="exception">The exception.</param>
    public static void Exception(this ILogger logger, string message, Exception exception)
    {
        logger.Log(LogLevel.Exception,
            $"{message}\n" +
            $"{exception}\n" +
            $"{exception.GetType()}:  {exception.Message}");
    }

    /// <summary>
    /// Logs an empty line.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public static void Separator(this ILogger logger)
    {
        logger.LogWithoutFormat("");
    }
}
#endif

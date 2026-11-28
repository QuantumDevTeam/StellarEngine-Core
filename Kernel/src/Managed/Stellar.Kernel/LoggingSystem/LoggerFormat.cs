using System;

namespace Stellar.Kernel.LoggingSystem;

/// <summary>
/// The formats of a logger, one per level.
/// </summary>
/// <param name="Info">The format of <see cref="LogLevel.Info"/>.</param>
/// <param name="Debug">The format of <see cref="LogLevel.Debug"/>.</param>
/// <param name="Success">The format of <see cref="LogLevel.Success"/>.</param>
/// <param name="Warning">The format of <see cref="LogLevel.Warning"/>.</param>
/// <param name="Error">The format of <see cref="LogLevel.Error"/>.</param>
/// <param name="Exception">The format of <see cref="LogLevel.Exception"/>.</param>
public readonly record struct LoggerFormat(
    LogFormat Info,
    LogFormat Debug,
    LogFormat Success,
    LogFormat Warning,
    LogFormat Error,
    LogFormat Exception
)
{
    /// <summary>
    /// The formats a logger has when none are given: every placeholder of <see cref="LogFormat"/>, colored by level.
    /// </summary>
    public static readonly LoggerFormat Default = new(
        new LogFormat
        {
            Format = "{0}:{1} | {2}:{3} | {4}      | {5}:{6}:{7} : {8}.{9} - {10}",
            ColorizedFormat = "\e[32m{0}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[2;32m{1}\e[0m " +
                              "\e[31m|\e[0m " +
                              "\e[32m{2}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[2;32m{3}\e[0m " +
                              "\e[31m|\e[0m " +
                              "\e[1;37m{4}\e[0m      " +
                              "\e[31m|\e[0m " +
                              "\e[36m{5}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[36m{6}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[36m{7}\e[0m " +
                              "\e[31m:\e[0m " +
                              "\e[36m{8}\e[0m" +
                              "\e[31m.\e[0m" +
                              "\e[36m{9}\e[0m " +
                              "\e[31m-\e[0m " +
                              "\e[1;37m{10}\e[0m"
        },
        new LogFormat
        {
            Format = "{0}:{1} | {2}:{3} | {4}     | {5}:{6}:{7} : {8}.{9} - {10}",
            ColorizedFormat = "\e[32m{0}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[2;32m{1}\e[0m " +
                              "\e[31m|\e[0m " +
                              "\e[32m{2}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[2;32m{3}\e[0m " +
                              "\e[31m|\e[0m " +
                              "\e[1;33m{4}\e[0m     " +
                              "\e[31m|\e[0m " +
                              "\e[36m{5}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[36m{6}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[36m{7}\e[0m " +
                              "\e[31m:\e[0m " +
                              "\e[36m{8}\e[0m" +
                              "\e[31m.\e[0m" +
                              "\e[36m{9}\e[0m " +
                              "\e[31m-\e[0m " +
                              "\e[1;33m{10}\e[0m"
        },
        new LogFormat
        {
            Format = "{0}:{1} | {2}:{3} | {4}   | {5}:{6}:{7} : {8}.{9} - {10}",
            ColorizedFormat = "\e[32m{0}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[2;32m{1}\e[0m " +
                              "\e[31m|\e[0m " +
                              "\e[32m{2}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[2;32m{3}\e[0m " +
                              "\e[31m|\e[0m " +
                              "\e[1;32m{4}\e[0m   " +
                              "\e[31m|\e[0m " +
                              "\e[36m{5}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[36m{6}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[36m{7}\e[0m " +
                              "\e[31m:\e[0m " +
                              "\e[36m{8}\e[0m" +
                              "\e[31m.\e[0m" +
                              "\e[36m{9}\e[0m " +
                              "\e[31m-\e[0m " +
                              "\e[1;32m{10}\e[0m"
        },
        new LogFormat
        {
            Format = "{0}:{1} | {2}:{3} | {4}   | {5}:{6}:{7} : {8}.{9} - {10}",
            ColorizedFormat = "\e[32m{0}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[2;32m{1}\e[0m " +
                              "\e[31m|\e[0m " +
                              "\e[32m{2}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[2;32m{3}\e[0m " +
                              "\e[31m|\e[0m " +
                              "\e[1;34m{4}\e[0m   " +
                              "\e[31m|\e[0m " +
                              "\e[36m{5}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[36m{6}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[36m{7}\e[0m " +
                              "\e[31m:\e[0m " +
                              "\e[36m{8}\e[0m" +
                              "\e[31m.\e[0m" +
                              "\e[36m{9}\e[0m " +
                              "\e[31m-\e[0m " +
                              "\e[1;34m{10}\e[0m"
        },
        new LogFormat
        {
            Format = "{0}:{1} | {2}:{3} | {4}     | {5}:{6}:{7} : {8}.{9} - {10}",
            ColorizedFormat = "\e[32m{0}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[2;32m{1}\e[0m " +
                              "\e[31m|\e[0m " +
                              "\e[32m{2}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[2;32m{3}\e[0m " +
                              "\e[31m|\e[0m " +
                              "\e[1;31m{4}\e[0m     " +
                              "\e[31m|\e[0m " +
                              "\e[36m{5}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[36m{6}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[36m{7}\e[0m " +
                              "\e[31m:\e[0m " +
                              "\e[36m{8}\e[0m" +
                              "\e[31m.\e[0m" +
                              "\e[36m{9}\e[0m " +
                              "\e[31m-\e[0m " +
                              "\e[1;37m{10}\e[0m"
        },
        new LogFormat
        {
            Format = "{0}:{1} | {2}:{3} | {4} | {5}:{6}:{7} : {8}.{9} - {10}",
            ColorizedFormat = "\e[32m{0}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[2;32m{1}\e[0m " +
                              "\e[31m|\e[0m " +
                              "\e[32m{2}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[2;32m{3}\e[0m " +
                              "\e[31m|\e[0m " +
                              "\e[1;31m{4}\e[0m " +
                              "\e[31m|\e[0m " +
                              "\e[36m{5}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[36m{6}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[36m{7}\e[0m " +
                              "\e[31m:\e[0m " +
                              "\e[36m{8}\e[0m" +
                              "\e[31m.\e[0m" +
                              "\e[36m{9}\e[0m " +
                              "\e[31m-\e[0m " +
                              "\e[1;31m{10}\e[0m"
        }
    );

    /// <summary>
    /// Gets the format of a level.
    /// </summary>
    /// <param name="level">The level.</param>
    /// <returns>The format of <paramref name="level"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="level"/> is not one of <see cref="LogLevel"/>.</exception>
    public LogFormat ForLevel(LogLevel level)
    {
        return level switch
        {
            LogLevel.Info => Info,
            LogLevel.Debug => Debug,
            LogLevel.Success => Success,
            LogLevel.Warning => Warning,
            LogLevel.Error => Error,
            LogLevel.Exception => Exception,
            _ => throw new ArgumentException("LoggerFormat.ForLevel operate only levels writen in LogLevel enum",
                nameof(level), null)
        };
    }
}

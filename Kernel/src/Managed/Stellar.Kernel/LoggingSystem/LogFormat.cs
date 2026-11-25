namespace Stellar.Kernel.LoggingSystem;

/// <summary>
/// The format of the lines of one level: a composite format string (<see cref="string.Format(string, object[])"/>).
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Placeholder</term><description>What it is</description></listheader>
/// <item><term>{0}</term><description>the date, yyyy-MM-dd</description></item>
/// <item><term>{1}</term><description>the time, HH:mm:ss.fff</description></item>
/// <item><term>{2}</term><description>the name of the engine clock</description></item>
/// <item><term>{3}</term><description>the time of the engine clock, HH:mm:ss.fff</description></item>
/// <item><term>{4}</term><description>the <see cref="LogLevel"/></description></item>
/// <item><term>{5}</term><description>the name of the engine</description></item>
/// <item><term>{6}</term><description>the name of the logger</description></item>
/// <item><term>{7}</term><description>the identifier of the managed thread</description></item>
/// <item><term>{8}</term><description>the type that logs; costs a walk of the stack, as {9} does</description></item>
/// <item><term>{9}</term><description>the method that logs</description></item>
/// <item><term>{10}</term><description>the message</description></item>
/// </list>
/// </remarks>
/// <param name="Format">The format string.</param>
/// <param name="ColorizedFormat">The same with ANSI colors, for consoles; <c>null</c> for none.</param>
public record struct LogFormat(string Format, string? ColorizedFormat = null);

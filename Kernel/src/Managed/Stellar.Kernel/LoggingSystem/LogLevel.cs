namespace Stellar.Kernel.LoggingSystem;

/// <summary>
/// How serious a message is, the least serious first: a logger that takes a level takes every one after it.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// For those who work on the code: steps, values.
    /// </summary>
    Debug = 0,

    /// <summary>
    /// What happens in the course of things.
    /// </summary>
    Info = 1,

    /// <summary>
    /// Something worth telling succeeded.
    /// </summary>
    Success = 2,

    /// <summary>
    /// Something is wrong, and the engine goes on as if it were not.
    /// </summary>
    Warning = 3,

    /// <summary>
    /// Something failed, and what depends on it does not work.
    /// </summary>
    Error = 4,

    /// <summary>
    /// Something failed with an exception, told with it.
    /// </summary>
    Exception = 5
}

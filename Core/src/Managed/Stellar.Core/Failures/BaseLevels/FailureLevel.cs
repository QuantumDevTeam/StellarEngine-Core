using Stellar.Core.Data.Collections;
using Stellar.Kernel;
using Stellar.Kernel.Failures;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Failures.BaseLevels;

/// <inheritdoc cref="IFailureLevel"/>
public abstract class FailureLevel
    : IFailureLevel
{
    private readonly IFailureDispatcherMeta _dispatcherMeta;

    /// <summary>
    /// Creates a level and adds it to the levels of a dispatcher.
    /// </summary>
    /// <param name="name">The name of the level.</param>
    /// <param name="dispatcherMeta">The meta of the dispatcher the level belongs to.</param>
    protected FailureLevel(string name, IFailureDispatcherMeta dispatcherMeta)
    {
        ArgumentNullException.ThrowIfNull(dispatcherMeta);

        // Levels belong to their dispatcher and two dispatchers may both have an "Error": the label is not global.
        Label = new Label(UID, name);

        _dispatcherMeta = dispatcherMeta;
        _dispatcherMeta.FailureLevels.Set(this);
    }

    /// <inheritdoc/>
    public Identifier UID { get; } = Identifier.New();

    /// <inheritdoc/>
    public Label Label { get; }

    /// <inheritdoc/>
    public abstract bool IsEnabled { get; set; }

    /// <inheritdoc/>
    public abstract bool IsLoggable { get; }

    /// <inheritdoc/>
    public abstract bool IsStopExecute { get; }

    /// <inheritdoc/>
    public abstract bool IsCritical { get; }

    /// <inheritdoc/>
    public abstract bool ShouldTerminate { get; }

    /// <summary>
    /// Takes the level out of the failure levels of its dispatcher.
    /// </summary>
    public void Dispose()
    {
        _dispatcherMeta.FailureLevels.Pop(UID);
    }
}
